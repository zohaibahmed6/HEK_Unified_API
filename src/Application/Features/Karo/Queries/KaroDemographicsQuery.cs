using HekCoreApi.Application.Common.Interfaces;
using MediatR;

namespace HekCoreApi.Application.Features.Karo.Queries;

public sealed record KaroDemographicsQuery(string? System, string? Pho, string? PatientId, string? EncounterId, string? BearerToken)
    : IRequest<KaroDemographicsQueryResult>;

public sealed record KaroDemographicsQueryResult(bool Succeeded, string? PatientId, KaroDemographicInfo? Demographic, IReadOnlyList<KaroCardInfo>? Cards, string? ErrorMessage);

/// <summary>
/// Ported from `APIController.cs`'s `GetDemographics` (`:274`) - the shared bearer-token-validated
/// read pipeline (identical structure to every other real KARO Get* operation) applied to
/// `HSSDA.GetHSSDemographics`.
/// </summary>
public sealed class KaroDemographicsQueryHandler : IRequestHandler<KaroDemographicsQuery, KaroDemographicsQueryResult>
{
    private readonly IKaroRequestParser _parser;
    private readonly IKaroTokenValidator _tokenValidator;
    private readonly IKaroDemographicsRepository _repository;

    public KaroDemographicsQueryHandler(IKaroRequestParser parser, IKaroTokenValidator tokenValidator, IKaroDemographicsRepository repository)
    {
        _parser = parser;
        _tokenValidator = tokenValidator;
        _repository = repository;
    }

    public async Task<KaroDemographicsQueryResult> Handle(KaroDemographicsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var (encounterId, practiceSuffix, _) = _parser.ParseEncounterId(request.EncounterId);
            var patientId = _parser.Decrypt(request.PatientId);

            var validation = await _tokenValidator.ValidateAsync(practiceSuffix, patientId, encounterId, request.BearerToken, request.Pho, cancellationToken);
            if (!validation.Valid)
            {
                return new KaroDemographicsQueryResult(false, null, null, null, "Invalid token value!");
            }

            var result = await _repository.GetAsync(practiceSuffix, patientId, cancellationToken);
            return new KaroDemographicsQueryResult(true, patientId, result.Demographic, result.Cards, null);
        }
        catch (Exception ex)
        {
            // Legacy: the raw exception message becomes the fault text (same pattern as HISO/KaroAuthenticate).
            return new KaroDemographicsQueryResult(false, null, null, null, ex.Message);
        }
    }
}
