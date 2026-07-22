using HekCoreApi.Contracts.Security;

namespace HekCoreApi.Contracts.Providers;

/// <summary>
/// Unified registered-practitioners list spanning HISO/ERMS only - KARO has no real registered-
/// practitioners operation (confirmed gap from the real compat controller, same as Allergies).
/// ERMS's real `[HSS].[uspGetRegisteredPractitioners]` and HISO's real `RegisteredPractitioner`
/// concept (`Hiso.uspGetRegisteredPractitioner`) both resolve to the same real fields.
/// </summary>
public sealed record PractitionerCanonical(
    string? ReferenceId,
    string? FullName,
    string? RegisteringBody,
    string? RegistrationNumber,
    string? Email,
    OriginScope Source);
