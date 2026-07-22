using HekCoreApi.Contracts.Security;

namespace HekCoreApi.Contracts.NextOfKin;

/// <summary>
/// Unified next-of-kin shape spanning HISO/ERMS only - KARO has no real next-of-kin operation
/// (confirmed gap). ERMS's real `[HSS].[uspGetNextOfKin]` and HISO's real `PatientNOK` concept
/// resolve to the same fields, confirmed live against patient 2459731.
/// </summary>
public sealed record NextOfKinCanonical(
    string? ReferenceId,
    string? FirstName,
    string? Surname,
    string? Relationship,
    string? Mobile,
    OriginScope Source);
