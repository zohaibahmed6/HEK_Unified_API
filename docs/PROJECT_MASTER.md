# PROJECT MASTER — Single Source of Truth

**Read this file first, every session.** It tells you what the project is, what
is actually done, what is open, and which other doc to open next. It links out
to detail docs instead of duplicating them — when you finish a task, update
**this file's "Current State" section**, then update whichever detail doc
actually changed (see the map below).

Last updated: 2026-07-30

---

## 1. What this project is

HEK Core API — a unified healthcare API hub sitting in front of three legacy
practice-management systems (HISO, KARO/HSS, ERMS) plus COL/Pegasus. It exposes:

- **Legacy-compat surface** (`/hiso/*`, `/karo/*`, `/erms/*`, `/erms/col/*`) —
  byte-for-byte reimplementations of the real legacy APIs, for zero-consumer-change
  migration. This is the surface real external clients use today.
- **Canonical/hub surface** (`/v1/patients/*`, etc.) — a new merged, field-scoped
  REST layer built additively on top of the same underlying data, not yet the
  public surface (see `hek_analysis/PROJECT_STATUS.md` 2026-07-22 decision).

## 2. Current state (update this every session)

- **Status (2026-08-03)**: Migrated from .NET 8 to .NET 10 (all 13 projects) — build clean,
  63/66 tests pass (3 pre-existing live-DB failures, unrelated to the migration), Docker
  containers healthy on net10. See `MIGRATION_STATUS.md` + `docs/migration/*.md` for full detail.
- **Status (2026-08-03)**: All 3 carried-forward open items now closed — ERMS
  Azure-forwarding gap + SOUTH-environment closed by Zohaib as accepted/out-of-scope;
  Measurements delimiter shape re-checked and found already correctly decoded (stale
  note), only the unbuilt canonical Measurements resource remains as a future-work
  item, not a blocker. See §2 bottom for detail.
- **Deliverable (2026-07-31)**: wrote `docs/azure-deployment-checklist.md` - a step-by-step,
  checkbox deployment-day checklist for Azure Container Apps (secrets hardening, resource
  provisioning, image build/push, Container App deploy, legacy host-domain DNS cutover reused
  verbatim from `docs/deployment.md`, post-deploy verification, known gaps). Region and Entra ID
  account are deliberately treated as not-yet-decided/switch-later, per Zohaib - deployment can
  proceed without them.
- **Deliverable (2026-07-31)**: built `crosscheck/HEK_Complete_Verified.postman_collection.json` (118
  requests) + updated `crosscheck/HEK_4APIs.postman_environment.json` - one ready-to-execute Postman
  collection covering all 59 tracked legacy operations (HISO 6, ERMS 23, KARO 23, COL 7) x2 (Legacy vs
  New API folders), every request's body/params sourced from `crosscheck/errors.md`,
  `crosscheck/SUMMARY.md`, `crosscheck/PARITY_MEMORY.md`, and the prior manual-verification collection
  (not invented), with a provenance note in each request's `description` field; 4 HISO ops (getData/
  saveContainer/getFormView) and 1 ERMS op (radiology reference id) flagged as needing a live capture
  since no full real success body was preserved verbatim in the docs.
- **Fix (2026-07-31)**: `GetRegisteredPractitioners`/`GetPrescribedMedications`/`GetRegularMedications`/
  `GetConsultNotes` row order made deterministic. Earlier same-day investigation found these 4 ERMS
  reads return the same complete row set as legacy, just in a different order each call - proved this
  is real non-determinism in the underlying SP (no stable `ORDER BY`) by calling real legacy itself
  twice and seeing its own order change too - and left it as "not a defect." Zohaib asked for the new
  API's order to match legacy's intent regardless of legacy's own inconsistency, so added
  `ErmsDataRepository.StableSort` (`src/Infrastructure/Legacy/Erms/ErmsDataRepository.cs`) - sorts the
  returned `DataTable` by its date column (`date`/`startDate`, honoring the requested `ASC`/`DESC`) then
  always by `ReferenceId` as a tiebreaker, applied to `GetRegisteredPractitionersAsync`,
  `GetConsultNotesAsync`, and `GetMedicationsAsync` (covers both Prescribed/Regular). Rebuilt/redeployed
  Docker `api`, live-verified: 3 repeated calls to each of the 4 operations now return byte-identical
  `referenceID` order every time (MD5-compared) - previously this would vary between calls.
- **Fix (2026-07-31)**: found and fixed a real ERMS bug during a requested retest of
  `GetScannedDetails`/`GetDischargeSummaryDetails`. The earlier crosscheck note ("legacy crashes, new
  API returns empty - behavioral difference, not a bug") only held because the original test record had
  no actual binary content. Retested `GetScannedDetails` against a different patient (2459731) with real
  scanned documents and got a genuine 500 on the new API: `Cannot set column 'Content'. The value
  violates the MaxLength limit of this column.` Root cause in `ErmsDataRepository.GetDocResultsAsync`
  (`src/Infrastructure/Legacy/Erms/ErmsDataRepository.cs`) - the AWS-enrichment code cleared `ReadOnly`
  on the `Content`/`DocumentId`/`DataType` `DataColumn`s before writing enriched data (needed since
  `uspGetDocResults_AWS`'s schema flags them read-only) but never cleared `MaxLength`, which is
  inherited narrow from the SQL schema since the column is normally empty pre-enrichment - a real
  base64 document overflows it. Fixed by also setting `MaxLength = -1` on all three columns. Rebuilt/
  redeployed Docker `api`, live-verified: same referenceId now returns the full real document
  (~194KB response, real base64 PDF) instead of crashing. `GetDischargeSummaryDetails` shares this exact
  code path (`isDischarge=true`), so the fix covers both operations. Separately corrected an imprecise
  claim: legacy's empty-`referenceId` behavior on `GetDischargeSummaryDetails` was described as
  "hangs indefinitely" - retested with a real timer, it's actually slow (2-14s) then throws a genuine
  `NullReferenceException`, not a true hang.
- **Fix (2026-07-31)**: KARO `SaveRecall` fully resolved - was never a new-API bug, just bad
  test data. Real valid `group`/`categoryId` derived live from `uspGetRecalls`/`uspGetRecallCategories`
  (`group="Vaccine"`, `categoryId=4690`) and retested against both real legacy (`localhost:2345`) and
  the new API - both now return identical `{"status":"success","message":""}`. All 60 KARO legacy
  operations now confirmed.
- **Fix (2026-07-31)**: COL `SaveInvoice` fully resolved - was never a NOT NULL/schema bug at all,
  root cause was a missing required business field. Zohaib supplied the real SP source
  (`legacy-reference/legacy SP/saveinvoice.txt`, `[OnlineClaim].[uspInsertUpdateService]`), which
  settled everything the earlier blind `sqlcmd` trial-and-error had gotten wrong:
  - `@pMasterServiceName` (the `"COL"` parameter) is **never referenced anywhere in the SP body** -
    it's a dead/unused parameter. The SP always resolves/creates the subservice under a hardcoded
    `'General Services'` master service instead, matched by `@pClaimShortCode` (not
    `@pSubServiceCode`/`@pSubServiceName` as assumed). The `('COL', InsertedBy=1)` row seeded into
    `[Billing].[tblMasterService]` earlier this session was consequently pointless - left in place
    (harmless, SP never reads it) since this DB user has no `DELETE` permission to remove it.
  - The real `InsertedBy` value for a new `tblMasterSubService` row is `@vProviderId`, resolved
    *inside* the SP: first by matching `@pServiceProvider` (an NZMCNO) against `Profile.tblProvider`,
    falling back to `Profile.tblPatientEnrollmentAgreement.ProviderId` for the patient if no provider
    was supplied. Confirmed live: patient 2450776's enrollment agreement has `ProviderId = NULL` -
    so any call omitting `ServiceProvider` was guaranteed to crash on the NOT NULL constraint,
    completely independent of any master/subservice seeding.
  - Fix: supply a real `ServiceProvider` (a real NZMCNO for the practice, e.g. `"111"` for practice
    901, confirmed via `Profile.tblProvider`) and a real `ClaimShortCode` in the request. Verified
    twice: once via direct `sqlcmd EXEC` of the SP (real invoice ID `18279074`), then end-to-end
    through the actual running API (`POST /COL/SaveInvoice` -> `{"status":"success","message":"18279076"}`).
  - Not a code defect on either side - legacy's own C# passes `ServiceProvider` straight through
    unvalidated too, so an omitted/invalid provider with no patient-enrollment fallback would fail
    identically on real legacy. Nothing to change in `ColDataRepository`/`ColSaveInvoiceRequest` -
    both already forward every field the SP actually needs; the earlier blocker was purely test-data
    (`ServiceProvider` was never supplied in prior test payloads).
- **Finding (2026-07-31)**: COL Authenticate + 4 dependent GET operations
  (`GetCurrentPatientData`/`GetProviderData`/`GetSurgeryData`/`GetDiagnosisData`), previously
  unconfirmed because real legacy always failed the test, are now confirmed working correctly on the
  new API - Zohaib supplied real production credentials for a live test. New API: succeeds in
  <1s with a real token and returns correct real data for all 4 dependent operations. Real legacy:
  genuinely **hangs** (4+ minutes, no response) on Authenticate and all 3 tested GET operations with
  the same credentials/patient. Root cause found in `Web.config` (`ermsapi/DevLocal/ERMSWebAPI/.../Web.config`):
  this account/practice's connection-string resolution points at a remote IP (`43.255.162.58`) that's
  unreachable from this dev machine - same class of issue as HISO's DMSProxy (legacy itself cannot
  reach a dependency in this environment, not a new-API defect). Not something to fix in the new
  API - it always resolves to the local dev connection regardless, so it isn't affected. Only
  `COL SaveInvoice` remains unconfirmed (needs a real request payload).
- **Follow-up (2026-07-31)**: Derived `COL SaveInvoice`'s exact real request shape directly from
  legacy source (`COLController.cs:465`'s `SaveInvoice` model - all 14 fields are plain `string`, no
  number-wrapper as an earlier session guess had assumed) - confirmed it already matches this port's
  `ColSaveInvoiceRequest` (`ColModels.cs`) field-for-field. Testing with this correct shape eliminated
  the earlier 400 shape-validation error entirely - the request now reaches
  `[OnlineClaim].[uspInsertUpdateService]` cleanly (confirmed via the new SP-call logging: exact
  parameters, no SQL exception). The real stored procedure itself rejects the values (`@pOutputParam`
  stays `-1` - no row inserted/updated) for 2 different plausible `ServiceCode`/`ServiceName`/
  `ServiceProvider` combinations tried. This is now a pure data-lookup gap, not a shape/wiring one -
  needs a real, DB-registered `ServiceCode` to complete; can't browse the lookup table directly
  (`sys.procedures`/schema browsing is permission-denied for this DB user, same restriction noted in
  `hek_analysis/v1.1-plan-status.md`'s step 8 entry).
- **Follow-up (2026-07-31, same investigation)**: called `[OnlineClaim].[uspInsertUpdateService]`
  directly via `sqlcmd` (bypassing both APIs entirely) to find the real root cause. Found:
  `[Billing].[tblMasterSubService].InsertedBy` is a `NOT NULL` column with no default. Confirmed via
  direct query that practice 901 has real, existing services (e.g. `"Medical Examination"`/`"#KF005"`,
  `InsertedBy=57789`) - but the "COL" master-service scope specifically has zero existing rows for
  this practice, so every call (even reusing that exact real name/code) takes the SP's INSERT branch,
  which fails on `InsertedBy` - nothing in either the ported C# call (`ColDataRepository.SaveInvoiceAsync`)
  or the real legacy call (`HSSDA.InsertUpdateService`) supplies this value; both have the identical
  parameter list. This is a genuine real-data/environment gap (this dev DB's practice 901 has simply
  never had a COL invoice before), not a new-API defect - and since real legacy's own COL Authenticate
  hangs on the unreachable remote DB (see the Finding above), legacy can never even reach this call to
  compare against in this environment either. Left unresolved, flagged precisely rather than guessed
  at further - would need either a real numeric `InsertedBy`/staff ID from Zohaib, or DB admin access
  to inspect the SP body (`sys.parameters`/`sys.types` are also permission-denied for this DB user).

- **Fix (2026-07-31)**: Cleared most of the remaining cosmetic mismatches from the crosscheck.
  (1) HISO SOAP `getVersion`/`processAction`/`getDeliveryOptions` response wrappers - real legacy
  uses a bare `<return>` element, ours emitted the WCF-default `{Type}Return` name since none of these
  3 response classes had an explicit `[MessageContract]`. Fixed by adding one plus `[XmlElement("return")]`
  on the body member - confirmed live that `[MessageBodyMember(Name=...)]` alone does nothing under
  SoapCore's `SoapSerializer.XmlSerializer` mode (first attempt), `[XmlElement]` is what that mode
  actually reads. Live-verified all 3 now emit `<return>`. (2) KARO `Authenticate` response
  (`HssAuthenticateResponse.cs`) always serialized all 5 fields, so success responses carried an
  extra `"message":null` legacy never sends (and fail responses carried extra
  `token`/`expiry`/`practiceId`:null). Added `JsonIgnore(WhenWritingNull)` to the 4 branch-specific
  fields - live-verified the fail branch now returns exactly `{"status","message"}`. (3) KARO
  `GetDemographics` was missing `EndEnrolmentDate` entirely (confirmed against real legacy's
  `APIModels.cs:42` - a genuinely dropped field, not a data issue, since `uspGetDemographics` returns
  the column) - added to `KaroDemographicInfo`/`KaroDemographicsRepository`, live-verified the field
  now appears in the response (null for this test patient, whose enrolment hasn't ended - correct).
  **Update (same day, later)**: both remaining items resolved after live-testing directly against the
  real legacy servers (found their real IIS Express ports via `applicationhost.config` -
  KARO/HSS `localhost:2345`, ERMS `localhost:2003` - both still running locally).
  - **Null-vs-empty-string**: direct call to real legacy (`http://localhost:2345/API/GetDemographics`)
    confirmed it genuinely sends `"dayPhone":""`/`"endEnrolmentDate":""`, contradicting the code-only
    analysis above. Real mechanism: `Convert.ChangeType(DBNull.Value, typeof(string))` does NOT throw
    (unlike value types) - `DBNull` implements `IConvertible.ToString()` as `string.Empty`, so
    legacy's reflection mapper succeeds and assigns `""`, it only skips-on-exception for non-string
    columns. Fixed `DataTableMapper.ToList<T>` (`src/Infrastructure/Legacy/Hiso/DataTableMapper.cs`)
    to set `""` for DBNull string properties instead of leaving them unset, and the two hand-written
    `Str()` helpers (`KaroDemographicsRepository.cs`, `KaroDataRepository.cs`) to return `""` instead
    of `null`. Live-verified: `GetDemographics` (dayPhone/endEnrolmentDate) and `GetClinicalNotes`
    (`appointmentAdvice` - was 42/50 records mismatched) both now match legacy with 0 diffs across
    every field, entry-by-entry.
  - **Line-ending mismatch**: traced to `ErmsRtfConverter.ConvertString2Rtf`
    (`src/Adapters.Erms/Hiso/ErmsRtfConverter.cs`) using `Environment.NewLine` - which is `"\r\n"` on
    Windows (where real legacy runs) but `"\n"` on Linux (this app's Docker container), a genuine
    cross-platform bug, not a legacy-parity gap in the usual sense. Replaced with the literal `"\r\n"`
    (2 sites). Live-verified against both APIs with the exact record from the original crosscheck
    finding (referenceID `B3A928B7-...`, patient 2450776) - decoded base64 content is now
    byte-for-byte identical (`DQoNCkNCQzoJMTUgIA==` both sides, hex `0d0a 0d0a 4342 433a...`).
  All 4 backing repos affected use the same shared `DataTableMapper`/converter, so this closes the
  gap for every KARO GET endpoint and ERMS's lab/radiology report details uniformly, not just the
  2 originally-flagged operations.
- **Fix (2026-07-31)**: `getDeliveryOptions`'s 4 config keys (`Hiso:PracticeEdi`/`UserId`/`Password`/`Url`,
  `GetDeliveryOptionsQuery.cs`) were entirely missing from `appsettings.json`, only ever present in the
  gitignored `appsettings.Development.local.json` - so every Docker/Azure build returned
  senderAccount/senderPassword/url all empty (the known crosscheck gap). Added the keys with
  `CHANGE_ME_HISO_DELIVERY_*` placeholders (`PracticeEdi` set to the real `"1"` - not a secret, just a
  boolean flag); `UserId`/`Password`/`Url` are real HealthLink delivery-account credentials with no
  known-real value to copy in (even local dev never had a real `Url`/`Password` set). Confirmed live:
  `senderAccount` now correctly resolves from the session's own PracticeEDI (real value, e.g.
  `Testn28n6ujh`) when present, `senderPassword`/`URL` surface the CHANGE_ME placeholders as a visible
  signal instead of silent blanks - fill in real values via `Hiso__UserId`/`Hiso__Password`/`Hiso__Url`
  env vars before relying on this in Azure. Caught and fixed a self-inflicted bug in the same pass:
  first attempt added those 3 as unconditional `docker-compose.yml` env mappings
  (`Hiso__UserId: ${HISO_DELIVERY_USERID}` etc) - confirmed live that an *unset* host env var still sets
  the container's env var to an *empty string*, and ASP.NET Core's config providers treat any set env
  var (even empty) as an override that beats appsettings.json, silently blanking the CHANGE_ME
  placeholders back to empty. Removed those 3 lines; add them back (mirroring the existing `HISO_DMS_*`
  pattern already there) only once real credentials exist in `.env`.
- **Fix (2026-07-31)**: Zohaib asked for full observability - every request's real payload, exactly
  which stored procedure it hit, and the exact response, all in the per-system log files, so a
  production issue can be diagnosed from logs alone (his stated goal: eventually feed these logs to
  an AI that can self-diagnose/fix issues without needing a live repro). Built in 3 pieces:
  (1) new `RequestResponseLoggingMiddleware` (registered early, before the SOAP endpoint) pushes an
  ambient `"System"` LogContext property per request (resolved from path for internal-route testing
  and the fixed `/FormSessionService.svc` SOAP path, or from Host header via the existing
  `LegacyHostRouting:Rules` config for real external pre-rewrite calls) and logs the full raw
  request+response body in one line - closes the earlier-found gap where write-op controllers only
  ever logged a hand-picked `PatientId`/`EncounterId` (empty for POST-body operations like KARO
  SaveInvoice) instead of the real submitted fields. (2) `LegacyDbExecutor` (the shared ADO.NET
  helper ~30 repositories call through) now logs every stored-procedure call centrally - procedure
  name, every parameter's final value (byte[] blobs recorded as size-only, e.g. `byte[12345]`, to
  avoid dumping raw document bytes), and the outcome (rows affected/returned, or the exception) -
  using Serilog's static `Log` logger (added a plain `Serilog` package reference to the
  Infrastructure project) so it inherits the ambient `"System"` from (1) without needing to know
  which system called it. (3) Confirmed live end-to-end via a real `/karo/demographics` call: both
  `[HSS].[uspInsertAndValidateToken]` and `[HSS].[uspGetDemographics]` now appear in
  `logs/karo/technical-*.log` with their exact parameters and row counts, alongside the full
  request/response body line from (1) - nothing about that call is only visible via `docker logs`
  anymore. Also fixed 7 pre-existing untagged `_logger.LogError/Warning` calls across
  `HisoDocumentHandler`/`AsposeMimeConverter`/`HisoConceptExecutor`/`HisoRequestEngine` (no `System`
  tag, so they only ever reached the console) by adding the tag directly - kept alongside the new
  ambient-tag middleware as belt-and-suspenders. Docker `api` rebuilt/redeployed with all of this.
- **Fix (2026-07-31)**: Several HISO Infrastructure-layer classes
  (`HisoDocumentHandler`, `AsposeMimeConverter`, `HisoConceptExecutor`,
  `HisoRequestEngine`) logged errors/warnings via plain `_logger.LogError/Warning(...)`
  with no `"System"` structured property, so Serilog's per-system file router
  never matched them - those log lines only ever reached the raw console
  (`docker logs`), never `logs/hiso/technical-*.log`. Confirmed live: the known
  `HisoDocumentHandler` "AddDocument failed" error was completely absent from
  both `logs/hiso/technical-20260731.log` and `readable-20260731.log`, while
  KARO's equivalent controller-level logging (which already tags `"System"`)
  correctly appears in `logs/karo/technical-*.log`. Fixed by adding `"{System}"`
  to each message template and passing `"hiso"` as the log argument (7 call
  sites across the 4 files) - all HISO Infrastructure-layer errors now route to
  the per-system log file like everywhere else. Rebuilt and redeployed the
  Docker `api` container with this fix.
- **Legacy-compat parity**: HISO fully parity-confirmed (5/5 ops, field-diffed).
  ERMS mostly confirmed; **1 known real gap**: Azure-forwarding proxy for
  `Authenticate` is unported (`LEGACY_PARITY_VALIDATOR.md`). KARO/COL: see that
  file for per-operation status (⬜/✅/❌).
- **Canonical hub**: 14 resources built (Demographics, Conditions, Documents,
  Clinical Notes, Lab Results, Medications, Allergies, CurrentProvider,
  Practitioners, COL Conditions, Radiology Reports, Smoking Status, Next of Kin,
  Recalls/RecallCategories/ScreeningCodes). Deliberately **not built**:
  Measurements (undecoded delimiter shape), Encounter Summary (legacy stub, not
  real data), Observations (no confident HISO concept match).
- **v1.1 infra plan**: all 17 steps done and live-verified (`hek_analysis/v1.1-plan-status.md`).
  HISO now served over real SOAP (`/FormSessionService.svc`); JSON HISO path is
  frontend-internal only, dropped from public Swagger.
- **Fix (2026-07-30)**: All 5 SOAP request DTOs (`getVersion`, `getData`,
  `saveContainer`, `getDeliveryOptions`, `processAction`) were deserializing to
  `null` on every real legacy request, throwing a generic null-ref SOAP fault.
  Root cause: WCF's default implicit wrapping nests each parameter one level
  deeper than real legacy's wire shape (`&lt;getData&gt;&lt;sessionKey&gt;...`
  directly, no extra parameter-name wrapper) — confirmed via a live capture
  against the real legacy endpoint. Fixed with explicit
  `[MessageContract(WrapperName=...)]`/`[MessageBodyMember]` attributes in
  `IFormSessionService.cs` matching the real single-level shape. Also fixed:
  `getData`'s `submittedData` unwrap was grabbing the real legacy `<dummy/>`
  placeholder instead of the actual `<form>` payload (`FormSessionService.cs`).
  Both verified live against the captured request (now reaches real business
  logic — returns a legitimate `Invalid Session Key` fault instead of crashing).
  Response DTOs untouched — no captured sample yet to verify their wire shape,
  worth revisiting if a real response capture becomes available.
- **Fix (2026-07-30)**: `HisoRequestEngine.ParseRequest` was matching **zero**
  `<field>` nodes on every real `getData` request, so the entire concept ->
  procedure resolution pipeline silently never ran (no DB call, no AWS
  enrichment, `content` always empty) - not a data/DMS issue as it first
  looked. Root cause: real `submittedData`'s `<form>` payload carries the
  `urn:net.healthlink.genericform.model` namespace, and once it round-trips
  through SoapCore's `[XmlAnyElement]` handling .NET re-serializes it with an
  explicit `urn:` prefix - `SelectNodes("//field")` (XPath, no-namespace-only)
  and single-arg `GetElementsByTagName("field")` (exact-qualified-name-only)
  both miss it. Fixed by switching to the namespace-wildcard overload,
  `GetElementsByTagName("field", "*")`/`("group","*")`/`("section","*")`, in
  `HisoRequestEngine.cs` (3 sites), `GetDataQuery.cs`'s DMS-stamp helper, and
  `HisoProcessActionSaveRepository.cs` (3 sites - same bug, `saveContainer`/
  `processAction` path). The same `urn:` prefix also broke plain `.Name`
  equality checks (`HisoRequestEngine.cs` `isParentGroup`/`sectionName`/
  `FillGroupAsync`'s `child.Name == "group"`, `HisoProcessActionSaveRepository.cs`'s
  `field.Name != "field"`) - switched to `LocalName`, which ignores prefixes.
  (Correcting an earlier wrong conclusion in this same session: `content` empty
  for referenceID `a30f8f2e-...` was NOT a missing/placeholder document - it's
  a real attachment for practice 901/patient 2460688, confirmed once these
  fixes landed.)
- **Fix (2026-07-30)**: `HisoConceptExecutor.EnrichWithAwsAsync` never filled
  `Content` for a document that isn't actually stored in S3 - the `_AWS`
  procedure's `Content` column is blank by design for such rows (real content
  lives on the plain, non-`_AWS` procedure's result instead), and there was no
  fallback. Fixed by also running the plain procedure and backfilling
  Content/Size/Filename/DataType from its matching row when the AWS path
  leaves them empty - with the same byte[]/varchar column-type coercion the
  AWS-download path already needed (raw `byte[]` into a string-typed
  `DataColumn` silently produces `"System.Byte[]"` garbage, not an error).
  Also: an actual S3-download failure (misconfigured `DocumentManager` base
  URL) was aborting the rest of that row's enrichment via the shared per-row
  catch block - isolated the download attempt into its own try/catch so the
  plain-procedure fallback still runs afterward. Verified live: `getData` now
  returns real, valid base64 PDF content for `Patient_Attachment_Content`,
  matching the real legacy reference API's response exactly.
- **Fix (2026-07-30)**: HISO's DMS (document) database was resolved via a
  single global secret (`Hiso:DmsConnectionString`), routing every practice to
  the same DMS database regardless of whose session was being served. Fixed
  with a new `IHisoDmsConnectionResolver`/`HisoDmsConnectionResolver`
  (`src/Infrastructure/Legacy/Hiso/HisoDmsConnectionResolver.cs`), modeled
  directly on the already-working `ErmsDmsConnectionResolver`/
  `KaroDmsConnectionResolver` pattern: same server/credentials as the
  practice's primary HISO connection, only the database name differs
  (`Hiso:DmsDatabaseName`, defaults `"DMS_PMS"`) - no new registry column/
  migration needed. Auto-registered via `InfrastructureServiceCollectionExtensions.
  AddInfrastructureRepositories`'s convention-based DI scan (no manual
  `Program.cs` line required - same mechanism already covers Erms/KaroDms
  resolvers, confirmed live, correcting an earlier wrong assumption in this
  session that those two were unregistered).
- **Fix (2026-07-30)**: `AsposeMimeConverter.ConvertImageToPdfAsync` crashed
  every scanned-image attachment on Linux (`System.Drawing.Common`, which the
  vendored `Aspose.Words.dll` - a .NET Framework 4.0 assembly - depends on
  internally, is unconditionally blocked by .NET 8 on non-Windows regardless of
  `libgdiplus` or the `System.Drawing.EnableUnixSupport` config switch - both
  tried and confirmed ineffective live). Real fix (Windows-based Docker image,
  or a modern cross-platform Aspose license) is a bigger infra decision than
  scoped for now. Interim fix matching this file's own existing pattern
  (`ConvertHtmlToPdfAsync` already does the same): on conversion failure, log a
  warning and return the original image bytes unconverted rather than
  crashing - a real, viewable image is far better than an empty field or a
  failed request. Verified live in the Linux Docker container: response now
  returns the real PNG bytes (`89 50 4E 47` signature confirmed) instead of
  crashing.
- **Fix (2026-07-30)**: SOAP HISO calls (`/FormSessionService.svc`) never
  landed in `logs/hiso/*.log` - only ever visible in console. Two causes:
  (1) `FormSessionService.cs` tags every call `System = "hiso-soap"`
  (deliberate, keeps SOAP distinguishable from REST in the OTel metric), but
  `Program.cs`'s per-system file router (`hek_analysis/LOGGING_OVERHAUL_PLAN.md`)
  matched on an *exact* `"hiso"` string - `"hiso-soap" != "hiso"` silently
  matched none of the 4 per-system loggers. Fixed: `StartsWith` instead of
  `Equals` (`Program.cs`). (2) The SOAP endpoint was registered *before*
  `UseCorrelationId()`/`RequestLoggingMiddleware` in the pipeline - SoapCore
  short-circuits matched paths, so SOAP requests never got a `CorrelationId`
  or reached the generic `logs/requests-*.log` safety net. Fixed: moved SOAP
  registration to after both. Also: `FormSessionService.cs`'s success paths
  only called `_logger.LogInformation` directly (never `_observer.Tag(...)`),
  so successful SOAP calls never appeared in `readable-*.log` (only failures
  did, via `RecordExpectedFailure`/`RecordUnexpectedFailure`) - added
  `_observer.Tag(...)` to all 5 success paths, matching `KaroCompatController`'s
  existing pattern for controllers that don't route success through
  `ObserveAsync`. Verified live: `technical-*.log`/`readable-*.log`/
  `requests-*.log` all now show matching `CorrelationId`s for the same SOAP
  call, for both success and failure. Explicitly NOT tied to the Aspire
  Dashboard/OTel in any way (confirmed those are separate, additive-only, and
  never gated file logging to begin with) - this fixes the file-logging bug
  directly, per Zohaib's explicit ask to keep it dashboard-independent.
  This fix is specific to HISO's SOAP door only (the only one of the 4
  systems with a SOAP endpoint) - ERMS/KARO/COL are REST-only and already
  correctly use `_observer.Tag(...)`/`ObserveSwallowedAsync` for their success
  paths (confirmed by grep), so they never had this gap. The `StartsWith`
  system-name-matching fix in `Program.cs` is generic across all 4 systems'
  loops, though, so it'd auto-cover a similar future naming mismatch on any of
  them too.
- **Fix (2026-07-30)**: added a dedicated, cross-system `logs/db-errors-*.log`
  (`Program.cs`) - a caught-and-swallowed internal DB exception (e.g. the one
  inside `HisoConceptExecutor.ExecuteAsync`, which logs via plain
  `_logger.LogError` with no `System` property, so it doesn't reach any
  per-system `errors-*.log`) previously only ever showed in console, from any
  of the 4 systems. Filters on the log event's actual `Exception` type being a
  `System.Data.Common.DbException` (covers `SqlException` and any other ADO.NET
  provider) rather than any string/property convention a caller could forget
  to set, so it can't be missed. `CorrelationId` comes for free via the
  existing `Enrich.FromLogContext()` - no code changes needed at any DB-calling
  call site. Verified via an isolated scratch test (identical filter/sink
  config, a real `SqlException` from a deliberately unreachable server): the
  file captured the exception with its correlation ID and full stack trace
  correctly.
- **Fix (2026-07-30)**: real KARO/ERMS/COL clients hit bare paths (`/api/...`
  for KARO and ERMS, `/COL/...` for COL) on their own dedicated real hostnames -
  they were never going to call this hub's internal `/karo/*`/`/erms/*`
  namespaced paths directly, and can't be asked to change their URL. Confirmed
  real hostnames: KARO/HSS = `hss.itsmyhealth.nz` (prod) /
  `devhss.itsmyhealth.nz` (dev); ERMS = `southerms.indici.nz` (prod) /
  `deverms.itsmyhealth.nz` (dev); COL shares ERMS's host (its controller lives
  inside the legacy ERMS web project, not its own host) - disambiguated from
  ERMS by path prefix (`/COL` vs `/api`), not hostname. HISO
  (`hiso.itsmyhealth.nz`) needs no rule - its SOAP endpoint doesn't share a host
  with anything else. `LegacyHostRoutingMiddleware`
  (`src/Api/Middleware/LegacyHostRoutingMiddleware.cs`) + `appsettings.json`'s
  `LegacyHostRouting:Rules` already existed for exactly this, but had a real
  bug: it picked only the *first* rule matching the request's `Host` header, so
  since ERMS and COL share one host, COL's rule never got a chance to fire -
  every COL call fell through unrewritten and 404'd. Fixed to try every rule
  matching the host and let each system's own external path prefix (`/api` vs
  `/COL`) decide, not just the first host match. `HostContains` substring
  matching means dev vs prod hostnames (which differ) both still resolve
  correctly without per-environment config entries. Verified live: started the
  API locally and sent requests with `Host:` header overrides for all three
  real hostnames - `southerms.indici.nz` + `/api/ping` → 200 (ERMS),
  `hss.itsmyhealth.nz` + `/api/ping` → 200 (KARO), `southerms.indici.nz` +
  `/COL/authenticate` → 400 reaching the real controller (previously 404).
  Also updated `docs/adr/ADR-012-...md` Decision 6, which had flagged this
  exact host-vs-path-prefix question as unconfirmed - now resolved.
- **Fix (2026-07-30)**: found via a full live crosscheck of all 60 legacy
  operations against the real legacy servers (`crosscheck/` in repo root) -
  every real KARO operation except `ping`/`authenticate` 404'd at its real
  external URL (e.g. `hss.itsmyhealth.nz/api/GetDemographics`), despite working
  fine at its internal route (`/karo/demographics`). Root cause:
  `LegacyHostRoutingMiddleware` only swapped the path prefix (`/api` ->
  `/karo`), it never translated the operation segment itself - fine for
  ERMS/COL, whose internal route names are spelled identically to the real
  external ones, but `KaroCompatController.cs` uses short, different internal
  names for every operation (`demographics` not `GetDemographics`, `document`
  not `SaveDocument`, etc.). Fixed by adding a `KaroOperationNameMap` to
  `LegacyHostRoutingMiddleware.cs` (single file, no controller changes) that
  translates the real external operation name to KARO's internal route name
  after the prefix swap. Verified live against the rebuilt Docker container:
  `GetDemographics`/`GetClinicalNotes`/`GetConditions`/`GetProvider`/
  `SaveScreeningCode` all now return 200 at their real external URL (previously
  404), each landing a `[CallLog]` line in `logs/karo/readable-*.log`;
  `ping`/`authenticate` confirmed unaffected (regression check).
- **Fix (2026-07-30)**: HISO's SOAP contract (`IFormSessionService.cs`) was
  missing `getFormView` entirely (6th real operation) - calling it returned a
  hard "No operation found" fault. A prior session's doc comment on
  `HisoCompatController.cs` had wrongly claimed this operation "has no SOAP
  equivalent... not a real legacy operation" - disproven by a live WSDL/XSD
  fetch against the real legacy server (`http://localhost:53507/FormSessionService.svc?wsdl`)
  during the crosscheck, confirming the real request/response shape
  (`sessionKey`+`formInstanceId` -> `resumePath`/`dataContainer`/`view`/`viewType`).
  Fixed by wiring `getFormView` into `IFormSessionService.cs`/`FormSessionService.cs`
  following the same pattern as the other 5 operations, reusing the
  already-working `GetFormViewQuery` handler (same one the JSON dashboard
  endpoint already used) - no new business logic. Verified live in the rebuilt
  Docker container: SOAP call now returns 200 (was a fault), logged correctly
  in `logs/hiso/readable-*.log`; `getVersion` confirmed unaffected (regression
  check). Corrected the stale doc comment.
- **Fix (2026-07-30)**: follow-up to the getFormView wiring above - a real test
  with real data (session `439cc902-...`, ACC45 row `650854`, DMS document
  `9d915ade-...`, all confirmed present in the live `PMS_NZ_V2` DB via
  `sqlcmd`) still came back empty. Root cause in
  `Acc45DefinitionRepository.GetDefinitionAsync`: compared against the real
  legacy source (`Acc45DefinitionBuilder.cs:142-145`), the port's
  `@pSessionKey` parameter sent `PracticeId` (e.g. `"901"`) whenever
  `ReferenceId` was null, but real legacy always sends the actual session GUID
  - `tblACC45Definition`'s `SessionKey` column holds a GUID, so `"901"` never
  matched. Deeper cause: `HealthLinkSession` never carried the raw session GUID
  at all. Fixed: added an optional `SessionKey` field to `HealthLinkSession`
  (`src/Application/Common/Models/HealthLinkSession.cs` - optional so the many
  canonical-hub controllers that build a `HealthLinkSession` without a HISO
  session are unaffected), threaded `request.SessionKey` through all 4 call
  sites (`GetFormViewQuery`/`GetDataQuery`/`SaveContainerCommand`/
  `ProcessActionCommand`), and fixed the repository to always send the real
  GUID. Verified live: the same real session/document now returns the exact
  real DB values (`resumePath`, `viewType`) matching the row found directly in
  the database via `sqlcmd`.
- **Fix (2026-07-31)**: `getFormView`'s `view` field (actual document bytes)
  still always came back null even after the fixes above - traced to
  `IDmsProxyService`, which called an external DMS proxy ASMX service
  (`DMSServiceURL`/`PMSDmsServices`) that was never implemented, only stubbed
  to always return null. Live-tested against the real legacy server: pointing
  `DMSServiceURL` at its real production value makes legacy itself hang ~44s
  then fault "Unable to connect to the remote server" - the private-LAN
  address is unreachable even to legacy in this environment, confirmed live,
  not assumed. A `DMSProxy` project Zohaib located on disk turned out to be
  only the *client* wrapper (a `Library`/DLL + generated WSDL reference stub),
  not the real server - that server-side project isn't present anywhere on
  this machine (searched broadly). Parked per Zohaib's instruction pending him
  locating the real deployed server project. Implemented the working
  alternative instead: `DmsDocumentRepository`
  (`src/Infrastructure/Legacy/Hiso/DmsDocumentRepository.cs`, replacing the
  `NotConfiguredDmsProxyService` stub) reads the document directly from the
  same `DMS_PMS` database HISO already connects to
  (`dbo.uspDocumentGetByDMSID`, reusing `IHisoDmsConnectionResolver` for the
  per-practice connection), instead of the unreachable external service.
  `IDmsProxyService.GetDocumentDataAsync` gained a `practiceId` parameter (the
  proc requires it); `GetFormViewQuery`'s one call site updated to pass
  `session.PracticeId`. Verified: (1) `uspDocumentGetByDMSID` returns real
  document bytes directly via `sqlcmd` for a real document/practice (128)
  confirmed present in `dbo.tblDocument`; (2) this dev DB's `tblACC45Definition`
  and `tblDocument` tables turned out to have zero overlapping DMS document IDs
  (checked ~100 real ACC45 document IDs against `tblDocument` - none found) -
  a genuine seed-data gap, not a code issue, so a full SOAP-level test with a
  perfectly matching real pair isn't possible with this dev data; (3) rebuilt
  Docker container regression-tested with the same real session used in the
  fixes above - still returns the same graceful (no-crash) result, confirming
  no regression while the underlying mechanism now works wherever real DMS
  data exists.
- **Fix (2026-07-31)**: `saveContainer` tested end-to-end with a real,
  complete client-captured form payload (real HISO Work Capacity Med Cert
  form, session `439cc902-...`) against both the real legacy server and this
  API - found and fixed 4 more real, distinct bugs beyond the getFormView ones
  above:
  1. `SaveContainerRequestSoap`'s wire shape didn't match the real WSDL (a
     wrong top-level `formMetaData` member that doesn't exist in the real
     request - `formMetaData` actually lives inside `dataContainer` - plus a
     missing `continueSession` field). Fixed to match the real
     `sessionKey/resumePath/dataContainer(FormData)/view/viewType/
     viewSignature/completed/continueSession` shape exactly.
  2. Same missing-config class of bug as before: 7 `UDT_tbl*` column-list keys
     (`UDT_tblACC45Definition`, `UDT_tblACC45Detail`, `UDT_tblPatient`, etc.)
     existed only in the gitignored dev-local config, never in
     `appsettings.json` - Docker/Azure built a zero-column TVP `DataTable` and
     SQL threw "Structured types must have at least one field." Added all 7 to
     `appsettings.json` (not secrets, safe there).
  3. Confirmed live against the real legacy server itself (not assumed): its
     `submittedData` `XmlNode[]` deserialization is whitespace-sensitive -
     *any* insignificant whitespace/newline anywhere inside the dummy+form
     content shifts the array indices legacy's own code relies on
     (`((XmlNode[])request.dataContainer.submittedData)[1]`), producing
     legacy's own generic "Data at the root level is invalid" fault. Not a
     new-API bug at all - a real, reproducible legacy quirk; noted for anyone
     hand-building a SOAP client for this operation.
  4. The real gap: `HisoDocumentHandler.AddDocumentAsync`'s `SqlParameter`s for
     `uspDocumentSave` were built with .NET types that didn't match the real
     proc's declared parameter types exactly (`@pProfileID`/`@pDocumentTypeID`
     as .NET `int` for `nvarchar` params, `@pPracticeID` as a bare string for
     an `int` param, `@pDocumentSize` as `Int32` for a `BIGINT` param) - this
     is silently rejected by SQL Server's RPC-style parameter binding
     ("expects parameter 'X', which was not supplied", even though the
     parameter genuinely was supplied) in a way that a literal inline `EXEC`
     batch never surfaces. The exception was caught and swallowed exactly as
     legacy's own code does ("logged only, never propagated"), so the SOAP
     call still returned `200`/`response=true` while silently never writing a
     document - only visible in the raw Docker container console log
     (`docker logs`), not the per-system `logs/hiso/*.log` files, since
     `HisoDocumentHandler` doesn't carry a `System` log property. Fixed by
     giving every `SqlParameter` an explicit `SqlDbType`/size matching
     `uspDocumentSave`'s real signature.
  Also found and fixed a real, separate seed-data gap while chasing this:
  practice 901 had no `dbo.TblPracticeDMSInfo` row in `DMS_PMS`, so its
  document saves resolved to no target database at all - inserted one
  (`PHOID=1`, `DataBaseName=DMS_PMS`) mirroring a working practice's shape.
  Verified end-to-end, repeatedly: real legacy save produced a real document
  in `DMS_PMS.dbo.tblDocument`/`tblDocumentDetail` with content matching
  exactly what was sent; after all 4 fixes, this API's save now produces the
  same - byte-for-byte identical `DocumentData` to what was sent, confirmed via
  direct `sqlcmd` inspection, not just a `200` response.
- **Fix (2026-07-31)**: follow-up - testing a genuinely *new* record (a session
  with no pre-existing `tblACC45Definition` row, so the real insert path runs
  instead of the update path) found the same `@pSessionKey` bug as the
  `GetDefinitionAsync` fix above, but in the sibling *write* method,
  `Acc45DefinitionRepository.SaveDefinitionAsync` - it sent `session.PracticeId`
  ("901") instead of the real session GUID for `@pSessionKey`, so the new row's
  `SessionKey` column got the wrong value (confirmed live: new row inserted
  with `SessionKey='901'` instead of the real session GUID). Fixed to send
  `session.SessionKey.ToString()`, matching real legacy
  (`FormSessionService.svc.cs:472` passes `objSessionKey.GUID.ToString()`).
  Verified end-to-end with a brand-new record end-to-end: real new row in
  `tblACC45Definition` (correct `SessionKey` GUID) and its document genuinely
  present in `DMS_PMS` with byte-for-byte matching content, both confirmed via
  `sqlcmd`.
- **Correction (2026-07-31)**: the crosscheck's "COL `GetSessionData` crashes"
  finding was wrong - re-investigated and confirmed it's an intentional,
  already-verified reproduction of a genuine real legacy bug
  (`ColDataRepository.GetSessionDataAsync` executes an empty stored-procedure
  name on purpose, matching `PHCO.cs:69`'s real `""` proc name in the supplied
  legacy source). Nothing to fix; `crosscheck/` docs corrected.
- **Fix (2026-07-31)**: COL's own `Authenticate` (`ColAuthenticateQueryHandler`
  in `ColQueries.cs`) had the same NZ-timezone "+0 instead of +12" `Expiry` bug
  originally found and fixed in ERMS's `Authenticate` - it built `Expiry` via
  `expiry.ToString("yyyy-MM-ddTHH:mm:ssz")`, relying on the container's ambient
  timezone rather than computing NZ's real offset explicitly. Added the same
  `FormatExpiryLikeLegacy` fix (explicit `Pacific/Auckland` lookup) to this
  handler too. Verified live: `Expiry` now returns `...+12`, matching legacy's
  real format. (COL's own real legacy `Authenticate` still fails with an NRE in
  this dev environment for reasons not yet investigated - separate, still-open
  item, unrelated to this fix.)
- **Fix (2026-07-31)**: set `TZ: Pacific/Auckland` on the `api` service in
  `docker-compose.yml` - the container previously defaulted to UTC, which
  looked like a bug (a saved record's `InsertedAt` showed a time ~12 hours
  "behind" the dev machine's own NZ-set clock) but was actually correct, since
  every timestamp this app writes uses `DateTime.UtcNow` deliberately. Setting
  `TZ` doesn't change what `UtcNow` returns (still correct, still UTC) - it
  only makes the container's own displayed clock (`docker exec ... date`) and
  any future `DateTime.Now` usage match NZ wall-clock time, avoiding this
  confusion going forward. `tzdata` was already present in the
  `mcr.microsoft.com/dotnet/aspnet:8.0` base image - no package install needed.
- **Correction (2026-07-31)**: the crosscheck's 4 ERMS "data gap" findings
  (`GetRegisteredPractitioners`/`GetPrescribedMedications`/`GetRegularMedications`/
  `GetConsultNotes` allegedly returning fewer/different rows than legacy) were
  wrong. Re-tested live: every case returns the exact same complete set of
  rows (sorted referenceID lists identical, 0 missing/extra) - only the row
  *order* differs, and that's inherent non-determinism in the real shared SQL
  query (no stable secondary sort key when rows tie on date), not a new-API
  defect. Proved by calling real legacy itself twice in a row and observing
  its own row order change between calls. Nothing to fix; `crosscheck/` docs
  corrected.
- **Fix (2026-07-31)**: KARO `GetRecallCategories` (`KaroCompatController.cs`)
  400'd on a blank/omitted `group` query param; real legacy returns
  `{"entry":[]}` for the same input. Root cause: legacy's own C# signature
  also declares `group` with no default (`string group`), but old ASP.NET Web
  API (`System.Web.Http`) never enforced non-nullable-by-default model binding
  the way ASP.NET Core + `[ApiController]` (with nullable reference types)
  does - so legacy silently tolerated a missing/blank value, this port didn't.
  Fixed by making the controller parameter `string?`. Verified live: both a
  blank `group=` and a fully omitted `group` now return `200` with
  `{"entry":[]}`, matching legacy; confirmed logged correctly in
  `logs/karo/readable-*.log`.
- **Fix (2026-07-31)**: HISO `getData` tested with a real, complete `formMetaData`
  (same pattern proven for `saveContainer`) against both the real legacy server
  and this API - found and fixed 2 more real structural bugs (values matched
  immediately; the XML *shapes* didn't):
  1. `FormMetaDataSoap` only modeled 7 of the real WSDL's 14 fields - missing
     `formInstanceCreationDate`/`formInstanceDescription`/
     `formDefinitionDescription`/`recipientAccount` entirely, so legacy's real
     "echo the request's formMetaData back unchanged" behavior silently
     dropped those 4 real values from every getData/getFormView/saveContainer
     response. Added all 4.
  2. The response's `submittedData` wrapper was missing - the code assigned
     the bare `<form>` element directly to the `submittedData` property, and
     since `[XmlAnyElement]` serializes whatever element it's given verbatim
     (using that element's own tag), the real
     `<submittedData><dummy/><form>...</form></submittedData>` wrapper
     (confirmed live against legacy, same `dummy`-placeholder pattern already
     known from the request side) was completely absent from every response.
     Fixed by building the real wrapper explicitly before assigning it.
  Verified live: new API's getData response now matches legacy's real XML
  shape field-for-field and structurally; `getVersion` regression-checked;
  confirmed logged correctly in `logs/hiso/readable-*.log`.
- **Fix (2026-07-31)**: KARO's 6 previously-untested write operations
  (`SaveClinicalNotes`/`SaveCondition`/`SaveRecall`/`SaveObservations`/
  `SaveDocument`/`SaveSummary`/`SaveInvoice`) tested with real request shapes
  derived directly from the real legacy source (`APIModels.cs`'s
  `ConsultNote`/`Condition`/`Recall`/`Observations`/`Document`/`Invoice`
  classes, and `KARO_HSS_doc.md`'s real `SaveSummary` sample bodies) - no
  guessing needed. `SaveClinicalNotes`/`SaveObservations`/`SaveSummary`/
  `SaveCondition` matched immediately (`SaveCondition`'s "already exists" vs
  plain-success difference on rerun is expected - legacy and this API share
  one live DB, so whichever call runs first wins the real `-5` idempotency
  sentinel). Found and fixed 2 more real bugs:
  1. `SaveDocument`: `KaroWriteRepository`'s (and, found while there,
     `ErmsWriteRepository`'s) `uspDocumentSave` `SqlParameter`s didn't match
     the real proc's declared types (`@pDocumentSize` Int32 vs BIGINT,
     `@pPracticeID` bare string vs int) - identical root cause to HISO's
     `HisoDocumentHandler` fix above, fixed the same way. Also:
     `Karo:DMSDocTypes`/`Erms:DMSDocTypes` config existed only in the
     gitignored dev-local file - resolved every document to type ID `-1`,
     which the real proc doesn't handle cleanly (its own internal "ROLLBACK
     TRANSACTION request has no corresponding BEGIN TRANSACTION" error, not a
     clean failure). Added both to `appsettings.json`.
  2. `SaveInvoice`: `KaroWriteRepository.SaveInvoiceAsync` sent `@pPayee`,
     which the real `[HSS].[uspInsertUpdateService]` proc doesn't declare at
     all - confirmed in `HSSDA.cs:1221-1224`, legacy's own C# has
     `@pPayee`/`@pCoPayment` commented out, so legacy never actually sends it
     despite accepting a `payee` request field. Removed the extra parameter.
  `SaveRecall` still fails on both legacy and this API with this test data
  (different error text each side) - legacy itself can't complete this save
  regardless of `CategoryId` tried, so this looks like a real, pre-existing
  legacy limitation with the test `Group` name, not a new-API defect; flagged
  as not fully resolved rather than silently closed. All fixes verified live
  (real document/invoice ID confirmed via `sqlcmd` where applicable),
  regression-checked (`ping`/`Authenticate` still 200), and confirmed logged
  correctly in `logs/karo/readable-*.log`.
- **Telemetry**: OpenTelemetry wired (traces + metrics), Aspire dashboard at
  `:18888`. Per-call field-scoping counters currently only on
  `CanonicalDemographicsController` — not yet replicated to the other 13.
- **Open items carried forward (updated 2026-08-03)**: ERMS Azure-forwarding gap and
  SOUTH-environment (practice 518, untestable locally) — closed by Zohaib as accepted/
  out-of-scope for now, not actionable without further input he hasn't provided; not
  re-opened unless he brings new info.
  - **Measurements delimiter — re-checked 2026-08-03, closed as decoded (was stale).** The
    "shape not decoded" note (`hek_analysis/PROJECT_STATUS.md` 2026-07-28) only ever blocked
    the *canonical* `/v1` Measurements resource, which was never built. The shape itself
    (`"{ref}|&|{value}|?|{type}|?|{code}|?|{label}|?|{date}"` on BPSYS/BPDIA/Weight/Height/BMI
    columns from `[HSS].[uspGetMeasurement]`) is already fully decoded, correctly, by
    `ErmsDataTableMapper.ToListHiso<T>` (`src/Adapters.Erms/Hiso/ErmsDataTableMapper.cs`) — an
    exact line-by-line port of real legacy's `ERMSDataTableToListHiso<T>`
    (`APIController.cs:1759-1856`): outer `"|&|"` split into (conceptId, text), inner `"|?|"`
    split of `text` into (text, name, qualifierId, qualifierName, dateTaken). This is proven
    correct, not just plausible — the legacy-compat `GetPatientMeasurement` endpoint uses this
    exact mapper and is confirmed byte-matching real legacy (`crosscheck/SUMMARY.md`:
    `GetPatientMeasurement ✅ Match`; `LEGACY_PARITY_VALIDATOR.md` line 39). No canonical
    Measurements resource exists yet, but that's an unbuilt-feature gap, not an
    unknown-data-shape blocker — see `hek_analysis/PROJECT_STATUS.md`'s canonical-resource
    build-order list if/when that gets picked up.
- **Docs infra (2026-07-29)**: this file + root `CLAUDE.md` created as the
  single-source-of-truth + auto-update mechanism. Not yet fixed: dead spec
  links in `DOCUMENT_INDEX.md`, duplicate AI usage logs (see §4).

## 3. Doc map — where to look for what

| Need | Doc |
|---|---|
| Full doc index + API contract source of truth | `docs/DOCUMENT_INDEX.md` |
| Session-by-session history, decisions, "why" | `hek_analysis/PROJECT_STATUS.md` (append-only) |
| Per-change log (granular) | `docs/CHANGELOG.md` |
| Legacy vs. unified parity, op-by-op | `LEGACY_PARITY_VALIDATOR.md` |
| v1.1 infra rollout plan + status | `hek_analysis/v1.1-full-plan.md` / `hek_analysis/v1.1-plan-status.md` |
| As-built architecture | `docs/architecture.md` |
| Deployment / docker / env vars | `docs/deployment.md` |
| Auth model per system | `docs/auth-guide.md` |
| Field inventories per legacy system | `docs/datasets/{hiso,karo,erms,unified-model}.md` |
| AI usage log | `docs/ai_usage_log.md` (was `AI_USAGE_LOG.md` at repo root — see note below) |

## 4. Known doc drift (fix opportunistically)

- ~~`docs/DOCUMENT_INDEX.md` dead spec links~~ ✅ FIXED 2026-07-31 — index now points at
  `docs/HEK_UNIFIED_API_SPEC_presentable.html`, added a Legacy Parity section pointing at
  `crosscheck/` as the current source (superseding the older `LEGACY_PARITY_VALIDATOR.md` pass).
- ~~Two AI usage logs~~ ✅ FIXED 2026-07-31 — `docs/DOCUMENT_INDEX.md` now explicitly marks
  `docs/ai_usage_log.md` as current/active and `AI_USAGE_LOG.md` (repo root) as stale/frozen
  2026-07-22, kept for history only.
- `hek_analysis/docs/Unified-Healthcare-API_EAD.md` is a known byte-identical
  duplicate of the `architecture/` copy — noted, not deleted (per past decision).
- `LEGACY_PARITY_VALIDATOR.md` (repo root) is now superseded by `crosscheck/` (2026-07-31's
  live-verified-against-real-legacy pass is more current/trustworthy than this file's 07-27/28
  code-only comparison) — kept for its per-field diff detail on ops `crosscheck/` didn't
  re-examine, cross-referenced from `DOCUMENT_INDEX.md`.

## 5. Update protocol

After any implementation, bug fix, or decision:
1. Update **§2 Current State** above (one or two lines, not a transcript).
2. Add a dated entry to `hek_analysis/PROJECT_STATUS.md` (the narrative log)
   and/or `docs/CHANGELOG.md` (the structured per-change entry) — whichever
   the change fits; don't duplicate into both.
3. If a legacy-compat operation's parity status changed, update
   `LEGACY_PARITY_VALIDATOR.md`.
4. If an architecture/contract file changed, update `docs/DOCUMENT_INDEX.md`'s
   description for that row.
