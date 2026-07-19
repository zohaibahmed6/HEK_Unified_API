# HISO — Architecture

**Summary:** HISO is a single-project, monolithic WCF service with an informal layered
structure (WCF façade → Builder/Mapper business classes → homegrown DAL → SQL Server stored
procedures), no dependency injection, and business-rule metadata partly stored in
`Web.config` rather than in code or database tables designed for that purpose.

## Findings

### Architecture style
**Layered monolith**, not Clean/Onion/CQRS. There is one deployable unit (`Hiso` web
application project) that:
1. Exposes a WCF SOAP contract (`FormSessionPortType` / `FormSessionService`) as the only
   entry point (evidence: `FormSessionService.svc`, `FormSessionService.svc.cs`).
2. Delegates to a set of static/instance "Builder" and "Mapper" classes that translate
   between HISO/HL7 form XML and ADO.NET `DataTable`s (evidence: `Acc45Builder.cs`,
   `Acc45DefinitionBuilder.cs`, `Acc45DiagnosisBuilder.cs`, `Acc45ReferralBuilder.cs`,
   `PatientBuilder.cs`, `PatientConsultBuilder.cs`, `PatientEmployerOrganisationBuilder.cs`,
   `PatientProblemBuilder.cs`, `RegisteredPractitionerBuilder.cs`, `Mapper.cs`).
3. Uses a **dynamic, database-driven "concept mapping" engine** (`ConceptMapper/` folder)
   that resolves HISO/HL7 "concept" identifiers to specific stored procedures and result
   columns at runtime, rather than hardcoding a mapping per field (evidence:
   `ConceptMapper/HisoConceptDetail.cs`, `DAL/DBMessages.cs` `ExecuteHisoProcedure` /
   `GetParamList` / `MapParamList`).
4. Persists/reads via a homegrown Data Access Layer with two competing implementations
   (`DAL/DALHelper.cs` — a parameterized-stored-procedure helper with a `SqlParameter` cache,
   and `DAL/DbAccess.cs` — an older, more ad-hoc static-connection helper) plus many classes
   that bypass both and open `SqlConnection`/`SqlCommand` directly inline (e.g. `Task.cs`,
   `Mapper.cs`, `Acc45DefinitionBuilder.cs`, `PatientBuilder.cs`). This indicates
   **inconsistent DAL usage across the codebase**, not a single consistent access pattern.
5. Generates/stores documents (PDF/HTML) via Aspose and a Document Management System
   integration (`DocumentHandler.cs`, `ConceptMapper/TypeConverter.cs`).

### Folder structure
```
Hiso/
  Acc45Builder.cs, Acc45DefinitionBuilder.cs, Acc45DiagnosisBuilder.cs, Acc45ReferralBuilder.cs   # ACC45 (ACC accident claim form) table builders
  ConceptMapper/
    HisoConceptDetail.cs      # concept dictionary, dynamic SP execution & XML field-filling engine
    TypeConverter.cs          # Aspose-based image/HTML -> PDF conversion helpers
  DAL/
    DALHelper.cs               # Modern-ish parameterized ADO.NET helper (Microsoft "SqlHelper" pattern derivative)
    DALHelperParameterCache.cs # SqlParameter caching companion to DALHelper
    DbAccess.cs                 # Legacy, static/shared-connection ADO.NET helper (thread-unsafe)
    DBMessages.cs                # HISO-specific dynamic stored-procedure orchestration + AWS/DMS document enrichment
  DocumentHandler.cs           # Adds generated documents to DMS (direct-DB or DMSProxy)
  FormSessionPortType.cs       # WCF-generated service contract / DataContract types (auto-generated from WSDL, 78KB)
  FormSessionService.svc / .svc.cs  # WCF service implementation (the 6 SOAP operations)
  Mapper.cs                    # Core XML<->DB mapping engine; also defines HealthLinkSession
  PatientBuilder.cs, PatientConsultBuilder.cs, PatientEmployerOrganisationBuilder.cs, PatientProblemBuilder.cs, RegisteredPractitionerBuilder.cs  # per-entity table builders/savers
  Properties/                  # AssemblyInfo, PublishProfiles, Settings
  Task.cs                      # "Add Task/Reminder" business action triggered from processAction
  Utitlity.cs                  # generic reflection-based DataTable<->object helpers, config-driven column lists
  Web.config / Web.Debug.config / Web.Release.config
  packages.config              # single NuGet package (Newtonsoft.Json)
```
(`Activity Log/`, `bin/`, `obj/`, `.vs/`, `packages/` excluded as build artifacts per instructions.)

### Dependency flow
```
SOAP client (HealthLink/Form-engine)
      │  basicHttpBinding, security=None
      ▼
FormSessionService (WCF) ─── Logger.dll (event/exception logging on every call)
      │
      ├─ HealthLinkSession.GetByGUID()  ── direct SqlConnection ──> PMS_NZ_V2 (Appointment.usptblHealthLinkSession_GetByGUID)
      │
      ├─ getData → ConceptMapper.HisoConceptDetail / DBMessages.ExecuteHisoProcedure
      │        ├─ decides PMS_NZ vs PMS_NZ_SecondNode connection based on procedure name allow-list
      │        ├─ dynamically executes N stored procedures (Parallel.ForEach when >1) driven by
      │        │  a DB-resident concept dictionary (Hiso.UspGetHisoConcepts, cached 10 min in MemoryCache)
      │        └─ optionally calls AWSDoc.IndiciDMS (AWS-backed doc enrichment) for attachment/letter SPs
      │
      ├─ saveContainer → DocumentHandler.AddDocument (Aspose-rendered HTML/PDF)
      │        ├─ direct-to-DB DMS write (Mapper.SaveDocumentToDMS -> PMS_NZ_DMS.dbo.uspDocumentSave), OR
      │        └─ DMSProxy.DMSProxy (external DMS ASMX/SOAP service) when AddDirectDMS=0
      │        then Acc45DefinitionBuilder / Acc45Builder / Acc45DiagnosisBuilder / Acc45ReferralBuilder
      │        → Mapper.SaveAccidentInformation (Appointment.usptblACC45Detail_InsertUpdate_New)
      │
      ├─ processAction("save")  → PatientBuilder / PatientConsultBuilder / PatientEmployerOrganisationBuilder /
      │                             PatientProblemBuilder / RegisteredPractitionerBuilder  (each Generate+Save)
      ├─ processAction("addTask") → Task.processTask / Task.AddTask (Task.uspAddTaskExternal)
      │
      └─ getFormView → Acc45DefinitionBuilder.GetACC45Definition → DMSProxy.InstanceDMSProxy.GetDocumentData
```

### Project references / shared libraries
- Single `ProjectReference` to `MHNEntity.csproj` (external, not in this source tree).
- All other cross-cutting dependencies (Aspose, DMSProxy, AWSDoc, Logger) are binary
  references, not source-shared projects, so their internal logic is opaque to this review.

### Cross-cutting concerns

**Logging** is threaded through almost every method via a static singleton,
`Logger.Logging.Instance.WriteEventLog(...)` / `WriteExceptionLog(...)`, called manually at
the start/end of most operations and inside `catch` blocks (evidence: pervasive across
`FormSessionService.svc.cs`, `DAL/DBMessages.cs`, builders). There is no middleware/filter —
logging is opt-in per call site, so coverage is inconsistent (see `LoggingAnalysis.md`).

**Error handling** is also manual and inconsistent: most public methods wrap their body in
`try { } catch (Exception ex) { throw ex; }` (which **destroys the original stack trace** —
should be `throw;`) or, worse, `catch (Exception) { }` that silently swallows errors and
returns `null`/default (e.g. `Mapper.HealthLinkSession.GetByGUID`, `Mapper.GetAccidentInformation`).
WCF operations catch `Exception` at the top level and rethrow as `FaultException(ex.Message)`,
which combined with `includeExceptionDetailInFaults="true"` in `Web.config` leaks internal
error detail to SOAP clients.

**Configuration** doubles as a business-rule store: `Web.config` `<appSettings>` holds
column-name lists per "UDT" (user-defined table type) used by every Builder
(`Utitlity.GetColumnNameByTableName`), qualifier code lists, DMS document-type IDs, and
task-status/priority IDs. This means core business behavior can be changed by editing
`Web.config` without a code change or code review — a significant governance/audit gap.

**Caching**: a single `System.Runtime.Caching.MemoryCache` instance caches the "concept
list" (`Hiso.UspGetHisoConcepts` results) for 10 minutes in-process
(`FormSessionService.svc.cs`, `getData`). This is per-instance/per-process cache with no
distributed invalidation — a concern if HISO were ever scaled out to multiple instances.

## Risks
- Two parallel, inconsistent DAL implementations (`DALHelper` vs `DbAccess` vs raw inline
  `SqlConnection`/`SqlCommand`) mean there is no single seam to intercept for logging,
  retries, connection pooling tuning, or migration to a new data layer.
- Business rules embedded in `Web.config` are easy to lose or silently break during
  migration; they must be captured as explicit code/config in the rewrite (see `BusinessRules.md`).
- `DbAccess`'s static, shared `SqlConnection`/`SqlCommand`/`DataSet` fields are a
  concurrency/scalability hazard (see `SecurityAnalysis.md` and `RiskAssessment.md`).

## Recommendations
- In the unified platform, replace the WCF façade with a versioned REST/JSON (or gRPC)
  interface, but preserve the *conceptual* dynamic-mapping engine's capability (concept →
  stored procedure → field) as an explicit, testable component rather than reimplementing it
  ad hoc.
- Consolidate to a single, connection-per-call, async-capable DAL; eliminate static/shared
  ADO.NET objects.
- Migrate `Web.config`-resident business rules (UDT column lists, qualifier lists, DMS type
  IDs, task IDs) into versioned configuration or database reference tables with change
  auditing.
