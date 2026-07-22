using HekCoreApi.Contracts.Security;

namespace HekCoreApi.Contracts.Recalls;

/// <summary>KARO-only canonical resource, same gap-acceptance rationale as <see cref="RecallCanonical"/>.</summary>
public sealed record RecallCategoryCanonical(
    string? Id,
    string? Name,
    string? Code,
    OriginScope Source);
