using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Models;
using MediatR;

namespace HekCoreApi.Application.Features.Karo.Queries;

/// <summary>
/// Ported from `APIController.cs`'s remaining real Get* operations. Each shares the exact real
/// pipeline (parse encounterId, decrypt patientId, validate bearer token, call the real HSSDA-backed
/// repository) already established for `KaroDemographicsQuery` - only the repository call and returned
/// entry list differ per operation.
/// </summary>
public sealed record KaroClinicalNotesQuery(string? System, string? Pho, string? PatientId, string? EncounterId, string? BearerToken) : IRequest<KaroListResult<KaroConsultNote>>;
public sealed record KaroConditionsQuery(string? System, string? Pho, string? PatientId, string? EncounterId, string? BearerToken) : IRequest<KaroListResult<KaroDiagnosis>>;
public sealed record KaroDocumentsQuery(string? System, string? Pho, string? PatientId, string? EncounterId, string? Identifier, string? BearerToken) : IRequest<KaroListResult<KaroDocumentInfo>>;
public sealed record KaroLabResultsQuery(string? System, string? Pho, string? PatientId, string? EncounterId, string? BearerToken) : IRequest<KaroListResult<KaroLabResult>>;
public sealed record KaroMedicationsQuery(string? System, string? Pho, string? PatientId, string? EncounterId, string? BearerToken) : IRequest<KaroListResult<KaroMedication>>;
public sealed record KaroObservationsQuery(string? System, string? Pho, string? PatientId, string? EncounterId, string? ConceptId, string? BearerToken) : IRequest<KaroListResult<KaroObservation>>;
public sealed record KaroProviderQuery(string? System, string? Pho, string? PatientId, string? EncounterId, string? UserId, string? BearerToken) : IRequest<KaroListResult<KaroProvider>>;
public sealed record KaroRecallCategoriesQuery(string? System, string? Pho, string? PatientId, string? EncounterId, string? Group, string? BearerToken) : IRequest<KaroListResult<KaroRecallCategory>>;

public sealed record KaroListResult<T>(bool Succeeded, string? PatientId, List<T>? Entries, string? ErrorMessage);

/// <summary>Shared real pipeline: parse encounterId/patientId, validate bearer token, run a repository call. Reused by every handler below.</summary>
internal sealed class KaroPipeline
{
    private readonly IKaroRequestParser _parser;
    private readonly IKaroTokenValidator _tokenValidator;
    private readonly IKaroRoutingResolver _routingResolver;

    public KaroPipeline(IKaroRequestParser parser, IKaroTokenValidator tokenValidator, IKaroRoutingResolver routingResolver)
    {
        _parser = parser;
        _tokenValidator = tokenValidator;
        _routingResolver = routingResolver;
    }

    public async Task<KaroListResult<T>> RunAsync<T>(
        string? pho, string? patientId, string? encounterId, string? bearerToken,
        Func<string, RoutingContext, string?, CancellationToken, Task<List<T>>> repositoryCall, CancellationToken ct)
    {
        try
        {
            var (parsedEncounterId, practiceSuffix, _) = _parser.ParseEncounterId(encounterId);
            var decryptedPatientId = _parser.Decrypt(patientId);

            // Tenant-registry routing context (ADR-001), built independently of the legacy
            // practiceSuffix quirks preserved above (those still feed the DMS resolver only).
            var routingContext = _routingResolver.Resolve(encounterId ?? string.Empty);

            var validation = await _tokenValidator.ValidateAsync(practiceSuffix, routingContext, decryptedPatientId, parsedEncounterId, bearerToken, pho, ct);
            if (!validation.Valid)
            {
                return new KaroListResult<T>(false, null, null, "Invalid token value!");
            }

            var entries = await repositoryCall(practiceSuffix, routingContext, decryptedPatientId, ct);
            return new KaroListResult<T>(true, decryptedPatientId, entries, null);
        }
        catch (Exception ex)
        {
            return new KaroListResult<T>(false, null, null, ex.Message);
        }
    }
}

public sealed class KaroClinicalNotesQueryHandler : IRequestHandler<KaroClinicalNotesQuery, KaroListResult<KaroConsultNote>>
{
    private readonly KaroPipeline _pipeline;
    private readonly IKaroDataRepository _repository;
    public KaroClinicalNotesQueryHandler(IKaroRequestParser parser, IKaroTokenValidator validator, IKaroRoutingResolver routingResolver, IKaroDataRepository repository)
    { _pipeline = new KaroPipeline(parser, validator, routingResolver); _repository = repository; }
    public Task<KaroListResult<KaroConsultNote>> Handle(KaroClinicalNotesQuery request, CancellationToken ct) =>
        _pipeline.RunAsync(request.Pho, request.PatientId, request.EncounterId, request.BearerToken,
            (suffix, routingContext, patientId, c) => _repository.GetConsultNotesAsync(suffix, routingContext, patientId, c), ct);
}

public sealed class KaroConditionsQueryHandler : IRequestHandler<KaroConditionsQuery, KaroListResult<KaroDiagnosis>>
{
    private readonly KaroPipeline _pipeline;
    private readonly IKaroDataRepository _repository;
    public KaroConditionsQueryHandler(IKaroRequestParser parser, IKaroTokenValidator validator, IKaroRoutingResolver routingResolver, IKaroDataRepository repository)
    { _pipeline = new KaroPipeline(parser, validator, routingResolver); _repository = repository; }
    public Task<KaroListResult<KaroDiagnosis>> Handle(KaroConditionsQuery request, CancellationToken ct) =>
        _pipeline.RunAsync(request.Pho, request.PatientId, request.EncounterId, request.BearerToken,
            (suffix, routingContext, patientId, c) => _repository.GetConditionsAsync(suffix, routingContext, patientId, c), ct);
}

public sealed class KaroDocumentsQueryHandler : IRequestHandler<KaroDocumentsQuery, KaroListResult<KaroDocumentInfo>>
{
    private readonly IKaroRequestParser _parser;
    private readonly IKaroTokenValidator _tokenValidator;
    private readonly IKaroRoutingResolver _routingResolver;
    private readonly IKaroDataRepository _repository;
    public KaroDocumentsQueryHandler(IKaroRequestParser parser, IKaroTokenValidator tokenValidator, IKaroRoutingResolver routingResolver, IKaroDataRepository repository)
    { _parser = parser; _tokenValidator = tokenValidator; _routingResolver = routingResolver; _repository = repository; }

    public async Task<KaroListResult<KaroDocumentInfo>> Handle(KaroDocumentsQuery request, CancellationToken ct)
    {
        try
        {
            var (encounterId, practiceSuffix, practiceSuffixNumeric) = _parser.ParseEncounterId(request.EncounterId);
            var patientId = _parser.Decrypt(request.PatientId);
            var routingContext = _routingResolver.Resolve(request.EncounterId ?? string.Empty);

            var validation = await _tokenValidator.ValidateAsync(practiceSuffix, routingContext, patientId, encounterId, request.BearerToken, request.Pho, ct);
            if (!validation.Valid)
            {
                return new KaroListResult<KaroDocumentInfo>(false, null, null, "Invalid token value!");
            }

            var entries = await _repository.GetDocumentsAsync(practiceSuffix, practiceSuffixNumeric, routingContext, patientId, request.Identifier, ct);
            return new KaroListResult<KaroDocumentInfo>(true, patientId, entries, null);
        }
        catch (Exception ex)
        {
            return new KaroListResult<KaroDocumentInfo>(false, null, null, ex.Message);
        }
    }
}

public sealed class KaroLabResultsQueryHandler : IRequestHandler<KaroLabResultsQuery, KaroListResult<KaroLabResult>>
{
    private readonly KaroPipeline _pipeline;
    private readonly IKaroDataRepository _repository;
    public KaroLabResultsQueryHandler(IKaroRequestParser parser, IKaroTokenValidator validator, IKaroRoutingResolver routingResolver, IKaroDataRepository repository)
    { _pipeline = new KaroPipeline(parser, validator, routingResolver); _repository = repository; }
    public Task<KaroListResult<KaroLabResult>> Handle(KaroLabResultsQuery request, CancellationToken ct) =>
        _pipeline.RunAsync(request.Pho, request.PatientId, request.EncounterId, request.BearerToken,
            (suffix, routingContext, patientId, c) => _repository.GetLabResultsAsync(suffix, routingContext, patientId, c), ct);
}

public sealed class KaroMedicationsQueryHandler : IRequestHandler<KaroMedicationsQuery, KaroListResult<KaroMedication>>
{
    private readonly KaroPipeline _pipeline;
    private readonly IKaroDataRepository _repository;
    public KaroMedicationsQueryHandler(IKaroRequestParser parser, IKaroTokenValidator validator, IKaroRoutingResolver routingResolver, IKaroDataRepository repository)
    { _pipeline = new KaroPipeline(parser, validator, routingResolver); _repository = repository; }
    public Task<KaroListResult<KaroMedication>> Handle(KaroMedicationsQuery request, CancellationToken ct) =>
        _pipeline.RunAsync(request.Pho, request.PatientId, request.EncounterId, request.BearerToken,
            (suffix, routingContext, patientId, c) => _repository.GetMedicationsAsync(suffix, routingContext, patientId, c), ct);
}

public sealed class KaroObservationsQueryHandler : IRequestHandler<KaroObservationsQuery, KaroListResult<KaroObservation>>
{
    private readonly KaroPipeline _pipeline;
    private readonly IKaroDataRepository _repository;
    public KaroObservationsQueryHandler(IKaroRequestParser parser, IKaroTokenValidator validator, IKaroRoutingResolver routingResolver, IKaroDataRepository repository)
    { _pipeline = new KaroPipeline(parser, validator, routingResolver); _repository = repository; }
    public Task<KaroListResult<KaroObservation>> Handle(KaroObservationsQuery request, CancellationToken ct) =>
        _pipeline.RunAsync(request.Pho, request.PatientId, request.EncounterId, request.BearerToken,
            (suffix, routingContext, patientId, c) => _repository.GetObservationsAsync(suffix, routingContext, patientId, request.ConceptId, c), ct);
}

/// <summary>Legacy: only the first `Provider` row is ever returned (`ListProvider[0]`), not the full list.</summary>
public sealed class KaroProviderQueryHandler : IRequestHandler<KaroProviderQuery, KaroListResult<KaroProvider>>
{
    private readonly IKaroRequestParser _parser;
    private readonly KaroPipeline _pipeline;
    private readonly IKaroDataRepository _repository;
    public KaroProviderQueryHandler(IKaroRequestParser parser, IKaroTokenValidator validator, IKaroRoutingResolver routingResolver, IKaroDataRepository repository)
    { _parser = parser; _pipeline = new KaroPipeline(parser, validator, routingResolver); _repository = repository; }
    public async Task<KaroListResult<KaroProvider>> Handle(KaroProviderQuery request, CancellationToken ct)
    {
        var userId = _parser.Decrypt(request.UserId);
        var result = await _pipeline.RunAsync(request.Pho, request.PatientId, request.EncounterId, request.BearerToken,
            (suffix, routingContext, patientId, c) => _repository.GetProviderAsync(suffix, routingContext, patientId, userId, c), ct);

        return result.Entries is { Count: > 0 } ? result with { Entries = [result.Entries[0]] } : result;
    }
}

public sealed class KaroRecallCategoriesQueryHandler : IRequestHandler<KaroRecallCategoriesQuery, KaroListResult<KaroRecallCategory>>
{
    private readonly KaroPipeline _pipeline;
    private readonly IKaroDataRepository _repository;
    public KaroRecallCategoriesQueryHandler(IKaroRequestParser parser, IKaroTokenValidator validator, IKaroRoutingResolver routingResolver, IKaroDataRepository repository)
    { _pipeline = new KaroPipeline(parser, validator, routingResolver); _repository = repository; }
    public Task<KaroListResult<KaroRecallCategory>> Handle(KaroRecallCategoriesQuery request, CancellationToken ct) =>
        _pipeline.RunAsync(request.Pho, request.PatientId, request.EncounterId, request.BearerToken,
            (suffix, routingContext, patientId, c) => _repository.GetRecallCategoriesAsync(suffix, routingContext, request.Group, c), ct);
}

public sealed record KaroRecallsQuery(string? System, string? Pho, string? PatientId, string? EncounterId, string? BearerToken) : IRequest<KaroListResult<KaroRecall>>;

public sealed class KaroRecallsQueryHandler : IRequestHandler<KaroRecallsQuery, KaroListResult<KaroRecall>>
{
    private readonly KaroPipeline _pipeline;
    private readonly IKaroDataRepository _repository;
    public KaroRecallsQueryHandler(IKaroRequestParser parser, IKaroTokenValidator validator, IKaroRoutingResolver routingResolver, IKaroDataRepository repository)
    { _pipeline = new KaroPipeline(parser, validator, routingResolver); _repository = repository; }
    public Task<KaroListResult<KaroRecall>> Handle(KaroRecallsQuery request, CancellationToken ct) =>
        _pipeline.RunAsync(request.Pho, request.PatientId, request.EncounterId, request.BearerToken,
            (suffix, routingContext, patientId, c) => _repository.GetRecallsAsync(suffix, routingContext, patientId, c), ct);
}

/// <summary>Legacy: real `@pPracticeId` is always the literal "6", not derived from the request.</summary>
public sealed record KaroScreeningCodesQuery(string? System, string? Pho, string? PatientId, string? EncounterId, string? BearerToken) : IRequest<KaroListResult<KaroScreeningCode>>;

public sealed class KaroScreeningCodesQueryHandler : IRequestHandler<KaroScreeningCodesQuery, KaroListResult<KaroScreeningCode>>
{
    private readonly KaroPipeline _pipeline;
    private readonly IKaroDataRepository _repository;
    public KaroScreeningCodesQueryHandler(IKaroRequestParser parser, IKaroTokenValidator validator, IKaroRoutingResolver routingResolver, IKaroDataRepository repository)
    { _pipeline = new KaroPipeline(parser, validator, routingResolver); _repository = repository; }
    public Task<KaroListResult<KaroScreeningCode>> Handle(KaroScreeningCodesQuery request, CancellationToken ct) =>
        _pipeline.RunAsync(request.Pho, request.PatientId, request.EncounterId, request.BearerToken,
            (suffix, routingContext, patientId, c) => _repository.GetScreeningCodesAsync(suffix, routingContext, c), ct);
}

/// <summary>
/// Ported from `APIController.cs`'s `GetPatientAttachment` (`:1193`) - `practiceSuffixNumeric` is the
/// real `practiceid2` variable (the raw numeric practice suffix segment, underscore stripped).
/// </summary>
public sealed record KaroPatientAttachmentQuery(string? System, string? Pho, string? PatientId, string? EncounterId, string? ReferenceId, string? SortOrder, string? Subject, DateTime? DateFrom, DateTime? DateTo, string? BearerToken) : IRequest<KaroListResult<KaroPatientAttachment>>;

public sealed class KaroPatientAttachmentQueryHandler : IRequestHandler<KaroPatientAttachmentQuery, KaroListResult<KaroPatientAttachment>>
{
    private readonly IKaroRequestParser _parser;
    private readonly IKaroTokenValidator _tokenValidator;
    private readonly IKaroRoutingResolver _routingResolver;
    private readonly IKaroDataRepository _repository;
    public KaroPatientAttachmentQueryHandler(IKaroRequestParser parser, IKaroTokenValidator tokenValidator, IKaroRoutingResolver routingResolver, IKaroDataRepository repository)
    { _parser = parser; _tokenValidator = tokenValidator; _routingResolver = routingResolver; _repository = repository; }

    public async Task<KaroListResult<KaroPatientAttachment>> Handle(KaroPatientAttachmentQuery request, CancellationToken ct)
    {
        try
        {
            var (encounterId, practiceSuffix, practiceSuffixNumeric) = _parser.ParseEncounterId(request.EncounterId);
            var patientId = _parser.Decrypt(request.PatientId);
            var routingContext = _routingResolver.Resolve(request.EncounterId ?? string.Empty);

            var validation = await _tokenValidator.ValidateAsync(practiceSuffix, routingContext, patientId, encounterId, request.BearerToken, request.Pho, ct);
            if (!validation.Valid)
            {
                return new KaroListResult<KaroPatientAttachment>(false, null, null, "Invalid token value!");
            }

            var entries = await _repository.GetPatientAttachmentAsync(practiceSuffix, practiceSuffixNumeric, routingContext, patientId, request.ReferenceId, request.SortOrder, request.Subject, request.DateFrom, request.DateTo, ct);
            return new KaroListResult<KaroPatientAttachment>(true, patientId, entries, null);
        }
        catch (Exception ex)
        {
            return new KaroListResult<KaroPatientAttachment>(false, null, null, ex.Message);
        }
    }
}

/// <summary>
/// Ported from `APIController.cs`'s `GetEncounterSummary` (`:407`) - real, genuine hardcoded stub
/// responses for `identifier` "diap"/"cvra" (literal JSON strings in legacy source, not DB-driven at
/// all), `"{}"` for anything else. Reproduced exactly, not "implemented for real" - this is what
/// legacy actually does.
/// </summary>
public sealed record KaroEncounterSummaryQuery(string? System, string? Pho, string? PatientId, string? EncounterId, string? Identifier, string? BearerToken) : IRequest<KaroRawJsonResult>;

public sealed record KaroRawJsonResult(bool Succeeded, string Json);

public sealed class KaroEncounterSummaryQueryHandler : IRequestHandler<KaroEncounterSummaryQuery, KaroRawJsonResult>
{
    private readonly IKaroRequestParser _parser;
    private readonly IKaroTokenValidator _tokenValidator;
    private readonly IKaroRoutingResolver _routingResolver;

    public KaroEncounterSummaryQueryHandler(IKaroRequestParser parser, IKaroTokenValidator tokenValidator, IKaroRoutingResolver routingResolver)
    {
        _parser = parser;
        _tokenValidator = tokenValidator;
        _routingResolver = routingResolver;
    }

    public async Task<KaroRawJsonResult> Handle(KaroEncounterSummaryQuery request, CancellationToken ct)
    {
        var (encounterId, practiceSuffix, _) = _parser.ParseEncounterId(request.EncounterId);
        var patientId = _parser.Decrypt(request.PatientId);
        var routingContext = _routingResolver.Resolve(request.EncounterId ?? string.Empty);

        var validation = await _tokenValidator.ValidateAsync(practiceSuffix, routingContext, patientId, encounterId, request.BearerToken, request.Pho, ct);
        if (!validation.Valid)
        {
            return new KaroRawJsonResult(false, "{\"status\":\"fail\",\"message\":\"Invalid token value!\"}");
        }

        var identifier = request.Identifier?.ToLowerInvariant();
        var json = identifier switch
        {
            "diap" => "{\"patientId\":\"941272\",\"resourceType\":\"EncounterSummary\",\"identifier\":\"DIAP\",\"dateTimeRecorded\":\"2016-05-23T13:30:00\",\"entry\":[{\"DIAP_OUTCOME\":\"D\",\"DIAP_TYPE\":\"Type 1\",\"DIAP_YOD\":2016,\"DIAP_HEIGHT\":175,\"DIAP_WEIGHT\":75,\"DIAP_RETINAL\":2015,\"DIAP_BP_SYS\":120,\"DIAP_BP_DIA\":80,\"DIAP_HBA1C\":50,\"DIAP_ALBUMIN\":45,\"DIAP_DIPSTICK\":\"Positive\",\"DIAP_CHOL\":5,\"DIAP_HDL\":7,\"DIAP_TRIG\":9,\"DIAP_ETHNICITY\":11,\"DIAP_NHI\":\"NZZ3100\",\"DIAP_SMOKER\":\"Recently Quit\",\"DIAP_CONSENT\":true,\"DIAP_MAILING\":false,\"DIAP_INSULIN\":\"Yes\",\"DIAP_ORAL\":\"No\",\"DIAP_DIET\":\"Yes\",\"DIAP_ACE\":\"No\",\"DIAP_BBLOCKER\":\"Yes\",\"DIAP_STATIN\":\"No\",\"DIAP_OTHERMEDS\":\"Yes\",\"DIAP_FOOT\":\"No\",\"DIAP_PVD\":\"No\",\"DIAP_FOOD\":\"Yes\",\"DIAP_ACTIVITY\":\"Yes\",\"DIAP_RISK\":\"Moderate\"}]}",
            "cvra" => "{\"patientId\":\"941272\",\"resourceType\":\"EncounterSummary\",\"identifier\":\"CVRA\",\"dateTimeRecorded\":\"2016-05-23T13:30:00\",\"entry\":[{\"Risk\":\"20\",\"Framingham\":\"30\"}]}",
            _ => "{}"
        };

        return new KaroRawJsonResult(true, json);
    }
}
