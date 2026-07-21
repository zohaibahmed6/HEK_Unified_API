namespace HekCoreApi.Application.Common.Interfaces;

/// <summary>
/// Ported from `HSSDA.InsertAndValidateToken(patientId, appointmentId, token, practiceid, pho, out error)`
/// (`HSSDA.cs:843`) - the bool-returning bearer-token validation overload used by every real operation
/// other than `Authenticate` itself (which uses the username/password overload, <see cref="IKaroAuthRepository"/>).
/// </summary>
public interface IKaroTokenValidator
{
    Task<KaroTokenValidationResult> ValidateAsync(string practiceSuffix, string? patientId, string? encounterId, string? token, string? pho, CancellationToken ct = default);
}

public sealed record KaroTokenValidationResult(bool Valid, string? ErrorMessage);
