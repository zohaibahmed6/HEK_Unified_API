namespace HekCoreApi.Application.Common.Options;

/// <summary>
/// ADR-007: "which address was called" is HISO's routing signal - the exact concrete addressing
/// scheme (how many server addresses exist today, how they're currently configured) is not detailed
/// in any source document at the level needed to hardcode it. Configurable placeholder pending
/// confirmation against the live HISO deployment (flagged in PROJECT_STATUS.md).
/// </summary>
public sealed class HisoServerAddressMapOptions
{
    public const string SectionName = "HisoServerAddressMap";

    /// <summary>Server address (as called by the HealthLink-style form engine) -> target DB connection key.</summary>
    public Dictionary<string, string> Addresses { get; set; } = new();
}
