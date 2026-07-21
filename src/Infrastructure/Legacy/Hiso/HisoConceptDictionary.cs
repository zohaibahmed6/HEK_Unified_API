using System.Data;
using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Models;
using Microsoft.Extensions.Caching.Memory;

namespace HekCoreApi.Infrastructure.Legacy.Hiso;

/// <summary>
/// Ported from legacy-reference/Hiso/DAL/DBMessages.cs's `GetHisoConceptDetail` (calls
/// `[Hiso].[UspGetHisoConcepts]`, no parameters) and legacy-reference/Hiso/FormSessionService.svc.cs's
/// `getData` (10-minute in-process cache under key `"ConceptList"`, `System.Runtime.Caching.MemoryCache`
/// there - `Microsoft.Extensions.Caching.Memory.IMemoryCache` here, same semantics: single-process,
/// not distributed). Cache key is scoped per practice here (legacy had one single global connection,
/// this project routes per practice via the tenant registry, so the concept dictionary can genuinely
/// differ by practice's database).
/// </summary>
public sealed class HisoConceptDictionary : IHisoConceptDictionary
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    private readonly ILegacyPracticeConnectionResolver _connectionResolver;
    private readonly IMemoryCache _cache;

    public HisoConceptDictionary(ILegacyPracticeConnectionResolver connectionResolver, IMemoryCache cache)
    {
        _connectionResolver = connectionResolver;
        _cache = cache;
    }

    public async Task<IReadOnlyList<HisoConceptDetail>> GetConceptsAsync(string practiceId, CancellationToken ct = default)
    {
        var cacheKey = $"ConceptList:{practiceId}";
        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<HisoConceptDetail>? cached) && cached is not null)
        {
            return cached;
        }

        var connectionString = await _connectionResolver.ResolveAsync(practiceId, ct);
        var table = await LegacyDbExecutor.ExecuteDataTableAsync(connectionString, CommandType.StoredProcedure, "[Hiso].[UspGetHisoConcepts]", ct: ct);
        var concepts = DataTableMapper.ToList<HisoConceptDetail>(table);

        _cache.Set(cacheKey, (IReadOnlyList<HisoConceptDetail>)concepts, CacheDuration);
        return concepts;
    }
}
