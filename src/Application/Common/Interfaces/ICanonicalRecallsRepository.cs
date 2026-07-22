using HekCoreApi.Application.Common.Models;
using HekCoreApi.Contracts.Recalls;

namespace HekCoreApi.Application.Common.Interfaces;

/// <summary>KARO-only canonical resource (2026-07-23) - no matching HISO concept, no ERMS operation.</summary>
public interface ICanonicalRecallsRepository
{
    Task<IReadOnlyList<RecallCanonical>> GetKaroAsync(RoutingContext routing, string? patientId, CancellationToken ct = default);
}
