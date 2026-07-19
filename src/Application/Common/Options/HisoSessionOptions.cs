namespace HekCoreApi.Application.Common.Options;

/// <summary>
/// New behavior added on top of HISO's unchanged SessionGUID mechanism (ADR-004): an enforced
/// expiry that did not exist before. 12 hours matches ERMS's existing expiry window.
/// </summary>
public sealed class HisoSessionOptions
{
    public const string SectionName = "HisoSession";

    public int ExpiryHours { get; set; } = 12;
}
