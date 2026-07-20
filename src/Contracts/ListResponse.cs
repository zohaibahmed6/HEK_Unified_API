namespace HekCoreApi.Contracts;

/// <summary>
/// Canonical list envelope (Contract Design doc Section 6.1, Section 11) - every list endpoint
/// returns the full filtered result set wrapped this way; no pagination in v1 (stakeholder
/// decision). The wrapper key is reserved so pagination metadata can be added later as a
/// non-breaking change.
/// </summary>
public sealed record ListResponse<T>(IReadOnlyList<T> Items);
