namespace HekCoreApi.Contracts.Observations;

public sealed record Observation(string ObservationId, string? ConceptId, string Value, DateTimeOffset RecordedAt);
