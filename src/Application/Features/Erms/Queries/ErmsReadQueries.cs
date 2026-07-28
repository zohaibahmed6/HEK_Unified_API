using System.Data;
using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Models;
using HekCoreApi.Contracts.Security;
using MediatR;

namespace HekCoreApi.Application.Features.Erms.Queries;

/// <summary>Shared result for all ERMS Get* pipelines - raw DataTable plus (for dated ops) the parsed min/max used for attribute stamping.</summary>
public sealed record ErmsReadResult(bool Succeeded, DataTable? Table, string? ErrorMessage, DateTime MinDate = default, DateTime MaxDate = default);

public sealed record ErmsGetPatientMeasurementQuery(string? PatientId, string? EncounterId, string? BearerToken) : IRequest<ErmsReadResult>;
public sealed record ErmsGetSmokingStatusQuery(string? PatientId, string? EncounterId, string? BearerToken) : IRequest<ErmsReadResult>;
public sealed record ErmsGetCurrentUserQuery(string? PatientId, string? EncounterId, string? LocationId, string? UserId, string? BearerToken) : IRequest<ErmsReadResult>;
public sealed record ErmsGetNextOfKinQuery(string? PatientId, string? EncounterId, string? BearerToken) : IRequest<ErmsReadResult>;
public sealed record ErmsGetRegisteredPractitionersQuery(string? PatientId, string? EncounterId, string? LocationId, string? BearerToken) : IRequest<ErmsReadResult>;
public sealed record ErmsGetAccidentsQuery(string? PatientId, string? EncounterId, string Order, string? MinDateTime, string? MaxDateTime, string? BearerToken) : IRequest<ErmsReadResult>;
public sealed record ErmsGetClassificationsQuery(string? PatientId, string? EncounterId, string Order, string? MinDateTime, string? MaxDateTime, string? BearerToken) : IRequest<ErmsReadResult>;
public sealed record ErmsGetConsultNotesQuery(string? PatientId, string? EncounterId, string Order, string? MinDateTime, string? MaxDateTime, string? BearerToken) : IRequest<ErmsReadResult>;
public sealed record ErmsGetMedicalAllergiesQuery(string? PatientId, string? EncounterId, string Order, string? MinDateTime, string? MaxDateTime, string? BearerToken) : IRequest<ErmsReadResult>;

/// <summary>
/// Ported from ERMS's Get* operations (`APIController.cs`) - the shared bearer-token-validated
/// read pipeline (base64 decode -&gt; real `"__"/"_"` split -&gt; decrypt -&gt; `[HSS].[uspInsertAndValidateToken]`
/// -&gt; data proc). One handler class per op over a shared pipeline, consolidated in one file
/// (same pattern as KARO's `KaroReadQueries.cs`). Legacy: invalid token with blank error =&gt;
/// "Invalid token value!"; any exception message becomes the &lt;Error&gt;&lt;Message&gt; text.
/// </summary>
internal sealed class ErmsReadPipeline
{
    private readonly IErmsRequestParser _parser;
    private readonly IErmsTokenValidator _tokenValidator;
    private readonly IErmsRoutingResolver _routingResolver;

    public ErmsReadPipeline(IErmsRequestParser parser, IErmsTokenValidator tokenValidator, IErmsRoutingResolver routingResolver)
    {
        _parser = parser;
        _tokenValidator = tokenValidator;
        _routingResolver = routingResolver;
    }

    public async Task<ErmsReadResult> RunAsync(string? patientId, string? encounterId, string? bearerToken,
        Func<string, RoutingContext, string?, string?, string?, Task<DataTable>> fetch, DateTime minDate = default, DateTime maxDate = default, CancellationToken ct = default,
        Action<string?>? preValidate = null)
    {
        try
        {
            var (parsedEncounterId, practiceSuffix, _, rawSecondSegment) = _parser.ParseEncounterId(encounterId);

            // Tenant-registry routing context (ADR-001): built from the same raw encounterId ParseEncounterId
            // consumes, via the dedicated IErmsRoutingResolver (composite PracticeId/PracticeCode/Environment
            // key - independent of the legacy practiceSuffix quirks preserved above for the DMS resolver).
            var routingContext = _routingResolver.Resolve(encounterId ?? string.Empty);

            // Legacy computes actualPracticeID (Convert.ToInt32) inside the split block, BEFORE token
            // validation - a non-numeric segment throws into the error envelope even on a bad token.
            preValidate?.Invoke(rawSecondSegment);
            var parsedPatientId = _parser.Decrypt(_parser.DecodeBase64(patientId));

            var validation = await _tokenValidator.ValidateAsync(practiceSuffix, routingContext, parsedPatientId, parsedEncounterId, bearerToken, ct);
            if (!validation.Valid)
            {
                return new ErmsReadResult(false, null, validation.ErrorMessage ?? "Invalid token value!");
            }

            var table = await fetch(practiceSuffix, routingContext, parsedPatientId, parsedEncounterId, rawSecondSegment);
            return new ErmsReadResult(true, table, null, minDate, maxDate);
        }
        catch (Exception ex)
        {
            return new ErmsReadResult(false, null, ex.Message);
        }
    }

    /// <summary>Legacy: `DateTime.TryParse(pmsMinDateTime, out dtMin)` - unparseable/blank stays `DateTime.MinValue`.</summary>
    public static (DateTime Min, DateTime Max) ParseDates(string? minDateTime, string? maxDateTime)
    {
        DateTime.TryParse(minDateTime, out var min);
        DateTime.TryParse(maxDateTime, out var max);
        return (min, max);
    }

    public string? Decrypt(string? value) => _parser.Decrypt(value);
}

public sealed class ErmsGetPatientMeasurementQueryHandler : IRequestHandler<ErmsGetPatientMeasurementQuery, ErmsReadResult>
{
    private readonly ErmsReadPipeline _pipeline;
    private readonly IErmsDataRepository _repository;

    public ErmsGetPatientMeasurementQueryHandler(IErmsRequestParser parser, IErmsTokenValidator tokenValidator, IErmsRoutingResolver routingResolver, IErmsDataRepository repository)
    {
        _pipeline = new ErmsReadPipeline(parser, tokenValidator, routingResolver);
        _repository = repository;
    }

    public Task<ErmsReadResult> Handle(ErmsGetPatientMeasurementQuery request, CancellationToken ct) =>
        _pipeline.RunAsync(request.PatientId, request.EncounterId, request.BearerToken,
            (suffix, routingContext, patientId, _, _) => _repository.GetMeasurementAsync(suffix, routingContext, patientId, ct), ct: ct);
}

public sealed class ErmsGetSmokingStatusQueryHandler : IRequestHandler<ErmsGetSmokingStatusQuery, ErmsReadResult>
{
    private readonly ErmsReadPipeline _pipeline;
    private readonly IErmsDataRepository _repository;

    public ErmsGetSmokingStatusQueryHandler(IErmsRequestParser parser, IErmsTokenValidator tokenValidator, IErmsRoutingResolver routingResolver, IErmsDataRepository repository)
    {
        _pipeline = new ErmsReadPipeline(parser, tokenValidator, routingResolver);
        _repository = repository;
    }

    public Task<ErmsReadResult> Handle(ErmsGetSmokingStatusQuery request, CancellationToken ct) =>
        _pipeline.RunAsync(request.PatientId, request.EncounterId, request.BearerToken,
            (suffix, routingContext, patientId, _, _) => _repository.GetSmokingStatusAsync(suffix, routingContext, patientId, ct), ct: ct);
}

public sealed class ErmsGetCurrentUserQueryHandler : IRequestHandler<ErmsGetCurrentUserQuery, ErmsReadResult>
{
    private readonly ErmsReadPipeline _pipeline;
    private readonly IErmsDataRepository _repository;

    public ErmsGetCurrentUserQueryHandler(IErmsRequestParser parser, IErmsTokenValidator tokenValidator, IErmsRoutingResolver routingResolver, IErmsDataRepository repository)
    {
        _pipeline = new ErmsReadPipeline(parser, tokenValidator, routingResolver);
        _repository = repository;
    }

    public Task<ErmsReadResult> Handle(ErmsGetCurrentUserQuery request, CancellationToken ct)
    {
        // Legacy (`APIController.cs:405-407`): pmsUserId and LocationId also go through GetDcrptValue (no base64 step).
        var userId = _pipeline.Decrypt(request.UserId);
        var locationId = _pipeline.Decrypt(request.LocationId);

        return _pipeline.RunAsync(request.PatientId, request.EncounterId, request.BearerToken,
            (suffix, routingContext, patientId, encounterId, _) => _repository.GetProviderAsync(suffix, routingContext, patientId, userId, locationId, encounterId, ct), ct: ct);
    }
}

public sealed class ErmsGetNextOfKinQueryHandler : IRequestHandler<ErmsGetNextOfKinQuery, ErmsReadResult>
{
    private readonly ErmsReadPipeline _pipeline;
    private readonly IErmsDataRepository _repository;

    public ErmsGetNextOfKinQueryHandler(IErmsRequestParser parser, IErmsTokenValidator tokenValidator, IErmsRoutingResolver routingResolver, IErmsDataRepository repository)
    {
        _pipeline = new ErmsReadPipeline(parser, tokenValidator, routingResolver);
        _repository = repository;
    }

    public Task<ErmsReadResult> Handle(ErmsGetNextOfKinQuery request, CancellationToken ct) =>
        _pipeline.RunAsync(request.PatientId, request.EncounterId, request.BearerToken,
            (suffix, routingContext, patientId, _, _) => _repository.GetNextOfKinAsync(suffix, routingContext, patientId, ct), ct: ct);
}

public sealed class ErmsGetRegisteredPractitionersQueryHandler : IRequestHandler<ErmsGetRegisteredPractitionersQuery, ErmsReadResult>
{
    private readonly ErmsReadPipeline _pipeline;
    private readonly IErmsDataRepository _repository;

    public ErmsGetRegisteredPractitionersQueryHandler(IErmsRequestParser parser, IErmsTokenValidator tokenValidator, IErmsRoutingResolver routingResolver, IErmsDataRepository repository)
    {
        _pipeline = new ErmsReadPipeline(parser, tokenValidator, routingResolver);
        _repository = repository;
    }

    public Task<ErmsReadResult> Handle(ErmsGetRegisteredPractitionersQuery request, CancellationToken ct) =>
        // Legacy (`APIController.cs:1312`): pmsLocationId is passed raw - never decoded or decrypted.
        _pipeline.RunAsync(request.PatientId, request.EncounterId, request.BearerToken,
            (suffix, routingContext, patientId, _, _) => _repository.GetRegisteredPractitionersAsync(suffix, routingContext, patientId, request.LocationId, ct), ct: ct);
}

public sealed class ErmsGetAccidentsQueryHandler : IRequestHandler<ErmsGetAccidentsQuery, ErmsReadResult>
{
    private readonly ErmsReadPipeline _pipeline;
    private readonly IErmsDataRepository _repository;

    public ErmsGetAccidentsQueryHandler(IErmsRequestParser parser, IErmsTokenValidator tokenValidator, IErmsRoutingResolver routingResolver, IErmsDataRepository repository)
    {
        _pipeline = new ErmsReadPipeline(parser, tokenValidator, routingResolver);
        _repository = repository;
    }

    public Task<ErmsReadResult> Handle(ErmsGetAccidentsQuery request, CancellationToken ct)
    {
        var (min, max) = ErmsReadPipeline.ParseDates(request.MinDateTime, request.MaxDateTime);
        return _pipeline.RunAsync(request.PatientId, request.EncounterId, request.BearerToken,
            (suffix, routingContext, patientId, _, _) => _repository.GetAcc45Async(suffix, routingContext, patientId, request.Order.ToUpper(), min, max, ct), min, max, ct);
    }
}

public sealed class ErmsGetClassificationsQueryHandler : IRequestHandler<ErmsGetClassificationsQuery, ErmsReadResult>
{
    private readonly ErmsReadPipeline _pipeline;
    private readonly IErmsDataRepository _repository;

    public ErmsGetClassificationsQueryHandler(IErmsRequestParser parser, IErmsTokenValidator tokenValidator, IErmsRoutingResolver routingResolver, IErmsDataRepository repository)
    {
        _pipeline = new ErmsReadPipeline(parser, tokenValidator, routingResolver);
        _repository = repository;
    }

    public Task<ErmsReadResult> Handle(ErmsGetClassificationsQuery request, CancellationToken ct)
    {
        var (min, max) = ErmsReadPipeline.ParseDates(request.MinDateTime, request.MaxDateTime);
        return _pipeline.RunAsync(request.PatientId, request.EncounterId, request.BearerToken,
            (suffix, routingContext, patientId, _, _) => _repository.GetConditionsAsync(suffix, routingContext, patientId, request.Order.ToUpper(), min, max, ct), min, max, ct);
    }
}

public sealed class ErmsGetConsultNotesQueryHandler : IRequestHandler<ErmsGetConsultNotesQuery, ErmsReadResult>
{
    private readonly ErmsReadPipeline _pipeline;
    private readonly IErmsDataRepository _repository;

    public ErmsGetConsultNotesQueryHandler(IErmsRequestParser parser, IErmsTokenValidator tokenValidator, IErmsRoutingResolver routingResolver, IErmsDataRepository repository)
    {
        _pipeline = new ErmsReadPipeline(parser, tokenValidator, routingResolver);
        _repository = repository;
    }

    public Task<ErmsReadResult> Handle(ErmsGetConsultNotesQuery request, CancellationToken ct)
    {
        var (min, max) = ErmsReadPipeline.ParseDates(request.MinDateTime, request.MaxDateTime);

        // Legacy (`APIController.cs:293-300`): blank strings default to a 24-month lookback / now.
        if (string.IsNullOrEmpty(request.MinDateTime))
        {
            min = DateTime.Now.AddMonths(-24);
        }

        if (string.IsNullOrEmpty(request.MaxDateTime))
        {
            max = DateTime.Now;
        }

        return _pipeline.RunAsync(request.PatientId, request.EncounterId, request.BearerToken,
            (suffix, routingContext, patientId, _, _) => _repository.GetConsultNotesAsync(suffix, routingContext, patientId, request.Order.ToUpper(), min, max, ct), min, max, ct);
    }
}

public sealed class ErmsGetMedicalAllergiesQueryHandler : IRequestHandler<ErmsGetMedicalAllergiesQuery, ErmsReadResult>
{
    private readonly ErmsReadPipeline _pipeline;
    private readonly IErmsDataRepository _repository;

    public ErmsGetMedicalAllergiesQueryHandler(IErmsRequestParser parser, IErmsTokenValidator tokenValidator, IErmsRoutingResolver routingResolver, IErmsDataRepository repository)
    {
        _pipeline = new ErmsReadPipeline(parser, tokenValidator, routingResolver);
        _repository = repository;
    }

    public Task<ErmsReadResult> Handle(ErmsGetMedicalAllergiesQuery request, CancellationToken ct)
    {
        var (min, max) = ErmsReadPipeline.ParseDates(request.MinDateTime, request.MaxDateTime);
        return _pipeline.RunAsync(request.PatientId, request.EncounterId, request.BearerToken,
            (suffix, routingContext, patientId, _, _) => _repository.GetMedicalAllergiesAsync(suffix, routingContext, patientId, request.Order.ToUpper(), min, max, ct), min, max, ct);
    }
}


public sealed record ErmsGetPrescribedMedicationsQuery(string? PatientId, string? EncounterId, string Order, string? MinDateTime, string? MaxDateTime, string? BearerToken) : IRequest<ErmsReadResult>;
public sealed record ErmsGetRegularMedicationsQuery(string? PatientId, string? EncounterId, string Order, string? MinDateTime, string? MaxDateTime, string? BearerToken) : IRequest<ErmsReadResult>;
public sealed record ErmsGetLaboratoryReportListQuery(string? PatientId, string? EncounterId, string Order, string? MinDateTime, string? MaxDateTime, string? BearerToken) : IRequest<ErmsReadResult>;
public sealed record ErmsGetRadiologyReportListQuery(string? PatientId, string? EncounterId, string Order, string? MinDateTime, string? MaxDateTime, string? BearerToken) : IRequest<ErmsReadResult>;
public sealed record ErmsGetDischargeSummaryReportListQuery(string? PatientId, string? EncounterId, string Order, string? MinDateTime, string? MaxDateTime, string? BearerToken) : IRequest<ErmsReadResult>;
public sealed record ErmsGetScannedListQuery(string? PatientId, string? EncounterId, string Order, string? MinDateTime, string? MaxDateTime, string? BearerToken) : IRequest<ErmsReadResult>;
public sealed record ErmsGetLaboratoryReportDetailsQuery(string? PatientId, string? EncounterId, string? ReferenceId, string? BearerToken) : IRequest<ErmsReadResult>;
public sealed record ErmsGetRadiologyReportDetailsQuery(string? PatientId, string? EncounterId, string? ReferenceId, string? BearerToken) : IRequest<ErmsReadResult>;
public sealed record ErmsGetDischargeSummaryDetailsQuery(string? PatientId, string? EncounterId, string? ReferenceId, string? BearerToken) : IRequest<ErmsReadResult>;
public sealed record ErmsGetScannedDetailsQuery(string? PatientId, string? EncounterId, string? ReferenceId, string? BearerToken) : IRequest<ErmsReadResult>;

public sealed class ErmsGetPrescribedMedicationsQueryHandler : IRequestHandler<ErmsGetPrescribedMedicationsQuery, ErmsReadResult>
{
    private readonly ErmsReadPipeline _pipeline;
    private readonly IErmsDataRepository _repository;

    public ErmsGetPrescribedMedicationsQueryHandler(IErmsRequestParser parser, IErmsTokenValidator tokenValidator, IErmsRoutingResolver routingResolver, IErmsDataRepository repository)
    {
        _pipeline = new ErmsReadPipeline(parser, tokenValidator, routingResolver);
        _repository = repository;
    }

    public Task<ErmsReadResult> Handle(ErmsGetPrescribedMedicationsQuery request, CancellationToken ct)
    {
        var (min, max) = ErmsReadPipeline.ParseDates(request.MinDateTime, request.MaxDateTime);
        return _pipeline.RunAsync(request.PatientId, request.EncounterId, request.BearerToken,
            (suffix, routingContext, patientId, _, _) => _repository.GetMedicationsAsync(suffix, routingContext, patientId, request.Order.ToUpper(), min, max, isLongTerm: false, ct), min, max, ct);
    }
}

public sealed class ErmsGetRegularMedicationsQueryHandler : IRequestHandler<ErmsGetRegularMedicationsQuery, ErmsReadResult>
{
    private readonly ErmsReadPipeline _pipeline;
    private readonly IErmsDataRepository _repository;

    public ErmsGetRegularMedicationsQueryHandler(IErmsRequestParser parser, IErmsTokenValidator tokenValidator, IErmsRoutingResolver routingResolver, IErmsDataRepository repository)
    {
        _pipeline = new ErmsReadPipeline(parser, tokenValidator, routingResolver);
        _repository = repository;
    }

    public Task<ErmsReadResult> Handle(ErmsGetRegularMedicationsQuery request, CancellationToken ct)
    {
        var (min, max) = ErmsReadPipeline.ParseDates(request.MinDateTime, request.MaxDateTime);
        return _pipeline.RunAsync(request.PatientId, request.EncounterId, request.BearerToken,
            (suffix, routingContext, patientId, _, _) => _repository.GetMedicationsAsync(suffix, routingContext, patientId, request.Order.ToUpper(), min, max, isLongTerm: true, ct), min, max, ct);
    }
}

public sealed class ErmsGetLaboratoryReportListQueryHandler : IRequestHandler<ErmsGetLaboratoryReportListQuery, ErmsReadResult>
{
    private readonly ErmsReadPipeline _pipeline;
    private readonly IErmsDataRepository _repository;

    public ErmsGetLaboratoryReportListQueryHandler(IErmsRequestParser parser, IErmsTokenValidator tokenValidator, IErmsRoutingResolver routingResolver, IErmsDataRepository repository)
    {
        _pipeline = new ErmsReadPipeline(parser, tokenValidator, routingResolver);
        _repository = repository;
    }

    public Task<ErmsReadResult> Handle(ErmsGetLaboratoryReportListQuery request, CancellationToken ct)
    {
        var (min, max) = ErmsReadPipeline.ParseDates(request.MinDateTime, request.MaxDateTime);
        return _pipeline.RunAsync(request.PatientId, request.EncounterId, request.BearerToken,
            (suffix, routingContext, patientId, _, _) => _repository.GetLabsAsync(suffix, routingContext, patientId, request.Order.ToUpper(), min, max, ct), min, max, ct);
    }
}

public sealed class ErmsGetRadiologyReportListQueryHandler : IRequestHandler<ErmsGetRadiologyReportListQuery, ErmsReadResult>
{
    private readonly ErmsReadPipeline _pipeline;
    private readonly IErmsDataRepository _repository;

    public ErmsGetRadiologyReportListQueryHandler(IErmsRequestParser parser, IErmsTokenValidator tokenValidator, IErmsRoutingResolver routingResolver, IErmsDataRepository repository)
    {
        _pipeline = new ErmsReadPipeline(parser, tokenValidator, routingResolver);
        _repository = repository;
    }

    public Task<ErmsReadResult> Handle(ErmsGetRadiologyReportListQuery request, CancellationToken ct)
    {
        var (min, max) = ErmsReadPipeline.ParseDates(request.MinDateTime, request.MaxDateTime);
        return _pipeline.RunAsync(request.PatientId, request.EncounterId, request.BearerToken,
            (suffix, routingContext, patientId, _, _) => _repository.GetRadsAsync(suffix, routingContext, patientId, request.Order.ToUpper(), min, max, ct), min, max, ct);
    }
}

/// <summary>Legacy quirk shared by the discharge/scanned ops: `actualPracticeID = Convert.ToInt32(splitEncounter[1])` - throws on a non-numeric segment (error envelope), stays 0 when the encounterId has no underscore (Convert.ToInt32(null) == 0).</summary>
public sealed class ErmsGetDischargeSummaryReportListQueryHandler : IRequestHandler<ErmsGetDischargeSummaryReportListQuery, ErmsReadResult>
{
    private readonly ErmsReadPipeline _pipeline;
    private readonly IErmsDataRepository _repository;

    public ErmsGetDischargeSummaryReportListQueryHandler(IErmsRequestParser parser, IErmsTokenValidator tokenValidator, IErmsRoutingResolver routingResolver, IErmsDataRepository repository)
    {
        _pipeline = new ErmsReadPipeline(parser, tokenValidator, routingResolver);
        _repository = repository;
    }

    public Task<ErmsReadResult> Handle(ErmsGetDischargeSummaryReportListQuery request, CancellationToken ct)
    {
        var (min, max) = ErmsReadPipeline.ParseDates(request.MinDateTime, request.MaxDateTime);
        return _pipeline.RunAsync(request.PatientId, request.EncounterId, request.BearerToken,
            (suffix, routingContext, patientId, _, suffixNumeric) => _repository.GetOtherDocsAsync(suffix, suffixNumeric ?? string.Empty, routingContext, patientId, request.Order.ToUpper(), min, max, isReferral: true, ct), min, max, ct, rs => Convert.ToInt32(rs));
    }
}

public sealed class ErmsGetScannedListQueryHandler : IRequestHandler<ErmsGetScannedListQuery, ErmsReadResult>
{
    private readonly ErmsReadPipeline _pipeline;
    private readonly IErmsDataRepository _repository;

    public ErmsGetScannedListQueryHandler(IErmsRequestParser parser, IErmsTokenValidator tokenValidator, IErmsRoutingResolver routingResolver, IErmsDataRepository repository)
    {
        _pipeline = new ErmsReadPipeline(parser, tokenValidator, routingResolver);
        _repository = repository;
    }

    public Task<ErmsReadResult> Handle(ErmsGetScannedListQuery request, CancellationToken ct)
    {
        var (min, max) = ErmsReadPipeline.ParseDates(request.MinDateTime, request.MaxDateTime);
        return _pipeline.RunAsync(request.PatientId, request.EncounterId, request.BearerToken,
            (suffix, routingContext, patientId, _, suffixNumeric) => _repository.GetOtherDocsAsync(suffix, suffixNumeric ?? string.Empty, routingContext, patientId, request.Order.ToUpper(), min, max, isReferral: false, ct), min, max, ct, rs => Convert.ToInt32(rs));
    }
}

public sealed class ErmsGetLaboratoryReportDetailsQueryHandler : IRequestHandler<ErmsGetLaboratoryReportDetailsQuery, ErmsReadResult>
{
    private readonly ErmsReadPipeline _pipeline;
    private readonly IErmsDataRepository _repository;

    public ErmsGetLaboratoryReportDetailsQueryHandler(IErmsRequestParser parser, IErmsTokenValidator tokenValidator, IErmsRoutingResolver routingResolver, IErmsDataRepository repository)
    {
        _pipeline = new ErmsReadPipeline(parser, tokenValidator, routingResolver);
        _repository = repository;
    }

    public Task<ErmsReadResult> Handle(ErmsGetLaboratoryReportDetailsQuery request, CancellationToken ct) =>
        _pipeline.RunAsync(request.PatientId, request.EncounterId, request.BearerToken,
            (suffix, routingContext, patientId, _, _) => _repository.GetLabResultsAsync(suffix, routingContext, patientId, request.ReferenceId, ct), ct: ct);
}

public sealed class ErmsGetRadiologyReportDetailsQueryHandler : IRequestHandler<ErmsGetRadiologyReportDetailsQuery, ErmsReadResult>
{
    private readonly ErmsReadPipeline _pipeline;
    private readonly IErmsDataRepository _repository;

    public ErmsGetRadiologyReportDetailsQueryHandler(IErmsRequestParser parser, IErmsTokenValidator tokenValidator, IErmsRoutingResolver routingResolver, IErmsDataRepository repository)
    {
        _pipeline = new ErmsReadPipeline(parser, tokenValidator, routingResolver);
        _repository = repository;
    }

    public Task<ErmsReadResult> Handle(ErmsGetRadiologyReportDetailsQuery request, CancellationToken ct) =>
        _pipeline.RunAsync(request.PatientId, request.EncounterId, request.BearerToken,
            (suffix, routingContext, patientId, _, _) => _repository.GetRadResultsAsync(suffix, routingContext, patientId, request.ReferenceId, ct), ct: ct);
}

public sealed class ErmsGetDischargeSummaryDetailsQueryHandler : IRequestHandler<ErmsGetDischargeSummaryDetailsQuery, ErmsReadResult>
{
    private readonly ErmsReadPipeline _pipeline;
    private readonly IErmsDataRepository _repository;

    public ErmsGetDischargeSummaryDetailsQueryHandler(IErmsRequestParser parser, IErmsTokenValidator tokenValidator, IErmsRoutingResolver routingResolver, IErmsDataRepository repository)
    {
        _pipeline = new ErmsReadPipeline(parser, tokenValidator, routingResolver);
        _repository = repository;
    }

    public Task<ErmsReadResult> Handle(ErmsGetDischargeSummaryDetailsQuery request, CancellationToken ct) =>
        _pipeline.RunAsync(request.PatientId, request.EncounterId, request.BearerToken,
            (suffix, routingContext, patientId, _, suffixNumeric) => _repository.GetDocResultsAsync(suffix, suffixNumeric ?? string.Empty, routingContext, request.ReferenceId, isDischarge: true, ct), ct: ct, preValidate: rs => Convert.ToInt32(rs));
}

public sealed class ErmsGetScannedDetailsQueryHandler : IRequestHandler<ErmsGetScannedDetailsQuery, ErmsReadResult>
{
    private readonly ErmsReadPipeline _pipeline;
    private readonly IErmsDataRepository _repository;

    public ErmsGetScannedDetailsQueryHandler(IErmsRequestParser parser, IErmsTokenValidator tokenValidator, IErmsRoutingResolver routingResolver, IErmsDataRepository repository)
    {
        _pipeline = new ErmsReadPipeline(parser, tokenValidator, routingResolver);
        _repository = repository;
    }

    public Task<ErmsReadResult> Handle(ErmsGetScannedDetailsQuery request, CancellationToken ct) =>
        _pipeline.RunAsync(request.PatientId, request.EncounterId, request.BearerToken,
            (suffix, routingContext, patientId, _, suffixNumeric) => _repository.GetDocResultsAsync(suffix, suffixNumeric ?? string.Empty, routingContext, request.ReferenceId, isDischarge: false, ct), ct: ct, preValidate: rs => Convert.ToInt32(rs));
}


