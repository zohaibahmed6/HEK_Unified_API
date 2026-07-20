using HekCoreApi.Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace HekCoreApi.Infrastructure.Idempotency;

/// <summary>Day-1 in-memory implementation - see <see cref="IIdempotencyStore"/> remarks for the flagged multi-instance limitation.</summary>
public sealed class InMemoryIdempotencyStore : IIdempotencyStore
{
    private static readonly TimeSpan Ttl = TimeSpan.FromHours(24);
    private readonly IMemoryCache _cache;

    public InMemoryIdempotencyStore(IMemoryCache cache)
    {
        _cache = cache;
    }

    public Task<T?> TryGetAsync<T>(string key, CancellationToken ct = default) where T : class
    {
        _cache.TryGetValue(key, out T? value);
        return Task.FromResult(value);
    }

    public Task SetAsync<T>(string key, T value, CancellationToken ct = default) where T : class
    {
        _cache.Set(key, value, Ttl);
        return Task.CompletedTask;
    }
}
