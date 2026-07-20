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
using Serilog;

// Bootstrap logger - captures anything that happens before the full Serilog pipeline is configured
// from appsettings (host-building failures, etc.).
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithEnvironmentName());

    // ---- Options (IOptions pattern - coding-standards: do not hardcode settings) ----
    builder.Services.Configure<AuthOptions>(builder.Configuration.GetSection(AuthOptions.SectionName));
    builder.Services.Configure<HisoSessionOptions>(builder.Configuration.GetSection(HisoSessionOptions.SectionName));
    builder.Services.Configure<HisoServerAddressMapOptions>(builder.Configuration.GetSection(HisoServerAddressMapOptions.SectionName));
    builder.Services.Configure<RateLimitOptions>(builder.Configuration.GetSection(RateLimitOptions.SectionName));
    builder.Services.Configure<CorsOptions>(builder.Configuration.GetSection(CorsOptions.SectionName));
    builder.Services.Configure<LegacyDmsOptions>(builder.Configuration.GetSection(LegacyDmsOptions.SectionName));
    builder.Services.Configure<TaskStatusOptions>(builder.Configuration.GetSection(TaskStatusOptions.SectionName));

    // ---- Secrets (Block 0 vertical slice) ----
    builder.Services.AddSingleton<ISecretProvider, EnvironmentVariableSecretProvider>();

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

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy(AuthorizationPolicyNames.ResourceScoped, policy =>
            policy.Requirements.Add(new ResourceScopeRequirement()));
        options.AddPolicy(AuthorizationPolicyNames.PlatformAdmin, policy =>
            policy.RequireClaim(HekClaimTypes.Scope, "platform-admin"));
        options.AddPolicy(AuthorizationPolicyNames.BillingWrite, policy =>
            policy.RequireClaim(HekClaimTypes.Scope, "billing:write"));
    });
    builder.Services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, ResourceScopeAuthorizationHandler>();

    // ---- Rate limiting (ADR-008, config-toggle default off) ----
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
        });
    }

    // ---- CORS (explicit allow-list, replacing legacy wildcard config) ----
    var corsOptions = builder.Configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>() ?? new CorsOptions();
    builder.Services.AddCors(options => options.AddPolicy(CorsOptions.PolicyName, policy =>
        policy.WithOrigins(corsOptions.AllowedOrigins).AllowAnyHeader().AllowAnyMethod()));

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

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    var app = builder.Build();

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
    app.UseExceptionHandler();

    app.UseHttpsRedirection();
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
