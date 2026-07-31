using System.Data;
using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Models;
using MediatR;

namespace HekCoreApi.Application.Features.Col;

public sealed record ColAuthenticateQuery(string? Username, string? Password, string? PatientId, string? EncounterId) : IRequest<ColAuthenticateResult>;

public sealed record ColAuthenticateResult(string? Token, string? Expiry, string? PracticeId, string Error, CallRoutingInfo? Routing = null);

/// <summary>Shared result for the COL read/write pipelines - raw DataTable plus the error text that (non-blank) becomes the whole response body.</summary>
public sealed record ColReadResult(bool Succeeded, DataTable? Table, string Error, CallRoutingInfo? Routing = null);

public sealed record ColGetCurrentPatientDataQuery(string? PatientId, string? EncounterId, string? BearerToken) : IRequest<ColReadResult>;
public sealed record ColGetSessionDataQuery(string? PatientId, string? EncounterId, string? BearerToken) : IRequest<ColReadResult>;
public sealed record ColGetProviderDataQuery(string? PatientId, string? EncounterId, string? BearerToken) : IRequest<ColReadResult>;
public sealed record ColGetSurgeryDataQuery(string? PatientId, string? EncounterId, string? LocationId, string? BearerToken) : IRequest<ColReadResult>;
public sealed record ColGetDiagnosisDataQuery(string? PatientId, string? EncounterId, string Order, string? MinDateTime, string? MaxDateTime, string? BearerToken) : IRequest<ColReadResult>;

public sealed record ColSaveInvoiceCommand(string? PatientId, string? AccountHolderId, string? EncounterId, string? ServiceName, string? ServiceCode,
    string? AmountInclGst, string? Description, string? Payee, string? ServiceProvider, string? ServiceProviderType, string? ServiceDate,
    string? PegasusReference, string? ClaimShortCode, string? BearerToken) : IRequest<ColSaveInvoiceResult>;

public sealed record ColSaveInvoiceResult(long ServiceMappingId, string Error, CallRoutingInfo? Routing = null);

/// <summary>
/// Ported from `COLController.Authenticate` (`COLController.cs:24`): NO base64 step (unlike ERMS),
/// COL's own split (3rd segment overwrites), decrypt patientId, `[HSS].[uspInsertAndValidateToken]`
/// with the real `ExpiryInDays` config, and the real two-tier failure messages
/// ("Authentication failed!" / "Invalid credentials!"). Always HTTP 200 JSON with an `error` field.
/// COL shares ERMS's auth/connection infrastructure (same in-process legacy controller), so its tenant-
/// registry routing goes through <see cref="IErmsRoutingResolver"/> too.
/// </summary>
public sealed class ColAuthenticateQueryHandler : IRequestHandler<ColAuthenticateQuery, ColAuthenticateResult>
{
    private readonly IColRequestParser _parser;
    private readonly IErmsAuthRepository _authRepository;
    private readonly ISecretProvider _secretProvider;
    private readonly IErmsRoutingResolver _routingResolver;
    private readonly ITenantRegistryService _tenantRegistryService;

    public ColAuthenticateQueryHandler(IColRequestParser parser, IErmsAuthRepository authRepository, ISecretProvider secretProvider, IErmsRoutingResolver routingResolver, ITenantRegistryService tenantRegistryService)
    {
        _parser = parser;
        _authRepository = authRepository;
        _secretProvider = secretProvider;
        _routingResolver = routingResolver;
        _tenantRegistryService = tenantRegistryService;
    }

    public async Task<ColAuthenticateResult> Handle(ColAuthenticateQuery request, CancellationToken ct)
    {
        try
        {
            var (encounterId, practiceSuffix) = _parser.ParseEncounterId(request.EncounterId);
            var patientId = _parser.Decrypt(request.PatientId);
            var routingContext = _routingResolver.Resolve(request.EncounterId ?? string.Empty);

            var route = await _tenantRegistryService.ResolveRouteAsync(routingContext, ct);
            var routing = route is not null ? CallRoutingInfo.FromPracticeRoute(route, routingContext.Environment) : null;

            var expiryRaw = await _secretProvider.GetSecretAsync("Erms:ExpiryInDays", ct);
            var expiryInDays = double.TryParse(expiryRaw, out var parsed) ? parsed : 0;

            var dbResult = await _authRepository.InsertAndValidateTokenAsync(practiceSuffix, routingContext, request.Username, request.Password, patientId, encounterId, token: null, expiryInDays, pho: null, ct);

            if (dbResult is not null && string.IsNullOrWhiteSpace(dbResult.StatusMessage))
            {
                if (dbResult.Expiry is { } expiry && expiry != DateTime.MinValue)
                {
                    return new ColAuthenticateResult(dbResult.Token, FormatExpiryLikeLegacy(expiry), dbResult.PracticeId, string.Empty, routing);
                }

                return new ColAuthenticateResult(null, null, null, "Invalid credentials!", routing);
            }

            return new ColAuthenticateResult(null, null, null, "Authentication failed!", routing);
        }
        catch (Exception ex)
        {
            return new ColAuthenticateResult(null, null, null, ex.Message);
        }
    }

    // Same fix as ERMS's `FormatExpiryLikeLegacy` (`ErmsCompatController.cs`, 2026-07-30) - legacy's
    // real server always ran with its local system clock set to NZ time, so its "z" format specifier
    // picked up NZ's UTC offset (+12/+13) implicitly. Relying on the container's own ambient timezone
    // for this is fragile (works today only because `docker-compose.yml`'s `TZ: Pacific/Auckland` was
    // added for an unrelated reason - Azure/other hosts aren't guaranteed to have that set), so this
    // computes the real NZ offset explicitly instead. This exact bug was found live 2026-07-31 in
    // COL's own `Authenticate` (separate handler from ERMS's, never got the same fix at the time).
    private static string FormatExpiryLikeLegacy(DateTime expiry)
    {
        var nzZone = TimeZoneInfo.FindSystemTimeZoneById(OperatingSystem.IsWindows() ? "New Zealand Standard Time" : "Pacific/Auckland");
        var offsetHours = nzZone.GetUtcOffset(DateTime.SpecifyKind(expiry, DateTimeKind.Unspecified)).Hours;
        return expiry.ToString("yyyy-MM-ddTHH:mm:ss") + (offsetHours >= 0 ? "+" : "-") + Math.Abs(offsetHours);
    }
}

/// <summary>
/// Shared COL read pipeline (`COLController.cs` Get* ops): COL split (no base64) -&gt; decrypt patientId
/// -&gt; same `[HSS].[uspInsertAndValidateToken]` bearer check the in-process legacy COLController uses
/// -&gt; PHCO proc. Invalid token with blank error =&gt; "Invalid token value!"; exception message =&gt; error.
/// </summary>
internal sealed class ColReadPipeline
{
    private readonly IColRequestParser _parser;
    private readonly IErmsTokenValidator _tokenValidator;
    private readonly IErmsRoutingResolver _routingResolver;
    private readonly ITenantRegistryService _tenantRegistryService;

    public ColReadPipeline(IColRequestParser parser, IErmsTokenValidator tokenValidator, IErmsRoutingResolver routingResolver, ITenantRegistryService tenantRegistryService)
    {
        _parser = parser;
        _tokenValidator = tokenValidator;
        _routingResolver = routingResolver;
        _tenantRegistryService = tenantRegistryService;
    }

    public async Task<ColReadResult> RunAsync(string? patientId, string? encounterId, string? bearerToken,
        Func<string, RoutingContext, string?, string?, Task<DataTable>> fetch, CancellationToken ct = default)
    {
        CallRoutingInfo? routing = null;
        try
        {
            var (parsedEncounterId, practiceSuffix) = _parser.ParseEncounterId(encounterId);
            var parsedPatientId = _parser.Decrypt(patientId);
            var routingContext = _routingResolver.Resolve(encounterId ?? string.Empty);

            var route = await _tenantRegistryService.ResolveRouteAsync(routingContext, ct);
            routing = route is not null ? CallRoutingInfo.FromPracticeRoute(route, routingContext.Environment) : null;

            var validation = await _tokenValidator.ValidateAsync(practiceSuffix, routingContext, parsedPatientId, parsedEncounterId, bearerToken, ct);
            if (!validation.Valid)
            {
                return new ColReadResult(false, null, validation.ErrorMessage ?? "Invalid token value!", routing);
            }

            var table = await fetch(practiceSuffix, routingContext, parsedPatientId, parsedEncounterId);
            return new ColReadResult(true, table, string.Empty, routing);
        }
        catch (Exception ex)
        {
            // routing may already be resolved by the time fetch() throws (e.g. the real legacy
            // GetSessionData bug - an empty stored-procedure name) - still surface it if so.
            return new ColReadResult(false, null, ex.Message, routing);
        }
    }
}

public sealed class ColGetCurrentPatientDataQueryHandler : IRequestHandler<ColGetCurrentPatientDataQuery, ColReadResult>
{
    private readonly ColReadPipeline _pipeline;
    private readonly IColDataRepository _repository;

    public ColGetCurrentPatientDataQueryHandler(IColRequestParser parser, IErmsTokenValidator tokenValidator, IErmsRoutingResolver routingResolver, IColDataRepository repository, ITenantRegistryService tenantRegistryService)
    {
        _pipeline = new ColReadPipeline(parser, tokenValidator, routingResolver, tenantRegistryService);
        _repository = repository;
    }

    public Task<ColReadResult> Handle(ColGetCurrentPatientDataQuery request, CancellationToken ct) =>
        _pipeline.RunAsync(request.PatientId, request.EncounterId, request.BearerToken,
            (suffix, routingContext, patientId, _) => _repository.GetCurrentPatientDataAsync(suffix, routingContext, patientId, ct), ct);
}

public sealed class ColGetSessionDataQueryHandler : IRequestHandler<ColGetSessionDataQuery, ColReadResult>
{
    private readonly ColReadPipeline _pipeline;
    private readonly IColDataRepository _repository;

    public ColGetSessionDataQueryHandler(IColRequestParser parser, IErmsTokenValidator tokenValidator, IErmsRoutingResolver routingResolver, IColDataRepository repository, ITenantRegistryService tenantRegistryService)
    {
        _pipeline = new ColReadPipeline(parser, tokenValidator, routingResolver, tenantRegistryService);
        _repository = repository;
    }

    public Task<ColReadResult> Handle(ColGetSessionDataQuery request, CancellationToken ct) =>
        _pipeline.RunAsync(request.PatientId, request.EncounterId, request.BearerToken,
            (suffix, routingContext, patientId, _) => _repository.GetSessionDataAsync(suffix, routingContext, patientId, ct), ct);
}

public sealed class ColGetProviderDataQueryHandler : IRequestHandler<ColGetProviderDataQuery, ColReadResult>
{
    private readonly ColReadPipeline _pipeline;
    private readonly IColDataRepository _repository;

    public ColGetProviderDataQueryHandler(IColRequestParser parser, IErmsTokenValidator tokenValidator, IErmsRoutingResolver routingResolver, IColDataRepository repository, ITenantRegistryService tenantRegistryService)
    {
        _pipeline = new ColReadPipeline(parser, tokenValidator, routingResolver, tenantRegistryService);
        _repository = repository;
    }

    public Task<ColReadResult> Handle(ColGetProviderDataQuery request, CancellationToken ct) =>
        _pipeline.RunAsync(request.PatientId, request.EncounterId, request.BearerToken,
            (suffix, routingContext, patientId, _) => _repository.GetProviderDataAsync(suffix, routingContext, patientId, ct), ct);
}

public sealed class ColGetSurgeryDataQueryHandler : IRequestHandler<ColGetSurgeryDataQuery, ColReadResult>
{
    private readonly ColReadPipeline _pipeline;
    private readonly IColDataRepository _repository;

    public ColGetSurgeryDataQueryHandler(IColRequestParser parser, IErmsTokenValidator tokenValidator, IErmsRoutingResolver routingResolver, IColDataRepository repository, ITenantRegistryService tenantRegistryService)
    {
        _pipeline = new ColReadPipeline(parser, tokenValidator, routingResolver, tenantRegistryService);
        _repository = repository;
    }

    public Task<ColReadResult> Handle(ColGetSurgeryDataQuery request, CancellationToken ct) =>
        // Legacy: LocationId is passed raw to PHCO.GetSurgeryData - never decoded or decrypted.
        _pipeline.RunAsync(request.PatientId, request.EncounterId, request.BearerToken,
            (suffix, routingContext, patientId, encounterId) => _repository.GetSurgeryDataAsync(suffix, routingContext, patientId, request.LocationId, encounterId, ct), ct);
}

public sealed class ColGetDiagnosisDataQueryHandler : IRequestHandler<ColGetDiagnosisDataQuery, ColReadResult>
{
    private readonly ColReadPipeline _pipeline;
    private readonly IColDataRepository _repository;

    public ColGetDiagnosisDataQueryHandler(IColRequestParser parser, IErmsTokenValidator tokenValidator, IErmsRoutingResolver routingResolver, IColDataRepository repository, ITenantRegistryService tenantRegistryService)
    {
        _pipeline = new ColReadPipeline(parser, tokenValidator, routingResolver, tenantRegistryService);
        _repository = repository;
    }

    public Task<ColReadResult> Handle(ColGetDiagnosisDataQuery request, CancellationToken ct)
    {
        DateTime.TryParse(request.MinDateTime, out var min);
        DateTime.TryParse(request.MaxDateTime, out var max);

        // Legacy quirk: COL passes pmsOrder raw (no ToUpper, unlike ERMS's date-range ops).
        return _pipeline.RunAsync(request.PatientId, request.EncounterId, request.BearerToken,
            (suffix, routingContext, patientId, _) => _repository.GetDiagnosisDataAsync(suffix, routingContext, patientId, request.Order, min, max, ct), ct);
    }
}

/// <summary>Ported from `COLController.SaveInvoice` (`COLController.cs:360`) - token check then `[OnlineClaim].[uspInsertUpdateService]`; sentinels handled by the controller (-3 duplicate, &gt;0 success, else "Invalid values passed!").</summary>
public sealed class ColSaveInvoiceCommandHandler : IRequestHandler<ColSaveInvoiceCommand, ColSaveInvoiceResult>
{
    private readonly IColRequestParser _parser;
    private readonly IErmsTokenValidator _tokenValidator;
    private readonly IErmsRoutingResolver _routingResolver;
    private readonly IColDataRepository _repository;
    private readonly ITenantRegistryService _tenantRegistryService;

    public ColSaveInvoiceCommandHandler(IColRequestParser parser, IErmsTokenValidator tokenValidator, IErmsRoutingResolver routingResolver, IColDataRepository repository, ITenantRegistryService tenantRegistryService)
    {
        _parser = parser;
        _tokenValidator = tokenValidator;
        _routingResolver = routingResolver;
        _repository = repository;
        _tenantRegistryService = tenantRegistryService;
    }

    public async Task<ColSaveInvoiceResult> Handle(ColSaveInvoiceCommand request, CancellationToken ct)
    {
        try
        {
            var (encounterId, practiceSuffix) = _parser.ParseEncounterId(request.EncounterId);
            var patientId = _parser.Decrypt(request.PatientId);
            var routingContext = _routingResolver.Resolve(request.EncounterId ?? string.Empty);

            var route = await _tenantRegistryService.ResolveRouteAsync(routingContext, ct);
            var routing = route is not null ? CallRoutingInfo.FromPracticeRoute(route, routingContext.Environment) : null;

            var validation = await _tokenValidator.ValidateAsync(practiceSuffix, routingContext, patientId, encounterId, request.BearerToken, ct);
            if (!validation.Valid)
            {
                return new ColSaveInvoiceResult(-1, validation.ErrorMessage ?? "Invalid token value!", routing);
            }

            var serviceMappingId = await _repository.SaveInvoiceAsync(practiceSuffix, routingContext, patientId, request.AccountHolderId, encounterId,
                request.ServiceName, request.ServiceCode, request.AmountInclGst, request.Description, request.Payee,
                request.ServiceProvider, request.ServiceProviderType, request.ServiceDate, request.PegasusReference, request.ClaimShortCode, ct);

            if (serviceMappingId is not (-3) && serviceMappingId <= 0)
            {
                return new ColSaveInvoiceResult(serviceMappingId, "Invalid values passed!", routing);
            }

            return new ColSaveInvoiceResult(serviceMappingId, string.Empty, routing);
        }
        catch (Exception ex)
        {
            return new ColSaveInvoiceResult(-1, ex.Message);
        }
    }
}
