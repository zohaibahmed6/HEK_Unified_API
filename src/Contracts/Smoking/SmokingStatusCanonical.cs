using HekCoreApi.Contracts.Security;

namespace HekCoreApi.Contracts.Smoking;

/// <summary>
/// Unified smoking-status shape spanning HISO/ERMS only - KARO has no real smoking-status operation
/// (confirmed gap). ERMS's real `[HSS].[uspGetSmokingStatus]` and HISO's real `Patient_Smoking`
/// concept resolve to the same fields, confirmed live against patient 2459731.
/// </summary>
public sealed record SmokingStatusCanonical(
    string? ReferenceId,
    string? ConsumptionDescription,
    string? Date,
    OriginScope Source);
