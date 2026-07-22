using HekCoreApi.Contracts.Security;

namespace HekCoreApi.Contracts.Recalls;

/// <summary>
/// KARO-only canonical resource - confirmed no matching HISO concept exists in the real dictionary
/// and ERMS has no recalls operation either (both genuine gaps, not oversights). HISO/ERMS-scoped
/// tokens get a clean 501 rather than an invented mapping.
/// </summary>
public sealed record RecallCanonical(
    string? CategoryId,
    string? Priority,
    string? DueDate,
    string? Reason,
    string? Notes,
    OriginScope Source);
