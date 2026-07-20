namespace HekCoreApi.Application.Common.Interfaces;

/// <summary>
/// Backs the Idempotency-Key header contract (Contract Design doc Section 12): a previously-seen
/// key, scoped to one patient+encounter+practice, returns the original result instead of a new
/// write. Keyed by an opaque string the caller builds (see IdempotencyKeyBuilder in Application).
///
/// DAY-1 SCAFFOLD LIMITATION, FLAGGED: the current implementation is in-process/in-memory (see
/// Infrastructure), which does not dedupe correctly across the multi-instance load-balanced fleet
/// ADR-005 targets - a request could hit a different instance than its earlier duplicate and miss
/// the cached result. A durable, shared store (e.g. a small SQL table or distributed cache) is
/// required before this is production-safe - tracked as a hardening-phase item, not silently
/// presented as complete.
/// </summary>
public interface IIdempotencyStore
{
    Task<T?> TryGetAsync<T>(string key, CancellationToken ct = default) where T : class;

    Task SetAsync<T>(string key, T value, CancellationToken ct = default) where T : class;
}
