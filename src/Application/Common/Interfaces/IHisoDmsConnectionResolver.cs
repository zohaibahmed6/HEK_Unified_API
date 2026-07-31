namespace HekCoreApi.Application.Common.Interfaces;

/// <summary>
/// HISO DMS connection routing - per-practice, backed by HekTenantRegistry.HisoSessions (same source
/// <see cref="IHisoPracticeConnectionResolver"/> uses). Confirmed live (2026-07-30) that the real DMS
/// database (`DMS_PMS`) sits on the SAME server, with the SAME credentials, as the practice's primary
/// connection - only the database name differs - same pattern already established for ERMS/KARO
/// (<see cref="IErmsDmsConnectionResolver"/>/<see cref="IKaroDmsConnectionResolver"/>). Replaces the
/// earlier single global `Hiso:DmsConnectionString` secret, which routed every practice to the same
/// DMS database regardless of which practice's session was actually being served.
/// </summary>
public interface IHisoDmsConnectionResolver
{
    /// <summary>Throws <see cref="Domain.Exceptions.NotFoundException"/> if no active HisoSessions row exists for <paramref name="practiceId"/>.</summary>
    Task<string> ResolveAsync(string practiceId, CancellationToken ct = default);
}
