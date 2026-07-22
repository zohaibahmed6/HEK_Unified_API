namespace HekCoreApi.Application.Common.Interfaces;

/// <summary>
/// HISO-only per-practice connection resolver, backed by HekTenantRegistry.HisoSessions - never
/// Practices. Every HISO repository (concept dictionary/executor, ACC45, tasks) routes through this
/// instead of <see cref="ILegacyPracticeConnectionResolver"/>, which stays reserved for ERMS/KARO/
/// generic routing (2026-07-22 correction - HISO must never resolve via Practices, anywhere).
/// </summary>
public interface IHisoPracticeConnectionResolver
{
    /// <summary>Throws <see cref="Domain.Exceptions.NotFoundException"/> if no active HisoSessions row exists for <paramref name="practiceId"/>.</summary>
    Task<string> ResolveAsync(string practiceId, CancellationToken ct = default);
}
