namespace HekCoreApi.Contracts.Medications;

public sealed record Medication(string MedicationId, string Name, string View, DateOnly? PrescribedDate);
