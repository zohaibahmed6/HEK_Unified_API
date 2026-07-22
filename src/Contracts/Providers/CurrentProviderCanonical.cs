using HekCoreApi.Contracts.Security;

namespace HekCoreApi.Contracts.Providers;

/// <summary>
/// Unified single-record "logged-in provider" shape spanning HISO/KARO/ERMS. All three resolve to
/// the same real underlying data: KARO's `provider` and ERMS's `GetCurrentUser` both call the
/// identical real `[HSS].[uspGetProvider]` (first row only); HISO's real `CurrentUser` concept
/// (`Hiso.uspGetCurrentUser`) is a flat, non-list concept, same as `Patient` for Demographics.
/// Deliberately scoped to the plain `Given`/`Family`/`Email`/`DayPhone` columns already confirmed
/// real elsewhere in this codebase (Demographics' KARO/ERMS bugfix) - `uspGetProvider` returned zero
/// rows for every patient/no-patient combination tried live, so the richer columns
/// (`FullName`/`RegisteringBody`/`RegistrationNumber`) could not be confirmed real vs. composite-
/// reference garbage the same way Demographics' equivalent columns were; not guessed.
/// </summary>
public sealed record CurrentProviderCanonical(
    string? GivenName,
    string? FamilyName,
    string? Email,
    string? Phone,
    OriginScope Source);
