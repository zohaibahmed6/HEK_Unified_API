using System.ServiceModel;
using System.Xml;
using HekCoreApi.Adapters.Hiso.GetVersion;
using HekCoreApi.Api.Telemetry;
using HekCoreApi.Application.Features.Hiso.Commands;
using HekCoreApi.Application.Features.Hiso.Queries;
using MediatR;

namespace HekCoreApi.Api.Features.Hiso.Soap;

/// <summary>
/// Real SOAP implementation, one door at the real address - see <see cref="IFormSessionService"/>.
/// v1.1 spec follow-through Step 16 (2026-07-24): wired to <see cref="LegacyOperationObserver"/>, the
/// same telemetry every other legacy compat controller (Hiso JSON/Karo/Erms/Col) already used - this
/// SOAP facade was a real gap, built in Step 4 without it. Tagged `hiso-soap` (not `hiso`) so the two
/// transports are distinguishable in the metric, since they're genuinely different entry points onto
/// the same business logic.
/// </summary>
public sealed class FormSessionService : IFormSessionService
{
    private const string System = "hiso-soap";

    private readonly IMediator _mediator;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly LegacyOperationObserver _observer;
    private readonly ILogger<FormSessionService> _logger;

    public FormSessionService(IMediator mediator, IHttpContextAccessor httpContextAccessor, LegacyOperationObserver observer, ILogger<FormSessionService> logger)
    {
        _mediator = mediator;
        _httpContextAccessor = httpContextAccessor;
        _observer = observer;
        _logger = logger;
    }

    public GetVersionResponseSoap getVersion(GetVersionRequestSoap getVersionRequest)
    {
        const string endpoint = "getVersion";
        var context = new Dictionary<string, object?> { ["SessionKey"] = getVersionRequest.sessionKey };

        try
        {
            var calledServerAddress = _httpContextAccessor.HttpContext?.Request.Host.Value ?? string.Empty;
            var sessionResolved = _mediator.Send(new GetVersionQuery(getVersionRequest.sessionKey, calledServerAddress)).GetAwaiter().GetResult();

            if (!sessionResolved)
            {
                _observer.RecordExpectedFailure(_logger, System, endpoint, "InvalidSessionKey", context);
                // Legacy: FaultException("Invalid Session Key") - reproduced as the literal message
                // text, matching HisoCompatController's JSON equivalent and the real legacy fault
                // string exactly.
                throw new FaultException("Invalid Session Key");
            }

            var real = GetVersionResponse.Real();
            var response = new GetVersionResponseSoap
            {
                GetVersionResponseReturn = new GetVersionResponseReturnSoap
                {
                    application = real.GetVersionResponseReturn.Application,
                    applicationVersion = real.GetVersionResponseReturn.ApplicationVersion,
                    hisoversion = real.GetVersionResponseReturn.Hisoversion,
                }
            };
            _logger.LogInformation("{System} {Endpoint} succeeded. Context: {@Context}", System, endpoint, context);
            return response;
        }
        catch (FaultException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _observer.RecordUnexpectedFailure(_logger, System, endpoint, ex, context);
            throw;
        }
    }

    public GetDataResponseSoap getData(GetDataRequestSoap getDataRequest)
    {
        const string endpoint = "getData";
        var context = new Dictionary<string, object?> { ["SessionKey"] = getDataRequest.sessionKey };

        try
        {
            var calledServerAddress = _httpContextAccessor.HttpContext?.Request.Host.Value ?? string.Empty;
            var submittedDataXml = ExtractPayloadXml(getDataRequest.dataContainer.submittedData);
            var operationMode = getDataRequest.dataContainer.formMetaData.formInstanceOperationMode;

            var result = _mediator.Send(new GetDataQuery(
                getDataRequest.sessionKey, calledServerAddress, operationMode, submittedDataXml)).GetAwaiter().GetResult();

            if (!result.SessionResolved)
            {
                _observer.RecordExpectedFailure(_logger, System, endpoint, "InvalidSessionKey", context);
                // Legacy: FaultException("Invalid Session Key") - same real fault string as
                // getVersion/getData's JSON compat equivalent (HisoCompatController.GetData).
                throw new FaultException("Invalid Session Key");
            }

            // Legacy: real dynamic-mode/new-form gate didn't pass - dataContainer stays null (static
            // mode, parked/resume mode are genuine empty stubs in legacy too, not gaps this project
            // introduced).
            if (result.FilledSubmittedDataXml is null)
            {
                _logger.LogInformation("{System} {Endpoint} succeeded. Context: {@Context}", System, endpoint, context);
                return new GetDataResponseSoap { GetDataResponseReturn = new GetDataResponseReturnSoap { dataContainer = null } };
            }

            var filledDoc = new XmlDocument();
            filledDoc.LoadXml(result.FilledSubmittedDataXml);

            var response = new GetDataResponseSoap
            {
                GetDataResponseReturn = new GetDataResponseReturnSoap
                {
                    dataContainer = new FormDataSoap
                    {
                        // Legacy: formMetaData is echoed back from the request unchanged.
                        formMetaData = getDataRequest.dataContainer.formMetaData,
                        submittedData = filledDoc.DocumentElement,
                    }
                }
            };
            _logger.LogInformation("{System} {Endpoint} succeeded. Context: {@Context}", System, endpoint, context);
            return response;
        }
        catch (FaultException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _observer.RecordUnexpectedFailure(_logger, System, endpoint, ex, context);
            throw;
        }
    }

    public SaveContainerResponseSoap saveContainer(SaveContainerRequestSoap saveContainerRequest)
    {
        const string endpoint = "saveContainer";
        var context = new Dictionary<string, object?> { ["SessionKey"] = saveContainerRequest.sessionKey };

        try
        {
            var calledServerAddress = _httpContextAccessor.HttpContext?.Request.Host.Value ?? string.Empty;
            var metaData = new SaveContainerFormMetaDataInput(
                saveContainerRequest.formMetaData.formInstanceId, saveContainerRequest.formMetaData.formInstanceVersion,
                saveContainerRequest.formMetaData.formEngineId, saveContainerRequest.formMetaData.formInstanceOperationMode,
                saveContainerRequest.formMetaData.formDefinitionId, saveContainerRequest.formMetaData.formDefinitionVersion,
                saveContainerRequest.formMetaData.formDefinitionTitle);

            var result = _mediator.Send(new SaveContainerCommand(
                saveContainerRequest.sessionKey, calledServerAddress, metaData, saveContainerRequest.resumePath,
                saveContainerRequest.view, saveContainerRequest.viewType, saveContainerRequest.viewSignature,
                saveContainerRequest.completed, ExtractPayloadXml(saveContainerRequest.dataContainer))).GetAwaiter().GetResult();

            if (!result.SessionResolved)
            {
                _observer.RecordExpectedFailure(_logger, System, endpoint, "InvalidSessionKey", context);
                // Legacy: FaultException("Invalid Session Key") - same real fault string as every other operation.
                throw new FaultException("Invalid Session Key");
            }

            var response = new SaveContainerResponseSoap
            {
                SaveContainerResponseReturn = new SaveContainerResponseReturnSoap { response = result.Response }
            };
            _logger.LogInformation("{System} {Endpoint} succeeded. Context: {@Context}", System, endpoint, context);
            return response;
        }
        catch (FaultException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _observer.RecordUnexpectedFailure(_logger, System, endpoint, ex, context);
            throw;
        }
    }

    public GetDeliveryOptionsResponseSoap getDeliveryOptions(GetDeliveryOptionsRequestSoap getDeliveryOptionsRequest)
    {
        const string endpoint = "getDeliveryOptions";
        var context = new Dictionary<string, object?> { ["SessionKey"] = getDeliveryOptionsRequest.sessionKey };

        try
        {
            var calledServerAddress = _httpContextAccessor.HttpContext?.Request.Host.Value ?? string.Empty;
            var result = _mediator.Send(new GetDeliveryOptionsQuery(getDeliveryOptionsRequest.sessionKey, calledServerAddress)).GetAwaiter().GetResult();

            if (!result.SessionResolved)
            {
                _observer.RecordExpectedFailure(_logger, System, endpoint, "InvalidSessionKey", context);
                throw new FaultException("Invalid Session Key");
            }

            var response = new GetDeliveryOptionsResponseSoap
            {
                GetDeliveryOptionsResponseReturn = new GetDeliveryOptionsResponseReturnSoap
                {
                    senderAccount = result.SenderAccount,
                    senderPassword = result.SenderPassword,
                    URL = result.Url,
                }
            };
            _logger.LogInformation("{System} {Endpoint} succeeded. Context: {@Context}", System, endpoint, context);
            return response;
        }
        catch (FaultException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _observer.RecordUnexpectedFailure(_logger, System, endpoint, ex, context);
            throw;
        }
    }

    public ProcessActionResponseSoap processAction(ProcessActionRequestSoap processActionRequest)
    {
        const string endpoint = "processAction";
        var context = new Dictionary<string, object?> { ["SessionKey"] = processActionRequest.sessionKey, ["ActionId"] = processActionRequest.actionId };

        try
        {
            var calledServerAddress = _httpContextAccessor.HttpContext?.Request.Host.Value ?? string.Empty;
            var actionContainerXml = ExtractPayloadXml(processActionRequest.actionContainer);

            var result = _mediator.Send(new ProcessActionCommand(
                processActionRequest.sessionKey, calledServerAddress, processActionRequest.actionId,
                ActionContainer: null, ActionContainerXml: actionContainerXml)).GetAwaiter().GetResult();

            if (!result.SessionResolved)
            {
                _observer.RecordExpectedFailure(_logger, System, endpoint, "InvalidSessionKey", context);
                throw new FaultException("Invalid Session Key");
            }

            var response = new ProcessActionResponseSoap
            {
                ProcessActionResponseReturn = new ProcessActionResponseReturnSoap { processed = result.Processed }
            };
            _logger.LogInformation("{System} {Endpoint} succeeded. Context: {@Context}", System, endpoint, context);
            return response;
        }
        catch (FaultException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _observer.RecordUnexpectedFailure(_logger, System, endpoint, ex, context);
            throw;
        }
    }

    /// <summary>
    /// [XmlAnyElement]-bound members observed empirically (via this session's live SOAP test) to wrap
    /// content in an element literally named after the C# property ("submittedData"), rather than
    /// surfacing the real inner payload element directly - unwrap one level when that wrapper is
    /// present. Flagged: real legacy's actual wire shape for this field hasn't been captured from a
    /// live legacy client, only inferred from `HISO_doc.md`'s "untyped XmlNode[]" description - revisit
    /// if a real captured request ever becomes available.
    /// </summary>
    private static string? ExtractPayloadXml(XmlElement? element)
    {
        if (element is null)
        {
            return null;
        }

        if (element.LocalName.Equals("submittedData", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var child in element.ChildNodes)
            {
                if (child is XmlElement childElement)
                {
                    return childElement.OuterXml;
                }
            }
            return null;
        }

        return element.OuterXml;
    }
}
