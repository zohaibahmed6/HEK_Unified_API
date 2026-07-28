using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Models;

namespace HekCoreApi.Infrastructure.Legacy.Erms;

/// <summary>
/// Ported from `HSSDA.InsertAndValidateToken(patientId, appointmentId, practiceid, token, out error)`
/// (`DAL/South/HSSDA.cs:986`) - valid iff a row comes back with an empty `StatusMessage`. Reuses
/// <see cref="IErmsAuthRepository"/> (username/password omitted, no pho, `expiryInDays` 0) exactly as
/// the legacy bool overload delegates to the sparse-parameter call.
/// </summary>
public sealed class ErmsTokenValidator : IErmsTokenValidator
{
    private readonly IErmsAuthRepository _authRepository;

    public ErmsTokenValidator(IErmsAuthRepository authRepository)
    {
        _authRepository = authRepository;
    }

    public async Task<KaroTokenValidationResult> ValidateAsync(string practiceSuffix, RoutingContext routingContext, string? patientId, string? encounterId, string? token, CancellationToken ct = default)
    {
        var result = await _authRepository.InsertAndValidateTokenAsync(practiceSuffix, routingContext, username: null, password: null, patientId, encounterId, token: token, ct: ct);

        return result is not null && string.IsNullOrEmpty(result.StatusMessage)
            ? new KaroTokenValidationResult(true, null)
            : new KaroTokenValidationResult(false, null);
    }
}
