using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Models;
using HekCoreApi.Contracts.Screening;
using HekCoreApi.Contracts.Security;

namespace HekCoreApi.Infrastructure.Legacy.Screening;

/// <summary>KARO-only canonical resource (2026-07-23) - reuses <see cref="IKaroDataRepository.GetScreeningCodesAsync"/>, no new DB code.</summary>
public sealed class CanonicalScreeningCodesRepository : ICanonicalScreeningCodesRepository
{
    private readonly IKaroDataRepository _karoRepository;

    public CanonicalScreeningCodesRepository(IKaroDataRepository karoRepository)
    {
        _karoRepository = karoRepository;
    }

    public async Task<IReadOnlyList<ScreeningCodeCanonical>> GetKaroAsync(RoutingContext routing, CancellationToken ct = default)
    {
        var codes = await _karoRepository.GetScreeningCodesAsync(routing.PracticeId, routing, ct);
        return codes.Select(c => new ScreeningCodeCanonical(c.ConceptId, c.ScreeningShortName, c.ScreeningName, OriginScope.Karo)).ToList();
    }
}
