using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Models;
using HekCoreApi.Application.Common.Options;
using HekCoreApi.Application.Features.Auth.Hiso;
using MediatR;
using Microsoft.Extensions.Options;

namespace HekCoreApi.Application.Features.Hiso.Commands;

public sealed record ProcessActionCommand(Guid SessionKey, string CalledServerAddress, string ActionId, Dictionary<string, object?>? ActionContainer, string? ActionContainerXml = null)
    : IRequest<ProcessActionCommandResult>;

public sealed record ProcessActionCommandResult(bool SessionResolved, bool Processed, CallRoutingInfo? Routing = null);

/// <summary>
/// Ported from `FormSessionService.svc.cs`'s `processAction`/`saveProcessAction`/`Task.processTask`.
/// `"save"` short-circuits to `processed=true` with no persistence when `IsDynamic` is off, exactly
/// like legacy; when `IsDynamic` is on, the real 5-builder patient/consult/employer/problem/practitioner
/// save pipeline (`HisoProcessActionSaveRepository`) runs for real. `"addTask"` is fully real.
/// `"addInvoice"`/`"launchForm"` are legacy's own genuine empty no-ops, reproduced exactly per
/// Zohaib's "legacy fidelity wins" decision - `processed` stays `false`.
/// </summary>
public sealed class ProcessActionCommandHandler : IRequestHandler<ProcessActionCommand, ProcessActionCommandResult>
{
    private readonly IMediator _mediator;
    private readonly IHisoTaskRepository _taskRepository;
    private readonly IHisoProcessActionSaveRepository _saveRepository;
    private readonly HisoGetDataOptions _options;
    private readonly IHisoSessionRegistryRepository _sessionRegistry;

    public ProcessActionCommandHandler(IMediator mediator, IHisoTaskRepository taskRepository, IHisoProcessActionSaveRepository saveRepository, IOptions<HisoGetDataOptions> options, IHisoSessionRegistryRepository sessionRegistry)
    {
        _mediator = mediator;
        _taskRepository = taskRepository;
        _saveRepository = saveRepository;
        _options = options.Value;
        _sessionRegistry = sessionRegistry;
    }

    public async Task<ProcessActionCommandResult> Handle(ProcessActionCommand request, CancellationToken cancellationToken)
    {
        var lookup = await _mediator.Send(new ResolveHisoSessionQuery(request.SessionKey, request.CalledServerAddress), cancellationToken);
        if (lookup.Status != HisoSessionLookupStatus.Success || lookup.Context is null)
        {
            return new ProcessActionCommandResult(false, false);
        }

        var sessionRoute = await _sessionRegistry.FindAsync(request.SessionKey, cancellationToken);
        var routing = sessionRoute is not null ? CallRoutingInfo.FromHisoSessionRoute(sessionRoute) : null;

        switch (request.ActionId)
        {
            case "save":
                if (!_options.IsDynamic)
                {
                    return new ProcessActionCommandResult(true, true, routing);
                }

                var session = Application.Common.Models.HealthLinkSession.FromSessionContext(lookup.Context, request.SessionKey);
                var xml = request.ActionContainerXml ?? "<container/>";

                // Legacy: PatientBuilder/PatientConsultBuilder/PatientProblemBuilder/RegisteredPractitionerBuilder
                // throw on failure and are NOT individually caught in saveProcessAction - a failure in
                // any of them propagates. PatientEmployerOrganisationBuilder swallows its own exceptions
                // (see repository doc), so it never aborts the sequence.
                await _saveRepository.SavePatientAsync(xml, session, cancellationToken);
                await _saveRepository.SavePatientConsultAsync(xml, session, cancellationToken);
                await _saveRepository.SavePatientEmployerOrganisationAsync(xml, session, cancellationToken);
                await _saveRepository.SavePatientProblemAsync(xml, session, cancellationToken);
                await _saveRepository.SaveRegisteredPractitionerAsync(xml, session, cancellationToken);

                // Legacy: individual builder save results are discarded - saveProcessAction always
                // returns true once all five have run (reproduced faithfully, not "fixed").
                return new ProcessActionCommandResult(true, true, routing);

            case "addTask":
                var container = request.ActionContainer ?? [];
                var code = container.GetValueOrDefault("code")?.ToString() ?? string.Empty;
                var description = container.GetValueOrDefault("taskDescription")?.ToString();
                var dueDateRaw = container.GetValueOrDefault("dueDate")?.ToString();
                var complete = bool.TryParse(container.GetValueOrDefault("complete")?.ToString(), out var isComplete) && isComplete;
                var dueDate = DateOnly.TryParse(dueDateRaw, out var parsedDate) ? parsedDate : DateOnly.FromDateTime(DateTime.UtcNow);

                var input = new HisoTaskInput(code, description, dueDate, complete, lookup.Context.ProviderId, lookup.Context.PatientId, null, lookup.Context.ProviderId);
                var processed = await _taskRepository.AddTaskAsync(input, lookup.Context.PracticeId, cancellationToken);
                return new ProcessActionCommandResult(true, processed, routing);

            case "addInvoice":
            case "launchForm":
                // Legacy: comment-only stubs, no code at all - reproduced exactly.
                return new ProcessActionCommandResult(true, false, routing);

            default:
                return new ProcessActionCommandResult(true, false, routing);
        }
    }
}
