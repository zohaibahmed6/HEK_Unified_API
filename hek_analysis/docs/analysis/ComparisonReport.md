# Phase 2 — Cross-API Comparison Report (HISO vs KARO vs ERMS)

**Scope:** This report compares the three independent Phase 1 reverse-engineering analyses
(`docs/analysis/hiso/`, `docs/analysis/karo/`, `docs/analysis/erms/`, 12 files each, 36 total)
without re-reading source code, except where a Phase 1 report was itself ambiguous. Every claim
below cites the Phase 1 report(s) it is drawn from. Items that cannot be confirmed from the
Phase 1 reports are marked `> Unable to verify from available source`.

---

## 1. Overview

**HISO** ("Health Information Standards Organisation" form-submission integration) is a
single-project, single-tier **WCF SOAP service** (`FormSessionService.svc`, contract
`FormSessionPortType`, 6 operations) implementing the NZ HISO 10014.2 "Form Session"
specification. It resolves an opaque session GUID into a Provider/Patient/Appointment/Practice
context, dynamically maps HL7/HISO "concepts" to stored procedures via a database-driven
dictionary, builds and persists ACC45 (ACC accident claim) clinical forms, and generates/stores
PDF/HTML documents via Aspose and a DMS/AWS document pipeline. It is the only one of the three
APIs that is SOAP-based rather than REST/JSON, and the only one with no external documentation
supplied at all (per `hiso/ExecutiveSummary.md`, `hiso/DocumentationGap.md`).

**KARO** (repo `HSSWebAPI`) is an **ASP.NET Web API 2** "fat controller" application: one
2,047-line controller (`APIController`, 24 endpoints) bridging the Indici Practice Management
System (PMS) and the HSS portal — patient demographics, clinical notes, conditions,
medications, labs, observations, recalls, documents, invoices, and templated "encounter
summary" clinical forms. It calls exactly one module (`DAL\South\HSSDA.cs`) of a much larger,
shared `DAL` project that also ships 11 unused integration modules (BPAC, BPI, DMS, GP2GP,
MHNAppointment, MHNHL7, Pegasus, Procare, Procon, Screening, UI) (per `karo/ExecutiveSummary.md`,
`karo/Architecture.md`).

**ERMS** (`ERMSWebAPI`) is also an ASP.NET Web API 2 application over the same Indici PMS, but
exposes **two controllers**: `APIController` (23 XML endpoints implementing the documented
HISO-concept eReferrals contract for the South Island NZ eReferrals platform) and
`COLController` (7 JSON endpoints, entirely undocumented, integrating with a "COL"/Pegasus
claiming-adjacent system, including a financial `SaveInvoice` write). Like KARO, ERMS sits on
top of a shared `DAL` project containing the same set of largely-unused integration modules
(per `erms/ExecutiveSummary.md`, `erms/Architecture.md`, `erms/DependencyAnalysis.md`).

---

## 2. Common Modules

### 2.1 The shared DAL project — confirmed across KARO and ERMS, structurally consistent with HISO

This is the single most important cross-cutting finding. Both KARO and ERMS ship a `DAL`
project with **identical subfolder names**: `BPAC`, `BPI`, `DMS`, `GP2GP`, `MHNAppointment`,
`MHNHL7`, `Pegasus`, `Procare`, `Procon`, `Screening`, `South`, `UI`, plus `HelperClasses`
(`DALHelper.cs`, `DbAccess.cs`, `DALHelperParameterCache.cs`) (per `karo/Architecture.md`,
`erms/Architecture.md`). Both APIs only call one or two of these modules from their own
controllers:
- KARO calls only `South\HSSDA.cs` (per `karo/Architecture.md`, `karo/DependencyAnalysis.md`).
- ERMS calls `South\HSSDA.cs` (from `APIController`) and `Pegasus\PHCO.cs` (from
  `COLController`) (per `erms/Architecture.md`, `erms/DependencyAnalysis.md`).

The other 9–11 modules in each project are present, compiled into the same `DAL.dll`, and
**not reachable from either API's own controller** — confirmed independently by both Phase 1
teams via `grep` for call sites (`karo/Architecture.md`: "No call sites... were found";
`erms/DependencyAnalysis.md`: same modules listed as "Called by ERMS controllers? No").

This strongly supports the hypothesis stated in the task background: **KARO and ERMS are two
thin API layers over the same shared integration/data-access library**, each using only a
narrow slice of it. Evidence for the shared-DAL claim specifically:
- Identical subfolder/class names (`BPACDA.cs`, `BPIDA.cs`, `DMSDA.cs`, `PIDAO.cs`/GP2GP,
  `MHNAppointmentDA.cs`, `DBMessages.cs`/MHNHL7, `PHCO.cs`/Pegasus, `ProcareApiDA.cs`,
  `ProconApiDA.cs`, `ScreeningDAL.cs`, `GUIDA.cs`/UI) appear in both `karo/Architecture.md` and
  `erms/Architecture.md`.
- Both DAL projects reference the same external `AWSDoc.dll` (via `HintPath`) for AWS-backed
  document storage (`karo/TechnologyStack.md`, `erms/TechnologyStack.md`).
- Both reference a `Logger.dll` via a `HintPath` that points **outside the repository**
  (`..\..\MHNPHMP-Integration\Logger\bin\Debug\Logger.dll` for KARO per `karo/Architecture.md`;
  ERMS's `Logger` project has an internal `using Logger;` pattern per `erms/Architecture.md`),
  confirming both are extracted subsets of a larger multi-application solution
  (`MHNPHMP-Integration`).
- Both share the identical hardcoded SQL symmetric-key decryption password
  `'tcpepms*1'` in `DALHelper.cs` (per `karo/SecurityAnalysis.md` finding #3,
  `hiso/SecurityAnalysis.md` finding #2 — **HISO also has this exact same string** in its own
  `DALHelper.cs`/`DALHelperParameterCache.cs`). This is strong evidence HISO's DAL helper code
  and KARO/ERMS's `DAL\HelperClasses\DALHelper.cs` derive from the same original source, even
  though HISO's is a separate, locally-embedded copy rather than a shared binary reference
  (HISO has no `HintPath` to an external DAL project — `hiso/Architecture.md`).
- Both KARO's `DMS\DMSDA.cs`/`MHNHL7\DBMessages.cs` and ERMS's identically-named
  `DMS\DMSDA.cs`/`MHNHL7\DBMessages.cs` contain the **same confirmed SQL-injection code
  smell** at effectively the same call sites (string-concatenated `UPDATE`/`SELECT`/`INSERT`
  statements) — see `karo/SecurityAnalysis.md` finding #1 and `erms/SecurityAnalysis.md`
  SEC-03, which cite near-identical line numbers and code snippets (`DMSDA.cs` line 27;
  `DBMessages.cs` lines 797/827/860-864). This is the strongest single piece of evidence that
  KARO and ERMS's `DAL` projects are the **same codebase**, not just similarly structured.
- Sibling near-duplicate integrations (`Procare`, `Procon`, `Pegasus`) reuse the same `[HSS]`
  schema stored procedures that KARO's `HSSDA` uses, per `karo/DatabaseAnalysis.md` ("Two
  near-duplicate implementations... reusing several of the same [HSS] schema procedures") and
  ERMS actively calls one of these siblings (`Pegasus\PHCO.cs`, via `COLController`) — i.e.
  what is "dead code" in KARO (`Pegasus`) is **live, load-bearing code in ERMS**. This means
  the "11 unused modules" framing for KARO and the "9 unused modules" framing for ERMS refer
  to overlapping-but-not-identical dead-code sets in the same shared library, since ERMS
  activates `Pegasus` while KARO does not.

### 2.2 Shared authentication pattern: DB-validated opaque token, re-checked per call

KARO and ERMS both implement the **same authentication design**: credentials POSTed to an
`Authenticate` action, validated via a stored procedure named `uspInsertAndValidateToken`
(`[HSS]` schema), returning a GUID token + expiry + practiceId; the token is then re-validated
against the **same stored procedure name** on every subsequent call, scoped to a specific
patient/encounter/practice tuple (per `karo/AuthenticationAuthorization.md`,
`erms/AuthenticationAuthorization.md`). This is not just conceptually similar — it is the same
named stored procedure (`[HSS].[uspInsertAndValidateToken]`) called via the same DAL class
(`HSSDA.InsertAndValidateToken`), confirming both APIs share this exact auth mechanism through
the same DAL module. HISO does not use this mechanism at all (see Section 7).

### 2.3 Shared "encrypted ID" pattern

KARO and ERMS both obfuscate patient/encounter IDs using a hardcoded, static Rijndael
(AES-family) key embedded in source (`Models\EncryptionManager.cs` in both projects), with the
identical fallback behavior of accepting a plain integer if `int.TryParse` succeeds (per
`karo/BusinessRules.md` BR-02, `erms/BusinessRules.md` BR-02, `karo/SecurityAnalysis.md`
finding #3, `erms/SecurityAnalysis.md` SEC-02). ERMS additionally layers an optional Base64
encoding step before decryption that KARO does not have (`erms/BusinessRules.md` BR-03). HISO
has no equivalent — it identifies sessions via an opaque GUID looked up directly in the
database, with no client-side ID obfuscation (`hiso/AuthenticationAuthorization.md`).

### 2.4 Shared multi-tenant/practice routing via connection-string-name concatenation

KARO and ERMS both derive a "practice id" by string-splitting a client-supplied `encounterId`
(delimiters `_`/`__`) and concatenating it onto a connection-string *name*
(`"ConnIndiciDB" + practiceid`), rather than using a central tenant registry (per
`karo/BusinessRules.md` BR-01/BR-03, `erms/BusinessRules.md` BR-01, both citing the identical
`ConfigurationManager.ConnectionStrings["ConnIndiciDB"+practiceid]` pattern in `HSSDA.cs` —
the same file in both DAL trees). ERMS additionally supports a 4th segment interpreted as
"PHO" that can trigger a silent Azure-mirror proxy forward (`erms/BusinessRules.md` BR-04) —
KARO has no equivalent Azure-forwarding behavior. HISO has no multi-tenant connection-string
routing at all; it uses a fixed small set of 4 named connections (`PMS_NZ`,
`PMS_NZ_SecondNode`, `Indici_Master`, `PMS_NZ_DMS`) selected by procedure-name allow-list, not
by tenant (`hiso/DatabaseAnalysis.md`).

### 2.5 Shared flat-file Logger

All three APIs use a proprietary, non-standard flat-file logging library named `Logger`/`Logger.dll`
with a static singleton (`Logging.Instance`) exposing `WriteEventLog`/`WriteExceptionLog`, no
correlation IDs, and per-day text files (per `hiso/LoggingAnalysis.md`, `karo/LoggingAnalysis.md`,
`erms/LoggingAnalysis.md`). KARO and ERMS's `Logger` projects are evidenced as the literal same
shared library (KARO's `HintPath` points to `..\..\MHNPHMP-Integration\Logger\bin\Debug\Logger.dll`,
outside its own repo — `karo/Architecture.md`). HISO's `Logger.dll` is referenced via a different
`HintPath` (`..\..\PMSdll\Logger.dll` — `hiso/TechnologyStack.md`) and could not be confirmed
identical to KARO/ERMS's copy from the Phase 1 reports alone.

> Unable to verify from available source whether HISO's `Logger.dll` and KARO/ERMS's
> `Logger.dll` are literally the same binary/source — the `HintPath`s differ
> (`..\..\PMSdll\Logger.dll` vs `..\..\MHNPHMP-Integration\Logger\bin\Debug\Logger.dll`), so
> this should be confirmed with the client rather than assumed.

### 2.6 Shared AWS-backed document storage (AWSDoc)

All three APIs reference the same precompiled, source-unavailable `AWSDoc.dll`
(`AWSDoc.IndiciDMS`) for AWS-backed document storage, used identically to check whether a
practice has "AWS enabled" and route document read/write calls to an `_AWS`-suffixed stored
procedure variant instead of the default one (per `hiso/DependencyAnalysis.md`,
`karo/TechnologyStack.md`, `erms/TechnologyStack.md`, `erms/BusinessRules.md` BR-09,
`karo/BusinessRules.md` BR-18). This is a genuine three-way shared dependency, not just a
KARO/ERMS pairing.

### 2.7 Shared hardcoded DAL symmetric-key password

As noted in 2.1, the identical string `'tcpepms*1'` (a SQL Server `OPEN SYMMETRIC KEY ...
DECRYPTION BY PASSWORD` command) appears in `DALHelper.cs`/`DALHelperParameterCache.cs` in
**all three** codebases (`hiso/SecurityAnalysis.md` finding #2, `karo/SecurityAnalysis.md`
finding #3, cross-referenced generically in `erms/SecurityAnalysis.md` SEC-01 for connection
strings but the DAL-helper password specifically is called out only in HISO's and KARO's
reports — `erms/DatabaseAnalysis.md` and `erms/SecurityAnalysis.md` do not explicitly quote
this string for ERMS).

> Unable to verify from available source whether ERMS's `DAL\HelperClasses\DALHelper.cs`
> contains the exact same `'tcpepms*1'` string — the ERMS Phase 1 reports describe the same
> `DALHelper.cs`/`DbAccess.cs` files and connection-string-credential pattern but do not quote
> this specific symmetric-key line. Given ERMS shares the same `DAL` project structurally with
> KARO, this is likely present but should be confirmed directly.

### 2.8 Document generation / DMS write pattern

All three APIs implement a two-database "document plus index" save pattern: write binary
content to a DMS database/service, then write a separate PMS-side index/pointer record
referencing the returned document key, with no distributed transaction across the two steps
(per `hiso/BusinessRules.md` BR-12/BR-21, `karo/BusinessRules.md` BR-15,
`erms/BusinessRules.md` BR-08/BR-19). HISO additionally renders documents via Aspose
(Words/Pdf/Cells) before storage — a capability KARO and ERMS do not have (they accept
pre-rendered base64 content from the caller rather than generating it server-side).

---

## 3. Unique Modules

| API | Unique capability | Evidence |
|---|---|---|
| HISO | WCF SOAP protocol/hosting model (only one of the three not REST/JSON) | `hiso/TechnologyStack.md`, `hiso/EndpointInventory.md` |
| HISO | ACC45 (ACC accident claim) form builder/save engine (`Acc45Builder`, `Acc45DefinitionBuilder`, `Acc45DiagnosisBuilder`, `Acc45ReferralBuilder`) | `hiso/Architecture.md`, `hiso/BusinessRules.md` BR-12–BR-18 |
| HISO | Database-driven "concept mapping" engine resolving HL7/HISO concept names to stored procedures/columns at runtime (`ConceptMapper/HisoConceptDetail.cs`) | `hiso/Architecture.md`, `hiso/BusinessRules.md` BR-03–BR-10 |
| HISO | Server-side document rendering via Aspose (HTML/image → PDF/DOCX conversion) | `hiso/TechnologyStack.md`, `hiso/SecurityAnalysis.md` finding #6 |
| HISO | Task/reminder creation from form actions (`Task.cs`, `processAction("addTask")`) | `hiso/BusinessRules.md` BR-19/BR-20 |
| KARO | Templated "encounter summary" clinical forms (diabetes review, foot exam, retinopathy) driven by a DB schema lookup (`GetTemplateSchema`) | `karo/BusinessRules.md` BR-10/BR-11 |
| KARO | `GetPatientAttachment` endpoint (base64 attachment retrieval with rich filtering) — undocumented, unique to KARO | `karo/EndpointInventory.md` |
| ERMS | `COLController` — an entire second, undocumented JSON API surface (Pegasus/"COL": session, provider, surgery, diagnosis data, and financial `SaveInvoice`) | `erms/ExecutiveSummary.md`, `erms/EndpointInventory.md` |
| ERMS | "Azure ERMS API" mirror-forwarding proxy (`ERMSAPIProxy.cs`) — transparently forwards requests for PHO-tagged practices to a separate Azure-hosted mirror deployment of the same API | `erms/BusinessRules.md` BR-04, `erms/DependencyAnalysis.md` |
| ERMS | RTF/HTML content transcoding for lab/radiology report text (`ConvertString2RTF`, HTML-stripping for accident diagnosis text) | `erms/BusinessRules.md` BR-13/BR-14 |
| ERMS | In-band `"|&|"`/`"|?|"`-delimited string protocol embedded in stored-procedure result columns, unpacked via reflection-based `ERMSDataTableToListHiso<T>` mapping | `erms/RiskAssessment.md` RISK-12 |

---

## 4. Duplicate Business Logic

These are cases where the same real-world business rule appears to be implemented
independently in 2+ APIs — flagged as migration landmines where behavior may subtly diverge.

| Business concept | HISO | KARO | ERMS | Divergence risk |
|---|---|---|---|---|
| **Token/session validation scoped to patient+encounter+practice** | Session GUID looked up once per call with **no re-validation contract concept** — a single lookup resolves a full context, no separate "is this token still valid for this specific resource" check (`hiso/AuthenticationAuthorization.md`) | Bearer token re-validated **per call** against `patientId+encounterId+practiceId+pho` via `[HSS].[uspInsertAndValidateToken]` (`karo/AuthenticationAuthorization.md` BR-05) | Same stored procedure name, same re-validation-per-call pattern (`erms/AuthenticationAuthorization.md`, BR-06) | KARO and ERMS implement the *same* mechanism via the same DAL call; HISO's model is architecturally different (single lookup, no per-call re-check) — a unified auth layer cannot simply "keep what two of three systems do," since HISO's session model has no direct analog to migrate. |
| **AWS vs. on-prem DMS document routing per practice** | HISO checks `AWSDoc.IndiciDMS.CheckAWSIsEnabled(practiceId)` and calls `_AWS`-suffixed procedures on a 6-procedure allow-list, with an explicit **fallback to non-AWS on exception** (`hiso/BusinessRules.md` BR-06/BR-07) | KARO checks the same flag in `GetDocuments`, with **no explicit documented fallback-on-exception behavior** noted in `karo/BusinessRules.md` BR-18 | ERMS checks the same flag in `UpdateExistingDocument`/`GetDocResults`/`GetOtherDocs`, again with **no fallback-on-exception documented** (`erms/BusinessRules.md` BR-09) | HISO's fallback-on-AWS-failure (BR-07) is not confirmed present in KARO or ERMS's Phase 1 reports — if this is a genuine behavioral difference (not just an under-documented commonality), a unified document-routing service must decide whether fallback-on-failure is the correct behavior everywhere, since silently reproducing HISO's fallback for KARO/ERMS (or omitting it for HISO) changes reliability characteristics. **Flag for stakeholder confirmation.** |
| **"Already exists" as success, not error, via magic stored-procedure return codes** | Not observed in HISO's reviewed source (no equivalent idempotent-save magic-code pattern found in `hiso/BusinessRules.md`) | `SaveCondition`: return code `-5` → "Diagnosis already exists" treated as success (BR-12). `SaveInvoice`: return code `-3` → "Invoice already exists" treated as success (BR-13) | `SaveInvoice` (COLController, via `OnlineClaim.uspInsertUpdateService`): return code `-3` → "Invoice Already exist" treated as success (BR-15) | KARO and ERMS both use `-3` for "invoice already exists," suggesting a shared stored-procedure contract for `uspInsertUpdateService`/`OnlineClaim.uspInsertUpdateService` — plausible they are the *same* procedure (or copies with the same contract). This should be verified directly against the database rather than assumed identical, since ERMS's version is under a different schema (`OnlineClaim` vs KARO's presumed `[HSS]`/default schema per `karo/DatabaseAnalysis.md` — KARO's own report does not state a schema for `uspHSSInsertUpdateService`, only names it, so the schema match cannot be confirmed from Phase 1 reports alone). |
| **Multi-tenant / practice-id resolution from a composite identifier** | Not applicable — HISO does not use practice-suffix routing; practice context comes from the session GUID lookup itself (`hiso/DatabaseAnalysis.md`) | `encounterId` = `<encryptedEncounterId>__<practiceId>[_<subPracticeId>]`, split on `__` (BR-01) | `EncounterId` split on `_`/`__`, with an **additional 4th "PHO" segment** used for Azure-forwarding (BR-01/BR-04) | KARO and ERMS use the *same underlying delimiter convention* on the *same underlying field* (`encounterId`), but ERMS extends it with a 4th segment KARO does not have. If a future unified tenant-resolution component reuses "the" delimiter convention, it must account for ERMS's extra segment or silently break Azure-mirror-routed ERMS practices. |
| **Practice-specific hardcoded overrides forcing a PHO/config value** | Not observed in HISO | Hardcoded: practice suffix `302_F3H045` forces `pho = "SCDHB"` in 5 different Save* actions (BR-04) | Not explicitly documented as present in ERMS's Phase 1 reports (ERMS's PHO segment is read directly from the EncounterId, not hardcoded-overridden) | This looks like a KARO-only special case, but since KARO and ERMS share so much underlying logic and the same practice (`302_F3H045`/`SCDHB`) plausibly exists in both systems' practice population, this should be explicitly checked against ERMS's behavior for the same practice ID before assuming it doesn't apply there too. `> Unable to verify from available source` whether ERMS has an equivalent override. |
| **Document `ItemType` ("in"/"out") → DMS category/type-code mapping** | Uses config-driven `DMSHTMLTypeId`/`DMSPDFTypeId` selected by MIME type, not by in/out direction (`hiso/BusinessRules.md` BR-22) — a **different classification axis** than KARO/ERMS | `ItemType.ToLower()=="in"` → DMS category 17 (inbox) / PMS type code 1; else → 18 (outbox) / type code 2 (BR-16) | Identical: `ItemType` "in" → DMS category 17 / type code 1; else → 18 / type code 2 (BR-08) | KARO and ERMS implement the **exact same magic numbers** (17/18, 1/2) for the same business concept — near-certain evidence of a shared stored-procedure contract. HISO instead classifies purely by MIME type, a structurally different (and non-equivalent) rule. A unified document-classification model must reconcile these two different axes (direction-based vs. content-type-based), not just pick one. |

---

## 5. Duplicate Endpoints

All three APIs expose overlapping patient/clinical-data retrieval capability against the same
underlying Indici PMS database. The table below groups functionally equivalent endpoints.

| Capability | HISO | KARO | ERMS (APIController) | ERMS (COLController) |
|---|---|---|---|---|
| Patient demographics | `getData` (concept-driven, generic) | `GetDemographics` | `GetPatientData` | `GetCurrentPatientData` |
| Clinical/consult notes | `getData` (concept-driven) | `GetClinicalNotes` / `SaveClinicalNotes` | `GetConsultNotes` | — |
| Conditions / diagnoses | `getData` (concept-driven), ACC45 diagnosis builders | `GetConditions` / `SaveCondition` | `GetClassifications` (Problems) | `GetDiagnosisData` |
| Medications | `getData` (concept-driven) | `GetMedications` | `GetPrescribedMedications` / `GetRegularMedications` | — |
| Lab results | `getData` (concept-driven) | `GetLabResults` | `GetLaboratoryReportList` / `GetLaboratoryReportDetails` | — |
| Documents / attachments | `saveContainer`, `getFormView` (DMS-backed) | `GetDocuments` / `SaveDocument` / `GetPatientAttachment` | `GetScannedList`/`GetScannedDetails`, `SaveDocument` | — |
| Provider/practitioner lookup | (implicit via session context) | `GetProvider` | `GetRegisteredPractitioners` | `GetProviderData` |
| Recalls | — (not present) | `GetRecalls` / `SaveRecall`, `GetRecallCategories` | — (not present in APIController per endpoint inventory) | — |
| Observations / measurements | `getData` (concept-driven, `GetPatientMeasurements`) | `GetObservations` / `SaveObservations` | `GetPatientMeasurement` | — |
| Invoicing / billing | ACC45 accident-claim save (not a generic invoice) | `SaveInvoice` | — | `SaveInvoice` |
| Authentication/token issuance | `GetSession` (GUID lookup, not a credential exchange) | `Authenticate` (GET+POST) | `Authenticate` (POST, XML) | `Authenticate` (POST, JSON) |
| Session/surgery/appointment context | `HealthLinkSession.GetByGUID` | (embedded in every call via token validation) | — | `GetSessionData`, `GetSurgeryData` |

**Key observation:** KARO and ERMS's `APIController` overlap almost completely in
*capability* (demographics, notes, conditions, medications, labs, documents, observations,
provider lookup) despite being separate codebases with separate endpoint names/casings and
separate response shapes (KARO returns JSON, ERMS's `APIController` returns XML). Both call
into the same underlying `[HSS]` schema stored procedures via the same `HSSDA` class (per
`karo/DatabaseAnalysis.md` and `erms/DatabaseAnalysis.md`, which list overlapping procedure
names such as `uspGetDemographics`, `uspGetConditions`, `uspGetLabResults`,
`uspGetConsultNotes`). This means KARO and ERMS are almost certainly **two different front-end
contracts (JSON vs XML, different consumer systems) over the very same clinical-data backend**
— a near-total duplication of read-side functionality that the unified platform should collapse
into one canonical data-access surface with multiple serialization/contract adapters if needed,
rather than porting two independent implementations.

ERMS's `COLController` additionally duplicates a subset of this same data (patient, provider,
diagnosis) under yet another schema (`OnlineClaim`), while KARO's dormant `Pegasus\PHCO.cs`
module (not wired into KARO's own controller) implements what appears to be the *same*
`OnlineClaim`-schema integration that ERMS's `COLController` actively uses — see
`karo/DatabaseAnalysis.md` ("Pegasus (`PHCO.cs`)... A separate online-claims integration
reading from the same PMS database via a different schema (`OnlineClaim`)") versus
`erms/DependencyAnalysis.md` ("Indici PMS (Pegasus/OnlineClaim schema)... Reachable via
`DAL/Pegasus/PHCO.cs`, called by every `COLController` action"). **This is strong evidence that
ERMS's COLController is simply KARO's dormant Pegasus module, activated.** Confirming this
directly (e.g., diffing `PHCO.cs` between the two repos) would materially change migration
scoping, since it means the "COL" functionality doesn't need to be built from ERMS alone — it's
effectively already partially validated by KARO's presence of the same class.

---

## 6. Database Differences

| Aspect | HISO | KARO | ERMS |
|---|---|---|---|
| Engine | SQL Server, `System.Data.SqlClient` | SQL Server, `System.Data.SqlClient` | SQL Server, `System.Data.SqlClient` |
| ORM | None — raw ADO.NET, stored procedures only | None — raw ADO.NET, stored procedures only (EF6 referenced, unused) | None — raw ADO.NET, stored procedures only (EF6 referenced, unused) |
| Data-access helper | Two competing internal helpers (`DALHelper.cs`, `DbAccess.cs`) plus widespread direct inline `SqlConnection`/`SqlCommand` usage — **inconsistent** (`hiso/Architecture.md`) | Consistent: all reachable code goes through `DALHelper`/`DbAccess` wrappers (`karo/DatabaseAnalysis.md`) | Consistent: `DALHelper` used for all reachable ERMS code; `DbAccess`'s static-state class present but **not called** by ERMS controllers (`erms/DatabaseAnalysis.md`) |
| Thread-unsafe shared connection pattern | **Present and reachable** — `DbAccess.cs` static `SqlConnection`/`SqlCommand`/`DataSet` fields, used inline in several classes (`hiso/SecurityAnalysis.md` finding #5, `hiso/RiskAssessment.md` R-02) | `DbAccess.cs` exists with the same static-state pattern but is **not confirmed reachable** from KARO's own controller path (KARO's own reports do not explicitly confirm reachability either way — treat as unconfirmed) | Same static-state `DbAccess.cs` class **confirmed present but only reachable via `DAL\UI\GUIDA.cs`**, which ERMS's controllers do not call (`erms/DatabaseAnalysis.md`) — i.e., a **latent** landmine, not currently live |
| Multi-tenancy model | Fixed 4 named connections, no per-practice routing; second-node/AWS routing via procedure-name allow-lists | Per-practice connection string name concatenation (`"ConnIndiciDB"+practiceid`), 12 distinct connection-string targets defined across the whole DAL (only 2 reachable from KARO) | Per-practice connection string name concatenation (same pattern/DAL code), 15 connection strings defined in `Web.config` (7 practice-keyed pairs + 1 default) |
| Credential handling | Plaintext in `Web.config`, same `pms_nz`/`pms@@nz` account reused across all 4 connections | Plaintext in `Web.config` across 6 environment variants, same credential pattern | Plaintext in `Web.config` across 15 connection strings, **several pointing at a public IP** (`43.255.162.58`) — the most exposed of the three |
| Stored procedure count (reachable) | 30+ (various schemas: `Hiso`, `Appointment`, `Billing`, `dbo`) | ~35 (`[HSS]` schema, plus `[dbo]` DMS procs) | ~40 combined (`[HSS]`, `[dbo]`, `OnlineClaim` schemas) |
| Soft-delete/audit convention | Confirmed via `IsActive`/`IsDeleted`/`InsertedBy`/`UpdatedBy` columns populated by C# code before save calls | Confirmed only where raw SQL text is visible (`DBMessages.cs`); not confirmed for `[HSS]`-schema tables used by KARO itself (opaque stored procedures) | Same as KARO — not confirmed for ERMS's own `[HSS]`/`OnlineClaim` tables; only inferred from adjacent (non-ERMS) DAL code |
| Confirmed SQL injection | **None found** in any reachable code path — only an in-memory `DataTable.Select()` string-building smell (low severity) | **Confirmed** in `DMSDA.cs`/`DBMessages.cs`, not reachable from KARO's own controller today | **Confirmed**, same files/near-identical line numbers as KARO — not reachable from ERMS's own controllers today |
| Connection lifetime management | Two competing patterns; `DbAccess.cs` uses unsafe static shared connections | `DALHelper` opens connection-per-call correctly; `DbAccess.cs`'s unsafe pattern present but unconfirmed-reachable | `DALHelper` opens connection-per-call correctly (explicitly confirmed via `using` blocks); `DbAccess.cs`'s unsafe pattern confirmed present but not reachable from ERMS's own controllers |

**Overall:** the three APIs' database access patterns are best described as one shared,
inconsistent DAL codebase (KARO/ERMS, and structurally similar/possibly-forked in HISO) that
happens to be *safe* wherever each API's own controller actually calls it (all parameterized
stored procedures), but carries dormant SQL-injection and thread-safety landmines in code paths
none of the three currently exercise. Multi-tenancy is handled completely differently in HISO
(fixed connections + allow-lists) versus KARO/ERMS (per-practice connection-string-name
concatenation) — the unified platform cannot simply "keep the common pattern" since HISO has no
equivalent tenant-routing concept to reconcile.

---

## 7. Authentication Differences

| Aspect | HISO | KARO | ERMS |
|---|---|---|---|
| Mechanism | Opaque session GUID looked up in DB per call (`HealthLinkSession.GetByGUID`) — **not a credential exchange**, no login step | Custom bearer-token string, issued by `Authenticate` (username/password → DB validation), re-validated **per call** against patient+encounter+practice via `[HSS].[uspInsertAndValidateToken]` | Same pattern as KARO: custom bearer-token, issued by `Authenticate`, re-validated per call against `[HSS].[uspInsertAndValidateToken]` |
| Framework-level auth | None — WCF `security mode="None"` | None — `<authentication mode="None"/>`, OWIN `Startup` empty despite full OAuth/Identity package references | None — identical: `<authentication mode="None"/>`, OWIN `Startup` empty, identical unused OAuth/Identity package set |
| Role/claims/permission model | None | None — single binary "valid for this patient+encounter+practice" outcome | None — identical binary outcome model |
| Token expiry | N/A (session GUID has no application-level expiry logic visible; presumably enforced in the stored procedure) | Configurable via `expiryInDays` parameter, but **never passed a non-zero value** from the controller — defaults entirely to the stored procedure | Configurable via `AppSettings["ExpiryInDays"]` (observed 0.5 days = 12 hours) — actually wired up, unlike KARO |
| Credentials-in-transit issues | `getDeliveryOptions` returns a plaintext EDI password in the SOAP response body | `GET /api/Authenticate` accepts username/password as URL query-string params (logged, URL-exposed); POST variant also logs plaintext password | No GET-based credential endpoint found; but `ERMSAPIProxy` logs the full `Authorization` header (live bearer token) when forwarding to the Azure mirror |
| Unauthenticated/weakly-authenticated endpoints | All 6 operations require *some* session GUID; no operation skips auth entirely | `SaveScreeningCode` — **no token validation at all**, unconditionally returns fake success | None found with zero auth — but "fail open to HTTP 200" (see Security) undermines detection either way |
| HTTP status semantics on auth failure | SOAP `FaultException` (distinguishable) | Always HTTP 200, error communicated only in JSON body | Always HTTP 200, error communicated only in XML/JSON body |

**Summary:** None of the three systems has real, modern authentication. HISO's "auth" is not
even a credential exchange — it is possession of an opaque, non-expiring GUID with no
single-use or expiry enforcement visible in the application layer. KARO and ERMS share what is
functionally the **same** custom bearer-token design (same stored-procedure name, same
re-validation-per-call pattern, same lack of roles/claims), differing only in minor details
(ERMS actually wires up token expiry; KARO does not). All three reference unused
OAuth/OWIN/Identity packages that could mislead an engineer into believing framework auth
exists. **None of the three mechanisms should be carried forward** — the unified platform needs
a single, real OAuth2/OIDC (or equivalent) layer designed from scratch, informed only by the
authorization *scoping* concept common to KARO/ERMS (token valid for a specific
patient+encounter+practice tuple), which is a legitimate design input even though the
implementation is not.

---

## 8. Security Differences

| Issue type | HISO | KARO | ERMS |
|---|---|---|---|
| Authentication/authorization | Critical — GUID-only, no expiry/single-use check | Critical — no framework auth, one endpoint (`SaveScreeningCode`) has zero validation | Critical — no framework auth, fails open to HTTP 200 on every auth failure |
| Hardcoded DB credentials | Critical — 4 connections, same account reused, plaintext in `Web.config` | Critical — 6 environment variants, plaintext in `Web.config` | Critical — **worst of the three**: 15 connection strings, several pointing at a **public IP** (`43.255.162.58`) |
| Hardcoded encryption/crypto secrets | Critical — hardcoded SQL symmetric-key password (`tcpepms*1`) | Critical — hardcoded Rijndael key (ID obfuscation) **and** the same hardcoded SQL symmetric-key password | Critical — hardcoded Rijndael key (ID obfuscation, explicitly the same design pattern/comment style as KARO's); SQL symmetric-key password not explicitly re-confirmed in ERMS reports (see Section 2.7) |
| SQL injection | None found in any reachable path (only a low-severity `DataTable.Select()` string-building smell) | Confirmed in dormant DAL modules (`DMSDA.cs`, `DBMessages.cs`), not reachable from KARO's own controller | Confirmed in the same dormant DAL modules, same code, not reachable from ERMS's own controllers |
| CORS | N/A (WCF SOAP, no CORS concept) | Wide open (`origins:"*"`) on the **entire controller**, including all patient-data endpoints | `APIController` has CORS **commented out** (safer default); `COLController` — which includes the financial `SaveInvoice` — has wildcard CORS **active** |
| Exception/fault detail exposure | `includeExceptionDetailInFaults="true"` — leaks internal exception messages to any SOAP caller | Debug compilation left enabled (`debug="true"` in base `Web.config`) | Same debug-compilation-enabled issue in base `Web.config` |
| Sensitive data in logs | Connection strings (with embedded credentials) and PHI-adjacent IDs logged in plaintext | Plaintext passwords and full clinical note text (SOAP/JSON bodies) logged | Plaintext bearer tokens (via `ERMSAPIProxy`), patient/encounter IDs, and full response bodies logged |
| Rate limiting | None | None | None |
| File upload validation | N/A (no direct file-upload endpoint; documents arrive via rendered content) | No size limit, no content/magic-byte validation, no AV scanning on `SaveDocument` | Same gap; `maxRequestLength` caps request size (10MB) but no content-sniffing/AV |
| Response-code semantics undermining tooling | WCF faults are distinguishable (a partial exception to the "fails open" pattern seen in KARO/ERMS) | **All** responses return HTTP 200 regardless of outcome | **All** responses return HTTP 200 regardless of outcome (except `SaveDocument`, which can return 400) |
| Unsafe document rendering | Aspose HTML→PDF conversion with no confirmed sanitization — potential SSRF vector via external resource loading | N/A (no server-side document rendering; KARO stores pre-rendered content) | N/A (same — ERMS stores pre-rendered content, does not render) |

**Overall severity ranking:** ERMS has the most exposed secrets footprint (public-IP database,
15 credential sets) and the most inconsistent CORS policy (financial endpoint wide open while
its sibling controller is safer). KARO has the broadest blast radius for its CORS
misconfiguration (applies to every endpoint, not just one). HISO is the only one of the three
with no confirmed dormant SQL-injection code and the only one with a genuinely distinct fault-
handling channel (SOAP faults), but its GUID-only session model with no expiry is arguably the
most conceptually broken authentication of the three. All three share the fundamental pattern
of "parameterized queries in the code path actually used, but real injection risk lurking in
dead/dormant code that ships in the same artifact" — none of this should be carried forward.

---

## 9. Logging Differences

| Aspect | HISO | KARO | ERMS |
|---|---|---|---|
| Framework | Proprietary `Logger.dll` singleton (`Logging.Instance`), different `HintPath` than KARO/ERMS's copy | Same proprietary `Logger.dll` singleton, confirmed shared across a wider `MHNPHMP-Integration` solution | Same proprietary `Logger.dll` (`Logging.Instance`); **also** has a second, unused `NLog` reference in one file (`DMSAWS.cs`) — two coexisting, non-integrated logging mechanisms |
| Sink | Local flat files, path from `LogRoot`/hardcoded (`D:\Publish`, `C:\Logs\...`) | Local flat files, one per day, category-based subfolders | Local flat files, one per day (`IntegrationServices\EventLogs`/`ExceptionLogs`), category-based subfolders |
| Correlation ID | None | None | None |
| Structured/JSON logging | None — free-text strings | None — free-text strings | None — free-text strings |
| Log levels | None (unconditional writes) | None (unconditional writes) | None (unconditional writes) |
| PHI/secret exposure | Connection strings + credentials logged; `PatientId`/`PracticeId` in plaintext | Plaintext passwords + full clinical note text + patient IDs | Plaintext bearer tokens (via proxy) + patient/encounter IDs + full request/response bodies, including raw incoming document payload in at least one place before scrubbing |
| Silent logging failure | Not explicitly documented as failing silently | Not explicitly documented as failing silently | **Explicitly confirmed**: all file I/O wrapped in empty `catch {}` blocks — logging can fail invisibly with no fallback/alert |
| Known self-documented gaps | "Static mode" `getData` branch has **zero logging**, with a literal TODO comment left in production code | None specifically flagged | None specifically flagged |
| Security-event logging (failed auth attempts, etc.) | None — failed session lookups are indistinguishable from DB errors (swallowed exception) | None — no dedicated audit trail distinguishing access from debug tracing | None — same gap |

**Overall:** logging maturity is uniformly poor and essentially identical in kind across all
three systems (same underlying library family, same anti-patterns: no correlation IDs, no
structured logging, no redaction, PHI/secrets logged by design). ERMS is marginally worse in
practice because it is the only one with a *confirmed* silent-failure mode in the logger itself
and a second, unintegrated logging framework. None of the three provide anything resembling
security-event/audit logging, which is a shared, three-way gap the unified platform must fill
from scratch.

---

## 10. Integration Differences

| External system | HISO | KARO | ERMS |
|---|---|---|---|
| SQL Server (primary PMS) | Live — `PMS_NZ_V2` via 2 named connections + allow-listed second-node routing | Live — `ConnIndiciDB[+practice]`, `[HSS]` schema | Live — `ConnIndiciDB[+practice]`, `[HSS]` and `OnlineClaim` schemas |
| DMS (Document Management System) | Live — both direct-DB (`PMS_NZ_DMS`) and external `DMSProxy` SOAP/ASMX service (plain HTTP, internal IP) | Live — `ConnDMSDB[+practice]`, direct DB only | Live — `ConnDMSDB[+practice]`, direct DB only |
| AWSDoc (AWS-backed document storage) | Live, conditional per-practice | Live, conditional per-practice | Live, conditional per-practice |
| Aspose (Words/Cells/Pdf) | Live — HTML/image → PDF/DOCX rendering, pinned to a ~2016-era Aspose.Words build | Not present | Not present |
| GP2GP (UK-style clinical record transfer) | Not present | Dormant — `DAL\GP2GP\PIDAO.cs`, not called by KARO's controller | Dormant — same module, not called by ERMS's controllers |
| MHNHL7 (HL7 messaging) | Not present as a named module (HISO's own concept-mapping engine is HISO/HL7-standard-based but is not the same as the `MHNHL7` DAL module) | Dormant — `DAL\MHNHL7\DBMessages.cs`, contains the confirmed SQL-injection code, not called | Dormant — same module/file, not called |
| BPAC / BPI (older PMS integration, ACC18 claims, NIR batch extract) | Not present | Dormant — not called | Dormant — not called |
| Pegasus / "OnlineClaim" schema (online claims integration) | Not present | Dormant — `DAL\Pegasus\PHCO.cs`, not called by KARO's own controller | **Live** — `DAL\Pegasus\PHCO.cs`, called by every `COLController` action (see Section 5 — likely the same module KARO carries dormant) |
| Procare / Procon | Not present | Dormant — near-duplicate `[HSS]`-schema integrations, not called | Not documented as present in ERMS's Phase 1 reports — `> Unable to verify from available source` whether ERMS's DAL also ships Procare/Procon (KARO's report lists them; ERMS's `DependencyAnalysis.md` does not list Procare/Procon in its "not called" table, suggesting they may not be present in ERMS's copy of the DAL — should be confirmed directly) |
| Screening | Not present | Dormant — not called | Dormant — `DAL\ScreeningDAL.cs`, not called |
| UI/GUIDA (admin/config support) | Not present | Dormant — not called | Dormant — not called, but is the sole caller of the unsafe static `DbAccess.cs` pattern |
| DMSAWS (`MHN.DAL.DMSAWS` namespace) | Not present as a named module | Not documented as present in KARO's Phase 1 reports | Present — `DAL\DMSAWS\DMSAWS.cs`, cross-application namespace (`MHN.*`), not called by ERMS's own controllers; evidence the DAL project's boundaries don't map cleanly to any one of the three systems |
| "Azure ERMS API" mirror | Not present | Not present | Live-conditionally — proxy-forwards requests for Azure-flagged PHOs to a separate deployment (`ERMSAPIProxy.cs`) |
| MHNEntity (external project reference) | Present, referenced but out of scope for HISO's own review | Not present | Not present |

**Summary:** the "shared DAL with mostly-dormant modules" pattern is real and three-way
consistent for GP2GP, MHNHL7, BPAC/BPI, Screening, UI — these are dormant in both KARO and
ERMS and absent from HISO entirely (HISO's DAL is not the same shared library — it is a
locally-embedded, differently-structured DAL). The one significant divergence is **Pegasus**:
dormant in KARO, but the *exact same functional area* is live in ERMS via `COLController`. This
is the clearest evidence in the whole comparison that the shared DAL genuinely serves multiple
sibling applications, with different applications activating different slices of the same
underlying integration surface.

---

## 11. Functionality That Should Be Merged Into The Unified Platform

This section synthesizes the above findings into concrete capabilities that currently exist
redundantly (2-3 times, independently implemented or duplicated-by-copy) and should exist
**exactly once** in the unified platform.

1. **One canonical patient/encounter/practice identity and tenant-resolution service.**
   Currently: HISO resolves context via session-GUID lookup; KARO and ERMS both parse a
   composite `encounterId` string via ad-hoc delimiter-splitting repeated ~25-30 times per
   codebase (Section 6, Section 4). None of these should be ported as-is. The unified platform
   needs a single, explicit, testable tenant/practice-resolution component that all clinical
   data endpoints call through — not string-splitting duplicated per-action.

2. **One real authentication/authorization layer (OAuth2/OIDC or equivalent), replacing all
   three ad-hoc mechanisms.** HISO's GUID-only session trust, and KARO/ERMS's shared
   hand-rolled bearer-token-revalidated-per-call scheme, must all be replaced. The one design
   input worth preserving conceptually (not implementing-as-is) is KARO/ERMS's pattern of
   scoping a token's validity to a specific patient+encounter+practice tuple, which is a
   legitimate fine-grained-authorization requirement even though today's implementation of it
   is insecure and DB-round-trip-per-call.

3. **One canonical clinical-data access surface (demographics, conditions, medications, labs,
   consult notes, observations, documents) instead of three parallel implementations.**
   Section 5 shows KARO's `APIController` and ERMS's `APIController` are near-total functional
   duplicates over the same `[HSS]`-schema stored procedures, differing mainly in response
   serialization (JSON vs XML) and endpoint naming. HISO's dynamic concept-mapping engine
   (`ConceptMapper/HisoConceptDetail.cs`) is architecturally the most flexible of the three
   approaches (DB-driven concept→procedure mapping, extensible without redeploy) and is worth
   evaluating as the *design pattern* for the unified data-access layer, even though its
   current implementation (config-driven business rules in `Web.config`, thread-unsafe DAL)
   must not be ported directly.

4. **One canonical document-generation and storage service**, consolidating: HISO's
   Aspose-based server-side rendering (HTML/image → PDF/DOCX), and the KARO/ERMS/HISO-shared
   two-step "DMS write + PMS index" pattern with AWS/on-prem routing per practice. Currently
   three separate implementations of "does this practice use AWS storage" exist
   (`hiso/BusinessRules.md` BR-06, `karo/BusinessRules.md` BR-18, `erms/BusinessRules.md`
   BR-09) with subtly different fallback behavior (HISO has an explicit fallback-on-exception;
   KARO/ERMS do not document one). This must become a single, explicitly-tested document
   storage abstraction, not three copies of the same AWS-enablement check.

5. **One shared secrets-management approach**, eliminating the identical hardcoded
   `'tcpepms*1'` SQL symmetric-key password (present in HISO and KARO's `DALHelper.cs`, likely
   also in ERMS's — see Section 2.7), the hardcoded Rijndael ID-obfuscation keys (KARO, ERMS),
   and all plaintext database credentials (all three, most severely exposed in ERMS's
   public-IP-facing connection strings). This is a single remediation effort, not three.

6. **One structured, correlated, redacted logging/observability layer**, replacing the shared
   `Logger.dll` family used (with minor variations) by all three systems. All three currently
   log PHI, credentials, and/or bearer tokens in plaintext with no correlation ID and no
   security-event category — this is a uniform three-way gap, not three separate problems, and
   should be solved once (structured logging framework + correlation ID middleware + mandatory
   field-level redaction) and applied everywhere.

7. **A single decision on the fate of the 9-11 dormant shared-DAL modules** (BPAC, BPI, DMS,
   GP2GP, MHNAppointment, MHNHL7, Procare, Procon, Screening, UI, DMSAWS). These modules are
   present across KARO and ERMS's DAL projects, structurally consistent with a shared
   `MHNPHMP-Integration` solution family, and contain the only confirmed SQL-injection code in
   any of the three codebases. Before the unified platform's design proceeds, the client must
   confirm: (a) which of these modules are genuinely needed by systems outside HISO/KARO/ERMS's
   scope (and therefore must be preserved somewhere, just not necessarily inside the unified
   platform), and (b) whether any are safe to retire outright. **Do not port any of them
   forward without parameterizing the confirmed SQL-injection sites first**, regardless of the
   retire/keep decision.

8. **A single, explicit decision on the Pegasus/OnlineClaim/"COL" integration**, since Section
   5/10 shows this is dormant in KARO but live in ERMS (`COLController`) — almost certainly the
   same underlying capability. The unified platform should implement this once, informed by
   ERMS's live usage (endpoint shapes, `SaveInvoice` idempotency contract via magic return code
   `-3`) rather than treating it as ERMS-exclusive net-new work.

9. **A single invoice/billing write path with an explicit idempotency contract**, replacing the
   magic-return-code convention (`-3` = "already exists" in both KARO's `SaveInvoice` and
   ERMS's `COLController.SaveInvoice`, `-5` in KARO's `SaveCondition`) with a documented,
   versioned API contract (e.g., a proper `409 Conflict`-with-existing-resource pattern or an
   explicit idempotency-key mechanism) rather than three different undocumented numeric
   conventions scattered across stored procedures.

10. **A single practice-onboarding/configuration model**, replacing HISO's fixed 4-connection
    setup, and KARO/ERMS's per-practice `Web.config` connection-string-name convention (12 and
    15 targets respectively, requiring a config change + redeploy to onboard a new practice).
    None of the three approaches scales to a "10,000 concurrent users, multiple locations"
    target platform; this must be replaced with a data-driven tenant registry, informed by all
    three systems' current practice lists but implemented once.

> Unable to verify from available source: the actual T-SQL definitions behind the ~35-40
> stored procedures each API calls (none of the Phase 1 reports had direct database access).
> Several of the "duplicate business logic" findings above (Section 4) — particularly whether
> KARO's `[HSS].[uspHSSInsertUpdateService]` and ERMS's `[OnlineClaim].[uspInsertUpdateService]`
> are the same procedure, and whether HISO's/KARO's/ERMS's AWS-fallback behavior genuinely
> differs — should be confirmed against the live database before the unified platform's data
> model is finalized, as recommended independently by all three Phase 1 `DatabaseAnalysis.md`
> reports.
