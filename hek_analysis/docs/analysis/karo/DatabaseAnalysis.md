# KARO — Database Analysis

**Summary:** KARO's reachable data layer talks to Microsoft SQL Server exclusively through parameterized stored-procedure calls (`[HSS]` schema of a PMS database plus a separate DMS database), but the wider DAL library it ships alongside connects to at least 10 additional databases/schemas and contains several raw string-concatenated SQL statements outside KARO's own reachable code path.

## Findings

### Database engine
Microsoft SQL Server (evidenced by `System.Data.SqlClient`, `SqlConnection`, T-SQL bracketed schema/object syntax `[HSS].[uspGetConditions]`, and SQL Server 2014-style instance names in `Web.config`, e.g. `Data Source=dbserver\sql2014`).

### Connection strings (redacted)
| Name | Purpose (inferred) | Credential status |
|---|---|---|
| `ConnIndiciDB` (+ optional practice suffix, e.g. `_485`, `_128`, `_301`, `_901_FZZ999-B`, `_LOCAL`) | Primary Indici PMS database per practice/environment (`PMS_NZ*`) | Credential present, redacted here — plaintext in `Web.config` lines 15, 18, 21, 23, 26, 29 |
| `ConnDMSDB` (+ same suffix pattern) | Document Management System database (`DMS_PMS`) | Credential present, redacted — `Web.config` lines 16, 19, 24, 27, 30 |
| `ConnBPI`, `ConnGP2GP`, `ConnMHN`, `ConnMHNBPAC`, `ConnMHNDMS`, `ConnMHNDataMigration`, `ConnMHNHL7`, `ConnMHNPMS`, `ConnMasterDatabase`, `ConnulmDB` | Referenced by other DAL modules (BPI, GP2GP, MHNAppointment, MHNHL7, DMS, Screening/UI) not called by KARO's controller | Not declared in this project's `Web.config` at all — `ConfigurationManager.ConnectionStrings["ConnMHN..."]` calls in those DAL files would throw `NullReferenceException` if invoked in this deployment, confirming these modules are not wired for KARO |

> Note: `Web.config` also contains a **second, commented-out set** of `ConnIndiciDB`/`ConnDMSDB` values pointing to different hosts (`dbserver\sql2014`, `VTAPS25`) alongside the live ones pointing to `43.255.162.58` — evidence of at least one prior environment migration left as dead config (`Web.config` lines 12-16).

### Schema / stored procedures actually reachable from KARO (`DAL\South\HSSDA.cs`)
All under the `[HSS]` schema of the `ConnIndiciDB*` database unless noted; representative sample (not exhaustive — ~35 total):
- Auth/session: `[HSS].[uspInsertAndValidateToken]`
- Demographics/clinical: `[HSS].[uspGetDemographics]`, `[HSS].[uspGetConditions]`, `[HSS].[uspGetConsultNotes]`, `[HSS].[uspGetLabResults]`, `[HSS].[uspGetAllergies]`, `[HSS].[uspGetACC45]`, `[HSS].[uspGetDocResults]`
- Documents: `[HSS].[uspGetDocuments]`, `[HSS].[uspGetDocuments_AWS]` (AWS-migrated practices)
- DMS (separate `ConnDMSDB*` database, `[dbo]` schema): `[dbo].[uspDocumentSave]`, `[dbo].[uspDocumentDelete]`, `[dbo].[uspUpdateExistingDoc]`
- Writes (referenced by name in `APIController.cs`/`HSSDA.cs`, procedure definitions not in source): `InsertUpdateConsultNotes`, `InsertUpdateDiagnosis`, `InsertSummary`, `InsertDocument`, `HSSInsertUpdateService`, `InsertUpdateObservation`, `InsertUpdateRecall`, `GetTemplateSchema`

> Unable to verify from available source: the actual T-SQL definitions of these stored procedures, their internal table structure, indexes, or constraints — no `.sql` script files were found in the repository; all procedure bodies live in the database itself, outside the code tree provided.

### Schema/stored procedures used by OTHER (not-KARO-reachable) DAL modules
Grouped by DAL subfolder, sampled via `grep` for `usp*` procedure name literals:
| DAL module | Connection string | Sample procedures | Apparent purpose |
|---|---|---|---|
| BPAC (`BPACDA.cs`) | `ConnMHNBPAC` (inferred) | `[Appointment].[uspACC18GetAll]`, `[Appointment].[uspGetDemographicPMS]`, `CommonForm.uspGetConformance`, `Profile.uspGetPatientDetailWKT` | A different/older PMS integration ("PMS" naming, ACC claims, common-form clinical data) — overlaps conceptually with `HSSDA` but is a distinct, separately-versioned code path |
| BPI (`BPIDA.cs`) | `ConnBPI` | `[localBCP].[uspPatientExtract]`, `[localBCP].[uspSCNCOMBO]`, `localBCP.uspNIRINLINE` | Bulk/batch patient extract, likely National Immunisation Register (NIR) related ("NIRINLINE") |
| DMS (`DMSDA.cs`) | `ConnMHNPMS`/`ConnMHNDMS` | `[dbo].[uspHL7SaveInbox]`, `[dbo].[uspDocumentSave]`, `[Task].[uspTaskPathLabInsertUpdate]`, `[Profile].[uspGetOrganizationByEDI]` | HL7 inbox handling + document storage for a different (MHN) system, plus raw string-built SQL (see Risks) |
| GP2GP (`PIDAO.cs`) | not directly declared in this project's config | `[GP2GP].[uspGetPatientDemography]`, `[GP2GP].[uspGetPatientAuthorAndCustodian]` | UK-style GP2GP clinical record transfer format support |
| MHNAppointment (`MHNAppointmentDA.cs`) | `ConnMHNDataMigration` | `[mhn].[uspInsertAppointmentData]`, `[mhn].[uspInsertHL7Data]`, `mhn.uspAuthenticateUser`, `mhn.uspAuthenticateUserSession` | Appointment/HL7 data migration tooling with its own authentication procs |
| MHNHL7 (`DBMessages.cs`) | `ConnMHNPMS`/`ConnMHN` | `[dbo].[uspInsertMessageHeader]`, `[dbo].[uspInsertPrescriptionMessage]`, `Config.uspInsertAFData`, `Config.uspServiceTemplateScreeningMapping` | HL7 message header/prescription handling, screening template configuration |
| Pegasus (`PHCO.cs`) | `ConnIndiciDB*` (shared with HSSDA) | `[OnlineClaim].[uspGetConditions]`, `[OnlineClaim].[uspGetPatientData]`, `[OnlineClaim].[uspGetProvider]`, `[OnlineClaim].[uspGetSurgeryData]` | A **separate online-claims integration** reading from the same PMS database via a different schema (`OnlineClaim`) — parallel/sibling API to HSS |
| Procare / Procon (`ProcareApiDA.cs`, `ProconApiDA.cs`) | `ConnIndiciDB*` | `[Auth].[uspProcareValidateToken]`, `[Admin].[uspProcareGetPracticeByPatientD]`, `[HSS].[uspGetACC45]`, `[HSS].[uspGetAllergies]`, `[HSS].[uspGetLabResults]` | Two **near-duplicate** integrations reusing much of the same `[HSS]` schema procedures as KARO itself, with their own `[Auth]`/`[Admin]` schemas for token validation — evidence of copy-pasted sibling APIs for different portal vendors |
| Screening (`ScreeningDAL.cs`) | not directly declared in this project's config | `[dbo].[uspGetScreeningTemplate]`, `[dbo].[uspSaveScreeningJSonData]`, `[dbo].[uspGetEthnicGroup]` | Screening template configuration/data capture, JSON-based |
| UI (`GUIDA.cs`) | uses `Hashtable` sqlParams pattern (older style) | `[dbo].[uspGetMessageHeader]`, `[dbo].[uspGetFieldValues]` | Small admin/config UI support class |

### Data access pattern
- Standard shape across `HSSDA.cs`: build `List<SqlParameter>`, call `DALHelper.ExecuteDataTable`/`ExecuteNonQuery`/`ExecuteScalar(connectionString, CommandType.StoredProcedure, procName, sqlParams.ToArray())`, wrap in try/catch that sets an `out string error` and swallows the exception (`DAL\South\HSSDA.cs`, consistent pattern across ~35 methods, e.g. lines 118-146).
- Optional parameters are conditionally added only if non-empty (`if (!string.IsNullOrWhiteSpace(sortOrder)) sqlParams.Add(...)`), relying on the stored procedure to have matching optional/default parameters.
- Output parameters used for returning generated IDs (`SqlParameter sqlParamOut = new SqlParameter("@pDocumentIDOut", SqlDbType.Int) { Direction = ParameterDirection.Output }`, `DAL\South\HSSDA.cs` lines 103-106).
- Table-valued parameters (`SqlDbType.Structured`) used in at least one MHNHL7 method for bulk insert (`DAL\MHNHL7\DBMessages.cs` line 887, `sqlParams[0] = new SqlParameter("@ptblAF", SqlDbType.Structured)`).

### Soft-delete / audit patterns
- `IsDeleted`/`IsActive` flags observed in raw SQL text in `DBMessages.cs` (`... and isnull(IsDeleted,0)=0 and IsActive=1 ...`, line 827) — indicates the underlying schema (at least for `Appointment.tblScreening`) uses a soft-delete convention, but this was only directly observable where raw SQL text was present; stored-procedure-hidden logic (the majority of the codebase) could not be inspected for the same convention.
> Unable to verify from available source whether `[HSS]` schema tables used by KARO follow the same soft-delete convention, since their access is entirely through opaque stored procedures.
- No explicit audit-trail table/pattern (e.g., `CreatedBy`/`ModifiedBy`/`RowVersion` columns) was observable from the C# code, since virtually all writes go through stored procedures whose bodies are not in this repository.

### Multi-database transactions
No evidence of distributed transactions (`TransactionScope`, `System.Transactions`) anywhere in the DAL. `SaveDocument` writes to the DMS database first, then the PMS database second, as two independent, non-atomic operations (`Controllers\APIController.cs` lines 1669-1673) — see `BusinessRules.md` BR-15.

## Evidence
Citations inline above; primary files: `E:\NZTFS\hsswebapi\DevLocal\DAL\South\HSSDA.cs`, `E:\NZTFS\hsswebapi\DevLocal\DAL\HelperClasses\DALHelper.cs`, `E:\NZTFS\hsswebapi\DevLocal\HSSWebAPI\Web.config`, and the other DAL subfolder files listed above.

## Risks
- Because so much of the schema/business logic lives in stored procedures not present in this repository, a faithful behavioral migration (rather than a reimplementation from an assumed spec) is only possible with direct database access to extract and review the actual procedure definitions — this analysis could only characterize inputs/outputs/return-code contracts as observed from the calling C# code.
- The DMS+PMS two-step, non-atomic write in `SaveDocument` (BR-15) is a data-consistency risk: a DMS save success followed by a PMS index failure leaves an orphaned document with no patient-facing record of it, or vice versa.
- Multiple sibling integrations (Pegasus, Procare, Procon) read/write the same `[HSS]` schema tables via different, independently-versioned DAL classes — any schema change made "for KARO" could silently break these other, unseen integrations, and vice versa.

## Recommendations
- Obtain and review the actual stored procedure definitions (via DB scripting/SSDT project export or `sys.sql_modules`) before finalizing the unified platform's data model — this document should be treated as a map of *entry points*, not a full schema documentation.
- When designing the unified platform's data layer, decide explicitly whether Pegasus/Procare/Procon's shared use of `[HSS]` schema objects needs to be preserved, coordinated, or intentionally decoupled, since they are currently coupled by accident (shared DAL/shared schema) rather than by design.
