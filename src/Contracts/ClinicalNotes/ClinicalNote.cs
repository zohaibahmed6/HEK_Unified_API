namespace HekCoreApi.Contracts.ClinicalNotes;

public sealed record ClinicalNote(string NoteId, int PatientId, int EncounterId, string Author, DateTimeOffset CreatedAt, string Content);
