namespace HekCoreApi.Contracts.Security;

/// <summary>
/// Which legacy system's credential/entry-point authenticated a request (ADR-003).
/// Always assigned structurally, server-side - never accepted as caller input.
/// </summary>
public enum OriginScope
{
    Hiso,
    Karo,
    Erms,
    Col
}
