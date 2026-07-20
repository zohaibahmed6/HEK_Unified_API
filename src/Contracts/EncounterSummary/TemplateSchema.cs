namespace HekCoreApi.Contracts.EncounterSummary;

public sealed record TemplateField(string Name, string Caption, string Type);

public sealed record TemplateSchema(string Identifier, IReadOnlyList<TemplateField> Fields);
