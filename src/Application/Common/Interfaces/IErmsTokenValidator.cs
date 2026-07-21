namespace HekCoreApi.Application.Common.Interfaces;

/// <summary>
/// Ported from ERMS's `HSSDA.InsertAndValidateToken(patientId, appointmentId, practiceid, token, out error)`
/// bool overload (`DAL/South/HSSDA.cs:986`) - the bearer-token validation used by every real ERMS
/// Get* operation (no pho, no expiry). Same `[HSS].[uspInsertAndValidateToken]` proc as
/// <see cref="IErmsAuthRepository"/>, just token-only. ERMS-owned twin of <see cref="IKaroTokenValidator"/> -
/// kept separate so the modules stay fully isolated.
/// </summary>
public interface IErmsTokenValidator
{
    Task<KaroTokenValidationResult> ValidateAsync(string practiceSuffix, string? patientId, string? encounterId, string? token, CancellationToken ct = default);
}
