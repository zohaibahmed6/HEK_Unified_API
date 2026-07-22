using HekCoreApi.Application.Common.Models;
using HekCoreApi.Contracts.Conditions;

namespace HekCoreApi.Application.Common.Interfaces;

/// <summary>Second canonical resource (2026-07-22), following the same per-source-method pattern as <see cref="IDemographicsRepository"/>.</summary>
public interface ICanonicalConditionsRepository
{
    Task<IReadOnlyList<ConditionCanonical>> GetHisoAsync(HealthLinkSession session, CancellationToken ct = default);

    Task<IReadOnlyList<ConditionCanonical>> GetKaroAsync(RoutingContext routing, string? patientId, CancellationToken ct = default);

    Task<IReadOnlyList<ConditionCanonical>> GetErmsAsync(RoutingContext routing, string? patientId, CancellationToken ct = default);

    /// <summary>COL: `[OnlineClaim].[uspGetConditions]` - a different real procedure than KARO/ERMS's `[HSS].[uspGetConditions]`, confirmed live (2026-07-23) against patient 2459731.</summary>
    Task<IReadOnlyList<ConditionCanonical>> GetColAsync(RoutingContext routing, string? patientId, CancellationToken ct = default);
}
