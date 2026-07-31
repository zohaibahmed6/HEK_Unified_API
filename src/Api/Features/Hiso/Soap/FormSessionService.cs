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
/// the same business logic - `Program.cs`'s per-system file router matches on a `"hiso"` prefix, so
/// this still lands in `logs/hiso/*.log` alongside the REST traffic (2026-07-30 fix). Success paths
/// also call `_observer.Tag(...)` (2026-07-30) - matching `KaroCompatController`'s existing pattern for
/// controllers that don't route their success through `ObserveAsync` - so successful calls show up in
/// `readable-*.log` too, not just failures.
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
            var versionResult = _mediator.Send(new GetVersionQuery(getVersionRequest.sessionKey, calledServerAddress)).GetAwaiter().GetResult();

            if (!versionResult.SessionResolved)
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
            _observer.Tag(_logger, System, endpoint, context);
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
            _observer.Tag(_logger, System, endpoint, context);
                return new GetDataResponseSoap { GetDataResponseReturn = new GetDataResponseReturnSoap { dataContainer = null } };
            }

            var filledDoc = new XmlDocument();
            filledDoc.LoadXml(result.FilledSubmittedDataXml);

            // Legacy: `submittedData` is itself a wrapper element (real namespace
            // `http://www.hiso.govt.nz/10014.2/1.0/formInstanceMeta`) containing a leading `<dummy/>`
            // placeholder before the real `<form>` payload - confirmed against a live legacy capture
            // (same pattern already relied on for parsing the *request* in `ExtractPayloadXml`, but
            // the *response* here was assigning the bare `<form>` element directly as `submittedData`,
            // which `[XmlAnyElement]` serializes verbatim under the form's own tag name - so the real
            // `<submittedData><dummy/>...</submittedData>` wrapper was silently missing from every
            // getData response until this fix, confirmed 2026-07-31 against the real legacy server.
            var submittedDataWrapper = filledDoc.CreateElement("submittedData", HisoSoapNamespaces.Meta);
            submittedDataWrapper.AppendChild(filledDoc.CreateElement("dummy", HisoSoapNamespaces.Meta));
            submittedDataWrapper.AppendChild(filledDoc.DocumentElement!);

            var response = new GetDataResponseSoap
            {
                GetDataResponseReturn = new GetDataResponseReturnSoap
                {
                    dataContainer = new FormDataSoap
                    {
                        // Legacy: formMetaData is echoed back from the request unchanged.
                        formMetaData = getDataRequest.dataContainer.formMetaData,
                        submittedData = submittedDataWrapper,
                    }
                }
            };
            _logger.LogInformation("{System} {Endpoint} succeeded. Context: {@Context}", System, endpoint, context);
            _observer.Tag(_logger, System, endpoint, context);
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
            var formMetaData = saveContainerRequest.dataContainer.formMetaData;
            var metaData = new SaveContainerFormMetaDataInput(
                formMetaData.formInstanceId, formMetaData.formInstanceVersion,
                formMetaData.formEngineId, formMetaData.formInstanceOperationMode,
                formMetaData.formDefinitionId, formMetaData.formDefinitionVersion,
                formMetaData.formDefinitionTitle);

            var result = _mediator.Send(new SaveContainerCommand(
                saveContainerRequest.sessionKey, calledServerAddress, metaData, saveContainerRequest.resumePath,
                saveContainerRequest.view, saveContainerRequest.viewType, saveContainerRequest.viewSignature,
                saveContainerRequest.completed, ExtractPayloadXml(saveContainerRequest.dataContainer.submittedData))).GetAwaiter().GetResult();

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
            _observer.Tag(_logger, System, endpoint, context);
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
            _observer.Tag(_logger, System, endpoint, context);
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

    public GetFormViewResponseSoap getFormView(GetFormViewRequestSoap getFormViewRequest)
    {
        const string endpoint = "getFormView";
        var context = new Dictionary<string, object?> { ["SessionKey"] = getFormViewRequest.sessionKey };

        try
        {
            var calledServerAddress = _httpContextAccessor.HttpContext?.Request.Host.Value ?? string.Empty;
            var result = _mediator.Send(new GetFormViewQuery(getFormViewRequest.sessionKey, calledServerAddress)).GetAwaiter().GetResult();

            if (!result.SessionResolved)
            {
                _observer.RecordExpectedFailure(_logger, System, endpoint, "InvalidSessionKey", context);
                // Legacy: FaultException("Invalid Session Key") - same real fault string as every other operation.
                throw new FaultException("Invalid Session Key");
            }

            var response = new GetFormViewResponseSoap
            {
                GetFormViewResponseReturn = new GetFormViewResponseReturnSoap
                {
                    resumePath = result.ResumePath,
                    view = result.View,
                    viewType = result.ViewType,
                    // Legacy: dataContainer is always empty/null on a real getFormView read - never
                    // populated, reproduced as-is (see GetFormViewResponseReturnSoap's doc comment).
                    dataContainer = null,
                }
            };
            _logger.LogInformation("{System} {Endpoint} succeeded. Context: {@Context}", System, endpoint, context);
            _observer.Tag(_logger, System, endpoint, context);
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
            _observer.Tag(_logger, System, endpoint, context);
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
    /// present. Confirmed against a live legacy capture: real `submittedData` carries a leading
    /// `&lt;dummy/&gt;` placeholder before the actual `&lt;form&gt;` payload element, so the first
    /// child element is not necessarily the real payload - skip `dummy` and take the first non-dummy
    /// element child instead.
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
                if (child is XmlElement childElement && !childElement.LocalName.Equals("dummy", StringComparison.OrdinalIgnoreCase))
                {
                    return childElement.OuterXml;
                }
            }
            return null;
        }

        return element.OuterXml;
    }
}
