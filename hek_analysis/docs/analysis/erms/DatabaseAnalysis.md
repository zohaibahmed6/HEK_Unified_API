# ERMS Web API — Database Analysis

## Summary
ERMS accesses SQL Server exclusively through parameterized stored procedures (no raw SQL, no LINQ-to-SQL/EF in the request path) split across two schemas (`HSS` and `dbo` on the "Indici" database, `OnlineClaim` for COL/Pegasus-related writes), with a distinct DMS database for documents, and per-practice connection-string routing rather than a single multi-tenant connection.

## Findings

### Database engine & access pattern
- Engine: Microsoft SQL Server (`System.Data.SqlClient`).
- Access pattern: **stored procedures only**, called via `DAL/HelperClasses/DALHelper.cs` (`ExecuteDataTable`, `ExecuteNonQuery`, `ExecuteDataset`), all with `CommandType.StoredProcedure` and `SqlParameter` objects — evidence: `E:\NZTFS\ermsapi\DevLocal\DAL\South\HSSDA.cs` (every call site uses `sqlParams.Add(new SqlParameter(...))`).
- Not Code First / not DB First EF despite `EntityFramework`/`EntityFramework.SqlServer` being referenced (see TechnologyStack.md) — that package reference is unused dead weight for the request path actually exercised by `APIController`/`COLController`.

### Multi-database / multi-tenant topology
Connection strings are resolved dynamically at runtime by string-concatenating a base name with a `practiceid` suffix pulled out of the request (see BusinessRules.md BR-01):
```csharp
string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB" + practiceid].ConnectionString);
```
Evidence: `E:\NZTFS\ermsapi\DevLocal\DAL\South\HSSDA.cs`, ~30 call sites (lines 45, 149, 182, 216, 244, 271, 369, 391, 466, 497, ...), and `E:\NZTFS\ermsapi\DevLocal\DAL\Pegasus\PHCO.cs` line 111.

`Web.config` defines 15 connection strings in 7 practice-keyed pairs (`ConnIndiciDB<suffix>` / `ConnDMSDB<suffix>`) plus one unsuffixed default pair:
| Suffix | Indici DB | DMS DB | Host |
|---|---|---|---|
| (none) | `PMS_NZ_Local_NZTFS` | `DMS_PMS` | `dbserver-local` |
| `_128` | (same, `_128` alias) | (same) | `dbserver-local` |
| `_485` | `DMS_PMS_EPX` | `DMS_PMS` | `43.255.162.58` |
| `_249` | `DMS_PMS_EPX` | `DMS_PMS` | `43.255.162.58` |
| `_491` | `DMS_PMS_EPX` | `DMS_PMS_EPX` | `43.255.162.58` |
| `_228` | `PMS_NZ_EPX` | `DMS_PMS_EPX` | `43.255.162.58` |
| `_901_FZZ999-B` | `PMS_NZ_V2` | `DMS_PMS` | `dbserver-local` |

This design means **the application's database topology is hard-coded per practice** in `Web.config` — onboarding a new practice requires an application config change/redeploy, not a data-driven registration. Evidence: `Web.config` `<connectionStrings>`.

### Stored procedures called (HSS/dbo/OnlineClaim schemas, via `HSSDA`/`PHCO`)
Read (Get): `uspGetACC45`, `uspGetAllergies`, `uspGetConditions`, `uspGetConsultNotes`, `uspGetDemographics`, `uspGetDocResults` / `uspGetDocResults_AWS`, `uspGetDocuments`, `uspGetLabResults`, `uspGetLabs`, `uspGetMeasurement`, `uspGetMedications`, `uspGetNextOfKin`, `uspGetObservations`, `uspGetOtherDocs` / `uspGetOtherDocs_AWS`, `uspGetPatientDMS`, `uspGetProvider`, `uspGetRadResults`, `uspGetRads`, `uspGetRecallCategories`, `uspGetRecalls`, `uspGetRegisteredPractitioners`, `uspGetScreeningCodes`, `uspGetSmokingStatus`, `uspGetTemplateSchema`; `OnlineClaim.uspGetConditions`, `OnlineClaim.uspGetPatientData`, `OnlineClaim.uspGetProvider`, `OnlineClaim.uspGetSurgeryData` (Pegasus/COL side).

Write (Insert/Update/Delete): `uspInsertAndValidateToken` (auth), `uspInsertDocument`, `uspInsertSummary`, `uspInsertUpdateConsultNotes`, `uspInsertUpdateDiagnosis`, `uspInsertUpdateObservation`, `uspInsertUpdateRecall`, `uspInsertUpdateService` (also `OnlineClaim.uspInsertUpdateService` — used by `SaveInvoice`), `uspUpdateExistingDoc` / `uspUpdateExistingDoc_AWS`, `dbo.uspDocumentDelete`, `dbo.uspDocumentSave`.

> Assumption: table/view/index/FK-level schema (the underlying tables these procedures read/write) is **not visible from this codebase** — no `.sql` scripts or schema definitions were found in the read scope. This report cannot enumerate actual tables, PKs/FKs, or indexes; only the stored-procedure surface consumed by the API is confirmed.

### AWS/on-prem document storage duality
`HSS.uspGetDocResults` has an `_AWS` counterpart, as does `uspGetOtherDocs` and `uspUpdateExistingDoc`, gated at runtime by `AWSDoc.IndiciDMS.CheckAWSIsEnabled(practiceId, connectionString)` — a **per-practice, per-request** database call that determines a second database call's target. Evidence: `E:\NZTFS\ermsapi\DevLocal\DAL\South\HSSDA.cs` lines 53-68, 271-311 (`GetDocResults` branching), 664-696 (`GetOtherDocs` branching).

### Soft-delete / audit patterns
> Unable to verify from available source — no `IsDeleted`/`IsActive`/audit-column handling is visible in the C# call sites for the ERMS-facing procedures (parameters are passed by name to opaque stored procedures whose bodies are not in this codebase). One adjacent DAL file (`MHNHL7/DBMessages.cs`, not called by ERMS controllers) does reference `isnull(IsDeleted,0)=0` in inline SQL, suggesting the broader Indici database does use soft-delete conventions, but this cannot be confirmed for the tables backing ERMS's own procedures without DB access.

### Connection lifetime management
`DALHelper.ExecuteDataTable`/`ExecuteNonQuery` (the path used by `HSSDA`/`PHCO`, and therefore by both ERMS controllers) correctly opens connections in a `using (SqlConnection connection = new SqlConnection(connectionString))` block per call — evidence: `E:\NZTFS\ermsapi\DevLocal\DAL\HelperClasses\DALHelper.cs` lines 315, 338, 557, 979, 1267, 1289, 1321. This is safe for concurrency.

However, a **separate, older data-access class** (`DAL/HelperClasses/DbAccess.cs`) uses `private static SqlConnection DbConn`, `private static SqlCommand DbCommand`, `private static SqlDataAdapter DbAdapter` — **one shared, mutable, static connection/command/adapter for the entire application process**. It is only consumed by `DAL/UI/GUIDA.cs` in this codebase (not by `HSSDA`/`PHCO`, so not directly reachable from the two ERMS controllers), but if `GUIDA` or a similar pattern is reused anywhere reachable from a public endpoint, this would cause cross-request data corruption / connection-state races under concurrent load — a direct threat to the "10,000 concurrent users" target for the unified platform. Evidence: `E:\NZTFS\ermsapi\DevLocal\DAL\HelperClasses\DbAccess.cs` lines 12-17.

## Evidence
- `E:\NZTFS\ermsapi\DevLocal\DAL\South\HSSDA.cs`
- `E:\NZTFS\ermsapi\DevLocal\DAL\Pegasus\PHCO.cs`
- `E:\NZTFS\ermsapi\DevLocal\DAL\HelperClasses\DALHelper.cs`, `DbAccess.cs`
- `E:\NZTFS\ermsapi\DevLocal\ERMSWebAPI\ERMSWebAPI\Web.config` (`<connectionStrings>`)

## Risks
- Hard-coded, per-practice connection strings mean the unified platform must either replicate this pattern (bad for scale/maintainability) or design a proper multi-tenant data routing layer as part of consolidation — this is a structural decision, not a code-level fix.
- No visibility into the actual table/schema design behind ~40 stored procedures is a significant blind spot for Phase 1; a full understanding of ERMS's data model requires either DBA access or the stored procedure source, neither of which was provided.
- The static `DbAccess` class, even though not reachable from the two ERMS controllers today, is a latent landmine if DAL code is refactored/reused during consolidation without removing it.
- Duplicated Get/Insert logic for AWS vs. on-prem DMS (parallel procedure pairs) doubles the surface area that must be understood and tested during migration.

## Recommendations
- Request the SQL stored-procedure source (or DBA access) for the `HSS`, `dbo`, and `OnlineClaim` schemas before Phase 2/3 data-model design — this report's DB understanding is capped at the C#-visible call contract.
- Do not carry the per-practice `Web.config` connection-string pattern into the unified platform; replace with a single multi-tenant connection + tenant/practice column-based isolation or equivalent, decided in Phase 4.
- Flag and remove (or fence off) `DbAccess.cs`'s static-state pattern during any DAL consolidation work, regardless of whether it is currently reachable from ERMS.
