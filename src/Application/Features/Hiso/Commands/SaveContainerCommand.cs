using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Features.Auth.Hiso;
using MediatR;

namespace HekCoreApi.Application.Features.Hiso.Commands;

public sealed record SaveContainerFormMetaDataInput(
    string? FormInstanceId, string? FormInstanceVersion, string? FormEngineId, string? FormInstanceOperationMode,
    string? FormDefinitionId, string? FormDefinitionVersion, string? FormDefinitionTitle);

public sealed record SaveContainerCommand(
    Guid SessionKey, string CalledServerAddress, SaveContainerFormMetaDataInput FormMetaData,
    string? ResumePath, string? View, string? ViewType, string? ViewSignature, bool Completed, string? SubmittedDataXml)
    : IRequest<SaveContainerCommandResult>;

public sealed record SaveContainerCommandResult(bool SessionResolved, bool Response);

/// <summary>
/// Ported from `FormSessionService.svc.cs`'s `saveContainer`/`saveDataContainer`. DMS save (only when
/// `completed==true`) and the ACC45 *definition* save are real. The ACC45 Detail/Diagnosis/Referral
/// TVP pipeline (`Acc45Builder`/`Acc45DiagnosisBuilder`/`Acc45ReferralBuilder` -> `SaveAccidentInformation`)
/// is not yet ported (`PROJECT_STATUS.md` HISO wire-compat rebuild) - throws rather than silently
/// skipping it, since legacy always runs this step regardless of `completed`.
/// </summary>
public sealed class SaveContainerCommandHandler : IRequestHandler<SaveContainerCommand, SaveContainerCommandResult>
{
    private readonly IMediator _mediator;
    private readonly IHisoDocumentHandler _documentHandler;
    private readonly IAcc45DefinitionRepository _definitionRepository;
    private readonly IAcc45DetailRepository _detailRepository;

    public SaveContainerCommandHandler(
        IMediator mediator,
        IHisoDocumentHandler documentHandler,
        IAcc45DefinitionRepository definitionRepository,
        IAcc45DetailRepository detailRepository)
    {
        _mediator = mediator;
        _documentHandler = documentHandler;
        _definitionRepository = definitionRepository;
        _detailRepository = detailRepository;
    }

    public async Task<SaveContainerCommandResult> Handle(SaveContainerCommand request, CancellationToken cancellationToken)
    {
        var lookup = await _mediator.Send(new ResolveHisoSessionQuery(request.SessionKey, request.CalledServerAddress), cancellationToken);
        if (lookup.Status != HisoSessionLookupStatus.Success || lookup.Context is null)
        {
            return new SaveContainerCommandResult(false, false);
        }

        var session = Application.Common.Models.HealthLinkSession.FromSessionContext(lookup.Context);

        var dmsGuid = Guid.Empty;
        if (request.Completed)
        {
            dmsGuid = await _documentHandler.AddDocumentAsync(request.View, request.ViewType ?? "text/html", request.FormMetaData.FormEngineId, session.PracticeId, cancellationToken);
        }

        var definitionInput = new Acc45DefinitionInput(
            request.FormMetaData.FormInstanceId, request.FormMetaData.FormInstanceVersion, request.FormMetaData.FormEngineId,
            request.FormMetaData.FormInstanceOperationMode, request.FormMetaData.FormDefinitionId, request.FormMetaData.FormDefinitionVersion,
            request.FormMetaData.FormDefinitionTitle, request.ViewType, request.ViewSignature, request.ResumePath,
            dmsGuid.ToString(), request.SubmittedDataXml ?? string.Empty, null);

        await _definitionRepository.SaveDefinitionAsync(definitionInput, session, cancellationToken);

        // Legacy always also runs the ACC45 Detail/Diagnosis/Referral save here, regardless of `completed`.
        await _detailRepository.SaveAccidentInformationAsync(request.SubmittedDataXml ?? string.Empty, session, request.FormMetaData.FormEngineId ?? string.Empty, cancellationToken);

        // Legacy: response.response is hardcoded true on the success path regardless of the actual
        // save outcome (a real legacy bug, reproduced on purpose per Zohaib's "match legacy exactly"
        // instruction, not silently "fixed").
        return new SaveContainerCommandResult(true, true);
    }
}
