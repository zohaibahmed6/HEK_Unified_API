using System.Text;
using System.Threading.RateLimiting;
using FluentValidation;
using HekCoreApi.Api.Configuration;
using HekCoreApi.Api.Errors;
using HekCoreApi.Api.HealthChecks;
using HekCoreApi.Api.Middleware;
using HekCoreApi.Api.Security;
using HekCoreApi.Adapters.Hiso.Session;
using HekCoreApi.Application.Common.Behaviors;
using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Options;
using HekCoreApi.Application.Features.Auth.Commands;
using HekCoreApi.Contracts.Security;
using HekCoreApi.Infrastructure.Auth;
using HekCoreApi.Infrastructure.DependencyInjection;
using HekCoreApi.Infrastructure.Legacy.Dormant.Dmsda;
using HekCoreApi.Infrastructure.Persistence;
using HekCoreApi.Infrastructure.Routing;
using HekCoreApi.Infrastructure.Secrets;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using HekCoreApi.Api.Telemetry;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using SoapCore;

// Bootstrap logger - captures anything that happens before the full Serilog pipeline is configured
// from appsettings (host-building failures, etc.).
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Local-only overrides (real connection strings, etc.) - gitignored via appsettings.*.local.json,
    // never committed. Optional, so nothing breaks when the file doesn't exist.
    builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.local.json", optional: true, reloadOnChange: true);

    builder.Host.UseSerilog((context, services, configuration) =>
    {
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentName();

        // Per-system logging overhaul (2026-07-29, see hek_analysis/LOGGING_OVERHAUL_PLAN.md):
        // every legacy-compat log line already carries a "System" property (the message templates
        // in LegacyOperationObserver use "{System} ..." as their first token), so routing by system
        // needs no extra enrichment - just filter on that existing property. Three files per system:
        // technical (JSON, everything, 30 days), readable (only "[CallLog]"-tagged plain-English
        // lines, 30 days), errors (Warning+, 60-90 days - kept longer since they're rarer and more
        // valuable for later troubleshooting). CorrelationIdMiddleware already pushes a per-request
        // "CorrelationId" property that ties entries across all three files together.
        foreach (var system in new[] { "hiso", "karo", "erms", "col" })
        {
            var systemName = system;

            // StartsWith (not Equals) so transport-suffixed system names still route here - e.g.
            // FormSessionService.cs (real HISO SOAP endpoint) deliberately tags its calls "hiso-soap"
            // (not "hiso") to stay distinguishable in the OTel metric, but that call still belongs in
            // logs/hiso/*.log alongside HisoCompatController's REST traffic. Confirmed live 2026-07-30:
            // under exact-match, every SOAP call silently matched none of the 4 per-system loggers.
            bool MatchesSystem(Serilog.Events.LogEvent evt) =>
                evt.Properties.TryGetValue("System", out var value) &&
                value.ToString().Trim('"').StartsWith(systemName, StringComparison.OrdinalIgnoreCase);

            configuration.WriteTo.Logger(lc => lc
                .Filter.ByIncludingOnly(MatchesSystem)
                .WriteTo.File(
                    new HekCoreApi.Api.Telemetry.SeparatedCompactJsonFormatter(),
                    $"logs/{systemName}/technical-.log",
                    rollingInterval: RollingInterval.Day,
                    retainedFileTimeLimit: TimeSpan.FromDays(30),
                    fileSizeLimitBytes: 52_428_800,
                    rollOnFileSizeLimit: true));

            configuration.WriteTo.Logger(lc => lc
                .Filter.ByIncludingOnly(evt => MatchesSystem(evt) && evt.MessageTemplate.Text.Contains("[CallLog]"))
                .WriteTo.File(
                    $"logs/{systemName}/readable-.log",
                    // [{CorrelationId}] ties this line to its full detail in technical-*.log/errors-*.log -
                    // CorrelationIdMiddleware already pushes this property on every request.
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{CorrelationId}] {Message:lj}{NewLine}--------------------------------------------------------------------------------{NewLine}",
                    rollingInterval: RollingInterval.Day,
                    retainedFileTimeLimit: TimeSpan.FromDays(30),
                    fileSizeLimitBytes: 52_428_800,
                    rollOnFileSizeLimit: true));

            configuration.WriteTo.Logger(lc => lc
                .Filter.ByIncludingOnly(evt => MatchesSystem(evt) && evt.Level >= Serilog.Events.LogEventLevel.Warning)
                .WriteTo.File(
                    new HekCoreApi.Api.Telemetry.SeparatedCompactJsonFormatter(),
                    $"logs/{systemName}/errors-.log",
                    rollingInterval: RollingInterval.Day,
                    retainedFileTimeLimit: TimeSpan.FromDays(90),
                    fileSizeLimitBytes: 52_428_800,
                    rollOnFileSizeLimit: true));
        }

        // Safety net: every request, even ones that never reach a controller (404s, CORS
        // rejections, wrong path) - see RequestLoggingMiddleware. These carry no "System" property
        // so they'd otherwise be invisible to the per-system files above.
        configuration.WriteTo.Logger(lc => lc
            .Filter.ByIncludingOnly(evt => evt.Properties.ContainsKey("SourceContext") &&
                evt.Properties["SourceContext"].ToString().Contains("RequestLoggingMiddleware"))
            .WriteTo.File(
                "logs/requests-.log",
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{CorrelationId}] [{Level:u3}] {Message:lj}{NewLine}--------------------------------------------------------------------------------{NewLine}",
                rollingInterval: RollingInterval.Day,
                retainedFileTimeLimit: TimeSpan.FromDays(14),
                fileSizeLimitBytes: 52_428_800,
                rollOnFileSizeLimit: true));

        // Dedicated cross-system DB-error file (2026-07-30, Zohaib's ask): any caught database
        // exception (SqlException etc. - all derive from DbException), from any of the 4 legacy
        // systems, in one place - regardless of whether the catching code happens to tag a "System"
        // property (some internal catch blocks, e.g. HisoConceptExecutor.ExecuteAsync, don't). Filters
        // on the log event's actual Exception type rather than any string/property convention, so it
        // can't be missed by a caller forgetting to tag it. CorrelationId comes for free via
        // Enrich.FromLogContext() above - every request-scoped log line already carries it.
        configuration.WriteTo.Logger(lc => lc
            .Filter.ByIncludingOnly(evt => evt.Exception is System.Data.Common.DbException)
            .WriteTo.File(
                "logs/db-errors-.log",
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{CorrelationId}] [{Level:u3}] {Message:lj}{NewLine}{Exception}{NewLine}--------------------------------------------------------------------------------{NewLine}",
                rollingInterval: RollingInterval.Day,
                retainedFileTimeLimit: TimeSpan.FromDays(90),
                fileSizeLimitBytes: 52_428_800,
                rollOnFileSizeLimit: true));
    });

    // ---- Options (IOptions pattern - coding-standards: do not hardcode settings) ----
    builder.Services.Configure<AuthOptions>(builder.Configuration.GetSection(AuthOptions.SectionName));
    builder.Services.Configure<HisoSessionOptions>(builder.Configuration.GetSection(HisoSessionOptions.SectionName));
    builder.Services.Configure<HisoServerAddressMapOptions>(builder.Configuration.GetSection(HisoServerAddressMapOptions.SectionName));
    builder.Services.Configure<RateLimitOptions>(builder.Configuration.GetSection(RateLimitOptions.SectionName));
    builder.Services.Configure<CorsOptions>(builder.Configuration.GetSection(CorsOptions.SectionName));
    builder.Services.Configure<LegacyDmsOptions>(builder.Configuration.GetSection(LegacyDmsOptions.SectionName));
    builder.Services.Configure<TaskStatusOptions>(builder.Configuration.GetSection(TaskStatusOptions.SectionName));
    builder.Services.Configure<LegacyPracticeResolutionOptions>(builder.Configuration.GetSection(LegacyPracticeResolutionOptions.SectionName));
    builder.Services.Configure<HisoConceptMappingOptions>(builder.Configuration.GetSection(HisoConceptMappingOptions.SectionName));
    builder.Services.Configure<HisoGetDataOptions>(builder.Configuration.GetSection(HisoGetDataOptions.SectionName));
    builder.Services.Configure<LegacyHostRoutingOptions>(builder.Configuration.GetSection(LegacyHostRoutingOptions.SectionName));
    builder.Services.Configure<VerboseDiagnosticLoggingOptions>(builder.Configuration.GetSection(VerboseDiagnosticLoggingOptions.SectionName));

    // ---- Secrets (Block 0 vertical slice) ----
    builder.Services.AddSingleton<ISecretProvider, EnvironmentVariableSecretProvider>();

    // ---- v1.1 spec follow-through Step 5: real AWSDoc.IndiciDMS (was a dormant no-op stub) ----
    builder.Services.AddScoped<IAwsDocumentService, HekCoreApi.Infrastructure.Legacy.Hiso.AwsDocumentService>();

    // ---- v1.1 spec follow-through Step 4: real HISO SOAP endpoint (SoapCore), see Features/Hiso/Soap ----
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddSoapCore();
    builder.Services.AddScoped<HekCoreApi.Api.Features.Hiso.Soap.IFormSessionService, HekCoreApi.Api.Features.Hiso.Soap.FormSessionService>();

    // ---- Dormant DAL module, SQL-injection-fixed (PROJECT_STATUS.md open item 23) ----
    // Registered so the fixed capability exists and is DI-resolvable, but no controller/endpoint
    // calls it - "dormant," per the stakeholder's "keep, don't delete" decision.
    builder.Services.AddScoped<DmsDocumentService>();

    // ---- Tenant registry (ADR-001) ----
    builder.Services.AddDbContext<TenantRegistryDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("TenantRegistry")));
    builder.Services.AddScoped<ITenantRegistryService, TenantRegistryService>();

    // ---- HISO session handling (ADR-004/ADR-007) ----
    builder.Services.AddScoped<IHisoSessionRepository, HekCoreApi.Infrastructure.Persistence.Hiso.HisoSessionRepository>();
    builder.Services.AddScoped<HisoSessionResolver>();

    // ---- Block 2 foundation: legacy practice DB routing + idempotency ----
    builder.Services.AddScoped<ILegacyPracticeConnectionResolver, HekCoreApi.Infrastructure.Routing.LegacyPracticeConnectionResolver>();
    builder.Services.AddMemoryCache();
    builder.Services.AddScoped<IIdempotencyStore, HekCoreApi.Infrastructure.Idempotency.InMemoryIdempotencyStore>();

    // ---- Auth core (ADR-002/ADR-003) ----
    builder.Services.AddScoped<IIdentityValidator, EntraIdIdentityValidator>();
    builder.Services.AddScoped<IJwtTokenIssuer, JwtTokenIssuer>();

    // ---- MediatR + FluentValidation pipeline ----
    builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<AuthenticateCommand>());
    builder.Services.AddValidatorsFromAssemblyContaining<AuthenticateCommand>();
    builder.Services.AddTransient(typeof(MediatR.IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

    // ---- JWT bearer authentication - validates HEK Core API's own tokens (ADR-003), signed with a
    // key resolved via ISecretProvider at startup. Only unauthenticated endpoints are /auth/token,
    // the legacy compat authenticate endpoints, and /health (OpenAPI spec).
    var secretProviderForStartup = new EnvironmentVariableSecretProvider(builder.Configuration);
    var signingKeySecretName = builder.Configuration[$"{AuthOptions.SectionName}:{nameof(AuthOptions.SigningKeySecretName)}"]
        ?? "Auth:JwtSigningKey";
    var signingKeyValue = builder.Configuration[signingKeySecretName]
        ?? "DEV-ONLY-INSECURE-SIGNING-KEY-REPLACE-VIA-SECRET-PROVIDER-0000000000";

    // ---- Entra ID inbound bearer scheme (2026-07-30 fix, ADR-002/item 13 of the auth research
    // log): a *calling application* that has its own Entra ID App Registration acquires its own
    // token via the Client Credentials flow and presents it to us as `Authorization: Bearer`. We
    // validate that inbound token against Entra's own public signing keys/issuer here. This is the
    // counterpart to (and replaces relying on) EntraIdIdentityValidator's flawed prior approach of
    // acquiring a token for our OWN app instead of checking the caller's - see that file's updated
    // comments. Wired only when both Entra config values are present, so local/dev setups without a
    // real tenant are unaffected.
    const string EntraBearerAuthenticationScheme = "EntraBearer";
    var entraTenantId = builder.Configuration["Entra:TenantId"];
    var entraAudience = builder.Configuration["Entra:ClientId"];
    var entraBearerConfigured = !string.IsNullOrWhiteSpace(entraTenantId) && !string.IsNullOrWhiteSpace(entraAudience);

    var authenticationBuilder = builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKeyValue))
            };
        });

    if (entraBearerConfigured)
    {
        authenticationBuilder.AddJwtBearer(EntraBearerAuthenticationScheme, options =>
        {
            options.Authority = $"https://login.microsoftonline.com/{entraTenantId}/v2.0";
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidAudience = entraAudience,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true
            };
        });
    }

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy(AuthorizationPolicyNames.ResourceScoped, policy =>
            policy.Requirements.Add(new ResourceScopeRequirement()));
        options.AddPolicy(AuthorizationPolicyNames.PlatformAdmin, policy =>
            policy.RequireClaim(HekClaimTypes.Scope, "platform-admin"));
        options.AddPolicy(AuthorizationPolicyNames.BillingWrite, policy =>
            policy.RequireClaim(HekClaimTypes.Scope, "billing:write"));

        // Default [Authorize] (no explicit policy) accepts either our own JWT or, once a real
        // Entra tenant/App Registration exists, an Entra-issued caller token - see the
        // AddJwtBearer(EntraBearerAuthenticationScheme, ...) registration above.
        var defaultSchemes = entraBearerConfigured
            ? new[] { JwtBearerDefaults.AuthenticationScheme, EntraBearerAuthenticationScheme }
            : new[] { JwtBearerDefaults.AuthenticationScheme };
        options.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder(defaultSchemes)
            .RequireAuthenticatedUser()
            .Build();
    });
    builder.Services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, ResourceScopeAuthorizationHandler>();

    // ---- Rate limiting (ADR-008, config-toggle default off) ----
    // Two tiers: a "standard" global limit for all endpoints, and a tighter "auth-strict" named
    // policy applied via [EnableRateLimiting] on login/authenticate endpoints specifically, to
    // guard against brute-force/credential-stuffing without disrupting normal data-endpoint use.
    var rateLimitOptions = builder.Configuration.GetSection(RateLimitOptions.SectionName).Get<RateLimitOptions>() ?? new RateLimitOptions();
    if (rateLimitOptions.Enabled)
    {
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = rateLimitOptions.PermitLimit,
                        Window = TimeSpan.FromSeconds(rateLimitOptions.WindowSeconds)
                    }));
            options.AddPolicy(RateLimitPolicyNames.AuthStrict, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = rateLimitOptions.AuthPermitLimit,
                        Window = TimeSpan.FromSeconds(rateLimitOptions.AuthWindowSeconds)
                    }));
        });
    }

    // ---- CORS (explicit allow-list, replacing legacy wildcard config) ----
    var corsOptions = builder.Configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>() ?? new CorsOptions();
    builder.Services.AddCors(options => options.AddPolicy(CorsOptions.PolicyName, policy =>
        policy.WithOrigins(corsOptions.AllowedOrigins).AllowAnyHeader().AllowAnyMethod()
            // FR-12: custom response headers are hidden from browser JS cross-origin unless explicitly
            // exposed - AllowAnyHeader() above only covers request headers, not response ones.
            .WithExposedHeaders("X-Hek-Routing-System", "X-Hek-Routing-Environment", "X-Hek-Routing-DbServer", "X-Hek-Routing-DbName", "X-Hek-Routing-PracticeId", "X-Hek-Routing-Summary")));

    // ---- Telemetry (NFR-5): OpenTelemetry traces + metrics, additive to Serilog (which keeps
    // handling text logs unchanged). Exports via OTLP to the Aspire Dashboard container in
    // docker-compose.yml when Otel:OtlpEndpoint is configured; otherwise falls back to the console
    // exporter so `dotnet run` alone still shows traces/metrics without any collector running.
    var otlpEndpoint = builder.Configuration["Otel:OtlpEndpoint"];
    builder.Services.AddMetrics();
    builder.Services.AddSingleton<HekTelemetry>();
    builder.Services.AddSingleton<LegacyOperationObserver>();

    // Structured logs (ILogger output, including the {@Context} data attached by
    // LegacyOperationObserver) are additive to Serilog's console sink - without this, the
    // Aspire dashboard's Structured Logs view has nothing to show even though traces/metrics work.
    builder.Logging.AddOpenTelemetry(logging =>
    {
        logging.IncludeFormattedMessage = true;
        logging.IncludeScopes = true;
        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
        {
            logging.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));
        }
        else
        {
            logging.AddConsoleExporter();
        }
    });

    builder.Services.AddOpenTelemetry()
        .ConfigureResource(resource => resource.AddService(
            serviceName: "HekCoreApi",
            serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown"))
        .WithTracing(tracing =>
        {
            tracing.AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddSqlClientInstrumentation(o => o.SetDbStatementForText = false);
            if (!string.IsNullOrWhiteSpace(otlpEndpoint))
            {
                tracing.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));
            }
            else
            {
                tracing.AddConsoleExporter();
            }
        })
        .WithMetrics(metrics =>
        {
            metrics.AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddMeter(HekTelemetry.MeterName);
            if (!string.IsNullOrWhiteSpace(otlpEndpoint))
            {
                metrics.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));
            }
            else
            {
                metrics.AddConsoleExporter();
            }
        });

    // ---- Centralized exception handling (RFC 7807-inspired, Contract Design doc Section 10) ----
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    // ---- Block 2 domain repositories - convention-scanned (see AddInfrastructureRepositories
    // remarks); called after all explicit registrations above so TryAddScoped never overrides them.
    builder.Services.AddInfrastructureRepositories();

    // ---- Health checks (Block 0) ----
    builder.Services.AddHealthChecks()
        .AddCheck<SelfHealthCheck>("self")
        .AddSqlServer(
            builder.Configuration.GetConnectionString("TenantRegistry") ?? string.Empty,
            name: "tenant-registry-sql",
            tags: ["ready"]);

    builder.Services.AddControllers(options =>
        options.InputFormatters.Insert(0, new HekCoreApi.Api.Formatters.PlainTextInputFormatter()));
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Paste just the JWT itself (no \"Bearer \" prefix - Swagger adds that for you)."
        });
        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
                Array.Empty<string>()
            }
        });
    });

    var app = builder.Build();

    // v1.1 spec follow-through (2026-07-24): real `AWSDocCore.DocumentManager` global config - confirmed
    // via reflection this is the .NET Core replacement for what the old .NET Framework AWSDoc.dll used
    // to read from its own App.config `<appSettings>` (DMS_AWS_BaseURL/SecretKey/etc). Must be set once
    // before any `DownloadFromAwsAsync` call - it's process-global static state inside the DLL, not
    // per-request, so this runs once at startup rather than per HISO call.
    {
        using var awsConfigScope = app.Services.CreateScope();
        var awsDocumentService = awsConfigScope.ServiceProvider.GetRequiredService<IAwsDocumentService>();
        var awsBaseUrl = builder.Configuration["Hiso:DmsAwsBaseUrl"];
        var awsSecretKey = builder.Configuration["Hiso:DmsAwsSecretKey"];
        if (!string.IsNullOrEmpty(awsBaseUrl) && !string.IsNullOrEmpty(awsSecretKey))
        {
            var jwtExpiryMinutes = builder.Configuration.GetValue("Hiso:DmsAwsJwtTokenExpiryInMinutes", 2);
            var s3TimeoutSeconds = builder.Configuration.GetValue("Hiso:DmsAwsS3BucketRequestTimeOut", 10);
            awsDocumentService.ConfigureAws(awsBaseUrl, awsSecretKey, jwtExpiryMinutes, s3TimeoutSeconds);
        }
    }

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TenantRegistryDbContext>();
        await TenantRegistrySeeder.SeedAsync(db);
    }

    // Correlation ID first - every downstream log line and the exception handler's traceId depend on it.
    app.UseCorrelationId();
    app.UseMiddleware<HekCoreApi.Api.Middleware.RequestLoggingMiddleware>();

    // Full request/response body capture + ambient "System" LogContext tag (2026-07-31) - see
    // RequestResponseLoggingMiddleware's doc comment. Positioned before the SOAP endpoint so it also
    // wraps HISO's SOAP traffic, which short-circuits the pipeline before reaching anything below it.
    app.UseRequestResponseLogging();

    // v1.1 spec follow-through Step 4: real HISO SOAP endpoint (SoapCore's own raw path-matching
    // middleware, independent of MVC routing) at the real address. Registered after
    // UseCorrelationId/RequestLoggingMiddleware (2026-07-30 fix, was registered before them) - both are
    // plain pass-through middleware (push a LogContext property / log after `await _next`), so this
    // doesn't affect SoapCore reading the request; it just means SOAP requests now get a CorrelationId
    // and land in logs/requests-*.log's safety net like every other request already does. Confirmed
    // live: SOAP calls previously never reached either middleware, since SoapCore short-circuits
    // matched paths before continuing the pipeline.
    ((IApplicationBuilder)app).UseSoapEndpoint<HekCoreApi.Api.Features.Hiso.Soap.IFormSessionService>(
        "/FormSessionService.svc",
        new SoapEncoderOptions(),
        SoapSerializer.XmlSerializer);
    app.UseExceptionHandler();

    // v1.1 spec follow-through Step 2: rewrite real external legacy paths (/api/..., /COL/...) onto
    // this hub's existing internal routes before MVC routing runs - see LegacyHostRoutingMiddleware.
    // Explicit UseRouting() is required here: without it, ASP.NET Core implicitly matches routes at
    // the very start of the pipeline, before this rewrite would ever run.
    app.UseLegacyHostRouting();
    app.UseRouting();

    app.UseHttpsRedirection();
    app.UseSecurityHeaders();
    app.UseCors(CorsOptions.PolicyName);

    if (rateLimitOptions.Enabled)
    {
        app.UseRateLimiter();
    }
    app.UseRateLimitHeaders();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    // OpenAPI spec: GET /health -> 200 { "status": "ok" } (KARO Ping / ERMS Ping equivalent).
    app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = _ => false,
        ResponseWriter = (context, _) =>
        {
            context.Response.ContentType = "application/json";
            return context.Response.WriteAsync("""{"status":"ok"}""");
        }
    });
    app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready")
    });

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "HEK Core API terminated unexpectedly during startup.");
}
finally
{
    Log.CloseAndFlush();
}

/// <summary>Exposed for WebApplicationFactory in Api.IntegrationTests.</summary>
public partial class Program
{
}
