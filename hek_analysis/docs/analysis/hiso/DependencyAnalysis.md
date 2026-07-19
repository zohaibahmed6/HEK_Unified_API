# HISO — Dependency Analysis

**Summary:** HISO's only inbound dependency is a SOAP-calling HISO/HealthLink form-engine
client; outbound it depends on multiple SQL Server databases, a proprietary Document
Management System (both direct-DB and SOAP-proxy paths), an AWS-backed document service, and
commercial Aspose document-generation libraries — there is no message queue, scheduler, or
background worker in this codebase.

## Findings

### Inbound
- **HISO/HealthLink form-engine client** — an external SOAP consumer of
  `FormSessionService.svc` implementing/expecting the HISO 10014.2 "Form Session" service
  contract (evidence: SOAP action namespaces `http://www.hiso.govt.nz/10014.2/1.0/formsession/...`
  in `FormSessionPortType.cs`). This is the NZ HISO clinical-forms standard; the specific
  client product is not identifiable from source alone.

### Outbound — Databases
| System | Access method | Purpose | Evidence |
|---|---|---|---|
| SQL Server `PMS_NZ_V2` (primary) | ADO.NET, stored procedures | Core practice-management data: patients, appointments, ACC45, tasks, sessions | `Web.config`, throughout DAL/Builders |
| SQL Server `PMS_NZ_V2` (second node) | ADO.NET, stored procedures | Reports/attachments/letters/problems (allow-listed procs) | `DAL/DBMessages.cs` |
| SQL Server `Indici_Master` | Configured, not observably used in this project's code | Unknown — likely used by `MHNEntity` or other PMS modules | `Web.config` |
| SQL Server `DMS_PMS` | ADO.NET, `[dbo].[uspDocumentSave]` | Direct document BLOB storage (DMS) | `Mapper.cs` `SaveDocumentToDMS` |

### Outbound — Document Management / Storage
- **`DMSProxy` (proprietary, external assembly)** — a SOAP/ASMX-style proxy client
  (`DMSProxy.DMSService.DocumentCollection`, `DMSProxy.DMSProxy.InstanceDMSProxy`) used when
  `AddDirectDMS` config is off, for both saving (`SaveDocument`) and retrieving
  (`GetDocumentData`) documents (evidence: `DocumentHandler.cs`, `Acc45DefinitionBuilder.cs`
  `GetACC45Definition`). Configured target: `Web.config` `DMSServiceURL =
  http://192.168.0.157/DMSService/ClientService.asmx` — **plain HTTP, internal IP**, not
  HTTPS.
- **`AWSDoc.IndiciDMS` (proprietary, external assembly)** — an AWS-backed document
  status/retrieval client used to check whether AWS storage is enabled per practice
  (`CheckAWSIsEnabled`), fetch document content by key
  (`DocumentGetByDocumentKeyJsonResult`), and get document status/MIME info
  (`GetDocumentStatusFromIndici`) (evidence: `DAL/DBMessages.cs`
  `ExecuteHisoProcedure`/`EnrichWithAWS`). Internal implementation and actual AWS
  service/region are **unable to verify from available source**.
- **Aspose.Words / Aspose.Pdf / Aspose.Cells** — commercial document-generation/conversion
  libraries used for HTML→PDF and image→PDF conversion of clinical form output
  (`ConceptMapper/TypeConverter.cs`), and PDF licensing (`FormSessionService.svc.cs`
  `getDeliveryOptions` loads `Aspose.Words.lic`/`Aspose.Pdf.lic` **on every call** — a
  potential performance concern, see below).

### Outbound — Other
- **Submission Gateway URL** (`Web.config` appSetting `URL`) — a configurable external
  submission endpoint referenced by `getDeliveryOptions` as `objdoResp.@return.URL`; currently
  blank in this config, with a commented-out example
  (`http://quantum.spectrumpms.nz:5088/SubmissionGateway`), suggesting this integration point
  exists but is not wired up/active in this environment.
- **`Billing.usptblConcept_GetByCode`** stored procedure (SQL, same primary DB) — used to
  resolve clinical codes for task subjects; not a separate external system but noted as a
  cross-module dependency within the PMS_NZ database.

### Background/async processing
**None found.** No Hangfire, Quartz, Windows Service, timer/cron, or `IHostedService`-style
background worker exists in this project. `ConceptMapper/HisoConceptDetail.cs`
`GetProcedureList` does use `Parallel.ForEach` to execute multiple stored procedures
concurrently within a single request (evidence: lines 85-105), but this is in-request
parallelism, not a background job.

## Risks
- The DMS proxy service is configured over **plain HTTP** to an internal IP
  (`http://192.168.0.157/DMSService/ClientService.asmx`) — if this traverses any
  non-trusted network segment, document content (potentially clinical) is exposed in transit.
- Loading Aspose license files (`SetLicense("Aspose.Words.lic")` /
  `SetLicense("Aspose.Pdf.lic")`) on every single `getDeliveryOptions` call
  (`FormSessionService.svc.cs` lines 72-75) is unnecessary per-call overhead — licenses should
  be set once at application startup.
- `AWSDoc`/`DMSProxy`/`Indici_Master` integrations are all opaque external dependencies whose
  internal behavior, versioning, and failure modes cannot be assessed from this codebase
  alone — a real risk for migration planning since their contracts must be treated as black
  boxes until their source/documentation is available.
- The unused/blank `SubmissionURL`/`URL` HISO submission-gateway integration suggests either
  a decommissioned feature or an incomplete one; must be clarified with the business.

## Recommendations
- Confirm whether the DMS proxy channel should be upgraded to HTTPS as part of any
  continued/migrated use.
- Move Aspose license loading to application startup (a quick low-risk fix if this service
  continues to run as-is during a transition period).
- Treat `DMSProxy`, `AWSDoc`, and `MHNEntity` as dependencies requiring their own review before
  the unified platform migration proceeds, since this Phase 1 review could not inspect their
  source.
- Clarify the status of the HISO Submission Gateway integration (active/decommissioned) with
  the business before deciding whether to carry it forward.
