using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Models;
using MediatR;

namespace HekCoreApi.Application.Features.Erms.Commands;

public sealed record ErmsSaveDocumentCommand(
    string? Id,
    string? DocumentId,
    string? PatientId,
    string? EncounterId,
    string? ProviderId,
    string? Type,
    string? ItemType,
    string? CreatedDate,
    string? ContentType,
    string? Content,
    string? BearerToken) : IRequest<ErmsSaveDocumentResult>;

/// <summary>Empty `Error` = success (legacy signals failure purely via the non-blank error string that drives the 400 response).</summary>
public sealed record ErmsSaveDocumentResult(string Error, CallRoutingInfo? Routing = null);

/// <summary>
/// Ported from ERMS's `SaveDocument` (`APIController.cs:1535`): base64+decrypt on PatiendID/EncounterID/
/// ProviderID, `practiceid2 = splitEncounter[1]`, token check, then `UpdateExistingDocument` (errors
/// swallowed) -&gt; `SaveToDMS` (category 17 "in" / 18 otherwise) -&gt; `InsertDocument` (dataSourceId "23").
/// Legacy quirks kept: "Invalid passed values!" when content/contentType/type missing, invalid-token
/// fallback message, every exception message becomes the error (-&gt; HTTP 400 "BadRequest").
/// </summary>
public sealed class ErmsSaveDocumentCommandHandler : IRequestHandler<ErmsSaveDocumentCommand, ErmsSaveDocumentResult>
{
    private readonly IErmsRequestParser _parser;
    private readonly IErmsTokenValidator _tokenValidator;
    private readonly IErmsRoutingResolver _routingResolver;
    private readonly IErmsWriteRepository _writeRepository;
    private readonly ITenantRegistryService _tenantRegistryService;

    public ErmsSaveDocumentCommandHandler(IErmsRequestParser parser, IErmsTokenValidator tokenValidator, IErmsRoutingResolver routingResolver, IErmsWriteRepository writeRepository, ITenantRegistryService tenantRegistryService)
    {
        _parser = parser;
        _tokenValidator = tokenValidator;
        _routingResolver = routingResolver;
        _writeRepository = writeRepository;
        _tenantRegistryService = tenantRegistryService;
    }

    public async Task<ErmsSaveDocumentResult> Handle(ErmsSaveDocumentCommand request, CancellationToken ct)
    {
        var error = string.Empty;
        CallRoutingInfo? routing = null;
        try
        {
            // Legacy: GetBase64Value on PatiendID/EncounterID/ProviderID, then the shared split
            // (practiceid2 = raw splitEncounter[1]), then GetDcrptValue on PatiendID/ProviderID.
            var (encounterId, practiceSuffix, _, practiceSuffixNumeric) = _parser.ParseEncounterId(request.EncounterId);
            var patientId = _parser.Decrypt(_parser.DecodeBase64(request.PatientId));
            var providerId = _parser.Decrypt(_parser.DecodeBase64(request.ProviderId));
            var routingContext = _routingResolver.Resolve(request.EncounterId ?? string.Empty);

            var route = await _tenantRegistryService.ResolveRouteAsync(routingContext, ct);
            routing = route is not null ? CallRoutingInfo.FromPracticeRoute(route, routingContext.Environment) : null;

            var validation = await _tokenValidator.ValidateAsync(practiceSuffix, routingContext, patientId, encounterId, request.BearerToken, ct);
            if (!validation.Valid)
            {
                return new ErmsSaveDocumentResult(validation.ErrorMessage ?? "Invalid token value!", routing);
            }

            var dmsGuidKey = string.Empty;
            var referralId = string.Empty;
            var base64Value = Convert.FromBase64String(request.Content ?? string.Empty);

            if (base64Value.Length > 0
                && !string.IsNullOrWhiteSpace(request.ContentType)
                && !string.IsNullOrWhiteSpace(request.Type))
            {
                referralId = request.Id + "_" + request.DocumentId;

                // Legacy quirk: ItemType.ToLower() NREs on a missing ItemType - the message lands in the error path.
                await _writeRepository.UpdateExistingDocumentAsync(practiceSuffix, routingContext, referralId, practiceSuffixNumeric, ct);
                dmsGuidKey = await _writeRepository.SaveToDmsAsync(routingContext, practiceSuffixNumeric, base64Value, request.ContentType, request.Type,
                    request.ItemType!.ToLower().Equals("in") ? 17 : 18, referralId, ct);
            }
            else
            {
                error = "Invalid passed values!";
            }

            if (!string.IsNullOrWhiteSpace(dmsGuidKey))
            {
                DateTime.TryParse(request.CreatedDate, out var resultDate);
                await _writeRepository.InsertDocumentAsync(practiceSuffix, routingContext, patientId, encounterId, request.Type, dmsGuidKey,
                    request.ItemType!.ToLower().Equals("in") ? 1 : 2, resultDate, referralId, providerId, ct);
            }
        }
        catch (Exception ex)
        {
            error = ex.Message;
        }

        return new ErmsSaveDocumentResult(error, routing);
    }
}
