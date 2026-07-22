using HekCoreApi.Application.Common.Models;
using HekCoreApi.Contracts.Screening;

namespace HekCoreApi.Application.Common.Interfaces;

/// <summary>KARO-only canonical resource (2026-07-23) - no matching HISO concept, no ERMS operation.</summary>
public interface ICanonicalScreeningCodesRepository
{
    Task<IReadOnlyList<ScreeningCodeCanonical>> GetKaroAsync(RoutingContext routing, CancellationToken ct = default);
}
