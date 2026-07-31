using System.ServiceModel;
using System.Xml;
using System.Xml.Serialization;

namespace HekCoreApi.Api.Features.Hiso.Soap;

internal static class HisoSoapNamespaces
{
    public const string FormSession = "http://www.hiso.govt.nz/10014.2/1.0/formsession";
    public const string Meta = "http://www.hiso.govt.nz/10014.2/1.0/formInstanceMeta";
}

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

    [OperationContract(Action = "http://www.hiso.govt.nz/10014.2/1.0/formsession/getFormView")]
    GetFormViewResponseSoap getFormView(GetFormViewRequestSoap getFormViewRequest);
}

/// <summary>
/// Real `getFormView` request/response shape - confirmed directly against the real legacy server's own
/// WSDL (2026-07-30, live `?wsdl=wsdl0`/`?xsd=xsd0` fetch against `http://localhost:53507/FormSessionService.svc`):
/// `GetFormViewRequest { sessionKey, formInstanceId }` -&gt;
/// `GetFormViewResponse { return { resumePath, dataContainer, view, viewType } }`. This operation was
/// previously missing from this SOAP contract entirely - a prior session's doc comment on
/// `HisoCompatController.GetFormView` claimed it "has no SOAP equivalent... not a real legacy
/// operation", which the live WSDL fetch disproves (it's the real service's 6th operation, same
/// contract as the other 5). Delegates to the same `GetFormViewQuery` the JSON dashboard endpoint
/// already uses - no new business logic, only the missing SOAP wire-up.
/// </summary>
[MessageContract(WrapperName = "getFormView", WrapperNamespace = HisoSoapNamespaces.FormSession, IsWrapped = true)]
public sealed class GetFormViewRequestSoap
{
    [MessageBodyMember(Namespace = HisoSoapNamespaces.FormSession, Order = 0)]
    public Guid sessionKey { get; set; }

    [MessageBodyMember(Namespace = HisoSoapNamespaces.FormSession, Order = 1)]
    public string? formInstanceId { get; set; }
}

public sealed class GetFormViewResponseSoap
{
    public GetFormViewResponseReturnSoap GetFormViewResponseReturn { get; set; } = new();
}

/// <summary>Legacy: `dataContainer` is always empty/null on a real `getFormView` read (never
/// populated, per `Adapters.Hiso.GetFormView.GetFormViewResponse`'s doc comment) - reproduced as-is,
/// not "improved".</summary>
public sealed class GetFormViewResponseReturnSoap
{
    public string? resumePath { get; set; }
    public FormDataSoap? dataContainer { get; set; }
    public string? view { get; set; }
    public string? viewType { get; set; }
}

/// <summary>Real `processAction` request shape (`HISO_doc.md` Section 6) - `actionId` dispatches
/// `"save"`/`"addTask"` (real logic) vs. `"addInvoice"`/`"launchForm"` (genuine legacy no-ops).
/// `MessageContract` with an explicit `WrapperName` matching the operation reproduces real legacy's
/// single-level wire shape (`&lt;processAction&gt;&lt;sessionKey&gt;...` directly, no extra
/// parameter-name nesting) - confirmed against a live legacy capture for `getData`/`getVersion`
/// (see those types); applied uniformly here since it's the same contract shape throughout.</summary>
[MessageContract(WrapperName = "processAction", WrapperNamespace = HisoSoapNamespaces.FormSession, IsWrapped = true)]
public sealed class ProcessActionRequestSoap
{
    [MessageBodyMember(Namespace = HisoSoapNamespaces.FormSession, Order = 0)]
    public Guid sessionKey { get; set; }

    [MessageBodyMember(Namespace = HisoSoapNamespaces.FormSession, Order = 1)]
    public string actionId { get; set; } = string.Empty;

    [MessageBodyMember(Order = 2)]
    [XmlAnyElement]
    public XmlElement? actionContainer { get; set; }
}

[MessageContract(WrapperName = "processActionResponse", WrapperNamespace = HisoSoapNamespaces.FormSession, IsWrapped = true)]
public sealed class ProcessActionResponseSoap
{
    [MessageBodyMember(Namespace = HisoSoapNamespaces.FormSession, Order = 0)]
    [XmlElement("return")]
    public ProcessActionResponseReturnSoap ProcessActionResponseReturn { get; set; } = new();
}

public sealed class ProcessActionResponseReturnSoap
{
    public bool processed { get; set; }
}

[MessageContract(WrapperName = "getDeliveryOptions", WrapperNamespace = HisoSoapNamespaces.FormSession, IsWrapped = true)]
public sealed class GetDeliveryOptionsRequestSoap
{
    [MessageBodyMember(Namespace = HisoSoapNamespaces.FormSession, Order = 0)]
    public Guid sessionKey { get; set; }
}

[MessageContract(WrapperName = "getDeliveryOptionsResponse", WrapperNamespace = HisoSoapNamespaces.FormSession, IsWrapped = true)]
public sealed class GetDeliveryOptionsResponseSoap
{
    [MessageBodyMember(Namespace = HisoSoapNamespaces.FormSession, Order = 0)]
    [XmlElement("return")]
    public GetDeliveryOptionsResponseReturnSoap GetDeliveryOptionsResponseReturn { get; set; } = new();
}

public sealed class GetDeliveryOptionsResponseReturnSoap
{
    public string? senderAccount { get; set; }
    public string? senderPassword { get; set; }
    public string? URL { get; set; }
}

/// <summary>
/// Real `saveContainer` request shape - confirmed 2026-07-31 against the real legacy server's own
/// WSDL/XSD (`?wsdl=wsdl0`/`?xsd=xsd0`): `sessionKey, resumePath, dataContainer(FormData), view,
/// viewType, viewSignature, completed, continueSession`. The previous version of this class was
/// wrong on two counts - a top-level `formMetaData` member that doesn't exist in the real request at
/// all (the real `formMetaData` lives *inside* `dataContainer`, same `FormData` shape `getData`
/// already uses), and a missing `continueSession` field - confirmed as the actual cause of a real
/// legacy-vs-new-API test failing identically on both a garbage payload and a real, fully-populated
/// one ("There are not enough fields in the Structured type"), i.e. a wire-shape bug, not a data
/// problem. Persists the rendered form view to the DMS, then (when `completed`) the ACC45
/// definition/detail/diagnosis data.
/// </summary>
[MessageContract(WrapperName = "saveContainer", WrapperNamespace = HisoSoapNamespaces.FormSession, IsWrapped = true)]
public sealed class SaveContainerRequestSoap
{
    [MessageBodyMember(Namespace = HisoSoapNamespaces.FormSession, Order = 0)]
    public Guid sessionKey { get; set; }

    [MessageBodyMember(Namespace = HisoSoapNamespaces.FormSession, Order = 1)]
    public string? resumePath { get; set; }

    [MessageBodyMember(Namespace = HisoSoapNamespaces.FormSession, Order = 2)]
    public FormDataSoap dataContainer { get; set; } = new();

    [MessageBodyMember(Namespace = HisoSoapNamespaces.FormSession, Order = 3)]
    public string? view { get; set; }

    [MessageBodyMember(Namespace = HisoSoapNamespaces.FormSession, Order = 4)]
    public string? viewType { get; set; }

    [MessageBodyMember(Namespace = HisoSoapNamespaces.FormSession, Order = 5)]
    public string? viewSignature { get; set; }

    [MessageBodyMember(Namespace = HisoSoapNamespaces.FormSession, Order = 6)]
    public bool completed { get; set; }

    [MessageBodyMember(Namespace = HisoSoapNamespaces.FormSession, Order = 7)]
    public bool continueSession { get; set; }
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
[MessageContract(WrapperName = "getData", WrapperNamespace = HisoSoapNamespaces.FormSession, IsWrapped = true)]
public sealed class GetDataRequestSoap
{
    [MessageBodyMember(Namespace = HisoSoapNamespaces.FormSession, Order = 0)]
    public Guid sessionKey { get; set; }

    [MessageBodyMember(Namespace = HisoSoapNamespaces.FormSession, Order = 1)]
    public FormDataSoap dataContainer { get; set; } = new();
}

public sealed class FormDataSoap
{
    private const string MetaNs = HisoSoapNamespaces.Meta;

    [XmlElement(Namespace = MetaNs)]
    public FormMetaDataSoap formMetaData { get; set; } = new();

    [XmlAnyElement]
    public XmlElement? submittedData { get; set; }
}

public sealed class FormMetaDataSoap
{
    private const string MetaNs = HisoSoapNamespaces.Meta;

    [XmlElement(Namespace = MetaNs)]
    public string? formInstanceId { get; set; }

    [XmlElement(Namespace = MetaNs)]
    public string? formInstanceVersion { get; set; }

    [XmlElement(Namespace = MetaNs)]
    public string? formEngineId { get; set; }

    // Confirmed missing 2026-07-31 against the real WSDL's FormMetaData type (xsd1) and a live real
    // client payload (Zohaib's captured saveContainer request) - legacy echoes all of these back
    // unchanged, but this class only had 7 of the real 14 fields, so 4 real, present-in-the-request
    // values were silently dropped from every getData/getFormView/saveContainer echo.
    [XmlElement(Namespace = MetaNs)]
    public DateOnly? formInstanceCreationDate { get; set; }

    [XmlElement(Namespace = MetaNs)]
    public string? formInstanceOperationMode { get; set; }

    [XmlElement(Namespace = MetaNs)]
    public string? formInstanceDescription { get; set; }

    [XmlElement(Namespace = MetaNs)]
    public string? formDefinitionId { get; set; }

    [XmlElement(Namespace = MetaNs)]
    public string? formDefinitionVersion { get; set; }

    [XmlElement(Namespace = MetaNs)]
    public string? formDefinitionDescription { get; set; }

    [XmlElement(Namespace = MetaNs)]
    public string? formDefinitionTitle { get; set; }

    [XmlElement(Namespace = MetaNs)]
    public string? recipientAccount { get; set; }
}

public sealed class GetDataResponseSoap
{
    public GetDataResponseReturnSoap GetDataResponseReturn { get; set; } = new();
}

public sealed class GetDataResponseReturnSoap
{
    public FormDataSoap? dataContainer { get; set; }
}

[MessageContract(WrapperName = "getVersion", WrapperNamespace = HisoSoapNamespaces.FormSession, IsWrapped = true)]
public sealed class GetVersionRequestSoap
{
    [MessageBodyMember(Namespace = HisoSoapNamespaces.FormSession, Order = 0)]
    public Guid sessionKey { get; set; }
}

// Real legacy's response wrapper element is a bare "return" (not the WCF-default
// "{operation}Result" -> "{Type}Return" double-nesting we previously emitted implicitly, since
// none of these 3 response classes had an explicit [MessageContract] - confirmed 2026-07-31 as one
// of the cosmetic/shape-only mismatches (values were already correct). Applied the same explicit
// [MessageContract]/[MessageBodyMember(Name="return")] pattern already used for every request type
// in this file, just on the response side this time.
[MessageContract(WrapperName = "getVersionResponse", WrapperNamespace = HisoSoapNamespaces.FormSession, IsWrapped = true)]
public sealed class GetVersionResponseSoap
{
    // SoapCore is configured with SoapSerializer.XmlSerializer (Program.cs) - in that mode,
    // MessageBodyMember.Name is NOT honored for the element name (confirmed live 2026-07-31: the
    // wire element stayed "GetVersionResponseReturn" despite Name="return" here); [XmlElement] is
    // the attribute XmlSerializer actually reads for renaming.
    [MessageBodyMember(Namespace = HisoSoapNamespaces.FormSession, Order = 0)]
    [XmlElement("return")]
    public GetVersionResponseReturnSoap GetVersionResponseReturn { get; set; } = new();
}

public sealed class GetVersionResponseReturnSoap
{
    public string application { get; set; } = string.Empty;
    public string applicationVersion { get; set; } = string.Empty;
    public int hisoversion { get; set; }
}
