# HISO — Database Analysis

**Summary:** HISO talks to Microsoft SQL Server exclusively through stored procedures called
via plain ADO.NET (no ORM), across four configured connection strings/databases, with no
visible soft-delete or audit-table pattern beyond `IsActive`/`IsDeleted`/`InsertedBy`/`UpdatedBy`/`InsertedAt`/`UpdatedAt` columns passed into table-valued parameters.

## Findings

### Database engine
Microsoft SQL Server (`providerName="System.Data.SqlClient"`), evidence: `Web.config`
`<connectionStrings>`.

### Configured connections
| Name | Server / Catalog | Purpose | Evidence |
|---|---|---|---|
| `ConectionStringPMS_NZ` | `dbserver-local` / `PMS_NZ_V2` | Primary practice-management database (patients, appointments, ACC45, tasks, sessions) | `Web.config` line 69 |
| `ConectionStringPMS_NZ_SecondNode` | `dbserver-local` / `PMS_NZ_V2` (same server/DB as primary in this environment) | Used for a specific allow-listed set of report/attachment/letter/problem procedures (BR-05) — likely points to a different physical node in production (read replica) even though dev config shows the same value | `Web.config` line 70; `DAL/DBMessages.cs` `CreateConnectionSecondNode` |
| `ConectionStringIndici_Master` | `192.168.0.6\sql2014` / `Indici_Master` | Configured but **no usage found** in the source files reviewed — possibly used by `MHNEntity` (out of scope) | `Web.config` line 71 |
| `ConectionStringPMS_NZ_DMS` | `dbserver-local` / `DMS_PMS` | Document Management System database (direct document BLOB storage) | `Web.config` line 72; `Mapper.cs`, `SaveDocumentToDMS` |

> Credentials are present in plaintext in `Web.config` for all four connections (same
> `pms_nz` / `pms@@nz` account reused across all databases) — **credential present, redacted
> in this report; see `SecurityAnalysis.md` for the finding.**

### Data access pattern
- **No ORM** (no Entity Framework/Dapper/NHibernate usage found). All data access is raw
  `System.Data.SqlClient` (`SqlConnection`/`SqlCommand`/`SqlDataAdapter`/`SqlDataReader`).
- Two internal helper layers exist:
  - `DAL/DALHelper.cs` — a classic "Microsoft Data Access Application Block"-style helper
    (`ExecuteDataset`, `ExecuteDataTable`, `ExecuteNonQuery`, `ExecuteReader`, `ExecuteScalar`,
    `ExecuteXmlReader`, `FillDataset`) that **only executes parameterized stored procedures or
    parameterized text commands** — no evidence of raw string-concatenated SQL being executed
    through this helper.
  - `DAL/DbAccess.cs` — an older, static-connection-based helper offering both stored
    procedure execution (parameterized) **and free-text SQL execution methods**
    (`selectQuery`, `executeQuery`, `GetColumnValue`, `eligibility_Reader`,
    `executeScalarQuery`, `executeDataReader`) that accept a raw SQL string from the caller.
    **No call site in this codebase was found that builds these raw strings via
    concatenation of externally-supplied input** (the class exists but its query-string
    methods do not appear to be invoked in the reviewed `.cs` files) — flagged as a latent
    risk if any caller outside this reviewed tree (e.g., MHNEntity) passes user input into
    `DbAccess.executeQuery`/`selectQuery`.
  - The majority of business logic classes bypass both helpers and open `SqlConnection`/
    `SqlCommand` directly inline (e.g., `Mapper.cs`, `Task.cs`, `Acc45DefinitionBuilder.cs`,
    `PatientBuilder.cs`), always using parameterized `SqlParameter` objects for values — **no
    string-concatenated SQL commands were found anywhere in the reviewed source.**
- Table-Valued Parameters (TVPs) are used extensively to pass whole `DataTable`s (built by
  the Builder classes) into stored procedures in a single call, e.g.
  `cmd.Parameters.Add(new SqlParameter("@tblACC45Detail", dt))` (evidence: `Mapper.cs`
  `SaveAccidentInformation`; `Acc45DefinitionBuilder.cs` `SaveAcc45Definition`; `PatientBuilder.cs`
  `Save`; `PatientEmployerOrganisationBuilder.cs` `Save`).
- `DAL/DALHelperParameterCache.cs` caches `SqlCommandBuilder.DeriveParameters` results per
  `connectionString:commandText` key in a `Hashtable.Synchronized` — an optimization to avoid
  repeated stored-procedure metadata discovery.

### Stored procedures referenced (by schema.name, as found in source)
| Procedure | Purpose | Evidence |
|---|---|---|
| `[Hiso].[UspGetHisoConcepts]` | Load the concept dictionary | `DAL/DBMessages.cs` |
| `[Hiso].[USPGetProcedureParamList]` | Get expected parameter list for a dynamic procedure | `DAL/DBMessages.cs` |
| `hiso.uspGetPatient_LaboratoryReport` / `_RadiologyReport` / `_Attachment` / `_IncomingLetter` / `_OutgoingLetter` / `_OutgoingLetter_Author` / `_Problem` (+ `_AWS` variants) | Dynamic patient-document/report retrieval (second-node + AWS routing) | `DAL/DBMessages.cs` |
| `Appointment.usptblHealthLinkSession_GetByGUID` | Resolve session GUID → context | `Mapper.cs` |
| `Appointment.uspGetHLFormDetailPMS` | Bulk static-mode form detail (referrer, patient, disabilities, settings, practitioners) | `Mapper.cs` `FillXml` |
| `Appointment.uspGetHLFormMeasurmentsDetailPMS` | Patient measurement groups (BP, weight, HbA1c, eGFR, etc.) | `Mapper.cs` `GetPatientMeasurements` |
| `Appointment.uspGetHL7ConceptMapping` | Concept-name → column-name mapping table used by all Builders | `Mapper.cs` `GetConceptMappingTable` |
| `Appointment.uspGetHLFormSocialHistoryDetailPMS`, `_SurgicalHistoryDetailPMS`, `_FamilyHistoryDetailPMS`, `_PatientProblemDetailPMS`, `_LabReportsDetailPMS`, `_RadReportsDetailPMS`, `_WarningsDetailPMS`, `_AccidentsDetailPMS`, `_PrescribedMedicationDetailPMS`, `_LongtermMedicationDetailPMS`, `_GeneratedDocsDetailPMS`, `_UploadedDocsDetailPMS`, `_PatientPostalAddressPMS`, `_PatientResidentialAddressPMS`, `_DiagnosisDetailPMS` | Group-level dynamic data retrieval per HISO group concept | `Mapper.cs` `GetGroupTableByGroup` |
| `Appointment.usptblACC45Detail_InsertUpdate_New` | Save ACC45 accident detail + diagnosis + referral (TVPs) | `Mapper.cs` `SaveAccidentInformation` |
| `Appointment.usptblACC45DefinitionInsert` | Save ACC45 form definition/view metadata | `Acc45DefinitionBuilder.cs` `SaveAcc45Definition` |
| `Appointment.usptblACC45XML_Get` | Retrieve stored ACC45 XML + provider/profile change tracking | `Acc45DefinitionBuilder.cs` `GetFormXML` |
| `Appointment.usptblACC45Definition_Get` | Retrieve ACC45 form definition/view for resuming a form | `Acc45DefinitionBuilder.cs` `GetACC45Definition` |
| `[Hiso].[uspPatient_Update]` | Save patient demographic writeback | `PatientBuilder.cs` `Save` |
| `[Hiso].[uspPatientEmployerOrganisation_AddUpdate]` | Save employer/organisation writeback (with country/city/suburb/designation lookups) | `PatientEmployerOrganisationBuilder.cs` `Save` |
| `Task.uspAddTaskExternal` | Create a PMS task/reminder from a form action | `Task.cs` `AddTask` |
| `Billing.usptblConcept_GetByCode` | Resolve SNOMED/Read code to a display name for task subject | `Task.cs` `GetConceptNameByReadCode` |
| `[dbo].[uspDocumentSave]` | Insert document BLOB into DMS database directly | `Mapper.cs` `SaveDocumentToDMS` |

> `PatientConsultBuilder.cs`, `PatientProblemBuilder.cs`, `RegisteredPractitionerBuilder.cs`
> were confirmed to exist and follow the same Generate/Save pattern as `PatientBuilder.cs`;
> their specific stored procedure names were not individually captured in the excerpts read —
> **Unable to fully verify from available source without a dedicated pass**; recommend a
> follow-up read if precise procedure names are required.

### Soft-delete / audit pattern
Every generated table (ACC45 detail/definition/diagnosis) includes `IsActive`, `IsDeleted`,
`InsertedBy`, `UpdatedBy`, `InsertedAt`, `UpdatedAt` columns populated by the Builder classes
before calling the save procedure (evidence: `Acc45DefinitionBuilder.cs` lines 47-52,
`Acc45DiagnosisBuilder.cs` lines 46-51) — indicating the underlying PMS_NZ schema follows a
consistent **soft-delete + audit-column convention**, even though this project's C# code
never reads `IsDeleted` back (deletion/undo is presumably handled entirely in stored
procedures / other PMS modules not in this codebase).

## Risks
- `ConectionStringPMS_NZ` and `ConectionStringPMS_NZ_SecondNode` are identical in this
  environment's config — the "second node" routing logic (BR-05) cannot be verified to
  actually reach a different server from this codebase alone; in production they likely
  differ, but that must be confirmed operationally.
- No connection pooling/timeout tuning is visible beyond a hardcoded `CommandTimeout = 5000`
  in `DALHelper.PrepareCommand` (5000 **milliseconds**, i.e. 5 seconds — unusually short for
  potentially large ACC45/document queries) — a performance risk at scale.
- `DbAccess.cs`'s free-text SQL execution methods (`selectQuery`, `executeQuery`, etc.) are
  a latent SQL-injection surface if any external caller (outside this reviewed source)
  passes unsanitized input into them.

## Recommendations
- Confirm with the client whether `ConectionStringPMS_NZ_SecondNode` points to a genuinely
  separate physical SQL node in production, and document the replication/consistency model
  between primary and second node.
- In the migration, replace direct stored-procedure-name string literals and connection
  routing logic with an explicit, testable "read replica routing" abstraction.
- Audit callers of `DbAccess`'s raw-SQL methods across the broader PMS solution (outside this
  Hiso project) before removing/replacing the class, since it may be used by code not
  included in this review.
