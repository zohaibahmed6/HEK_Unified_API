namespace HekCoreApi.Application.Common.Idempotency;

/// <summary>
/// Builds a stable idempotency-store key, scoped to one patient+encounter+practice per Contract
/// Design doc Section 12 ("a previously-seen key, scoped to the same patient+encounter+practice").
/// </summary>
public static class IdempotencyKeyBuilder
{
    public static string Build(string domain, string practiceId, string? patientId, string? encounterId, string headerOrNaturalKey) =>
        $"{domain}:{practiceId}:{patientId}:{encounterId}:{headerOrNaturalKey}";
}
