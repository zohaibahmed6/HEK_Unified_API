using HekCoreApi.Application.Common.Interfaces;
using MediatR;

namespace HekCoreApi.Application.Features.Karo.Commands;

public sealed record KaroWriteResult(bool Succeeded, string? SuccessMessage, string? ErrorMessage);

/// <summary>
/// Ported from `APIController.cs`'s remaining real Save* operations. Each shares the same real
/// pipeline (parse encounterId/practiceSuffix, decrypt patientId/userId, validate bearer token, call
/// the real HSSDA-backed write) as the Get* operations.
/// </summary>
public sealed record KaroSaveClinicalNotesCommand(string? PatientId, string? EncounterId, string? UserId, string? SubjectiveNotes, string? ObjectiveNotes, string? Assessment, string? Plans, string? BearerToken) : IRequest<KaroWriteResult>;

public sealed class KaroSaveClinicalNotesCommandHandler : IRequestHandler<KaroSaveClinicalNotesCommand, KaroWriteResult>
{
    private readonly IKaroRequestParser _parser;
    private readonly IKaroTokenValidator _tokenValidator;
    private readonly IKaroWriteRepository _repository;
    public KaroSaveClinicalNotesCommandHandler(IKaroRequestParser parser, IKaroTokenValidator tokenValidator, IKaroWriteRepository repository)
    { _parser = parser; _tokenValidator = tokenValidator; _repository = repository; }

    public async Task<KaroWriteResult> Handle(KaroSaveClinicalNotesCommand request, CancellationToken ct)
    {
        try
        {
            var (encounterId, practiceSuffix, _) = _parser.ParseEncounterId(request.EncounterId);
            var patientId = _parser.Decrypt(request.PatientId);
            var userId = _parser.Decrypt(request.UserId);

            var validation = await _tokenValidator.ValidateAsync(practiceSuffix, patientId, encounterId, request.BearerToken, null, ct);
            if (!validation.Valid)
            {
                return new KaroWriteResult(false, null, "Invalid token value!");
            }

            var saved = await _repository.SaveClinicalNotesAsync(practiceSuffix, patientId, encounterId, userId, request.SubjectiveNotes, request.ObjectiveNotes, request.Assessment, request.Plans, ct);
            return saved > 0
                ? new KaroWriteResult(true, string.Empty, null)
                : new KaroWriteResult(false, null, "Unable to Save Notes.Please try again or contact INDICI support.");
        }
        catch (Exception ex)
        {
            return new KaroWriteResult(false, null, ex.Message);
        }
    }
}

public sealed record KaroSaveConditionCommand(string? PatientId, string? EncounterId, string? UserId, string? Type, string? OnSetDate, string? Summary, string? IsLongTerm, string? ConceptId, string? Name, string? FSN, string? BearerToken) : IRequest<KaroWriteResult>;

public sealed class KaroSaveConditionCommandHandler : IRequestHandler<KaroSaveConditionCommand, KaroWriteResult>
{
    private readonly IKaroRequestParser _parser;
    private readonly IKaroTokenValidator _tokenValidator;
    private readonly IKaroWriteRepository _repository;
    public KaroSaveConditionCommandHandler(IKaroRequestParser parser, IKaroTokenValidator tokenValidator, IKaroWriteRepository repository)
    { _parser = parser; _tokenValidator = tokenValidator; _repository = repository; }

    public async Task<KaroWriteResult> Handle(KaroSaveConditionCommand request, CancellationToken ct)
    {
        try
        {
            var (encounterId, practiceSuffix, _) = _parser.ParseEncounterId(request.EncounterId);
            var patientId = _parser.Decrypt(request.PatientId);
            var userId = _parser.Decrypt(request.UserId);
            DateTime.TryParse(request.OnSetDate, out var onsetDate);

            var validation = await _tokenValidator.ValidateAsync(practiceSuffix, patientId, encounterId, request.BearerToken, null, ct);
            if (!validation.Valid)
            {
                return new KaroWriteResult(false, null, "Invalid token value!");
            }

            var isLongTerm = string.Equals(request.IsLongTerm, "true", StringComparison.OrdinalIgnoreCase);
            var saved = await _repository.SaveConditionAsync(practiceSuffix, patientId, encounterId, userId, request.Type, onsetDate == DateTime.MinValue ? null : onsetDate, request.Summary, isLongTerm, request.ConceptId, request.Name, request.FSN, ct);

            if (saved > 0)
            {
                return new KaroWriteResult(true, string.Empty, null);
            }

            if (saved == -5)
            {
                return new KaroWriteResult(true, "Diagnosis already exits against current Appointment.", null);
            }

            return new KaroWriteResult(false, null, "Unable to Save Diagnosis.Please try again or contact INDICI support.");
        }
        catch (Exception ex)
        {
            return new KaroWriteResult(false, null, ex.Message);
        }
    }
}

/// <summary>Legacy: on success, `message` is a random-6-digit string overwritten by the real service-mapping ID if the save succeeds (or "Invoice already exits." for the -3 sentinel).</summary>
public sealed record KaroSaveInvoiceCommand(string? PatientId, string? EncounterId, string? UserId, string? Name, string? Code, string? Fee, string? Payee, string? BearerToken) : IRequest<KaroWriteResult>;

public sealed class KaroSaveInvoiceCommandHandler : IRequestHandler<KaroSaveInvoiceCommand, KaroWriteResult>
{
    private readonly IKaroRequestParser _parser;
    private readonly IKaroTokenValidator _tokenValidator;
    private readonly IKaroWriteRepository _repository;
    public KaroSaveInvoiceCommandHandler(IKaroRequestParser parser, IKaroTokenValidator tokenValidator, IKaroWriteRepository repository)
    { _parser = parser; _tokenValidator = tokenValidator; _repository = repository; }

    public async Task<KaroWriteResult> Handle(KaroSaveInvoiceCommand request, CancellationToken ct)
    {
        try
        {
            var (encounterId, practiceSuffix, _) = _parser.ParseEncounterId(request.EncounterId);
            var patientId = _parser.Decrypt(request.PatientId);
            var userId = _parser.Decrypt(request.UserId);
            var pho = practiceSuffix.Contains("302_F3H045") ? "SCDHB" : null; // Legacy: real, narrow PHO override quirk.

            var validation = await _tokenValidator.ValidateAsync(practiceSuffix, patientId, encounterId, request.BearerToken, pho, ct);
            if (!validation.Valid)
            {
                return new KaroWriteResult(false, null, "Invalid token value!");
            }

            var serviceMappingId = await _repository.SaveInvoiceAsync(practiceSuffix, patientId, encounterId, request.Name, request.Code, request.Fee, userId, request.Payee, ct);

            if (serviceMappingId > 0)
            {
                return new KaroWriteResult(true, serviceMappingId.ToString(), null);
            }

            if (serviceMappingId == -3)
            {
                return new KaroWriteResult(true, "Invoice already exits.", null);
            }

            return new KaroWriteResult(false, null, "Unable to Save Invoice.Please try again or contact INDICI support.");
        }
        catch (Exception ex)
        {
            return new KaroWriteResult(false, null, ex.Message);
        }
    }
}

/// <summary>Legacy: skips the save entirely (matches `calltoApi = false`) if every screening value is empty - not an error condition until then.</summary>
public sealed record KaroSaveObservationsCommand(string? PatientId, string? EncounterId, string? UserId, string? Temperature, string? WaistCircumference, string? Height, string? Weight, string? BPSys, string? BPDia, string? HeartRate, string? Notes, string? Risk, string? Framingham, string? BearerToken) : IRequest<KaroWriteResult>;

public sealed class KaroSaveObservationsCommandHandler : IRequestHandler<KaroSaveObservationsCommand, KaroWriteResult>
{
    private readonly IKaroRequestParser _parser;
    private readonly IKaroTokenValidator _tokenValidator;
    private readonly IKaroWriteRepository _repository;
    public KaroSaveObservationsCommandHandler(IKaroRequestParser parser, IKaroTokenValidator tokenValidator, IKaroWriteRepository repository)
    { _parser = parser; _tokenValidator = tokenValidator; _repository = repository; }

    public async Task<KaroWriteResult> Handle(KaroSaveObservationsCommand request, CancellationToken ct)
    {
        try
        {
            var (encounterId, practiceSuffix, _) = _parser.ParseEncounterId(request.EncounterId);
            var patientId = _parser.Decrypt(request.PatientId);
            var userId = _parser.Decrypt(request.UserId);
            var pho = practiceSuffix.Contains("302_F3H045") ? "SCDHB" : null; // Legacy: real, narrow PHO override quirk.

            var validation = await _tokenValidator.ValidateAsync(practiceSuffix, patientId, encounterId, request.BearerToken, pho, ct);
            if (!validation.Valid)
            {
                return new KaroWriteResult(false, null, "Invalid token value!");
            }

            var hasAnyValue = !(string.IsNullOrEmpty(request.BPDia) && string.IsNullOrEmpty(request.BPSys) && string.IsNullOrEmpty(request.Temperature) &&
                string.IsNullOrEmpty(request.WaistCircumference) && string.IsNullOrEmpty(request.Height) && string.IsNullOrEmpty(request.Weight) &&
                string.IsNullOrEmpty(request.HeartRate) && string.IsNullOrEmpty(request.Risk) && string.IsNullOrEmpty(request.Framingham));

            if (!hasAnyValue)
            {
                return new KaroWriteResult(false, null, "Unable to Save Observation. Please Send at least one screening value to save Observation in the INDICI.");
            }

            var saved = await _repository.SaveObservationsAsync(practiceSuffix, patientId, encounterId, userId, request.Temperature, request.WaistCircumference, request.Height, request.Weight, request.BPSys, request.BPDia, request.HeartRate, request.Notes, request.Risk, request.Framingham, ct);
            return saved > 0
                ? new KaroWriteResult(true, string.Empty, null)
                : new KaroWriteResult(false, null, "Unable to Save Observation.Please try again or contact INDICI support.");
        }
        catch (Exception ex)
        {
            return new KaroWriteResult(false, null, ex.Message);
        }
    }
}

public sealed record KaroSaveRecallCommand(string? PatientId, string? EncounterId, string? UserId, string? Priority, string? Group, string? DueDate, string? Notes, string? CategoryId, string? BearerToken) : IRequest<KaroWriteResult>;

public sealed class KaroSaveRecallCommandHandler : IRequestHandler<KaroSaveRecallCommand, KaroWriteResult>
{
    private readonly IKaroRequestParser _parser;
    private readonly IKaroTokenValidator _tokenValidator;
    private readonly IKaroWriteRepository _repository;
    public KaroSaveRecallCommandHandler(IKaroRequestParser parser, IKaroTokenValidator tokenValidator, IKaroWriteRepository repository)
    { _parser = parser; _tokenValidator = tokenValidator; _repository = repository; }

    public async Task<KaroWriteResult> Handle(KaroSaveRecallCommand request, CancellationToken ct)
    {
        try
        {
            var (encounterId, practiceSuffix, _) = _parser.ParseEncounterId(request.EncounterId);
            var patientId = _parser.Decrypt(request.PatientId);
            var userId = _parser.Decrypt(request.UserId);
            DateTime.TryParse(request.DueDate, out var dueDate);
            var pho = practiceSuffix.Contains("302_F3H045") ? "SCDHB" : null; // Legacy: real, narrow PHO override quirk.

            var validation = await _tokenValidator.ValidateAsync(practiceSuffix, patientId, encounterId, request.BearerToken, pho, ct);
            if (!validation.Valid)
            {
                return new KaroWriteResult(false, null, "Invalid token value!");
            }

            var saved = await _repository.SaveRecallAsync(practiceSuffix, patientId, encounterId, userId, request.Priority, request.Group, dueDate == DateTime.MinValue ? null : dueDate, request.Notes, request.CategoryId, ct);
            return saved > 0
                ? new KaroWriteResult(true, string.Empty, null)
                : new KaroWriteResult(false, null, "Unable to Save Recall.Please try again or contact INDICI support.");
        }
        catch (Exception ex)
        {
            return new KaroWriteResult(false, null, ex.Message);
        }
    }
}

/// <summary>Legacy: `POST SaveDocument()` (`APIController.cs:1625`) - real DMS write (`[dbo].[uspDocumentSave]`) then real Indici insert (`[HSS].[uspInsertDocument]`). On success, `message` is the real DMS document key GUID.</summary>
public sealed record KaroSaveDocumentCommand(string? PatientId, string? EncounterId, byte[]? MessageData, string? ContentType, string? MessageSubject, string? ItemType, string? BearerToken) : IRequest<KaroWriteResult>;

public sealed class KaroSaveDocumentCommandHandler : IRequestHandler<KaroSaveDocumentCommand, KaroWriteResult>
{
    private readonly IKaroRequestParser _parser;
    private readonly IKaroTokenValidator _tokenValidator;
    private readonly IKaroWriteRepository _repository;
    public KaroSaveDocumentCommandHandler(IKaroRequestParser parser, IKaroTokenValidator tokenValidator, IKaroWriteRepository repository)
    { _parser = parser; _tokenValidator = tokenValidator; _repository = repository; }

    public async Task<KaroWriteResult> Handle(KaroSaveDocumentCommand request, CancellationToken ct)
    {
        try
        {
            var (encounterId, practiceSuffix, practiceSuffixNumeric) = _parser.ParseEncounterId(request.EncounterId);
            var patientId = _parser.Decrypt(request.PatientId);
            var pho = practiceSuffix.Contains("302_F3H045") ? "SCDHB" : null; // Legacy: real, narrow PHO override quirk.

            var validation = await _tokenValidator.ValidateAsync(practiceSuffix, patientId, encounterId, request.BearerToken, pho, ct);
            if (!validation.Valid)
            {
                return new KaroWriteResult(false, null, "Invalid token value!");
            }

            var result = await _repository.SaveDocumentAsync(practiceSuffix, practiceSuffixNumeric, patientId, encounterId, request.MessageData, request.ContentType, request.MessageSubject, request.ItemType, ct);
            return result.Succeeded
                ? new KaroWriteResult(true, result.DmsGuidKey, null)
                : new KaroWriteResult(false, null, "Unable to save document. Please try again or contact INDICI support.");
        }
        catch (Exception ex)
        {
            return new KaroWriteResult(false, null, ex.Message);
        }
    }
}

/// <summary>
/// Legacy: `POST SaveSummary()` (`APIController.cs:978`) - the dynamic, schema-driven encounter-summary
/// save (diabetes/diabetic-foot-exam/retinopathy screening). Real sentinels: `-4` = invalid
/// identifier/schema-not-found (also the real "Error Parsing the JSON Data" case), `-5` = invalid
/// outcome, other &lt;=0 = generic insertion failure.
/// </summary>
public sealed record KaroSaveSummaryCommand(string? PatientId, string? EncounterId, string? System, string? Identifier, string? ProviderId, string? DateTimeRecorded, string EntriesJson, string? BearerToken) : IRequest<KaroWriteResult>;

public sealed class KaroSaveSummaryCommandHandler : IRequestHandler<KaroSaveSummaryCommand, KaroWriteResult>
{
    private readonly IKaroRequestParser _parser;
    private readonly IKaroTokenValidator _tokenValidator;
    private readonly IKaroWriteRepository _repository;
    public KaroSaveSummaryCommandHandler(IKaroRequestParser parser, IKaroTokenValidator tokenValidator, IKaroWriteRepository repository)
    { _parser = parser; _tokenValidator = tokenValidator; _repository = repository; }

    public async Task<KaroWriteResult> Handle(KaroSaveSummaryCommand request, CancellationToken ct)
    {
        try
        {
            var (encounterId, practiceSuffix, _) = _parser.ParseEncounterId(request.EncounterId);
            var pho = practiceSuffix.Contains("302_F3H045") ? "SCDHB" : null; // Legacy: real, narrow PHO override quirk.
            var patientId = _parser.Decrypt(request.PatientId);

            var validation = await _tokenValidator.ValidateAsync(practiceSuffix, patientId, encounterId, request.BearerToken, pho, ct);
            if (!validation.Valid)
            {
                return new KaroWriteResult(false, null, " Invalid token value.");
            }

            var providerId = _parser.Decrypt(request.ProviderId);
            var result = await _repository.SaveSummaryAsync(practiceSuffix, patientId, encounterId, providerId, request.Identifier, request.DateTimeRecorded, request.EntriesJson, ct);

            if (result > 0)
            {
                return new KaroWriteResult(true, null, null);
            }

            return result switch
            {
                KaroSaveSummarySentinels.SchemaNotFound => new KaroWriteResult(false, null, "Invalid values passed!"),
                KaroSaveSummarySentinels.ParseFailed => new KaroWriteResult(false, null, "Error Parsing the JSON Data.Please send correct Data values or contact INDIC support."),
                -4 => new KaroWriteResult(false, null, "Invalid Identifier.Please send correct Identifier or contact INDIC support."),
                -5 => new KaroWriteResult(false, null, "Invalid OutCome.Please send correct Outcome or contact INDIC support."),
                _ => new KaroWriteResult(false, null, "Insertion Failed.Please contact INDIC support.")
            };
        }
        catch (Exception ex)
        {
            return new KaroWriteResult(false, null, ex.Message);
        }
    }
}
