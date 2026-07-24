using System.ServiceModel;
using System.Xml;
using System.Xml.Serialization;

namespace HekCoreApi.Api.Features.Hiso.Soap;

/// <summary>
/// v1.1 spec follow-through, Step 4: the real HISO SOAP contract (`HISO_doc.md` - service
/// `Hiso.FormSessionService`, contract `FormSessionPortType`, `basicHttpBinding`). Hosted via SoapCore
/// at the real address `/FormSessionService.svc` (Program.cs), replacing the JSON-only
/// `HisoCompatController` endpoints one operation at a time - each method here delegates to the exact
/// same already-verified MediatR handler the JSON endpoint used, so the business logic is unchanged,
/// only the envelope (SOAP/XML vs. JSON) and address are corrected to match real HISO.
/// </summary>
[ServiceContract(Namespace = "http://www.hiso.govt.nz/10014.2/1.0/formsession")]
public interface IFormSessionService
{
    [OperationContract(Action = "http://www.hiso.govt.nz/10014.2/1.0/formsession/getVersion")]
    GetVersionResponseSoap getVersion(GetVersionRequestSoap getVersionRequest);

    [OperationContract(Action = "http://www.hiso.govt.nz/10014.2/1.0/formsession/getData")]
    GetDataResponseSoap getData(GetDataRequestSoap getDataRequest);

    [OperationContract(Action = "http://www.hiso.govt.nz/10014.2/1.0/formsession/saveContainer")]
    SaveContainerResponseSoap saveContainer(SaveContainerRequestSoap saveContainerRequest);

    [OperationContract(Action = "http://www.hiso.govt.nz/10014.2/1.0/formsession/getDeliveryOptions")]
    GetDeliveryOptionsResponseSoap getDeliveryOptions(GetDeliveryOptionsRequestSoap getDeliveryOptionsRequest);

    [OperationContract(Action = "http://www.hiso.govt.nz/10014.2/1.0/formsession/processAction")]
    ProcessActionResponseSoap processAction(ProcessActionRequestSoap processActionRequest);
}

/// <summary>Real `processAction` request shape (`HISO_doc.md` Section 6) - `actionId` dispatches
/// `"save"`/`"addTask"` (real logic) vs. `"addInvoice"`/`"launchForm"` (genuine legacy no-ops).</summary>
public sealed class ProcessActionRequestSoap
{
    public Guid sessionKey { get; set; }
    public string actionId { get; set; } = string.Empty;

    [XmlAnyElement]
    public XmlElement? actionContainer { get; set; }
}

public sealed class ProcessActionResponseSoap
{
    public ProcessActionResponseReturnSoap ProcessActionResponseReturn { get; set; } = new();
}

public sealed class ProcessActionResponseReturnSoap
{
    public bool processed { get; set; }
}

public sealed class GetDeliveryOptionsRequestSoap
{
    public Guid sessionKey { get; set; }
}

public sealed class GetDeliveryOptionsResponseSoap
{
    public GetDeliveryOptionsResponseReturnSoap GetDeliveryOptionsResponseReturn { get; set; } = new();
}

public sealed class GetDeliveryOptionsResponseReturnSoap
{
    public string? senderAccount { get; set; }
    public string? senderPassword { get; set; }
    public string? URL { get; set; }
}

/// <summary>Real `saveContainer` request shape (`HISO_doc.md` Section 4) - persists the rendered form
/// view to the DMS, then (when `completed`) the ACC45 definition/detail/diagnosis data.</summary>
public sealed class SaveContainerRequestSoap
{
    public Guid sessionKey { get; set; }
    public FormMetaDataSoap formMetaData { get; set; } = new();
    public string? resumePath { get; set; }
    public string? view { get; set; }
    public string? viewType { get; set; }
    public string? viewSignature { get; set; }
    public bool completed { get; set; }

    [XmlAnyElement]
    public XmlElement? dataContainer { get; set; }
}

public sealed class SaveContainerResponseSoap
{
    public SaveContainerResponseReturnSoap SaveContainerResponseReturn { get; set; } = new();
}

public sealed class SaveContainerResponseReturnSoap
{
    public bool response { get; set; }
}

/// <summary>Mirrors real `FormData`/`FormMetaData` (see `Adapters.Hiso.GetData.GetDataRequest` doc comment)
/// - `submittedData` carried as raw XML (`XmlElement`), matching legacy's real untyped `XmlNode[]` wire
/// shape, rather than reinventing the form/concept/section structure as typed members.</summary>
public sealed class GetDataRequestSoap
{
    public Guid sessionKey { get; set; }
    public FormDataSoap dataContainer { get; set; } = new();
}

public sealed class FormDataSoap
{
    public FormMetaDataSoap formMetaData { get; set; } = new();

    [XmlAnyElement]
    public XmlElement? submittedData { get; set; }
}

public sealed class FormMetaDataSoap
{
    public string? formInstanceId { get; set; }
    public string? formInstanceVersion { get; set; }
    public string? formEngineId { get; set; }
    public string? formInstanceOperationMode { get; set; }
    public string? formDefinitionId { get; set; }
    public string? formDefinitionVersion { get; set; }
    public string? formDefinitionTitle { get; set; }
}

public sealed class GetDataResponseSoap
{
    public GetDataResponseReturnSoap GetDataResponseReturn { get; set; } = new();
}

public sealed class GetDataResponseReturnSoap
{
    public FormDataSoap? dataContainer { get; set; }
}

public sealed class GetVersionRequestSoap
{
    public Guid sessionKey { get; set; }
}

public sealed class GetVersionResponseSoap
{
    public GetVersionResponseReturnSoap GetVersionResponseReturn { get; set; } = new();
}

public sealed class GetVersionResponseReturnSoap
{
    public string application { get; set; } = string.Empty;
    public string applicationVersion { get; set; } = string.Empty;
    public int hisoversion { get; set; }
}
