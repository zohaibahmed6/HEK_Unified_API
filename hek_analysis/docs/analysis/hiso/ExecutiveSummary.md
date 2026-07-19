# HISO — Executive Summary

## System Overview

HISO ("Health Information Standards Organisation" form-submission integration) is a legacy
ASP.NET Web Application (Class Library-hosted Web Application project, `RootNamespace: Hiso`,
`OutputType: Library`) that exposes a single **WCF SOAP service**, `FormSessionService.svc`
(contract `FormSessionPortType`), implementing the HISO 10014.2 "Form Session" web-service
specification used by NZ HealthLink-style clinical form engines (e.g. ACC45 accident claim
forms) to exchange data with the practice management system (PMS_NZ database).

Rather than a set of REST/HTTP endpoints, HISO is a **single WCF service with 6 SOAP
operations** (`getVersion`, `getDeliveryOptions`, `getData`, `saveContainer`, `getFormView`,
`processAction`). Internally it acts as a dynamic mapping/orchestration layer that:
- Resolves an opaque session GUID (`HealthLinkSession`) into a Provider/Patient/Appointment/Practice context (no separate login step — evidence: `Mapper.cs`, class `HealthLinkSession`, method `GetByGUID`).
- Reads a database-driven "concept" dictionary (`Hiso.UspGetHisoConcepts`) that maps HL7/HISO
  concept names to stored procedures and table columns, then dynamically executes those
  stored procedures to assemble/return clinical form XML (evidence: `DAL/DBMessages.cs`,
  method `ExecuteHisoProcedure`; `ConceptMapper/HisoConceptDetail.cs`).
- Persists completed ACC45 (ACC accident claim) forms, patient/employer/practitioner
  data back into `PMS_NZ` via stored procedures (evidence: `Acc45Builder.cs`, `PatientBuilder.cs`,
  `PatientEmployerOrganisationBuilder.cs`, etc.).
- Converts submitted form output to PDF/HTML/DOCX using Aspose libraries and stores the
  rendered document in a Document Management System (DMS), either directly in a DMS SQL
  database or via a AWS-backed document proxy (evidence: `DocumentHandler.cs`, `Mapper.SaveDocumentToDMS`, `AWSDoc.IndiciDMS`).

## Technology Stack Summary
- **.NET Framework 4.8** (Web Application, `TargetFrameworkVersion=v4.8`; `httpRuntime targetFramework="4.6"`).
- **WCF** (`System.ServiceModel`) hosted via `.svc` with `basicHttpBinding`, `security mode="None"`.
- ADO.NET direct against **Microsoft SQL Server** (`System.Data.SqlClient`), heavy use of stored procedures, no ORM.
- Third-party: Aspose.Words, Aspose.Cells, Aspose.Pdf (document generation/conversion), a proprietary `DMSProxy` and `AWSDoc` client for document storage, a proprietary `Logger.dll`, `Newtonsoft.Json` 13.0.3, and JWT-related Microsoft.IdentityModel assemblies referenced (`bin\...dll`) but **not observably used** in any source file inspected.
- Project references `MHNEntity.csproj` (out of scope / not included in this source tree).

## Architecture Style
Single-tier **monolithic WCF service** with an ad-hoc layered structure: a thin WCF façade
(`FormSessionService.svc.cs`) → business "Builder"/"Mapper" classes → a homegrown Data
Access Layer (`DAL/DALHelper.cs`, `DAL/DbAccess.cs`, `DAL/DBMessages.cs`) → SQL Server stored
procedures. No dependency injection, no unit tests found, no interfaces/abstractions beyond
one abstract `TableBuilder` class. Configuration-driven metadata (column lists, qualifier
codes) is stored in `Web.config` `<appSettings>`, which is unusual and fragile.

## Counts (from this codebase)
| Metric | Count |
|---|---|
| WCF service contracts | 1 (`FormSessionPortType` / `FormSessionService`) |
| SOAP operations (endpoints) | 6 |
| Top-level business/mapper classes | ~16 (Builders, Mapper, DocumentHandler, Task, ConceptMapper) |
| Business rules catalogued | 22 (see `BusinessRules.md`) |
| Distinct DB connection strings configured | 4 (`PMS_NZ`, `PMS_NZ_SecondNode`, `Indici_Master`, `PMS_NZ_DMS`) |
| Stored procedures referenced by name in source | 30+ (see `DatabaseAnalysis.md`) |
| External systems integrated | SQL Server (multi-node), DMS (direct SQL + `DMSProxy` SOAP/ASMX), AWS-backed document service (`AWSDoc.IndiciDMS`), Aspose licensing servers (local `.lic` files) |

## Top 5 Security Issues (see `SecurityAnalysis.md` for full detail)
1. **No authentication/authorization on the WCF endpoint.** `security mode="None"` in `Web.config`; the only "auth" is looking up an opaque session GUID (`HealthLinkSession.GetByGUID`) with no expiry, single-use, or rate-limit check — anyone holding a valid GUID can impersonate that clinical session (`FormSessionService.svc.cs`, `GetSession`).
2. **Hardcoded/plaintext database credentials and a symmetric-key decryption password in source and `Web.config`** (`Web.config` connectionStrings; `DALHelper.cs` line 17, `mstrEncryptionCommand = "OPEN SYMMETRIC KEY DBDX DECRYPTION BY PASSWORD = 'tcpepms*1'"`).
3. **WCF fault details exposed to callers.** `serviceDebug includeExceptionDetailInFaults="true"` in `Web.config` leaks internal exception messages/stack info to any caller.
4. **Unsafe/uncontrolled document rendering.** User/clinic-submitted HTML and images are fed directly into Aspose.Words (`ConceptMapper/TypeConverter.cs`, `ConvertHTMLToByte`) with no sanitization, and rendered documents are persisted to DMS — a vector for SSRF/resource injection depending on Aspose's HTML resource-fetching behavior.
5. **In-memory "SQL-like" injection surface via `DataTable.Select()`.** Numerous builders (`Acc45Builder.cs`, `PatientBuilder.cs`, `PatientEmployerOrganisationBuilder.cs`, `Acc45DiagnosisBuilder.cs`, `Acc45ReferralBuilder.cs`) build filter expressions by string concatenation of clinical form field names, e.g. `dtMapping.Select("ConceptName = '" + item.Key + "' OR Description = '" + item.Key + "'")` — if a field name/key contains a single quote it breaks or manipulates the row filter (low DB risk since it is not raw SQL, but a real robustness/injection-adjacent code smell).

## Top 5 Risks (see `RiskAssessment.md` for full detail)
1. **.NET Framework 4.8 / WCF end-of-investment risk** — WCF server hosting has no first-class support in modern .NET; a rewrite must choose CoreWCF, a REST/JSON shim, or eliminate SOAP entirely, which is a compatibility risk for existing HealthLink-certified clients.
2. **Static mutable shared state in the DAL** (`DAL/DbAccess.cs`: static `SqlConnection`, `SqlCommand`, `SqlDataAdapter`, `DataSet` fields shared across all callers) is **not thread-safe** and will not scale to 10,000 concurrent users without a full rewrite of data access.
3. **Business logic embedded in `Web.config` `<appSettings>`** (column-list "UDT_*" keys, qualifier code lists) makes behavior configuration-fragile and undocumented; losing/mistyping an appSetting silently breaks form mapping.
4. **Dead/unreachable code paths** in critical save logic (e.g. `FormSessionService.svc.cs`, `saveProcessAction`, code after `return true;` at line 535 is unreachable) suggests incomplete or abandoned business rules that must be clarified with the business before migration, not silently dropped.
5. **Tight coupling to commercial Aspose licenses and a proprietary `DMSProxy`/`AWSDoc` document pipeline** — document generation/storage behavior cannot be replicated without re-licensing Aspose and reverse engineering the undocumented `DMSProxy`/`AWSDoc` contracts (external assemblies, source not available).

## Overall Recommendation
HISO is a narrowly-scoped but business-critical integration service (HISO/ACC45 clinical
form exchange) built as a single legacy WCF endpoint over direct ADO.NET/stored-procedure
access, with no independent authentication layer, thread-unsafe shared DAL state, and
substantial "dynamic" (config- and database-driven) mapping logic that will be very easy to
lose silently during a rewrite. For the unified platform, HISO's functional surface should be
re-implemented as a small, well-tested "clinical forms integration" module behind the new
unified API's own authentication/authorization layer (do not carry forward GUID-only
session auth), with the concept-mapping and ACC45-specific business rules extracted into
explicit, versioned, testable code rather than `Web.config` key/value pairs — while the
Aspose/DMS document generation and storage integration is isolated behind a clear internal
interface so it can be swapped or modernized independently.
