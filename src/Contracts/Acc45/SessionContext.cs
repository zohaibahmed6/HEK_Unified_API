namespace HekCoreApi.Contracts.Acc45;

public sealed record SessionContext(string ProviderId, int PatientId, string AppointmentId, string PracticeId, string? ReferenceId, string? PracticeLocationId, string? PracticeEdi);
