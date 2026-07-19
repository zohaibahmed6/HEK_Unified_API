# HISO — Endpoint Inventory

**Summary:** HISO exposes exactly one WCF SOAP service with 6 operations (no REST/HTTP
controllers exist in this codebase); every operation requires only an opaque session GUID,
not a credential-based authentication.

## Findings

Service: `Hiso.FormSessionService` implementing contract `FormSessionPortType`.
Endpoint: `basicHttpBinding`, `security mode="None"`, address = service root (`FormSessionService.svc`).
Evidence: `Web.config` lines 44-53; `FormSessionService.svc` (`<%@ ServiceHost ... Service="Hiso.FormSessionService" %>` — content of `.svc` file).

| Operation | SOAP Action | Handler class.method | Request Model | Response Model | Auth Required | Notes |
|---|---|---|---|---|---|---|
| `getVersion` | `http://www.hiso.govt.nz/10014.2/1.0/formsession/getVersion` | `FormSessionService.getVersion(getVersionRequest)` | `getVersionRequest { sessionKey }` | `getVersionResponse { GetVersionResponseReturn { application, applicationVersion, dictionaryVersion, hisoversion } }` | Session GUID only (`GetSession`) | Hardcodes `application="PMS"`, `applicationVersion="1.0"`, `hisoversion=1` (evidence: `FormSessionService.svc.cs` lines 35-68). |
| `getDeliveryOptions` | `.../getDeliveryOptions` | `FormSessionService.getDeliveryOptions(getDeliveryOptionsRequest)` | `getDeliveryOptionsRequest { sessionKey }` | `getDeliveryOptionsResponse { senderAccount, senderPassword, URL, ... }` | Session GUID only | Returns EDI sender account/password and submission URL sourced from `appSettings` (`UserID`, `Password`, `URL`, `PracticeEDI`); loads Aspose Words/Pdf license files on every call (evidence: lines 70-147). |
| `getData` | `.../getData` | `FormSessionService.getData(getDataRequest)` | `getDataRequest { sessionKey, dataContainer: FormData }` | `getDataResponse { GetDataResponseReturn { dataContainer } }` | Session GUID only | Core "dynamic mode" data-fill operation; only executes when `appSettings["IsDynamic"]=="1"`; "static mode" branch is an unimplemented stub (evidence: lines 156-288, `#region Static` empty). |
| `saveContainer` | `.../saveContainer` | `FormSessionService.saveContainer(saveContainerRequest)` | `saveContainerRequest { sessionKey, resumePath, dataContainer, view, viewType, viewSignature, completed, continueSession }` | `saveContainerResponse { SaveContainerResponseReturn { response } }` | Session GUID only | Persists rendered form view to DMS (`DocumentHandler.AddDocument`) and ACC45/definition/diagnosis/referral tables to `PMS_NZ` when `request.completed==true` (evidence: lines 299-335, 443-501). |
| `getFormView` | `.../getFormView` | `FormSessionService.getFormView(getFormViewRequest)` | `getFormViewRequest { sessionKey, formInstanceId }` | `getFormViewResponse { GetFormViewResponseReturn { resumePath, viewType, view, dataContainer } }` | Session GUID only | Retrieves previously stored ACC45 form definition/view from DMS via `Acc45DefinitionBuilder.GetACC45Definition` (evidence: lines 337-379). |
| `processAction` | `.../processAction` | `FormSessionService.processAction(processActionRequest)` | `processActionRequest { sessionKey, actionId, actionContainer: XmlElement }` | `processActionResponse { ProcessActionResponseReturn { processed } }` | Session GUID only | Dispatches on `actionId`: `"save"` → `saveProcessAction` (patient/consult/org/problem/practitioner save, gated by `appSettings["IsDynamic"]`), `"addTask"` → `Task.processTask`/`AddTask`, `"addInvoice"`/`"launchForm"` → **no-op stubs (not implemented)** (evidence: lines 381-422, 502-593). |

### Notes on "authentication"
None of the 6 operations perform credential-based authentication. Each calls
`GetSession(request.sessionKey)` → `HealthLinkSession.GetByGUID(Guid)`, which looks up a
`SessionGUID` in table/proc `Appointment.usptblHealthLinkSession_GetByGUID` and returns a
context object (`ProviderId`, `PatientId`, `AppointmentId`, `PracticeId`, `ReferenceId`,
`PracticeLocationID`, `PracticeEDI`). If no row is found, a `FaultException("Invalid Session
Key")` is thrown; but the underlying DB errors are swallowed (`catch (Exception) { }` in
`GetByGUID`) so a **DB failure looks identical to an invalid session** to the caller — no
distinct error code. See `AuthenticationAuthorization.md`.

### No REST/HTTP JSON endpoints
No ASP.NET MVC/Web API controllers, ASMX services, or Minimal API endpoints exist in this
project; `serviceMetadata httpGetEnabled="true"` only exposes the WSDL for the SOAP contract,
not a REST surface.

## Risks
- Because auth = "possession of a GUID", any endpoint is fully open to anyone who can obtain
  or guess a session GUID (GUIDs are cryptographically hard to guess, but there is no
  additional check such as expiry, IP binding, or single-use).
- `getData`'s entire "static mode" is an empty stub — if any client actually depends on
  `IsDynamic=0` behavior, that behavior is currently silently missing.
- `processAction`'s `addInvoice` and `launchForm` actions are unimplemented no-ops that
  report `processed=false` implicitly (since `processed` is only set for `save`/`addTask`) —
  clients may believe an action was attempted when nothing happened.

## Recommendations
- In the unified API, replace this single multi-purpose SOAP contract with clearly-scoped,
  versioned REST/JSON endpoints per capability (session validation, form data retrieval, form
  save, task creation) with real authentication (OAuth2/JWT) and per-operation authorization.
- Explicitly decide whether "static mode" and `addInvoice`/`launchForm` are dead requirements
  to drop or gaps to fill — do not silently carry forward the stubs.
