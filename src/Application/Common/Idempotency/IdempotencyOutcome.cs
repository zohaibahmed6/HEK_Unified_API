namespace HekCoreApi.Application.Common.Idempotency;

/// <summary>Result of an idempotency-aware write - <see cref="WasDuplicate"/> tells the controller whether to return 200 (existing resource) or 201 (created), per Contract Design doc Section 12.</summary>
public sealed record IdempotencyOutcome<T>(bool WasDuplicate, T Resource) where T : class;
