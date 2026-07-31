using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Models;
using MediatR;

namespace HekCoreApi.Application.Features.Karo.Queries;

public sealed record KaroDemographicsQuery(string? System, string? Pho, string? PatientId, string? EncounterId, string? BearerToken)
    : IRequest<KaroDemographicsQueryResult>;

public sealed record KaroDemographicsQueryResult(bool Succeeded, string? PatientId, KaroDemographicInfo? Demographic, IReadOnlyList<KaroCardInfo>? Cards, string? ErrorMessage, CallRoutingInfo? Routing = null);

/// <summary>
/// Ported from `APIController.cs`'s `GetDemographics` (`:274`) - the shared bearer-token-validated
/// read pipeline (identical structure to every other real KARO Get* operation) applied to
/// `HSSDA.GetHSSDemographics`.
/// </summary>
public sealed class KaroDemographicsQueryHandler : IRequestHandler<KaroDemographicsQuery, KaroDemographicsQueryResult>
{
    private readonly IKaroRequestParser _parser;
    private readonly IKaroTokenValidator _tokenValidator;
    private readonly IKaroRoutingResolver _routingResolver;
    private readonly IKaroDemographicsRepository _repository;
    private readonly ITenantRegistryService _tenantRegistryService;

    public KaroDemographicsQueryHandler(IKaroRequestParser parser, IKaroTokenValidator tokenValidator, IKaroRoutingResolver routingResolver, IKaroDemographicsRepository repository, ITenantRegistryService tenantRegistryService)
    {
        _parser = parser;
        _tokenValidator = tokenValidator;
        _routingResolver = routingResolver;
        _repository = repository;
        _tenantRegistryService = tenantRegistryService;
    }

    public async Task<KaroDemographicsQueryResult> Handle(KaroDemographicsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var (encounterId, practiceSuffix, _) = _parser.ParseEncounterId(request.EncounterId);
            var patientId = _parser.Decrypt(request.PatientId);
            var routingContext = _routingResolver.Resolve(request.EncounterId ?? string.Empty);

            var route = await _tenantRegistryService.ResolveRouteAsync(routingContext, cancellationToken);
            var routing = route is not null ? CallRoutingInfo.FromPracticeRoute(route, routingContext.Environment) : null;

            var validation = await _tokenValidator.ValidateAsync(practiceSuffix, routingContext, patientId, encounterId, request.BearerToken, request.Pho, cancellationToken);
            if (!validation.Valid)
            {
                return new KaroDemographicsQueryResult(false, null, null, null, "Invalid token value!", routing);
            }

            var result = await _repository.GetAsync(practiceSuffix, routingContext, patientId, cancellationToken);
            return new KaroDemographicsQueryResult(true, patientId, result.Demographic, result.Cards, null, routing);
        }
        catch (Exception ex)
        {
            // Legacy: the raw exception message becomes the fault text (same pattern as HISO/KaroAuthenticate).
            return new KaroDemographicsQueryResult(false, null, null, null, ex.Message);
        }
    }
}
