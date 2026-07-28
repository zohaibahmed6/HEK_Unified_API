using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Models;

namespace HekCoreApi.Infrastructure.Legacy.Karo;

/// <summary>
/// Ported from `HSSDA.InsertAndValidateToken(patientId, appointmentId, token, practiceid, pho, out error)`
/// bool overload (`HSSDA.cs:843`) - internally calls the exact same `[HSS].[uspInsertAndValidateToken]`
/// proc as the username/password overload (<see cref="IKaroAuthRepository"/>), just with
/// username/password omitted and a token supplied instead. Reuses <see cref="IKaroAuthRepository"/>
/// rather than duplicating the sparse-parameter SQL call logic.
/// </summary>
public sealed class KaroTokenValidator : IKaroTokenValidator
{
    private readonly IKaroAuthRepository _authRepository;

    public KaroTokenValidator(IKaroAuthRepository authRepository)
    {
        _authRepository = authRepository;
    }

    public async Task<KaroTokenValidationResult> ValidateAsync(string practiceSuffix, RoutingContext routingContext, string? patientId, string? encounterId, string? token, string? pho, CancellationToken ct = default)
    {
        var result = await _authRepository.InsertAndValidateTokenAsync(practiceSuffix, routingContext, username: null, password: null, patientId, encounterId, token, pho, ct);

        return result is not null && string.IsNullOrEmpty(result.StatusMessage)
            ? new KaroTokenValidationResult(true, null)
            : new KaroTokenValidationResult(false, null);
    }
}
