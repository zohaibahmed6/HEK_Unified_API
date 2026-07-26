namespace HekCoreApi.Application.Common.Interfaces;

/// <summary>
/// Ported from `HSSDA.cs`'s remaining real GET-operation DAL methods (`GetConsultNotes`, `GetConditions`,
/// `GetDocuments`, `GetLabResults`, `GetMedications`, `GetObservations`, `GetProvider`,
/// `GetRecallCategories`). One consolidated repository (matches the single, shared `HSSDA` static class
/// legacy itself uses) rather than 8 separate interfaces.
/// </summary>
public interface IKaroDataRepository
{
    Task<List<KaroConsultNote>> GetConsultNotesAsync(string practiceSuffix, string? patientId, CancellationToken ct = default);
    Task<List<KaroDiagnosis>> GetConditionsAsync(string practiceSuffix, string? patientId, CancellationToken ct = default);
    /// <summary>Legacy (`hsswebapi/DAL/South/HSSDA.cs:262-373`): real branch is `CheckAWSIsEnabled(practiceSuffixNumeric, connectionString)` -> `[HSS].[uspGetDocuments_AWS]` (+ per-row `GetDocumentStatusFromIndici` ContentType enrichment when no identifier, or `DocumentGetByDocumentKeyJsonResult` single-doc content when an identifier is given) vs plain `[HSS].[uspGetDocuments]`.</summary>
    Task<List<KaroDocumentInfo>> GetDocumentsAsync(string practiceSuffix, string practiceSuffixNumeric, string? patientId, string? identifier, CancellationToken ct = default);
    Task<List<KaroLabResult>> GetLabResultsAsync(string practiceSuffix, string? patientId, CancellationToken ct = default);
    Task<List<KaroMedication>> GetMedicationsAsync(string practiceSuffix, string? patientId, CancellationToken ct = default);
    Task<List<KaroObservation>> GetObservationsAsync(string practiceSuffix, string? patientId, string? conceptId, CancellationToken ct = default);
    Task<List<KaroProvider>> GetProviderAsync(string practiceSuffix, string? patientId, string? userId, CancellationToken ct = default);
    Task<List<KaroRecallCategory>> GetRecallCategoriesAsync(string practiceSuffix, string? group, CancellationToken ct = default);
    Task<List<KaroRecall>> GetRecallsAsync(string practiceSuffix, string? patientId, CancellationToken ct = default);
    /// <summary>Legacy: `practiceId` param is always the literal "6" (a real hardcoded constant, not derived from the request).</summary>
    Task<List<KaroScreeningCode>> GetScreeningCodesAsync(string practiceSuffix, CancellationToken ct = default);
    Task<List<KaroPatientAttachment>> GetPatientAttachmentAsync(string practiceSuffix, string practiceSuffixNumeric, string? patientId, string? referenceId, string? sortOrder, string? subject, DateTime? dateFrom, DateTime? dateTo, CancellationToken ct = default);
}

// Legacy field names/casing preserved exactly (`Models/APIModels.cs`) - mutable classes, not records, so
// they're compatible with the project's existing `DataTableMapper.ToList<T>` (requires a parameterless
// constructor and settable properties).

public sealed class KaroConsultNote
{
    public string? SubjectiveNotes { get; set; }
    public string? ObjectiveNotes { get; set; }
    public string? Assessment { get; set; }
    public string? Plans { get; set; }
    public string? AppointmentAdvice { get; set; }
    public string? Date { get; set; }
}

public sealed class KaroDiagnosis
{
    public string? DiagnosisDate { get; set; }
    public string? ConceptId { get; set; }
    public string? Name { get; set; }
    public string? FSN { get; set; }
    public string? Type { get; set; }
    public string? OnSetDate { get; set; }
    public string? Summary { get; set; }
    public string? IsLongTerm { get; set; }
}

public sealed class KaroDocumentInfo
{
    public string? CreatedDateTime { get; set; }
    public string? MessageSubject { get; set; }
    public string? Identifier { get; set; }
    public string? MessageTitle { get; set; }
    public byte[]? MessageData { get; set; }
    public string? ContentType { get; set; }
}

public sealed class KaroLabResult
{
    public string? MessageSubject { get; set; }
    public string? Title { get; set; }
    public string? Code { get; set; }
    public string? EffectiveDateTime { get; set; }
    public string? Value { get; set; }
}

public sealed class KaroMedication
{
    public string? Sctid { get; set; }
    public string? MedicineName { get; set; }
    public string? Dosage { get; set; }
    public string? Route { get; set; }
    public string? ExpectedDuration { get; set; }
    public string? StartDate { get; set; }
    public string? IsLongterm { get; set; }
    public string? Directions { get; set; }
}

public sealed class KaroObservation
{
    public string? ObservationDate { get; set; }
    public string? ConceptId { get; set; }
    public string? ShortName { get; set; }
    public string? Name { get; set; }
    public string? Value { get; set; }
    public string? Units { get; set; }
}

public sealed class KaroProvider
{
    public string? Type { get; set; }
    public string? Nzmc { get; set; }
    public string? BirthDate { get; set; }
    public string? TitleCode { get; set; }
    public string? Given { get; set; }
    public string? Family { get; set; }
    public string? Gender { get; set; }
    public string? DayPhone { get; set; }
    public string? Email { get; set; }
}

public sealed class KaroRecallCategory
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Code { get; set; }
}

public sealed class KaroRecall
{
    public string? Group { get; set; }
    public string? CategoryId { get; set; }
    public string? Priority { get; set; }
    public string? DueDate { get; set; }
    public string? Notes { get; set; }
    public string? Reason { get; set; }
}

public sealed class KaroScreeningCode
{
    public string? ConceptId { get; set; }
    public string? ScreeningShortName { get; set; }
    public string? ScreeningName { get; set; }
}

/// <summary>Legacy: `PatientDMS2` - `AttachmentContent` is the base64 string form, not raw bytes (matching what legacy actually returns on the wire).</summary>
public sealed class KaroPatientAttachment
{
    public string? AttachmentReferenceID { get; set; }
    public string? AttachmentSubject { get; set; }
    public string? AttachmentComments { get; set; }
    public string? AttachmentType { get; set; }
    public string? AttachmentDataType { get; set; }
    public string? AttachmentCreationDate { get; set; }
    public string? AttachmentContent { get; set; }
    public string? AttachmentSize { get; set; }
}
