namespace HekCoreApi.Adapters.Karo;

// Legacy field names preserved exactly (`Models/APIModels.cs`'s `ConsultNote`/`Condition`/`Invoice`/`Observations`/`Recall`).

public sealed record KaroConsultNoteRequest(string? PatientId, string? EncounterId, string? UserId, string? SubjectiveNotes, string? ObjectiveNotes, string? Assessment, string? Plans, string? AppointmentAdvice, string? Date);

public sealed record KaroConditionRequest(string? PatientId, string? EncounterId, string? UserId, string? DiagnosisDate, string? ConceptId, string? Name, string? FSN, string? Type, string? OnSetDate, string? Summary, string? IsLongTerm);

public sealed record KaroInvoiceRequest(string? PatientId, string? EncounterId, string? UserId, string? LocationId, string? Name, string? Code, string? ClaimType, string? Fee, string? payee);

public sealed record KaroObservationsRequest(string? PatientId, string? EncounterId, string? UserId, string? Temperature, string? WaistCircumference, string? Height, string? Weight, string? BPSys, string? BPDia, string? HeartRate, string? Risk, string? Framingham, string? Notes);

public sealed record KaroRecallRequest(string? PatientId, string? EncounterId, string? UserId, string? Group, string? CategoryId, string? Priority, string? DueDate, string? Notes, string? Reason);

public sealed record KaroDocumentRequest(string? PatientId, string? EncounterId, string? MessageSubject, byte[]? MessageData, string? ContentType, string? ItemType);
