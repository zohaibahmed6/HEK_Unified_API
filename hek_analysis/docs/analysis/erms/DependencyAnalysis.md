# ERMS Web API — Dependency Analysis

## Summary
ERMS's two public controllers only exercise two DAL classes (`HSSDA` for the ERMS/HISO side, `PHCO` for the COL/Pegasus side) plus a hand-rolled AWS document-storage abstraction and a same-solution "Azure ERMS API" mirror; the shared `DAL` project additionally ships nine other integration modules (GP2GP, MHNHL7, MHNAppointment, Procare, Procon, BPAC, BPI, Screening, DMS) that are **not called by ERMS's controllers** and appear to belong to sibling application(s) sharing the same DAL assembly.

## Findings

### Integrations actually reachable from ERMS's public endpoints

| Integration | Purpose (from code) | Reachable via | Evidence |
|---|---|---|---|
| Indici PMS ("South"/HSS schema) | Core clinical data source — demographics, conditions, medications, labs, radiology, consult notes, accidents, next-of-kin, documents, tokens | `DAL/South/HSSDA.cs`, called by every `APIController` action | ~35 stored procs, see DatabaseAnalysis.md |
| Indici PMS ("Pegasus"/OnlineClaim schema) | Patient/session/provider/surgery/diagnosis data and invoice write-back for the COL/Pegasus consumer | `DAL/Pegasus/PHCO.cs`, called by every `COLController` action | `OnlineClaim.uspGet*`, `OnlineClaim.uspInsertUpdateService` |
| DMS (Document Management System) | Stores/retrieves referral and scanned documents; legacy on-prem SQL-backed path | `HSSDA.DocumentSave`/`DocumentDelete`/`GetDocResults`/`GetOtherDocs` against `ConnDMSDB<practiceid>` | `Web.config` connection strings, `HSSDA.cs` |
| AWS-backed document storage (`AWSDoc` assembly) | Per-practice opt-in alternative to on-prem DMS for document storage; queried via `AWSDoc.IndiciDMS.CheckAWSIsEnabled` | `HSSDA.UpdateExistingDocument`, `HSSDA.GetDocResults`/`GetOtherDocs` `_AWS` variants | `DAL.csproj` `<Reference Include="AWSDoc" ...><HintPath>bin\Debug\AWSDoc.dll</HintPath>`; `HSSDA.cs` lines 53-68 |
| "Azure ERMS API" (self, mirrored deployment) | A second deployment of this same API (implied by endpoint-path forwarding, e.g. `/api/GetAccidents`) hosted at `AppSettings["AzureEMRSAPI"]` (observed value `https://deverms.vitonta.com`); requests for practices flagged with a `"...azure"` suffix in their encoded EncounterId are transparently proxied there instead of served locally | `Helpers/ERMSAPIProxy.cs`, gated by `AppSettings["EnableAzureERMSAPI"]` | See BusinessRules.md BR-04 |

### DAL modules present but NOT called by APIController/COLController (in this codebase)

| Module | Class | Apparent purpose (from code inspection) | Called by ERMS controllers? |
|---|---|---|---|
| BPAC | `DAL.BPACDA` (`BPAC/BPACDA.cs`, 1991 lines) | Large standalone data-access class; not referenced by ERMS controllers | No |
| BPI | `DAL.BPI.BPIDA` (`BPI/BPIDA.cs`, 1020 lines) | Standalone data-access class | No |
| GP2GP | `DAL.GP2GP.PIDAO` (`GP2GP/PIDAO.cs`) | GP-to-GP patient record transfer (NZ/UK health data portability standard) — data-access only | No |
| MHNAppointment | `DAL.MHNAppointment.MHNAppointmentDA` | Appointment-related data access, namespace prefix `MHN` suggests a different parent application | No |
| MHNHL7 | `DAL.MHNHL7.DBMessages` (1052 lines) | HL7 message handling/screening template config — contains inline string-concatenated SQL (SecurityAnalysis.md SEC-03) | No |
| Procare | `DAL.ProcareApiDA` (1733 lines) | Standalone API-style data-access class named "Procare" (a NZ PHO/provider network) | No |
| Procon | `DAL.ProconApiDA` (1653 lines) | Standalone API-style data-access class named "Procon" | No |
| Screening | `DAL.ScreeningDAL` | Screening program data access | No |
| DMS (PdfSharp-based) | `DAL.DMS.DMSDA` (482 lines) | PDF generation/manipulation for documents; contains inline string-concatenated SQL (SecurityAnalysis.md SEC-03) | No |
| DMSAWS | `MHN.DAL.DMSAWS.DMSAWS` | AWS document download/status abstraction; namespace is `MHN.DAL.DMSAWS`, not `DAL.*`, and references `MHN.Entity.Inbox`/`MHN.DAL.Extentions`/`NLog` — strongly suggests this file originates from a different application ("MHN") and was copied into this DAL project | No (ERMS uses `AWSDoc.IndiciDMS` directly from `HSSDA.cs`, not this class) |
| UI | `DAL.UI.GUIDA` | Generic/UI-support data access; uses the static, non-thread-safe `DbAccess` class (DatabaseAnalysis.md) | No |

> Assumption: these modules are shared with one or more sibling applications (possibly HISO, KARO, or an internal admin/portal tool not in scope for this analysis) that reference the same `DAL.dll`. This cannot be confirmed without visibility into those other codebases; the Phase 4 cross-API comparison should verify this directly, since it materially affects how much of `DAL` can be safely deprecated when ERMS is migrated versus how much must be preserved for other consumers.

### Message queues / SMTP / cloud storage
- No message queue (MSMQ, RabbitMQ, Azure Service Bus) usage found anywhere in the reviewed source.
- No SMTP/email sending found in the reviewed source.
- AWS is used **only** for document storage via the `AWSDoc` assembly (S3-backed, per the `DocumentDownlaodInfo`/`DocumentManager.Download`/`DocumentManager` API surface referenced in `DMSAWS.cs`); no other AWS services are referenced.
- No Redis or other distributed cache found.

### External HTTP dependency
- `ERMSAPIProxy.cs` makes outbound `HttpClient` calls to `AppSettings["AzureEMRSAPI"]` — the only outbound HTTP call in the reviewed codebase, and it is a peer/mirror instance of this same API, not a third-party integration.

## Evidence
- `E:\NZTFS\ermsapi\DevLocal\DAL\*` (directory structure and class/namespace headers, per-subfolder `grep`)
- `E:\NZTFS\ermsapi\DevLocal\DAL\DAL.csproj` (`AWSDoc` reference)
- `E:\NZTFS\ermsapi\DevLocal\ERMSWebAPI\ERMSWebAPI\Helpers\ERMSAPIProxy.cs`
- `E:\NZTFS\ermsapi\DevLocal\ERMSWebAPI\ERMSWebAPI\Web.config` (`AzureEMRSAPI`, `EnableAzureERMSAPI` app settings)

## Risks
- Nine unused-by-ERMS DAL modules (several 1000+ lines each, ~9,600 lines total) inflate the perceived scope of "the ERMS codebase" and could lead to wasted migration effort if not correctly scoped out — or, conversely, to a broken sibling application if incorrectly assumed to be ERMS-exclusive and dropped.
- The "Azure ERMS API" mirror is an undocumented piece of the system's topology; if it is still live, any migration plan must account for whichever practices are currently routed there, or risk silently losing data access for those practices.
- `DMSAWS.cs`'s cross-application namespace (`MHN.*`) is direct evidence that this DAL project's boundaries do not cleanly map to "ERMS" — a red flag for assuming clean extraction.

## Recommendations
- Before Phase 4 (cross-API comparison), explicitly ask the client which applications consume the shared `DAL` project besides ERMS, and confirm whether "Azure ERMS API" is a currently-live deployment or decommissioned.
- Scope the unified platform's ERMS-equivalent module to only `HSSDA`/`PHCO`'s reachable surface (stored procedures listed in DatabaseAnalysis.md) plus the AWS/DMS document flow — do not assume the rest of `DAL` needs to be ported as part of ERMS migration.
