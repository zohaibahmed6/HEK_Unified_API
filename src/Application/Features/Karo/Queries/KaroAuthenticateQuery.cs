using HekCoreApi.Application.Common.Interfaces;
using MediatR;

namespace HekCoreApi.Application.Features.Karo.Queries;

public sealed record KaroAuthenticateQuery(
    string? Username, string? Password, string? PatientId, string? EncounterId, string? System, string? Pho)
    : IRequest<KaroAuthenticateResult>;

public sealed record KaroAuthenticateResult(bool Succeeded, string? Token, DateTime? Expiry, string? PracticeId, string? ErrorMessage);

/// <summary>
/// Ported from legacy-reference/hsswebapi/DevLocal/HSSWebAPI/Controllers/APIController.cs's shared
/// `Authenticate` logic (both the GET query-string and POST JSON-body overloads converge on this).
/// Reproduces the real `encounterId` practice-suffix parsing (including its "4th segment overwrites
/// practiceid entirely" quirk), the real `GetDcrptValue` numeric-passthrough-else-decrypt rule, and
/// the two distinct real failure messages ("Invalid credentials!" vs "Authentication failed!") -
/// faithfully, not "fixed".
/// </summary>
public sealed class KaroAuthenticateQueryHandler : IRequestHandler<KaroAuthenticateQuery, KaroAuthenticateResult>
{
    private readonly IKaroRequestParser _parser;
    private readonly IKaroAuthRepository _authRepository;

    public KaroAuthenticateQueryHandler(IKaroRequestParser parser, IKaroAuthRepository authRepository)
    {
        _parser = parser;
        _authRepository = authRepository;
    }

    public async Task<KaroAuthenticateResult> Handle(KaroAuthenticateQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var (encounterId, practiceSuffix, _) = _parser.ParseEncounterId(request.EncounterId);
            var patientId = _parser.Decrypt(request.PatientId);

            var dbResult = await _authRepository.InsertAndValidateTokenAsync(
                practiceSuffix, request.Username, request.Password, patientId, encounterId, token: null, request.Pho, cancellationToken);

            if (dbResult is not null && string.IsNullOrWhiteSpace(dbResult.StatusMessage))
            {
                if (dbResult.Expiry is { } expiry && expiry != DateTime.MinValue)
                {
                    return new KaroAuthenticateResult(true, dbResult.Token, expiry, dbResult.PracticeId, null);
                }

                return new KaroAuthenticateResult(false, null, null, null, "Invalid credentials!");
            }

            return new KaroAuthenticateResult(false, null, null, null, "Authentication failed!");
        }
        catch (Exception ex)
        {
            // Legacy: the raw exception message becomes the fault text (see HISO's identical, already-flagged pattern).
            return new KaroAuthenticateResult(false, null, null, null, ex.Message);
        }
    }
}
