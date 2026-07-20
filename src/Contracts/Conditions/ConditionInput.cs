namespace HekCoreApi.Contracts.Conditions;

public sealed record ConditionInput(string DiagnosisCode, string? Description, bool IsLongTerm = false, string? SideCode = null, string? SideDescription = null);
