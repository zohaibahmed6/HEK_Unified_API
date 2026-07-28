using HekCoreApi.Application.Common.Models;

namespace HekCoreApi.Application.Common.Interfaces;

/// <summary>
/// Ported from legacy-reference/hsswebapi/DevLocal/DAL/South/HSSDA.cs's `InsertAndValidateToken`
/// (the 8-parameter real overload) - calls the real `[HSS].[uspInsertAndValidateToken]` procedure.
/// </summary>
public interface IKaroAuthRepository
{
    /// <summary>
    /// Legacy: only non-empty string params are actually added to the SQL call (a real sparse-parameter
    /// pattern - <paramref name="username"/>/<paramref name="password"/>/<paramref name="patientId"/>/
    /// <paramref name="appointmentId"/>/<paramref name="token"/>/<paramref name="pho"/> are each omitted
    /// entirely from the procedure call when null/empty, not passed as empty string or DBNull).
    /// </summary>
    Task<KaroAuthResult?> InsertAndValidateTokenAsync(
        string practiceSuffix,
        RoutingContext routingContext,
        string? username,
        string? password,
        string? patientId,
        string? appointmentId,
        string? token,
        string? pho,
        CancellationToken ct = default);
}

/// <summary>Legacy: the real result columns from `[HSS].[uspInsertAndValidateToken]` (`StatusMessage`/`Expiry`/`Token`/`PracticeId`).</summary>
public sealed record KaroAuthResult(string? StatusMessage, DateTime? Expiry, string? Token, string? PracticeId);
