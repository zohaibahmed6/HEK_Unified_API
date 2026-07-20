namespace HekCoreApi.Contracts.EncounterSummary;

public sealed record EncounterSummaryData(string Identifier, IDictionary<string, object?> Fields);

public sealed record EncounterSummaryInput(string Identifier, IDictionary<string, object?> Fields);
