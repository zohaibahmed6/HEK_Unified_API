namespace HekCoreApi.Contracts.Conditions;

public sealed record Condition(string ConditionId, string Status, string DiagnosisCode, string? Description, bool IsLongTerm, string? SideCode, string? SideDescription);
