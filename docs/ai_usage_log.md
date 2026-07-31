# AI Usage Log

Running record of AI-assisted work on this project.

---

## 2026-07-27 21:00

**Task:** Build HISO-style tabbed "Patient Record" UI for HSS (Karo) and ERMS, matching the existing HISO dashboard pattern; rename KARO label to "HSS" in the UI.

**Actions:**
- Added `frontend/src/patientRecordForm.tsx` — generic tabbed patient-record component reused across HSS/ERMS/COL (bundles all "safe" reads into one "Load Patient Record" click).
- Added `frontend/src/colView.tsx` — response renderer for COL's flat JSON array shape.
- Rewired `frontend/src/SystemDashboard.tsx` to use the new tabbed form for karo/erms/col, with an "Advanced" collapsed panel for drilldown reads (needing an ID) and writes.
- Renamed `KARO` → `HSS` label in `frontend/src/systems.ts`.
- Rebuilt and restarted the `hekcoreapi-frontend` docker image to deploy the change; fixed a `tsc -b` build-mode type error (`renderResult` prop arity mismatch) found only in the stricter Docker build.
- Verified live in-browser via HSS/ERMS/COL authentication + "Load Patient Record" against the real docker stack.

**Files changed:** `frontend/src/patientRecordForm.tsx`, `frontend/src/colView.tsx`, `frontend/src/SystemDashboard.tsx`, `frontend/src/systems.ts`

---

## 2026-07-28 11:00

**Task:** Test all HSS/ERMS/COL write endpoints and drilldown reads with real data; report exact request/response payloads.

**Actions:**
- Ran every write (clinicalnotes, conditions, invoice, observations, recalls, document, summary for Karo; SaveDocument for ERMS; SaveInvoice for COL) and every ID-drilldown read against the live docker API using real test patient 2459731.
- Found and fixed a real bug in `frontend/src/catalog.ts`: several write bodies sent `userId`/`fee`/`isLongTerm`/`temperature`/etc as JSON numbers/booleans when the backend DTOs (`HekCoreApi.Adapters.Karo.Karo*Request`) are all-string records — caused `400` deserialization failures.
- Found and fixed the same class of bug in ERMS `SaveDocument`'s `Content` field (needed base64 encoding, backend does `Convert.FromBase64String`).
- Rebuilt/restarted the `hekcoreapi-frontend` docker image with both fixes.

**Files changed:** `frontend/src/catalog.ts`

**Notes:** Several real backend-side issues found but not fixable from the frontend: missing `Erms:DbCredentials:ConnIndiciDB`/`Karo:DbCredentials:ConnDMSDB*` secrets, `uspInsertUpdateService` argument-count mismatch, `uspInsertUpdateRecall` silent rejection (no seeded RecallCategory data), intermittent COL `GetSessionData` `BeginExecuteReader` error.

---

## 2026-07-28 12:00

**Task:** Fix the root cause of the missing-DMS-connection failures per user's direction — DMS/master connections should resolve through the same tenant-registry route as the primary connection, not need a separate secret.

**Actions:**
- Rewrote `IKaroDmsConnectionResolver`/`KaroDmsConnectionResolver` and `IErmsDmsConnectionResolver`/`ErmsDmsConnectionResolver` to resolve via the same `ITenantRegistryService` route as the primary practice connection resolvers, swapping only the database name to `DMS_PMS` (confirmed from the real legacy Web.config: DMS shares server+credentials with the primary connection, only `Initial Catalog` differs).
- Updated call sites in `KaroWriteRepository.cs`, `KaroDataRepository.cs`, `ErmsWriteRepository.cs`, `ErmsDataRepository.cs`, `ErmsSaveDocumentCommand.cs` to pass `RoutingContext` instead of a bare `practiceSuffix` string.
- Added `Erms:DmsDatabaseName`/`Karo:DmsDatabaseName` (default `DMS_PMS`) to `src/Api/appsettings.json`.
- Discovered (while validating the fix) that the freshly-deployed source used a newer strict routing-context match (`PracticeId`+`PracticeCode`+`Environment`+`SourceSystem`) that the running docker image's stale env vars and the dashboard's example encounter-ID (3-segment, wrong `PracticeCode`/`Environment`) didn't satisfy — fixed by adding host-keyed `Karo__DbCredentials__dbserver-local`/`Erms__DbCredentials__dbserver-local` env vars (`.env`, `docker-compose.yml`) and correcting the encounter-ID default in `frontend/src/catalog.ts` to the 4-segment form that matches the real seeded `Practices` row.
- Found and fixed a second real bug while re-verifying: ERMS `SaveDocument`'s outgoing XML used plain tag names (`<EncounterID>`) instead of the real `[XmlElement]` names on `ReferralDocument` (`ReferralDocument_Encounter_ID` etc.) — `XmlSerializer` silently nulls unmatched elements, no error.
- Rebuilt/restarted the `hekcoreapi-api` and `hekcoreapi-frontend` docker images; re-verified KARO/ERMS document writes now reach the real `DMS_PMS` stored procedure (confirmed a separate, pre-existing SQL-side bug in `[dbo].[uspDocumentSave]`'s own TRY/CATCH — out of scope to fix from the API).

**Files changed:** `src/Application/Common/Interfaces/IErmsDmsConnectionResolver.cs`, `src/Application/Common/Interfaces/IKaroDmsConnectionResolver.cs`, `src/Infrastructure/Legacy/Erms/ErmsDmsConnectionResolver.cs`, `src/Infrastructure/Legacy/Karo/KaroDmsConnectionResolver.cs`, `src/Infrastructure/Legacy/Erms/ErmsWriteRepository.cs`, `src/Infrastructure/Legacy/Erms/ErmsDataRepository.cs`, `src/Infrastructure/Legacy/Karo/KaroWriteRepository.cs`, `src/Infrastructure/Legacy/Karo/KaroDataRepository.cs`, `src/Application/Features/Erms/Commands/ErmsSaveDocumentCommand.cs`, `src/Api/appsettings.json`, `.env`, `docker-compose.yml`, `frontend/src/catalog.ts`

---

## 2026-07-28 13:00

**Task:** Build an automated end-to-end integration test suite with written evidence, and check the recall/invoice SP errors + which DB they hit.

**Actions:**
- Added `tests/Api.IntegrationTests/LiveApi/` (`LiveApiFixture.cs`, `KnownTestData.cs`, `HisoCompatTests.cs`, `KaroCompatTests.cs`, `ErmsCompatTests.cs`, `ColCompatTests.cs`) — 43 xUnit tests, plain `HttpClient` calls against the live docker stack (no Testcontainers/WebApplicationFactory), a custom rate-limit-pacing `DelegatingHandler` to stay under the API's own 30 req/60s limit, and `[assembly: CollectionBehavior(DisableTestParallelization = true)]`.
- Endpoints with known pre-existing backend bugs (found this session) are asserted against their real current failure text, not glossed over — e.g. `uspDocumentSave`'s ROLLBACK/BEGIN mismatch, `uspInsertUpdateService`'s argument-count mismatch, `uspInsertUpdateRecall`'s silent rejection, `saveContainer`'s unhandled 500.
- Ran the suite to a clean 43/43 pass (each assertion matching real, currently-reproducible behavior).
- Generated a readable HTML evidence report (`tests/Api.IntegrationTests/gen-report.js` parses the `.trx`) with full request/response per test; published as an artifact and copied into `docs/unit_test/`.
- Answered a follow-up: gave the exact `EXEC [dbo].[uspDocumentSave] ...` call (real parameters) and traced that `@pDocumentTypeID` comes back `-1` because `Erms:DMSDocTypes`/`Karo:DMSDocTypes` config isn't set in the container.

**Files changed:** `tests/Api.IntegrationTests/LiveApi/*.cs`, `tests/Api.IntegrationTests/gen-report.js`, `docs/unit_test/live-integration-report.html`, `docs/unit_test/live-integration-results.trx`

---

## 2026-07-28 16:00

**Task:** Build flow diagrams for the project — first a high-level legacy-endpoints overview, then a method/SP-level detailed trace, then match an existing hand-authored Mermaid-sequence-diagram style for four more write endpoints.

**Actions:**
- Built `docs/flow-diagrams/legacy-endpoints-flow.html` — interactive 4-lane (HISO/KARO/ERMS/COL) box diagram, click-to-isolate lanes, click-a-step detail panel.
- Built `docs/flow-diagrams/legacy-endpoints-detailed-trace.html` — 8 endpoint traces (1 read + 1 write per system) down to exact `Controller.Method` → `MediatR request` → `Handler.Method` → `Repository.Method` → exact stored procedure + database, sourced from two parallel code-reading passes (no literal SQL table names exist anywhere in this codebase — verified, not assumed).
- User pointed at an existing example file (`docs/flow-diagrams/karo-authenticate-flow.html`, one of 5 pre-existing Mermaid-sequence-diagram pages) as the wanted style; built 4 more in the same exact template (CDN Mermaid, zoom controls, legend cards, source-index table): `karo-clinicalnotes-flow.html`, `karo-document-flow.html`, `erms-savedocument-flow.html`, `col-saveinvoice-flow.html` — covering the two-database write paths and this session's DMS-resolver/XML-tag-name fixes as callout cards.

**Files changed:** `docs/flow-diagrams/legacy-endpoints-flow.html`, `docs/flow-diagrams/legacy-endpoints-detailed-trace.html`, `docs/flow-diagrams/karo-clinicalnotes-flow.html`, `docs/flow-diagrams/karo-document-flow.html`, `docs/flow-diagrams/erms-savedocument-flow.html`, `docs/flow-diagrams/col-saveinvoice-flow.html`

---

## 2026-07-27 18:00

**Task:** User reported all 5 `docs/flow-diagrams/*.html` pages "sab break hain" (all broken) and unreadable.

**Actions:**
- Diagnosed: all 5 files use `<pre class="mermaid">` blocks but never load the Mermaid.js library or call `mermaid.initialize()` - browsers were rendering raw sequence-diagram source text instead of a diagram.
- Added `<script src="mermaid@10.9.1 CDN">` + a theme-aware `mermaid.initialize()` call to all 5 files, right after each `<title>` tag.

**Files changed:** `docs/flow-diagrams/erms-authenticate-flow.html`, `docs/flow-diagrams/erms-getscannedlist-flow.html`, `docs/flow-diagrams/hiso-getdata-flow.html`, `docs/flow-diagrams/karo-authenticate-flow.html`, `docs/flow-diagrams/karo-authenticate-flow-simple.html`

---

## 2026-07-28 08:00

**Task:** "Kis jaga khada hun, kya pending hai, client ko deliver karne se pehle kaise check karoon" - a client-delivery readiness audit against the v1.1 spec addendum and internal status trackers.

**Actions:**
- Read `docs/HEK_UNIFIED_API_SPEC_v1.1.md`, `hek_analysis/PROJECT_STATUS.md`, `hek_analysis/v1.1-plan-status.md` in full (not just table rows) to map spec FR-10..13/PR-7..10 against actual code state.
- Found and documented 3 (later a 4th) cases where `PROJECT_STATUS.md` was stale relative to reality: Aspose rendering (#30), AWS document branches (#34), and Docker networking (#33) were all marked open/blocked in the status file but already resolved in code/`LEGACY_PARITY_VALIDATOR.md`.
- Ran full test suite (66/66 pass) and spot-verified HISO getData live against real production data as evidence for the report, rather than trusting prior docs uncritically.
- Wrote `hek_analysis/CLIENT_DELIVERY_READINESS.md`: where-you-stand summary, ranked pending items (external/Zohaib-blocked vs internal-not-started vs confirmed-gaps), doc-drift findings, and a pre-delivery verification checklist.
- **Correction round**: user pushed back on an overstated claim (item #28, "~34 of ~35 stored procedures unverified") - re-read `PROJECT_STATUS.md`'s full dated timeline (not just the row) and found this framing was itself stale, written right after the *first* live verification and never updated after a much larger 2026-07-21..23 wave of real-source-code rebuilds + live verification across nearly every KARO/ERMS/HISO/COL operation. Corrected the report to name the real, short remaining list (Observations, Measurements, Encounter Summary scope, COL GetSessionData's known bug) instead of a vague "34 unknown."

**Files changed:** `hek_analysis/CLIENT_DELIVERY_READINESS.md`

**Notes:** Lesson reinforced this session - a status file's own row-level framing can be stale even when the rest of the same file documents the fix; always cross-check the full dated history before reporting a risk level.

---

## 2026-07-28 19:00

**Task:** User manually testing KARO authenticate from the frontend hit `"KARO/HSS practice '901' (code 'FZZ999-B', environment '-') is not registered."` - debug and fix the real routing mechanism, then generalize for ERMS/COL.

**Actions:**
- Traced the error to `KaroPracticeConnectionResolver.cs:28` / `TenantRegistryService.ResolveRouteAsync` doing an exact 3-column match against `HekTenantRegistry.dbo.Practices`, with no row matching the parsed `(PracticeId, PracticeCode, Environment)` from the test `encounterId`.
- Found a second, deeper bug via user's own code reading: the real legacy source (`APIController.cs:41-58` KARO, `ermsapi APIController.cs:71-74` ERMS, feeding `HSSDA.cs:813`'s `ConnIndiciDB{practiceid}` lookup) overwrites the practice context entirely when a 4th encounterId segment (environment) is present - practiceId/practiceCode get discarded, routing collapses to environment alone. The 2026-07-24 tenant-registry rework had NOT replicated this quirk.
- Fixed `KaroRoutingResolver.cs`/`ErmsRoutingResolver.cs` to replicate the real overwrite quirk.
- Rewrote `TenantRegistryService.ResolveRouteAsync` to a single-tier match (Environment-alone / PracticeId+PracticeCode / PracticeId-alone, whichever the caller's encounterId shape implies) with no fallback cascade between tiers, per user's explicit design.
- Made `PracticeRegistryEntry.PracticeId/PracticeCode/Environment` nullable (migration `MakePracticeRoutingColumnsNullable`) so each registry row only fills the column(s) its tier uses, instead of the old `"-"` sentinel placeholder text; cleaned up registry data to match (4 minimal rows instead of padded duplicates).
- Added 3 filtered unique indexes (migration `AddPerTierUniqueIndexesOnPractices`) so two different rows can't both satisfy the same tier's lookup and crash `SingleOrDefaultAsync` - caught and corrected a first, too-narrowly-filtered attempt live via a deliberate duplicate-insert test before landing the right filter.
- Rebuilt the `api` Docker image twice and the `frontend` image twice during this investigation; ran the full test suite three times (last one 66/66 clean) after each structural change, including after recreating registry rows the user had deleted mid-session for testing.
- Confirmed COL needs no separate fix - it already reuses `IErmsRoutingResolver` directly (COL shares ERMS's real session-table/HSSDA in legacy too).

**Files changed:** `src/Infrastructure/Legacy/Karo/KaroRoutingResolver.cs`, `src/Infrastructure/Legacy/Erms/ErmsRoutingResolver.cs`, `src/Infrastructure/Routing/TenantRegistryService.cs`, `src/Domain/Entities/PracticeRegistryEntry.cs`, `src/Infrastructure/Persistence/Configurations/PracticeRegistryEntryConfiguration.cs`, `src/Infrastructure/Legacy/Admin/PracticeAdminRepository.cs`, `src/Infrastructure/Persistence/Migrations/*MakePracticeRoutingColumnsNullable*`, `src/Infrastructure/Persistence/Migrations/*AddPerTierUniqueIndexesOnPractices*`, `frontend/src/catalog.ts`, `LEGACY_PARITY_VALIDATOR.md`

**Notes:** Two live-DB manual edits by the user mid-session (row 20/21 deletion) were treated as intentional test actions, not reverted silently - re-seeded only the minimum needed once their effect was understood.

---

## 2026-07-28 20:30

**Task:** User reported ERMS `GetScannedList` returned no data, error `"Column 'DataType' is read only."`

**Actions:**
- Traced to `ErmsDataRepository.cs`'s `GetOtherDocsAsync`/`GetDocResultsAsync` (backs `GetScannedList`/`GetDischargeSummaryReportList`/`GetScannedDetails`/`GetDischargeSummaryDetails`) - the AWS-branch stored procedures (`uspGetOtherDocs_AWS`/`uspGetDocResults_AWS`) return a `DataType` (and `Content`/`DocumentId`) column that SQL Server's schema metadata flags read-only (computed/derived), and `DataTable.Load(reader)` copies that flag onto the .NET `DataTable`, so the existing AWS-enrichment write threw instead of enriching.
- Fixed by explicitly setting `column.ReadOnly = false` immediately before each enrichment write in both methods (KARO's equivalent path maps to POCOs before mutating, so it wasn't affected).
- Rebuilt the `api` image; added a missing ERMS tenant-registry row along the way (`901`/`FZZ999-B`/practiceCode-tier) discovered while live-testing the fix - ERMS had only a bare-PracticeId row from earlier, not the PracticeId+PracticeCode one this session's KARO fix pattern required.
- Verified live: `GetScannedList` for practice 901 now returns 40+ real document records with real `DataType` values (`application/pdf`, `text/html`) instead of crashing. Full test suite 66/66 pass.

**Files changed:** `src/Infrastructure/Legacy/Erms/ErmsDataRepository.cs`, `LEGACY_PARITY_VALIDATOR.md`

---

## 2026-07-28 21:15

**Task:** KARO/ERMS/COL dashboard credentials were pre-filled defaults only via `useState`, not persisted like `patientId`/`encounterId` - user had to retype them after every browser reload.

**Actions:**
- Added `username`/`password` to `SystemAuthState` (`store.ts`), widened `setAuth`'s patch type to include them so they save to `localStorage` (`hek-dashboard-state-v1`) the same way `token`/`practiceId` already do.
- Changed `SystemDashboard.tsx` to read/write username/password through `auth`/`onSetAuth` instead of local component `useState`, falling back to the known-good default credential (`hsslive`/`ermsdev`/`indiCOLProd`) only the first time, before anything's been saved yet.
- Type-checked (`tsc -b`, clean) and rebuilt the `frontend` image.

**Files changed:** `frontend/src/store.ts`, `frontend/src/SystemDashboard.tsx`

**Notes:** Existing `localStorage` entries from before this change won't have `username`/`password` keys yet, so the default-credential fallback will still apply once per system until the user's first edit (or the existing default fields) get saved - expected, not a bug.

---

## 2026-07-28 22:00

**Task:** Build a two-page presentable "Required vs Done vs Remaining" status document, tracing each claim back to the project's own source-of-truth files.

**Actions:**
- Used a subagent to locate the source-of-truth files: `docs/HEK_UNIFIED_API_SPEC.md` (v1.0) + `_v1.1.md` addendum for requirements, `hek_analysis/PROJECT_STATUS.md` (17-phase tracker) + `hek_analysis/v1.1-plan-status.md` for done/in-progress work, `LEGACY_PARITY_VALIDATOR.md` for legacy-parity evidence.
- Read all four files in full (not summaries) to pull accurate current text before drafting.
- Reused the CSS/structure from the existing `docs/HEK_UNIFIED_API_SPEC_presentable.html` as the visual base for consistency.
- Built `docs/PROJECT_STATUS_SUMMARY.html`: page 1 condenses FR-1..13/NFR/PR into a single Required table; page 2 splits into Done (phases 1-12 complete, all 17 v1.1 plan steps done, 60/61 legacy operations confirmed matching) and Remaining (phases 13-17 not started, the ERMS Authenticate Azure-forwarding gap, the unresolved Docker networking issue, and open spec questions like "Florence"/"~18 environments"), each item captioned with its source file.

**Files changed:** `docs/PROJECT_STATUS_SUMMARY.html`

**Notes:** This is a point-in-time snapshot (2026-07-28), not live-synced to the underlying trackers - footer states this explicitly so it doesn't get mistaken for a live dashboard.

---

## 2026-07-28 22:45

**Task:** User flagged that `PROJECT_STATUS.md` items 33 (Docker networking) and 28 (Block 2 stored-procedure verification) were stale/incorrect - asked to update by cross-checking other project docs.

**Actions:**
- Read `hek_analysis/CLIENT_DELIVERY_READINESS.md` (a prior session's audit that had already found this exact drift) and `LEGACY_PARITY_VALIDATOR.md` to confirm the real current state before editing anything.
- Corrected 4 stale rows in `hek_analysis/PROJECT_STATUS.md` §3 (Open Decisions): item 28 (was "~34 of ~35 unverified", corrected to the short named remaining list - Observations/Measurements/EncounterSummary/COL GetSessionData), item 33 (Docker networking - closed, resolved via `host.docker.internal` architecture change, not a container-to-container fix), item 30 (Aspose - closed, real DLL was already present, resolved 2026-07-26), item 34 (data-retrieval legacy wire-compat - closed, all 6 HISO ops + KARO/ERMS/COL confirmed via validator).
- Updated the §3 summary line and the §1 "Current position" paragraph to stop understating progress.
- Appended a dated Change Log entry documenting the sync pass and its sources.
- Updated `docs/PROJECT_STATUS_SUMMARY.html` (the two-page doc from the previous task) to match: moved Docker networking and the vague stored-procedure claim out of "Remaining" into a new "Resolved (previously mis-tracked as open)" list, and named the real remaining gap list under "Known gaps."

**Files changed:** `hek_analysis/PROJECT_STATUS.md`, `docs/PROJECT_STATUS_SUMMARY.html`

**Notes:** No code changes - documentation-only correction pass, cross-verified against two independently-maintained tracker files rather than taking either at face value.

---

## 2026-07-28 23:10

**Task:** User corrected two framings in `docs/PROJECT_STATUS_SUMMARY.html` and asked which ERMS operation is the "23rd" one.

**Actions:**
- Reclassified ERMS Authenticate's Azure-forwarding proxy: moved out of "Known gaps" into a new "Working as-is, no action currently required" list per user's correction — it's implemented and functioning on the local path, not a defect; a retire-vs-port decision can wait.
- Updated the legacy-parity stat line from "60 of 61 ... ERMS 22/23" to "61 of 61 ... ERMS 23/23" with a parenthetical explaining the Azure-forward substitution, since the user doesn't want it counted as unmatched.
- Removed the "Deferred by explicit stakeholder decision" section entirely (Database Architecture/Security-Observability-Infrastructure folding, Demographics reconciliation) per user's request.
- Answered inline: the ERMS operation behind the 22/23 count was always `Authenticate` itself (present and working) — specifically its Azure-forwarding sub-path, not a missing endpoint.

**Files changed:** `docs/PROJECT_STATUS_SUMMARY.html`

---

## 2026-07-29 00:15

**Task:** Implement FR-12 (call-flow traceability) as a pilot on Authenticate + demographics-equivalent endpoints across all 4 legacy systems (HISO/KARO/ERMS/COL), per plan approved in plan mode.

**Actions:**
- Added `CallRoutingInfo` (`src/Application/Common/Models/`) built from the tenant registry's already-computed `PracticeRoute` (KARO/ERMS/COL) or `HisoSessionRoute` (HISO) - info that was already resolved on every request but previously discarded right after building a connection string.
- Threaded `Routing` through 7 result records (`KaroAuthenticateResult`, `KaroDemographicsQueryResult`, `ErmsAuthenticateResult`, `ErmsGetPatientDataResult`, `ColAuthenticateResult`, `ColReadResult`, `GetDataQueryResult`) and their handlers, calling `ITenantRegistryService.ResolveRouteAsync`/`IHisoSessionRegistryRepository.FindAsync` alongside the existing routing-resolver calls.
- Added `RoutingSummaryFormatter` + `RoutingHeaderWriter` (`src/Api/Telemetry/`): builds a plain-English sentence (e.g. "This ERMS call was for practice 901 and was routed to the Local Development environment...") and writes it plus raw `X-Hek-Routing-*` headers onto the response - additive only, never touches a legacy-compat response body (preserves FR-10's zero-change guarantee).
- Wired routing into `LegacyOperationObserver`'s existing context-dict tagging (no changes to that class) so it also shows up as OpenTelemetry span tags in the already-linked Aspire dashboard.
- Updated CORS policy (`Program.cs`) with `WithExposedHeaders` for the new headers - `AllowAnyHeader()` alone doesn't expose custom response headers to browser JS cross-origin.
- Frontend: added `routingSummary`/`RunResult`/`AuthResult` fields (`runner.ts`, `api.ts`) reading the new header, and a "Call Flow" card in `EndpointCard.tsx`, `patientRecordForm.tsx`, `hisoView.tsx`, and the Authenticate status area in `SystemDashboard.tsx`.
- Verified live against the real Docker stack (rebuilt both `api` and `frontend` images): all 7 endpoints return correct headers/summaries with unchanged response bodies; full 43-test live integration suite still 43/43; visually confirmed the Call Flow card renders in-browser for HISO `getData` ("routed to Local Development... database dbserver-local/PMS_NZ_V2") and KARO `Authenticate`.
- Updated `hek_analysis/PROJECT_STATUS.md` (Change Log) and `docs/PROJECT_STATUS_SUMMARY.html` to reflect FR-12 as piloted (not fully rolled out to the remaining ~54 legacy-compat operations).

**Files changed:** `src/Application/Common/Models/CallRoutingInfo.cs` (new), `src/Api/Telemetry/RoutingSummaryFormatter.cs` (new), `src/Api/Telemetry/RoutingHeaderWriter.cs` (new), `src/Application/Features/Karo/Queries/KaroAuthenticateQuery.cs`, `src/Application/Features/Karo/Queries/KaroDemographicsQuery.cs`, `src/Application/Features/Erms/Queries/ErmsAuthenticateQuery.cs`, `src/Application/Features/Erms/Queries/ErmsGetPatientDataQuery.cs`, `src/Application/Features/Col/ColQueries.cs`, `src/Application/Features/Hiso/Queries/GetDataQuery.cs`, `src/Api/Features/Auth/Controllers/{KaroCompatController,ErmsCompatController,ColCompatController}.cs`, `src/Api/Features/Hiso/Controllers/HisoCompatController.cs`, `src/Api/Program.cs`, `frontend/src/{runner.ts,api.ts,EndpointCard.tsx,patientRecordForm.tsx,hisoView.tsx,SystemDashboard.tsx,App.css}`, `hek_analysis/PROJECT_STATUS.md`, `docs/PROJECT_STATUS_SUMMARY.html`

**Notes:** Discovered along the way that COL's routing header shows `SourceSystem: Erms` (COL shares ERMS's registry/DB routing, as already documented) while the plain-English summary correctly says "This COL call..." - intentional, not a bug, since the summary uses the caller-facing system name while the header shows the real backing system.

---

## 2026-07-29 00:40

**Task:** Investigate why the new FR-12 routing summary wasn't visible in the Aspire dashboard (user tried it live), build a fully human-readable per-call log line instead, then figure out how to retain ~30 days of logs without unbounded file growth or extra memory use.

**Actions:**
- Live-checked the Aspire trace UI for a real KARO Authenticate call: confirmed none of the custom `legacy.*` span tags (not just the new one - existing ones like `legacy.PatientId` too) actually appear in the trace's tag list or structured logs view - a real, pre-existing gap in this environment, unrelated to this session's change. Flagged to the user rather than claiming success.
- Built `RoutingSummaryFormatter.BuildCallLogLine(...)` - one plain-English `ILogger.LogInformation` sentence per call covering: which system/action, which patient/encounter, success/failure, which fields were returned (by name only, never value - PHI must not land in plain-text logs per the project's NZ HIPC compliance stance), and which DB server/environment served it. Added `GetNonNullPropertyNames` (reflection-based) to list returned field names without hardcoding a schema per endpoint.
- Wired into KARO `Authenticate` and `GetDemographics` (`KaroCompatController.cs`) as the concrete demo the user asked to see. Verified live via `docker logs` - both lines render exactly as designed.
- User then asked how to keep ~30 days of history without unbounded file size/memory. Investigated: a Serilog file sink was already configured (`appsettings.json`, 14-day retention) but the log file lived inside the `api` container's ephemeral filesystem with no volume - every rebuild/restart (which happened repeatedly this session) wiped it, and 14 days was short of "ek maheena" anyway.
- Fixed both gaps with config only (no new packages, `Serilog.Sinks.File` already referenced): `retainedFileCountLimit: 14` → `retainedFileTimeLimit: "30.00:00:00"` + `fileSizeLimitBytes`/`rollOnFileSizeLimit` (bounds per-file size) in `appsettings.json`; added a named Docker volume (`api-logs:/app/logs`) in `docker-compose.yml`.
- Found and fixed a real bug while verifying: the new volume mounted as root-owned, but the container runs as non-root user `hek` - Serilog's file writes were silently failing (file sink swallows write errors by default). Fixed in `Dockerfile` by creating `/app/logs` and `chown`-ing it to `hek` before the `USER hek` switch, so Docker's volume-populate-from-image-on-first-mount step carries the right ownership.
- Verified end-to-end: triggered a real call, confirmed the log line was written, then did a full `docker compose stop/rm/up` (harder than a plain restart) and confirmed the same line was still present afterward - proves persistence actually survives what previously wiped it. Full 43-test live integration suite still 43/43 after all changes.

**Files changed:** `src/Api/Telemetry/RoutingSummaryFormatter.cs`, `src/Api/Features/Auth/Controllers/KaroCompatController.cs`, `src/Api/appsettings.json`, `docker-compose.yml`, `src/Api/Dockerfile`

**Notes:** Deliberately did not add `Serilog.Sinks.Async` or any buffered/in-memory queue - current traffic doesn't need it, and it would add memory overhead the user explicitly asked to avoid. Only KARO Authenticate/GetDemographics have the new human-readable log line so far (demo scope); the same pattern can be extended to the other 5 FR-12 pilot endpoints if wanted.

---

## 2026-07-29 01:10

**Task:** User asked where to actually *see* the plain-English call log without a terminal - build a "Logs" tab in the existing dashboard. Chose in-dashboard tab (not standalone page), showing only the readable call-summary lines (not raw Serilog noise).

**Actions:**
- Added `src/Api/Features/Admin/Controllers/LogsController.cs` (`GET /admin/logs/recent?take=50`) - internal-only, `[ApiExplorerSettings(IgnoreApi = true)]` (same justification as `HisoCompatController`: dashboard-support tooling, not a real domain operation), no MediatR. Reads the log directory/filename from `Serilog:WriteTo:1:Args:path` config (never hardcoded), globs the 3 most recent rolled `hek-core-api-*.log` files, and returns only lines tagged with a `[CallLog]` marker (added to the message template so filtering is exact, not a fragile text-contains guess), newest first, marker stripped.
- Tagged the 2 existing `RoutingSummaryFormatter.BuildCallLogLine` call sites in `KaroCompatController.cs` with the `[CallLog]` prefix.
- Frontend: new `LogsPanel.tsx` (manual-refresh list, matches the dashboard's existing manual-run pattern - no auto-polling) + `getRecentCallLogs()` in `api.ts`. Wired into `App.tsx`: widened the nav's `active` state to `SystemId | "logs"`, added a 5th sidebar button (outside the `systems.map()` loop, since `systems.ts`'s `SystemId` union is typed to the 4 real legacy systems only and widening it would ripple into `SystemDashboard.tsx`'s `RECORD_SYSTEMS`/`AUTH_DEFAULTS`), and a conditional render swapping `SystemDashboard` for `LogsPanel` when the Logs tab is active.
- Found and fixed an off-by-one in the timestamp substring (Serilog's `"yyyy-MM-dd HH:mm:ss.fff zzz"` format is 30 chars, code sliced 29) while verifying live.
- Verified end-to-end: rebuilt api+frontend, triggered a real KARO Authenticate call, confirmed `GET /admin/logs/recent` returns it, then opened the dashboard in a real browser and confirmed the new "Logs" tab renders the same two real entries with correct timestamps. Full 43-test live integration suite still 43/43.

**Files changed:** `src/Api/Features/Admin/Controllers/LogsController.cs` (new), `src/Api/Features/Auth/Controllers/KaroCompatController.cs`, `frontend/src/LogsPanel.tsx` (new), `frontend/src/api.ts`, `frontend/src/App.tsx`, `frontend/src/App.css`

**Notes:** Scope is currently KARO Authenticate/GetDemographics only (the two endpoints with the `[CallLog]`-tagged line) - the tab itself works for any endpoint that adopts the same logging pattern, so extending to the other FR-12 pilot endpoints is a small follow-up, not a redesign.

---

## 2026-07-29 02:00

**Task:** Implement the full per-system logging overhaul, approved plan at `hek_analysis/LOGGING_OVERHAUL_PLAN.md`: real host folder (not Docker volume), 3 files per system (technical JSON, readable plain-English, errors-only), covering every legacy-compat endpoint (not just the 2-endpoint KARO pilot), with a shared Request ID linking entries across files.

**Actions:**
- Discovered `CorrelationIdMiddleware` (`src/Api/Middleware/`) already existed and pushes a per-request `CorrelationId` into Serilog's LogContext - reused it directly as the cross-file Request ID instead of building a new one.
- Added `Serilog.Formatting.Compact` package (`Directory.Packages.props`, `HekCoreApi.Api.csproj`) for JSON-structured technical/errors output.
- Rewired `Program.cs`'s `UseSerilog` callback: added 12 sub-logger blocks (`WriteTo.Logger` + `Filter.ByIncludingOnly`) - 4 systems × {technical (JSON, 30gd), readable (plain-English `[CallLog]` lines only, 30d), errors (Warning+, 90d)} - filtering on the `System` property that was already present on every `LegacyOperationObserver` log line (its message templates already use `"{System} ..."`), so no new enrichment code was needed for that part.
- **Key design decision**: centralized `[CallLog]` emission inside `LegacyOperationObserver` itself (`LogSuccess`, `RecordExpectedFailure`, `RecordUnexpectedFailure`, and a widened `Tag` method that now takes `ILogger`) via a new private `EmitCallLog` helper, instead of hand-wiring every controller action. Since every legacy-compat endpoint already funnels through this one class, this turned out to cover far more than the original 5-7 endpoint pilot scope for free.
- Widened `LegacyOperationObserver.Tag(...)`'s signature to accept `ILogger` (previously didn't log at all, just tagged the trace span) - updated all 8 call sites across `KaroCompatController`/`ErmsCompatController`/`ColCompatController` via `sed`.
- Removed the now-redundant manual `[CallLog]` lines from `KaroCompatController`'s Authenticate/GetDemographics (the ones added in the earlier pilot session) since the centralized version now emits them automatically; added an optional `context["FieldsReturned"]` convention so callers that know their exact field names (like KARO's two pilot endpoints) can still enrich the line, while generic callers work without it.
- `docker-compose.yml`: replaced the `api-logs` named volume with a bind mount (`./logs:/app/logs`) so the files are real, directly browsable folders on the host (`E:\claude_projects\HEK Core API\logs\`), not hidden inside Docker. Removed the now-unused `api-logs` volume declaration and the stale Docker volume itself.
- `LogsController` rewritten to read across all 4 systems' `readable-*.log` folders (previously one hardcoded flat-file pattern), aggregating and sorting by timestamp, with an optional `?system=` filter; `CallLogEntry` gained a `System` field. Frontend (`api.ts`, `LogsPanel.tsx`, `App.css`) updated to display the system alongside each entry.
- Verified end-to-end, live: rebuilt api+frontend containers, confirmed the host `logs/` folder populates with real subfolders/files on first matching call (no `chown` issue this time - Docker Desktop's Windows bind-mount permissions worked out of the box, unlike the earlier named-volume case). Deliberately triggered a KARO auth failure (wrong password) and confirmed it appears in `karo/errors-20260728.log` (JSON, with `CorrelationId`) and `karo/readable-20260728.log` (plain English) with matching `CorrelationId` values. Ran the full 43-test live integration suite - it incidentally exercised ~15 different KARO operations (GetClinicalNotes, SaveInvoice, GetDocuments, SaveDocument, GetDemographics, etc.) plus ERMS/COL/HISO operations, and **every one of them produced correct entries in all 3 of that system's files** with zero additional code - concrete proof the centralized design works, not just the 2 originally-piloted endpoints. Suite still 43/43 pass (purely additive logging, no response body touched).
- Updated `hek_analysis/LOGGING_OVERHAUL_PLAN.md` to mark the plan done, and noted one real remaining gap: most controllers' context dicts only carry `Routing.DbServerHost`/`Routing.Environment`, not `Routing.DbName`/`Routing.PracticeId`, so the readable line shows "database -" even where routing is otherwise resolved.

**Files changed:** `Directory.Packages.props`, `src/Api/HekCoreApi.Api.csproj`, `src/Api/Program.cs`, `src/Api/Telemetry/LegacyOperationObserver.cs`, `src/Api/Features/Auth/Controllers/{KaroCompatController,ErmsCompatController,ColCompatController}.cs`, `src/Api/appsettings.json`, `docker-compose.yml`, `src/Api/Features/Admin/Controllers/LogsController.cs`, `frontend/src/{api.ts,LogsPanel.tsx,App.css}`, `hek_analysis/LOGGING_OVERHAUL_PLAN.md`

**Notes:** Endpoints without routing resolution added yet (most of the ~54 outside the original FR-12 pilot) honestly log "routing could not be determined" rather than fabricating a DB server/environment - matches the project's standing rule against inventing unconfirmed values. Closing that gap (adding `ITenantRegistryService`/`IHisoSessionRegistryRepository` calls to the remaining handlers) is the one piece of follow-up work left, not a blocker to the logging infrastructure itself, which is fully working end to end today.

---

## 2026-07-29 03:00

**Task:** Close the remaining logging gaps: wire real routing into all ~45 previously-uncovered endpoints (KARO/ERMS/COL/HISO), make CorrelationId visible in the readable log file (not just technical/errors JSON), fix the known `Routing.DbName`/`Routing.PracticeId` gap, and add a "Verbose Diagnostic Logging" toggle for full request/response payload logging during production troubleshooting (agreed design: default OFF, technical file only, never readable file - discussed and agreed with user across several messages given the tension between "need full detail to debug" and "must never put PHI in plain-text logs").

**Actions:**
- **KARO (20 endpoints)**: found the shared `KaroPipeline.RunAsync<T>` class already used by 9 of the read handlers - added `ITenantRegistryService` there once, covering all 9 in one edit. The other 3 custom handlers (Documents, PatientAttachment, EncounterSummary) and all 7 write-command handlers got the same one-liner added individually (used `perl -0777` multi-line regex to apply the identical constructor/resolution pattern across all of them in a couple of passes, verified by rebuilding after each). Added `Routing` field to `KaroListResult<T>`, `KaroRawJsonResult`, `KaroWriteResult`. Controller side: found `KaroCompatController` already had 2 shared helpers (`WriteResult`, `RootOrFail`) covering nearly all 20 actions - added one new `BuildContext` helper there and wired both into it, rather than touching each action.
- **ERMS (19 endpoints)**: same trick - `ErmsReadPipeline` was already shared by 18 of 19 handlers, one edit covered all 18; `ErmsSaveDocumentCommand` (the 19th, a separate write handler) got its own routing resolution. Controller side: `ErmsCompatController`'s shared `Render` method already covered ~18 actions in one place.
- **COL**: confirmed via the Explore agent's earlier inventory that 4 of 5 remaining actions already flowed through the existing `ColReadPipeline`/`RenderList<T>` from the original pilot - only `SaveInvoice` (a separate write handler) genuinely needed new routing resolution. Found and fixed a real pre-existing bug while touching this: `ColReadPipeline`'s catch block dropped already-resolved routing on exception (e.g. COL `GetSessionData`'s real legacy bug - an empty stored-procedure name) - moved the `routing` variable outside the `try` so exceptions after resolution still carry it.
- **HISO (5 endpoints: GetVersion, GetDeliveryOptions, ProcessAction, SaveContainer, GetFormView)**: same pattern as the already-piloted `GetDataQuery` (`IHisoSessionRegistryRepository.FindAsync`), applied to each handler/result record individually - no shared pipeline exists here, so this genuinely needed 5 separate edits. `GetVersionQuery` previously returned a bare `bool` - changed to a proper `GetVersionQueryResult` record (with a `Routing` field) since a bare `bool` can't carry it, which also required fixing the real SOAP door (`FormSessionService.svc`'s `getVersion`) that called the same query.
- **Real design gap found and fixed**: `LegacyOperationObserver.ObserveAsync` (used only by `HisoCompatController`) logs success/failure using a `context` dict built *before* the mediator call - but HISO's routing is only known *after* the session resolves. Added an optional `enrichContext` callback parameter that runs after the result is available, merging extra entries (routing) into the context right before logging - fixes this for all 6 HISO actions (the pilot `getData` had this same latent gap, not just the 5 new ones).
- **CorrelationId visibility**: added `[{CorrelationId}]` to the readable-file output template in `Program.cs` (reusing the existing `CorrelationIdMiddleware`, no new correlation mechanism needed). Rewrote `LogsController`'s line-parsing regex to extract it, added `CorrelationId` to `CallLogEntry`, surfaced it as a small tag in `LogsPanel.tsx`.
- **Verbose Diagnostic Logging toggle**: new `VerboseDiagnosticLoggingOptions` (`Enabled`, default `false`), new `LegacyOperationObserver.LogVerbosePayload(logger, system, endpoint, request, response)` - logs at Information level with a template that deliberately excludes `[CallLog]` (so the readable-file filter naturally skips it) and isn't Warning+ (so the errors-file filter skips it too) - lands in `technical-*.log` only, by construction, no extra plumbing. Not yet called from any controller action - the mechanism exists, wiring it into individual endpoints is a further follow-up if wanted.
- Verified end-to-end, live: rebuilt the api container twice (once after the main routing wiring, once after the COL catch-block fix), ran the full 43-test live integration suite both times (43/43 both times - zero legacy-compat response bodies touched). Manually inspected `logs/{karo,erms,col,hiso}/readable-*.log` after a full test run - confirmed real, previously-"routing could not be determined" endpoints (KARO SaveRecall, HISO getVersion/getDeliveryOptions/getFormView/getData, COL GetSessionData's failure path) now show real `dbserver-local`/`PMS_NZ_V2`/environment detail, each with a visible `[CorrelationId]` tag.

**Files changed:** `src/Application/Features/Karo/Queries/KaroReadQueries.cs`, `src/Application/Features/Karo/Commands/KaroWriteCommands.cs`, `src/Application/Features/Erms/Queries/ErmsReadQueries.cs`, `src/Application/Features/Erms/Commands/ErmsSaveDocumentCommand.cs`, `src/Application/Features/Col/ColQueries.cs`, `src/Application/Features/Hiso/Queries/{GetVersionQuery,GetDeliveryOptionsQuery,GetFormViewQuery}.cs`, `src/Application/Features/Hiso/Commands/{ProcessActionCommand,SaveContainerCommand}.cs`, `src/Api/Features/Auth/Controllers/{KaroCompatController,ErmsCompatController,ColCompatController}.cs`, `src/Api/Features/Hiso/Controllers/HisoCompatController.cs`, `src/Api/Features/Hiso/Soap/FormSessionService.cs`, `src/Api/Telemetry/LegacyOperationObserver.cs`, `src/Api/Configuration/VerboseDiagnosticLoggingOptions.cs` (new), `src/Api/Program.cs`, `src/Api/appsettings.json`, `src/Api/Features/Admin/Controllers/LogsController.cs`, `frontend/src/{api.ts,LogsPanel.tsx,App.css}`, `hek_analysis/LOGGING_OVERHAUL_PLAN.md`

**Notes:** The "find and extend the shared pipeline/helper instead of touching every endpoint" approach (already proven in the earlier FR-12 pilot session) is what made ~45 endpoints tractable in one session - KARO and ERMS each needed only 1-2 core edits plus a handful of custom-handler/controller-helper edits, not 20+19 fully independent changes. HISO was the one system without this shortcut available, hence needing all 5 done by hand plus the new `enrichContext` mechanism.

---

## 2026-07-29 03:45

**Task:** User asked how to identify the actual request/response data in the technical log (noted it wasn't there yet) - wire the previously-built-but-unused `VerboseDiagnosticLogging` toggle into every endpoint, so turning it on shows the real request/response payload.

**Actions:**
- Widened `LegacyOperationObserver`'s shared methods (`Tag`, `RecordExpectedFailure`, `LogSuccess`, `ObserveAsync`, `ObserveSwallowedAsync`) to accept an optional `response` object, calling the existing `LogVerbosePayload` internally - all backward-compatible (`response = null` default), so no call site broke.
- This meant `ObserveAsync`/`ObserveSwallowedAsync` - already used by all 6 HISO endpoints and, via `ErmsCompatController.Render`/`ColCompatController.RenderList`, by 19 ERMS + 5 COL read endpoints - got full request/response verbose logging with **zero controller changes**, since those methods already have the result in scope.
- For KARO (uses `Tag`/`RecordExpectedFailure` directly, not the Observe* wrappers): updated the 2 shared helpers (`WriteResult`, `RootOrFail`, covering ~18 ops) plus 3 standalone calls (Authenticate, GetDemographics, GetEncounterSummary) to pass `result` through - via `sed` line-targeted edits.
- For the remaining standalone Authenticate/SaveDocument/SaveInvoice call sites in ERMS/COL (6 more), same one-line addition each.
- Found and fixed a real scope bug while doing this: `ErmsCompatController.SaveDocument`'s late `_observer.Tag(...)` call is outside the `try` block where `result` is declared (only `objDocument`, the scrubbed echo object, is in scope there) - passed `objDocument` instead of `result`, which is actually the more correct payload for that call site anyway (echoes what legacy actually returns).
- Verified live end-to-end: rebuilt the api container, called KARO Authenticate with the toggle at its default (OFF) - confirmed zero "verbose payload" lines anywhere. Then flipped `appsettings.json`'s `VerboseDiagnosticLogging.Enabled` to `true`, rebuilt, called Authenticate again - confirmed a real "verbose payload" JSON line appeared in `technical-*.log` with the full request context (patientId/encounterId/routing) and full response object (real token value, expiry, routing detail) - and confirmed zero leakage into `readable-*.log`/`errors-*.log` (the two other sub-loggers' filters naturally exclude this Information-level, non-"[CallLog]"-tagged, non-Warning+ line). Reverted the toggle to `false`, rebuilt again, ran the full 43-test live integration suite - 43/43 pass.
- Along the way, discovered and correctly interpreted a UTC day-rollover: Serilog had rolled from `*-20260728.log` to `*-20260729.log` mid-session (container clock crossed midnight UTC) - not a bug, just meant checking the newly-dated files instead of the ones used earlier in the session.

**Files changed:** `src/Api/Telemetry/LegacyOperationObserver.cs`, `src/Api/Features/Auth/Controllers/{KaroCompatController,ErmsCompatController,ColCompatController}.cs`, `src/Api/appsettings.json` (toggled true then back to false), `hek_analysis/LOGGING_OVERHAUL_PLAN.md`

**Notes:** The verbose payload for `KaroAuthenticateResult` genuinely includes the real bearer token in plaintext when the toggle is on - this is by design (that's the point of "full detail for troubleshooting") but confirms why the toggle defaults off and why the technical file needs the access-control/encryption-at-rest treatment discussed earlier, the moment anyone actually turns it on.

---

## 2026-07-29 05:55

**Task:** User reported a manual HSS/KARO call from the dashboard produced no log entries at all (neither readable nor technical) - asked to make sure every endpoint's complete logs are written.

**Actions:**
- Investigated via `docker logs`/`logs/karo/*.log`: confirmed zero KARO-tagged log lines anywhere in the ~25 minutes around the user's report, while ERMS/HISO calls in the same window logged correctly - ruled out a Serilog/pipeline bug (the per-system sub-loggers work fine when a controller actually runs).
- Root cause: `Microsoft.AspNetCore`'s own request-diagnostics log category is suppressed to `Warning` in `appsettings.json` (needed to keep routine framework noise out of the files), so a request that never matches a real route (404 - wrong path, e.g. trying `/hss/...` instead of the real `/karo/...` prefix) or gets blocked before reaching a controller produces **zero log output anywhere** - completely silent, by design gap, not a bug in the per-system logging itself.
- Added `src/Api/Middleware/RequestLoggingMiddleware.cs` (new) - a safety-net middleware registered first in the pipeline (`app.UseMiddleware<RequestLoggingMiddleware>()`, right after `UseCorrelationId()`) that logs every single request's method/path/status/elapsed time, Warning level for 4xx/5xx, Information for everything else - so nothing can silently vanish again.
- Added a matching Serilog sub-logger in `Program.cs` writing these to `logs/requests-.log` (14-day retention), filtered on `SourceContext` containing `RequestLoggingMiddleware` so it doesn't duplicate into the per-system files.
- Verified live: rebuilt + restarted the `api` container, hit a deliberately wrong path (`/hss/authenticate`) and a real one (`/karo/ping`) - confirmed the wrong path now shows up as a `404 WRN` line in `logs/requests-.log` (previously silent), the real one shows `200 INF`. Full 43-test live integration suite still 43/43 after the change.

**Files changed:** `src/Api/Middleware/RequestLoggingMiddleware.cs` (new), `src/Api/Program.cs`

**Notes:** User confirmed the call was made via the frontend dashboard, not a raw manual URL - the dashboard's own paths (`catalog.ts`) are correct (`/karo/...`), so the likely trigger was something upstream of the controller (wrong host/port/proxy hiccup, or a request that never actually left the browser) rather than a typo'd path by hand. The new `requests-*.log` safety net means the next time this happens, it'll be visible immediately regardless of the exact cause - ask the user to retry the HSS call and check `logs/requests-.log` for what path/status it actually hit.

---

## 2026-07-29 06:15

**Task:** User asked for hard proof that verbose diagnostic logging (technical file, full request+response) actually works on every one of the ~61 endpoints, not just the ones exercised earlier - wanted a concrete per-endpoint list with CorrelationId so they could open the log file themselves and check.

**Actions:**
- With `VerboseDiagnosticLogging.Enabled: true` already on, ran the full 43-test live integration suite, then wrote a one-off Node script to parse `logs/{system}/technical-*.log` (CompactJsonFormatter output - `@mt` holds the message template, not `@m`) and list every unique `Endpoint` that produced a "verbose payload" line, with its `CorrelationId`.
- First pass found real gaps: 6 KARO actions (`GetObservations`, `GetRecallCategories`, `GetEncounterSummary`, `GetPatientAttachment`, `SaveSummary`) and 4 ERMS drilldown actions (`GetLaboratoryReportDetails`/`GetRadiologyReportDetails`/`GetDischargeSummaryDetails`/`GetScannedDetails`) and 2 HISO actions (`processAction`, `saveContainer`) simply aren't exercised by the test suite (confirmed by grepping the test files) - not a code gap, a test-coverage gap.
- Manually called each missing endpoint directly via `curl` (real test patient 2459731/encounter, real KARO/ERMS routes) to close every reachable gap - all produced correct verbose-payload lines with real CorrelationIds once actually called.
- **`saveContainer` genuinely can't produce a verbose-payload line**: it's a real, pre-existing legacy bug (`Acc45DefinitionRepository.SaveDefinitionAsync` throws `ArgumentException: There are not enough fields in the Structured type` before the handler returns) - the request never reaches the success path where `LogVerbosePayload` fires. Confirmed this is *not* silent, though: `RecordUnexpectedFailure` already logs the full context + complete exception stack trace to `errors-*.log`/`technical-*.log` on every unhandled exception - a different, already-complete code path, not a hole. Explained this distinction to the user rather than "fixing" a non-bug.
- Final tally: **55 of 55 reachable endpoints** confirmed with real verbose-payload lines + CorrelationIds (KARO 21, ERMS 22, COL 7, HISO 5); `saveContainer` is the one HISO action whose failure mode routes through the exception-log path instead (fully logged, just via a different, already-verified mechanism). The 2 `ping` health-check routes and KARO's `screeningcodes` POST (a genuine legacy no-op, confirmed never touches the DB or validates a token) never call the observer by design - correctly excluded, not gaps.
- Ran the full 43-test live integration suite again after all the manual verification calls - still 43/43.

**Files changed:** none (verification-only session; temporary Node script used and deleted, no code changes)

**Notes:** Gave the user the full per-endpoint CorrelationId list inline so they can open `logs/{system}/technical-20260729.log`, search for each ID, and see the real request+response JSON themselves rather than trusting a summary claim.

---

## 2026-07-29 06:45

**Task:** User said the log files are hard to read - asked for a visual separator between entries so one request is clearly distinct from the next.

**Actions:**
- Added `src/Api/Telemetry/SeparatedCompactJsonFormatter.cs` (new) - wraps `CompactJsonFormatter` and appends a `====...====` line after each JSON event, for `technical-*.log`/`errors-*.log` (raw JSON otherwise reads as one unbroken wall of text per line).
- Added a `----...----` separator line to the `readable-*.log` and `requests-*.log` output templates (both already plain-text, one line per event).
- Verified `LogsController`'s existing line-parsing regex (used by the dashboard's "Logs" tab) correctly ignores the new separator lines (they don't match the `timestamp [correlationId] message` pattern, so they're silently skipped) - confirmed live via `GET /admin/logs/recent`, still returns correct entries.
- Rebuilt + restarted the `api` container, triggered real calls, confirmed separators appear correctly in all 3 file kinds across systems. Full 43-test live integration suite still 43/43.

**Files changed:** `src/Api/Telemetry/SeparatedCompactJsonFormatter.cs` (new), `src/Api/Program.cs`

---

## 2026-07-29 (session continued)

**Task:** User asked to use the "project memory" skill to check what's missing in project documentation, build a single source-of-truth status doc, and make it update automatically after every future task (no manual reminder needed).

**Actions:**
- Audited existing docs (`docs/DOCUMENT_INDEX.md`, `docs/CHANGELOG.md`, `hek_analysis/PROJECT_STATUS.md`, `hek_analysis/v1.1-plan-status.md`, `LEGACY_PARITY_VALIDATOR.md`) - found real drift: `docs/DOCUMENT_INDEX.md` linked two spec `.md` files that were actually deleted from the repo (replaced by `docs/HEK_UNIFIED_API_SPEC_presentable.html`), and two AI usage logs exist in parallel (`AI_USAGE_LOG.md` at root, `docs/ai_usage_log.md`).
- Created `docs/PROJECT_MASTER.md` - single source of truth: project summary, current state, doc map, known drift, update protocol.
- Linked it from the top of `docs/DOCUMENT_INDEX.md`.
- Created root `CLAUDE.md` (project didn't have one) instructing future sessions to update `PROJECT_MASTER.md` §2 automatically after any meaningful change, without being asked. Trimmed on request to keep per-session token cost minimal (~50 words).

**Files changed:** `docs/PROJECT_MASTER.md` (new), `docs/DOCUMENT_INDEX.md`, `CLAUDE.md` (new), `docs/ai_usage_log.md` (this entry).

**Notes:** This is itself the first real-world exercise of the new update protocol - confirms it works, but proves it isn't retroactive: it only fires when *invoked* on a task, so this session's own doc work had to be logged manually. Known drift items (dead spec links, duplicate AI usage logs) are flagged in `PROJECT_MASTER.md` §4, not yet fixed.

---
