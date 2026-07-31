using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Models;
using MediatR;

namespace HekCoreApi.Application.Features.Erms.Queries;

public sealed record ErmsAuthenticateQuery(string? Username, string? Password, string? PatientId, string? EncounterId) : IRequest<ErmsAuthenticateResult>;

public sealed record ErmsAuthenticateResult(bool Succeeded, string? Token, DateTime? Expiry, string? PracticeId, string? ErrorMessage, CallRoutingInfo? Routing = null);

/// <summary>Ported from ERMS's real `Authenticate` (`APIController.cs:35`). Real `expiryInDays` sourced via `ISecretProvider` (`Erms:ExpiryInDays`, Web.config real value `"0.5"`), not hardcoded.</summary>
public sealed class ErmsAuthenticateQueryHandler : IRequestHandler<ErmsAuthenticateQuery, ErmsAuthenticateResult>
{
    private readonly IErmsRequestParser _parser;
    private readonly IErmsAuthRepository _authRepository;
    private readonly ISecretProvider _secretProvider;
    private readonly IErmsRoutingResolver _routingResolver;
    private readonly ITenantRegistryService _tenantRegistryService;

    public ErmsAuthenticateQueryHandler(IErmsRequestParser parser, IErmsAuthRepository authRepository, ISecretProvider secretProvider, IErmsRoutingResolver routingResolver, ITenantRegistryService tenantRegistryService)
    {
        _parser = parser;
        _authRepository = authRepository;
        _secretProvider = secretProvider;
        _routingResolver = routingResolver;
        _tenantRegistryService = tenantRegistryService;
    }

    public async Task<ErmsAuthenticateResult> Handle(ErmsAuthenticateQuery request, CancellationToken ct)
    {
        try
        {
            var (encounterId, practiceSuffix, pho, _) = _parser.ParseEncounterId(request.EncounterId);
            var patientId = _parser.Decrypt(_parser.DecodeBase64(request.PatientId));
            var routingContext = _routingResolver.Resolve(request.EncounterId ?? string.Empty);

            var route = await _tenantRegistryService.ResolveRouteAsync(routingContext, ct);
            var routing = route is not null ? CallRoutingInfo.FromPracticeRoute(route, routingContext.Environment) : null;

            var expiryRaw = await _secretProvider.GetSecretAsync("Erms:ExpiryInDays", ct);
            var expiryInDays = double.TryParse(expiryRaw, out var parsed) ? parsed : 0;

            var dbResult = await _authRepository.InsertAndValidateTokenAsync(practiceSuffix, routingContext, request.Username, request.Password, patientId, encounterId, token: null, expiryInDays, pho, ct);

            if (dbResult is not null && string.IsNullOrWhiteSpace(dbResult.StatusMessage))
            {
                if (dbResult.Expiry is { } expiry && expiry != DateTime.MinValue)
                {
                    return new ErmsAuthenticateResult(true, dbResult.Token, expiry, dbResult.PracticeId, null, routing);
                }

                return new ErmsAuthenticateResult(false, null, null, null, "Invalid credentials!", routing);
            }

            return new ErmsAuthenticateResult(false, null, null, null, "Authentication failed!", routing);
        }
        catch (Exception ex)
        {
            return new ErmsAuthenticateResult(false, null, null, null, ex.Message);
        }
    }
}
