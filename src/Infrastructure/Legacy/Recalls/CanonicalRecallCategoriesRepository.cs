using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Models;
using HekCoreApi.Contracts.Recalls;
using HekCoreApi.Contracts.Security;

namespace HekCoreApi.Infrastructure.Legacy.Recalls;

/// <summary>KARO-only canonical resource (2026-07-23) - reuses <see cref="IKaroDataRepository.GetRecallCategoriesAsync"/>, no new DB code.</summary>
public sealed class CanonicalRecallCategoriesRepository : ICanonicalRecallCategoriesRepository
{
    private readonly IKaroDataRepository _karoRepository;

    public CanonicalRecallCategoriesRepository(IKaroDataRepository karoRepository)
    {
        _karoRepository = karoRepository;
    }

    public async Task<IReadOnlyList<RecallCategoryCanonical>> GetKaroAsync(RoutingContext routing, string? group, CancellationToken ct = default)
    {
        var categories = await _karoRepository.GetRecallCategoriesAsync(routing.PracticeId, routing, group, ct);
        return categories.Select(c => new RecallCategoryCanonical(c.Id, c.Name, c.Code, OriginScope.Karo)).ToList();
    }
}
