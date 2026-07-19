# KARO — Endpoint Inventory

**Summary:** 24 endpoints are exposed from a single controller, all reachable at `/api/{action}` (default Web API route `{controller}/{action}/{id}` — `App_Start\WebApiConfig.cs` line 22-26, plus `AcceptVerbs` on each action); every response returns HTTP 200 with a JSON envelope indicating success/failure in the body.

## Findings

### Routing mechanics
- Route template: `config.Routes.MapHttpRoute("DefaultApi", "{controller}/{action}/{id}", ...)` (`App_Start\WebApiConfig.cs` lines 22-26). Combined with `config.MapHttpAttributeRoutes()` (line 17), but **no `[Route]` attributes were found on any action** in `APIController.cs`, so the convention-based `{controller}/{action}` path is what is actually used, matching the doc's `/api/{Operation-Name}` convention.
- CORS: `[EnableCors(origins: "*", headers: "*", methods: "*")]` at the controller level (`Controllers\APIController.cs` line 22) — applies to every endpoint below.
- Auth: no `[Authorize]` attribute anywhere; every action manually reads the `Authorization` header via `GetAuthorizationToken()` and passes it into `HSSDA.InsertAndValidateToken(...)`, which validates against a DB-stored token. "Auth required" below means "the action calls `InsertAndValidateToken`/token validation before doing work", not a framework-level guarantee.
- Response codes: **every** action returns `HttpStatusCode.OK` (200) via the shared `SetToJson()` helper (`Controllers\APIController.cs` lines 2010-2019), even for authentication failures, validation errors, and unhandled exceptions (caught and turned into `{"status":"fail","message":...}`). There is no 4xx/5xx usage anywhere in the controller.

### Endpoint table

| Method | Route (convention) | Controller | Action | Request model | Response shape | Auth required | Response codes | Notes |
|---|---|---|---|---|---|---|---|---|
| GET | `/api/Ping` | APIController | `Ping()` | none | `{"status":"success"}` | No | 200 always | Health check only |
| GET | `/api/Authenticate` | APIController | `Authenticate(username,password,patientId,encounterId,system,pho)` | query string | `{status,token,expiry,practiceId}` | N/A (this issues the token) | 200 always | **Credentials passed in query string and logged in plaintext**; undocumented in `KARO_HSS_doc.md` (doc only shows POST) |
| POST | `/api/Authenticate` | APIController | `Authenticate()` | JSON body → `Credential` | `{status,token,expiry,practiceId}` | N/A | 200 always | Matches doc's documented `/authenticate` |
| GET | `/api/GetClinicalNotes` | APIController | `GetClinicalNotes(system,pho,patientId,encounterId)` | query string | `Root<ConsultNotes>` | Yes | 200 always | Matches doc `/getClinicalNotes` |
| GET | `/api/GetConditions` | APIController | `GetConditions(...)` | query string | `Root<Diagnosis>` | Yes | 200 always | Matches doc `/getConditions` |
| GET | `/api/GetDemographics` | APIController | `GetDemographics(...)` | query string | `Root<Demographic>` | Yes | 200 always | Matches doc `/GetDemographics`; doc's sample fields (`dateOfEnrolment`, `endEnrolmentDate`) map to code's `DemographicInfo` properties |
| GET | `/api/GetDocuments` | APIController | `GetDocuments(system,pho,patientId,encounterId,identifier="")` | query string | `Root<Documents>` | Yes | 200 always | Matches doc `/getDocuments` (both collection and single-document modes); code additionally branches on an AWS-backed storage path (`HSSDA.GetDocuments`, not in doc) |
| GET | `/api/GetEncounterSummary` | APIController | `GetEncounterSummary(system,pho,patientId,encounterId,identifier)` | query string | Hardcoded JSON for `identifier=diap`/`cvra`, else `{}` | Yes | 200 always | **Not documented at all** in `KARO_HSS_doc.md` (doc only documents the POST `/api/SaveSummary` side of "Encounter Summary"). Returns static mock data unrelated to the requested patient — see `BusinessRules.md` BR-09 |
| GET | `/api/GetLabResults` | APIController | `GetLabResults(...)` | query string | `Root<LabResults>` | Yes | 200 always | Matches doc `/getLabResults` |
| GET | `/api/GetMedications` | APIController | `GetMedications(...)` | query string | `Root<Medications>` | Yes | 200 always | Matches doc `/getMedications` |
| GET | `/api/GetObservations` | APIController | `GetObservations(system,pho,patientId,encounterId,conceptId="")` | query string | `Root<Observation>` | Yes | 200 always | Matches doc `/getObservations` (with/without `conceptId`) |
| GET | `/api/GetProvider` | APIController | `GetProvider(system,pho,patientId,encounterId,userId="")` | query string | `Root<Provider>` | Yes | 200 always | Matches doc `/getProvider` |
| GET | `/api/GetRecallCategories` | APIController | `GetRecallCategories(system,pho,patientId,encounterId,group)` | query string | `Root<RecallCategories>` | Yes | 200 always | Matches doc `/getRecallCategories` |
| GET | `/api/GetRecalls` | APIController | `GetRecalls(...)` | query string | `Root<Recalls>` | Yes | 200 always | Matches doc `/getRecalls` |
| GET | `/api/GetScreeningCodes` | APIController | `GetScreeningCodes(...)` | query string | `Root<ScreeningCodes>` | Yes | 200 always | Matches doc `/getScreeningCodes` |
| GET | `/api/GetPatientAttachment` | APIController | `GetPatientAttachment(system,pho,patientId,encounterId,referenceID="",sortOrder="dateDescend",Subject="",dateFrom=null,dateTo=null)` | query string | `Root<PatientDMS2>` (base64 attachment content) | Yes | 200 always | **Not documented at all** in `KARO_HSS_doc.md` — undocumented endpoint returning full attachment binary content as base64 |
| POST | `/api/SaveClinicalNotes` | APIController | `SaveClinicalNotes()` | JSON body → `ConsultNote` | `{status,message}` | Yes | 200 always | Matches doc `/saveClinicalNotes` |
| POST | `/api/SaveCondition` | APIController | `SaveCondition()` | JSON body → `Condition` | `{status,message}` (incl. special "already exists" success message) | Yes | 200 always | Matches doc `/saveCondition` |
| POST | `/api/SaveSummary` | APIController | `SaveSummary()` | raw JSON, dynamically parsed (`JObject`) | `{status}` or `{status:fail,message}` | Yes | 200 always | Matches doc's "Encounter Summary" POST samples (DIAP, DS:FS, DS:RET); schema-driven via `HSSDA.GetTemplateSchema` |
| POST | `/api/GetPatientAttachment` (see above, GET) | — | — | — | — | — | — | (listed once above) |
| POST | `/api/SaveDocument` | APIController | `SaveDocument()` | JSON body → `Document` (base64 `MessageData`) | `{status,message:<dmsGuid>}` | Yes | 200 always | Matches doc `/saveDocument`; saves to DMS via `SaveToDMS()` then indexes via `HSSDA.InsertDocument` |
| POST | `/api/SaveInvoice` | APIController | `SaveInvoice()` | JSON body → `Invoice` | `{status,message}` | Yes | 200 always | Matches doc `/saveInvoice` (doc has no response sample; code returns a service mapping ID or "Invoice already exists") |
| POST | `/api/SaveObservations` | APIController | `SaveObservations()` | JSON body → `Observations` | `{status,message}` | Yes | 200 always | Matches doc `/saveObservations`; code additionally **requires at least one screening value** or rejects the save (see `BusinessRules.md` BR-14) |
| POST | `/api/SaveRecall` | APIController | `SaveRecall()` | JSON body → `Recall` | `{status,message}` | Yes | 200 always | Matches doc `/saveRecall` |
| POST | `/api/SaveScreeningCode` | APIController | `SaveScreeningCode()` | raw JSON (parsed but discarded) | `{status:success,message:""}` always | Nominally yes, but **no `InsertAndValidateToken` call in this action at all** | 200 always | Doc's changelog references "SaveScreeningCodes" (v2.0.1) but the doc body has no dedicated section; **code is a stub — it logs the payload and returns success without persisting or validating the token** — see `BusinessRules.md` BR-15 and `SecurityAnalysis.md` |

**Total distinct controller actions: 24** (`Ping`, `Authenticate`×2, `GetClinicalNotes`, `GetConditions`, `GetDemographics`, `GetDocuments`, `GetEncounterSummary`, `GetLabResults`, `GetMedications`, `GetObservations`, `GetProvider`, `GetRecallCategories`, `GetRecalls`, `GetScreeningCodes`, `SaveClinicalNotes`, `SaveCondition`, `SaveSummary`, `GetPatientAttachment`, `SaveDocument`, `SaveInvoice`, `SaveObservations`, `SaveRecall`, `SaveScreeningCode`).

### Doc-vs-code cross reference summary
| In doc, in code | In doc, NOT confirmed in code | In code, NOT in doc |
|---|---|---|
| Ping, Authenticate(POST), GetDemographics, GetProvider, GetMedications, GetClinicalNotes(+Save), GetScreeningCodes, SaveSummary(DIAP/FS/RET), GetLabResults, GetDocuments(+Save), GetObservations(+Save), GetConditions(+Save), GetRecalls(+Save), GetRecallCategories, SaveInvoice | (none found — all documented ops have a corresponding action) | Authenticate(GET), GetEncounterSummary(GET), GetPatientAttachment(GET), SaveScreeningCode's actual no-op behavior |

Full detail and implications are in `DocumentationGap.md`.

## Evidence
See file/line citations inline in the table above; all drawn from `E:\NZTFS\hsswebapi\DevLocal\HSSWebAPI\Controllers\APIController.cs`.

## Risks
- Clients cannot rely on HTTP status codes for error handling (see Routing mechanics above) — any modern API gateway, health check, or retry policy built against standard REST semantics will misbehave against this API as-is.
- Two undocumented endpoints (`GetEncounterSummary`, `GetPatientAttachment`) return PHI or mock data with no specification to validate against; both must be reverse-engineered from the DB stored procedures during any rewrite.

## Recommendations
- Treat this table, not `KARO_HSS_doc.md`, as the authoritative endpoint list for the unified platform's design phase.
- Flag `GetEncounterSummary` and `SaveScreeningCode` for stakeholder clarification before porting — their current behavior is unlikely to be the intended behavior.
