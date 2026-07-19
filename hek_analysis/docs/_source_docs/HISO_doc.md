> **⚠️ Reverse-engineered specification — not an original vendor document.**
> No original HISO specification exists anywhere in the source/documentation provided for this
> project (unlike KARO and ERMS, which shipped Word specs). This document was generated purely
> from static analysis of the HISO source code (`Hiso.csproj`, `FormSessionService.svc(.cs)`,
> `Web.config`, `Mapper.cs`, `ConceptMapper/`, `DAL/`, and the `*Builder.cs` classes) so it can sit
> alongside `KARO_HSS_doc.md` and `ERMS_doc.md` in the same format for the Phase 2/3 comparison and
> future unified SRS work. Anything that could not be confirmed from the code is explicitly marked
> **Assumption** or **Unable to verify from available source** — never guessed. Full evidence
> citations (file/class/method/line) live in `docs/analysis/hiso/*.md`; this document is the
> spec-style summary of that analysis.

# Indici HISO (Health Information Standards Online) Web API — Reverse-Engineered Specification

# 1. Document Details
## Version
Derived from source as of this analysis pass (2026-07-18). No version number is embedded in the
HISO codebase itself — **Unable to verify from available source** whether HISO carries its own
release/version identifier outside of `getVersion`'s hardcoded response values (see §5.1).

# Purpose
This document describes the interface exposed by the Indici Practice Management System's HISO
integration — a SOAP web service used to drive **ACC45 accident-claim clinical forms** (and
related patient/consult/task data) for external form-rendering clients (e.g. a HealthLink-style
form engine). Unlike KARO and ERMS, which are REST/JSON APIs consumed by external portals, HISO
is the **PMS acting as a SOAP service provider** for a form-session client.

# Overview
HISO exposes exactly **one WCF SOAP service** (`Hiso.FormSessionService`, contract
`FormSessionPortType`) with **six operations**. There is no REST/HTTP JSON surface in this
codebase — no ASP.NET MVC/Web API controllers or ASMX services exist. The service is entirely
session-oriented: every operation takes a `sessionKey` (GUID) and nothing else identifies the
caller. A central **database-driven "concept mapping" engine** resolves abstract clinical
"concept" identifiers (arriving from the form client) to specific PMS stored procedures and
result columns at runtime, rather than hardcoding a mapping per field — this is the architectural
core of the service (see `ConceptMapper/HisoConceptDetail.cs`).

XML is the wire format (WCF/SOAP), not JSON — this is the main structural difference from
KARO/ERMS.

# Flow Diagram
```
SOAP client (form engine)
      │  basicHttpBinding, security mode="None"
      ▼
FormSessionService (WCF)
      │
      ├─ GetSession(sessionKey) ──────────► Appointment.usptblHealthLinkSession_GetByGUID
      │                                      (Provider/Patient/Appointment/Practice context)
      │
      ├─ getData ─────► ConceptMapper (concept → stored procedure → column) ─► N stored procedures
      │                  (allow-listed procs run against a second DB node; AWS-enabled procs
      │                   enrich results from an external AWS document service)
      │
      ├─ saveContainer ─► DocumentHandler.AddDocument (DMS write, direct-DB or DMSProxy)
      │                    then ACC45 detail/definition/diagnosis/referral save
      │
      ├─ processAction("save") ──► Patient/Consult/Employer/Problem/Practitioner save
      ├─ processAction("addTask") ──► Task creation
      │
      └─ getFormView ──► retrieve previously stored ACC45 form definition/view from DMS
```

## Authentication
**There is no credential-based authentication.** (See `docs/analysis/hiso/AuthenticationAuthorization.md`
for full evidence.) Every operation trusts a **session GUID** (`sessionKey`) supplied by the
caller:

```csharp
private HealthLinkSession GetSession(string sessionKey)
{
    HealthLinkSession objSessionKey = HealthLinkSession.GetByGUID(Guid.Parse(sessionKey));
    if (objSessionKey == null)
        throw new FaultException("Invalid Session Key");
    return objSessionKey;
}
```

The WCF binding explicitly sets `security mode="None"` — no transport or message-level security
is enforced by the application itself. There is no token expiry, single-use marking, or IP
binding visible in the reviewed code; whatever lifecycle exists must live entirely inside the
`Appointment.usptblHealthLinkSession_GetByGUID` stored procedure body, which is **not included in
this codebase — Unable to verify.**

**Assumption:** the session GUID is expected to be issued to the client out-of-band (e.g. by the
PMS when launching the external form engine), analogous to how KARO/ERMS pass `patientId`/
`encounterId` as query-string parameters on portal launch — but no HISO launch-URL construction
code was found in this codebase to confirm the exact mechanism.

Unused `Microsoft.IdentityModel.JsonWebTokens` / `System.IdentityModel.Tokens.Jwt` library
references exist in `Hiso.csproj`, but no code constructs, issues, or validates a JWT anywhere in
the reviewed source. This looks like abandoned or unfinished groundwork for a real auth upgrade —
flagged for client confirmation.

## Session Key, Patient ID, and Related Identifiers
### Session Key (`sessionKey`)
A GUID string, required on every one of the 6 operations. Resolved server-side via
`HealthLinkSession.GetByGUID` into a context object exposing `ProviderId`, `PatientId`,
`AppointmentId`, `PracticeId`, `ReferenceId`, `PracticeLocationID`, and `PracticeEDI`. The caller
never supplies Patient ID, Provider ID, or Practice ID directly — they are all derived from the
session.

### Form Instance ID (`formInstanceId`)
Used by `getFormView` to retrieve a previously stored ACC45 form definition/view. **Unable to
verify from available source** exactly how/when a form instance ID is first issued to the client
— likely returned as part of a prior `saveContainer` or `getData` response, but this could not be
confirmed from the operations reviewed.

### Action ID (`actionId`)
Used by `processAction` to dispatch behavior: `"save"`, `"addTask"` are implemented;
`"addInvoice"` and `"launchForm"` are **unimplemented no-op stubs** (see §6).

## Invocation
### HISO WCF Service
Host/binding (from `Web.config`):
```
Binding: basicHttpBinding
Security mode: None
Service: Hiso.FormSessionService
Contract: FormSessionPortType
Metadata: serviceMetadata httpGetEnabled="true" (exposes WSDL only, not a REST surface)
includeExceptionDetailInFaults: true  (⚠ leaks internal exception detail to callers — see SecurityAnalysis.md)
```
Endpoint address is the service root, i.e. `{host}/FormSessionService.svc`.
**Unable to verify from available source** what the actual production/development hostnames are
— no environment-specific `Web.*.config` host values were found for this binding (unlike
KARO/ERMS's documented dev/prod hostnames).

### Sample SOAP request shape (illustrative, reconstructed from the WCF contract — not a captured
real request)
```xml
<soap:Envelope ...>
  <soap:Body>
    <getVersion xmlns="http://www.hiso.govt.nz/10014.2/1.0/formsession">
      <getVersionRequest>
        <sessionKey>5895CBDF-AB10-4A22-99F8-DC26E372104B</sessionKey>
      </getVersionRequest>
    </getVersion>
  </soap:Body>
</soap:Envelope>
```

# Categories of Operations

## 1. getVersion
**Type:** SOAP operation (read-only)
**SOAP Action:** `http://www.hiso.govt.nz/10014.2/1.0/formsession/getVersion`
**Handler:** `FormSessionService.getVersion(getVersionRequest)`

Request:
```json
{ "sessionKey": "5895CBDF-AB10-4A22-99F8-DC26E372104B" }
```
Response:
```json
{
  "GetVersionResponseReturn": {
    "application": "PMS",
    "applicationVersion": "1.0",
    "dictionaryVersion": "<from DB>",
    "hisoversion": 1
  }
}
```
Note: `application`, `applicationVersion`, and `hisoversion` are **hardcoded constants** in the
handler, not derived from an actual build/version source (evidence: `FormSessionService.svc.cs`
lines 35-68).

## 2. getDeliveryOptions
**Type:** SOAP operation (read-only)
**SOAP Action:** `.../getDeliveryOptions`
**Handler:** `FormSessionService.getDeliveryOptions(getDeliveryOptionsRequest)`

Request:
```json
{ "sessionKey": "5895CBDF-AB10-4A22-99F8-DC26E372104B" }
```
Response:
```json
{
  "GetDeliveryOptionsResponseReturn": {
    "senderAccount": "<from appSettings UserID>",
    "senderPassword": "<from appSettings Password>",
    "URL": "<from appSettings URL>"
  }
}
```
⚠ Returns a **plaintext EDI sender password** in the SOAP response body. Also loads Aspose
Words/Pdf license files on every call. See `SecurityAnalysis.md`.

## 3. getData
**Type:** SOAP operation (read/compute)
**SOAP Action:** `.../getData`
**Handler:** `FormSessionService.getData(getDataRequest)`

Request:
```json
{
  "sessionKey": "5895CBDF-AB10-4A22-99F8-DC26E372104B",
  "dataContainer": { "...FormData (concept/section structure)...": "" }
}
```
Response:
```json
{ "GetDataResponseReturn": { "dataContainer": "...populated FormData..." } }
```
Core "dynamic mode" data-fill operation. For each concept referenced in the request, the
concept-mapping engine resolves the backing stored procedure and executes it (in parallel when
multiple concepts are requested), then maps result columns back onto the response's data
container. Only runs when `appSettings["IsDynamic"] == "1"`; the "static mode" branch is an
**empty, unimplemented stub**. Binary attachment values are opportunistically converted to PDF
(PNG/BMP/HTML sources) before being returned — see BR-11 in `BusinessRules.md`.

## 4. saveContainer
**Type:** SOAP operation (write)
**SOAP Action:** `.../saveContainer`
**Handler:** `FormSessionService.saveContainer(saveContainerRequest)`

Request:
```json
{
  "sessionKey": "5895CBDF-AB10-4A22-99F8-DC26E372104B",
  "resumePath": "string",
  "dataContainer": "...FormData...",
  "view": "<rendered HTML or base64 PDF>",
  "viewType": "html | pdf",
  "viewSignature": "string",
  "completed": true,
  "continueSession": false
}
```
Response:
```json
{ "SaveContainerResponseReturn": { "response": "string" } }
```
Persists the rendered form view to the Document Management System first, then — only if
`completed == true` — persists ACC45 detail/definition/diagnosis/referral data to `PMS_NZ`. The
returned DMS document GUID is linked into the ACC45 definition record.

## 5. getFormView
**Type:** SOAP operation (read-only)
**SOAP Action:** `.../getFormView`
**Handler:** `FormSessionService.getFormView(getFormViewRequest)`

Request:
```json
{
  "sessionKey": "5895CBDF-AB10-4A22-99F8-DC26E372104B",
  "formInstanceId": "string"
}
```
Response:
```json
{
  "GetFormViewResponseReturn": {
    "resumePath": "string",
    "viewType": "html | pdf",
    "view": "string",
    "dataContainer": "...FormData..."
  }
}
```
Retrieves a previously stored ACC45 form definition/view via `Acc45DefinitionBuilder.GetACC45Definition`.

## 6. processAction
**Type:** SOAP operation (write / dispatch)
**SOAP Action:** `.../processAction`
**Handler:** `FormSessionService.processAction(processActionRequest)`

Request:
```json
{
  "sessionKey": "5895CBDF-AB10-4A22-99F8-DC26E372104B",
  "actionId": "save | addTask | addInvoice | launchForm",
  "actionContainer": "<XML element, action-specific payload>"
}
```
Response:
```json
{ "ProcessActionResponseReturn": { "processed": true } }
```
Dispatches on `actionId`:
- `"save"` → patient/consult/organisation/problem/practitioner save, gated by
  `appSettings["IsDynamic"]` (a no-op returning `true` without persisting anything when the
  practice is in static mode — BR-17).
- `"addTask"` → creates a PMS task/reminder linked to the session's patient/provider (BR-19,
  BR-20).
- `"addInvoice"`, `"launchForm"` → **not implemented.** These are silent no-ops; `processed` is
  never explicitly set to `true` for them, so the caller may believe an action was attempted when
  nothing happened. Flagged as a hard gap — confirm with the client whether these are dead or
  simply unfinished before the unified rewrite.

# Key Business Rules (see `BusinessRules.md` for the full catalog of 22)
- A resumed ("old") form only refreshes "current user" concepts, never the whole form, to avoid
  overwriting previously captured clinical data (BR-08).
- Certain report/attachment/letter/problem procedures must run against a second database node —
  an explicit six-procedure allow-list (BR-05).
- When AWS-backed document storage is enabled for a practice, an allow-listed `_AWS` procedure
  variant is called instead, with fallback to the normal procedure on failure (BR-06/BR-07).
- Table-valued-parameter column lists for every "UDT" used by the Builder classes live in
  `Web.config`, not in code (BR-16) — a governance gap to close in the rewrite.

# Known Gaps / Unimplemented Behavior (for future SRS input)
| Item | Status | Evidence |
|---|---|---|
| `getData` static mode (`IsDynamic=0`) | Empty stub, not implemented | `FormSessionService.svc.cs`, `#region Static` |
| `processAction("addInvoice")` | Not implemented (silent no-op) | `FormSessionService.svc.cs` |
| `processAction("launchForm")` | Not implemented (silent no-op) | `FormSessionService.svc.cs` |
| JWT/IdentityModel library references | Present in `.csproj`, never used in code | Unused `HintPath` references |
| Session expiry/single-use | Cannot be confirmed — logic (if any) lives in an unreviewed stored procedure | `Appointment.usptblHealthLinkSession_GetByGUID` body not in source tree |

# Cross-References
- Full evidence-cited analysis: `docs/analysis/hiso/*.md` (12 reports)
- Cross-API comparison: `docs/analysis/ComparisonReport.md`
- Migration classification: `docs/analysis/MigrationRecommendations.md`
