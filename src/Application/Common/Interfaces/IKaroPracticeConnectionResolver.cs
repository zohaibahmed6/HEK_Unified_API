using HekCoreApi.Application.Common.Models;

namespace HekCoreApi.Application.Common.Interfaces;

/// <summary>
/// KARO/HSS's connection routing, now backed by the central tenant registry (ADR-001) instead of the
/// old flat per-practice `Karo:DbCredentials:ConnIndiciDB{practiceSuffix}` secret convention. The
/// composite <see cref="RoutingContext"/> (PracticeId + PracticeCode + Environment) lets the same
/// practiceId route to different rows when a practice runs multiple environments/PHO codes on
/// different servers - same model as <see cref="ILegacyPracticeConnectionResolver"/>, kept as a
/// separate interface per Zohaib's isolation requirement.
/// </summary>
public interface IKaroPracticeConnectionResolver
{
    /// <summary>
    /// Resolves the real connection string for <paramref name="context"/> via the tenant registry.
    /// Throws <see cref="Domain.Exceptions.NotFoundException"/> if no matching practice route is
    /// registered - never falls back to a different target silently.
    /// </summary>
    Task<string> ResolveAsync(RoutingContext context, CancellationToken ct = default);
}
