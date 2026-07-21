# AI Usage Log — HEK Unified Healthcare API

This is an append-only record of AI-assisted work on this codebase: what was asked, what the AI
(Claude Code) did, which files were touched, and what decisions were made along the way. It
exists for transparency/audit purposes, separate from `hek_analysis/PROJECT_STATUS.md` (the
project's single source of truth for decisions/facts) and `docs/adr/` (formal architecture
decisions) — this log is specifically about *what the AI did and when*, not project status in
general. Update it after every meaningful step, not just at block boundaries.

**How to read this log:** newest entries at the bottom (chronological, unlike PROJECT_STATUS.md's
newest-first change log) so it reads as a build timeline. Each entry: date/time, what was
requested, what was done, files touched.

---

## 2026-07-20 — Day-1 build sprint, Block 0 (Scaffolding)

**Requested:** Build the Day-1 sprint per `docs/Unified-Healthcare-API_ImplementationPlan.md`,
starting with Block 0. Stop for sign-off before Block 2.

**Done:** New git repo initialized at project root. 8-project Clean Architecture solution
(`HekCoreApi.sln`: `Domain, Contracts, Application, Infrastructure, Adapters.Hiso, Adapters.Karo,
Adapters.Erms, Api` + 5 test projects) wired with dependency-direction-enforcing project
references. `ISecretProvider`/`EnvironmentVariableSecretProvider` vertical slice (replaces the
legacy hardcoded `tcpepms*1` pattern). Serilog + correlation-ID middleware. `/health` (liveness)
+ `/health/ready` (SQL-dependent readiness). `src/Api/Dockerfile`, `docker-compose.yml`/`.override.yml`,
`.env.example`. `.github/workflows/ci.yml`. ADR-012 drafted documenting structural deviations
from the Implementation Plan's literal 7-project list (added `Contracts`, COL folded into
`Adapters.Erms/Col/`).

**Verified:** Solution builds clean (Debug + Release, 0 warnings/errors). Ran the built binary
directly (Docker unavailable in the execution sandbox) and confirmed `/health` returns 200
immediately while `/health/ready` correctly differs when SQL is unreachable.

**Key files:** `HekCoreApi.sln`, `Directory.Build.props`, `src/Api/Program.cs`,
`src/Infrastructure/Secrets/EnvironmentVariableSecretProvider.cs`, `docker-compose.yml`,
`docs/adr/ADR-012-solution-structure-and-block1-inferences.md`.

---

## 2026-07-20 — Block 1 (Security Core)

**Requested:** Continue to Block 1 per the same plan (auth, tenant routing, HISO session
handling, rate limiting, RFC 7807 errors, CORS) — no domain business endpoints yet.

**Done:** `Contracts` project populated with wire-shape DTOs matching the OpenAPI spec exactly
(`TokenRequest`/`TokenResponse`, `Error`, `ResourceScope`, `OriginScope`). Tenant registry
(`PracticeRegistryEntry` + `TenantRegistryDbContext` + EF Core migration + `ITenantRegistryService`)
per ADR-001 — schema explicitly flagged as an inference, no source document gives a literal
column list. Canonical `POST /auth/token` (MediatR pipeline: `AuthenticateCommand` →
`EntraIdIdentityValidator` (MSAL) → `JwtTokenIssuer` minting HEK's own resource-scoped JWT per
ADR-003). Legacy compat auth endpoints built from the **actual documented wire shapes** found in
`hek_analysis/docs/_source_docs/KARO_HSS_doc.md` and `ERMS_doc.md` (not guessed) — JSON for KARO,
XML for ERMS, JSON for COL (COL itself flagged as undocumented per the SRS).
`ResourceScopeAuthorizationHandler` + policy (built, unit-tested, not yet wired to any endpoint —
that's Block 2's job). HISO session handling: parameterized query against the documented
`tblHealthLinkSession` columns, new 12-hour expiry, structured security-event logging replacing
the legacy swallowed-exception pattern. Rate limiting (off by default per ADR-008). RFC 7807
error middleware (`GlobalExceptionHandler`, generic non-PHI message catalog). CORS mechanism.

**Verified:** 8 unit tests added and passing, covering auth success/failure paths, HISO session
expiry logic, and the authorization handler's claim validation.

**Key files:** `src/Application/Features/Auth/`, `src/Infrastructure/Auth/JwtTokenIssuer.cs`,
`src/Adapters.Karo/Auth/HssAuthenticateTranslator.cs`, `src/Adapters.Erms/Auth/`,
`src/Adapters.Hiso/Session/HisoSessionResolver.cs`, `src/Api/Errors/GlobalExceptionHandler.cs`.

---

## 2026-07-20 — Dormant DAL modules: DMSDA.cs ported, DBMessages.cs deferred

**Requested:** User supplied real legacy source (`legacy-reference/DAL/DMS/DMSDA.cs`,
`legacy-reference/DAL/MHNHL7/DBMessages.cs`) after Block 1, asked to port them (fix the confirmed
SQL injection, per the SRS Phase A hard-blocker).

**Done:** Read both real files. `DMSDA.cs`'s confirmed injection — `UpdateInboxFolderDocuments`,
a `private`, never-called method with string-concatenated `CommandText` — was ported to
`src/Infrastructure/Legacy/Dormant/Dmsda/DmsDocumentService.cs` with the injection fixed via full
parameterization; the rest of the file's already-safe methods ported faithfully. A new unit test
(`DmsDocumentServiceTests.cs`) proves a SQL-metacharacter payload now fails only as a connection
error, never a syntax error. `DBMessages.cs`, on inspection, contained **no actual injection** —
contradicting the expected framing — and was tightly coupled to undocumented HISO types
(`HealthLinkSession`, `HisoRequest`, `DynamicParam`). Flagged back to the user rather than
inventing those types; user decided to defer it to Block 2.

**Key files:** `src/Infrastructure/Legacy/Dormant/Dmsda/DmsDocumentService.cs`,
`src/Infrastructure/Legacy/LegacyDbExecutor.cs`,
`tests/Infrastructure.UnitTests/Legacy/DmsDocumentServiceTests.cs`.

---

## 2026-07-20 — Block 2 (all 18 domain endpoint groups) + Block 3 (contract re-verification)

**Requested:** "Start block 2."

**Done:** Built all 18 domain groups / 42 canonical OpenAPI operations (Demographics, Clinical
Notes, Conditions, Medications, Lab/Radiology Results, Documents, Observations, ACC45, Encounter
Summary Templates, Tasks, Recalls, Screening, Providers, Practice Context, Billing, Tenant Admin,
Health, HISO dead-code paths), following the same Contracts → Application/MediatR → Infrastructure
→ Api pattern from Block 1, incremental build-checked after every group.

**Mid-block discovery:** `DBMessages.cs` (deferred above) turned out to be foundational
infrastructure for most HISO-sourced read endpoints, not an ACC45-only concern — ported its real
engine (`HisoConceptExecutor`) using the actual supplied source, reconstructing
`HealthLinkSession`/`HisoRequest`/`DynamicParam` directly from their observable usage in that
file (not invented from nothing).

**Block 3:** Every controller route was extracted and cross-checked against the OpenAPI spec's 42
operations — full path/verb match. One deliberate drift (`POST /auth/token` returns `501` since
no source document defines its origin scope for a direct caller) was corrected **in the spec
itself** (`openapi.yaml` → 1.1.1, new `x-changelog` entry, documented `501` response) rather than
left silently mismatched.

**Verified:** Full solution build (Release, 0 warnings/errors) + all 9 tests passing throughout.

**Real gaps flagged, not hidden** (see `hek_analysis/PROJECT_STATUS.md` §3 items 28-31): every
Block 2 repository's stored-procedure/column names are inferred (no live schema access anywhere);
`billing:write` scope is checked but never granted by anything; Aspose ACC45 rendering isn't
implemented (no license available).

**Key files:** `src/Api/Features/*/Controllers/`, `src/Infrastructure/Legacy/*/`,
`src/Application/Features/*/`, `hek_analysis/docs/architecture/Unified-Healthcare-API_openapi.yaml`.

---

## 2026-07-20 — Local database setup (post-sprint, resolving open item #28)

**Requested:** User wants to connect the app to a real local database to test end-to-end, then
build out the actual schema, in order to start replacing the inferred stored-procedure/column
names with confirmed ones.

**Done:**
- Wired an optional, gitignored local config override: `Program.cs` now loads
  `appsettings.{Environment}.local.json` if present (`appsettings.*.local.json` was already in
  `.gitignore`), so real connection strings never risk being committed.
- User supplied a connection string for `dbserver-local.fff` / `PMS_NZ_V2` (via login `pms_nz`).
  Confirmed connectivity (read-only check first) — connected fine, but the login lacks
  `CREATE TABLE` rights on that database, so the tenant-registry migration was generated as a
  standalone idempotent SQL script (`db-scripts/01_TenantRegistry_InitialCreate.sql`) instead of
  applied live, for the user to run with an account that has DDL rights.
- User asked about using a different local server for the registry specifically, keeping
  `dbserver-local.fff` for "further operations" (legacy domain data). Confirmed this matches the
  tenant-registry routing model exactly: `ConnectionStrings:TenantRegistry` is the platform's own
  DB; other servers are resolved per-practice via a `Practices` row + a
  `Legacy:DbCredentials:{DbServerHost}` secret. Added
  `Legacy:DbCredentials:dbserver-local.fff` using the already-supplied credential (no need to
  re-share it), and prepared `db-scripts/02_SeedPractice_dbserver-local.sql` to register a test
  practice pointing there once the registry exists.
- User supplied a second connection string (`Server=localhost;Database=ClinicBooking;...`) as the
  candidate registry target. **Before running anything**, checked what was already in that
  database — found 17 existing tables (`Appointments`, `Patients`, `Providers`, `Users`, `Roles`,
  etc.) belonging to a clearly unrelated, already-functioning application. Flagged this to the
  user rather than silently adding a `Practices` table into someone else's database. User
  confirmed: wrong database, create a fresh dedicated one instead.
- Created a new, empty `HekTenantRegistry` database on `localhost` (via `CREATE DATABASE`, same
  trusted local connection). Updated `TenantRegistry` in the local config to point at it. Applied
  the EF Core migration live against it successfully (`dotnet ef database update` — the `Practices`
  table now exists there).

**Key files:** `src/Api/Program.cs` (local-config loading), `src/Api/appsettings.Development.local.json`
(gitignored, holds real connection strings — not shown here), `db-scripts/01_TenantRegistry_InitialCreate.sql`,
`db-scripts/02_SeedPractice_dbserver-local.sql`.

**Continued:** created `AI_USAGE_LOG.md` (this file) per user request, to track AI-driven actions
across the project going forward, separate from `PROJECT_STATUS.md`'s decision/facts log.

- Ran `db-scripts/02_SeedPractice_dbserver-local.sql` against `HekTenantRegistry` — confirmed one
  row inserted (`TEST-PRACTICE-001` → `dbserver-local.fff` / `PMS_NZ_V2`).
- Started the built `Api.dll` locally (Development environment) pointed at the real
  `HekTenantRegistry` database. First attempt (via a background shell) exited immediately with no
  captured error - re-ran in the foreground from the correct working directory
  (`src/Api`, so the content root and `appsettings.Development.local.json` resolve correctly) and
  it started cleanly.
- **Verified against a real database for the first time in this project:**
  `GET /health` → `200 {"status":"ok"}`; `GET /health/ready` → `200 Healthy`, genuinely querying
  `HekTenantRegistry` and succeeding (not a stub/mock). Stopped the process after confirming.

**Key files this step:** `AI_USAGE_LOG.md` (new), `db-scripts/02_SeedPractice_dbserver-local.sql`
(executed), `src/Api/appsettings.Development.local.json` (gitignored, holds the real connection
strings/credentials - not reproduced here).

**Redirected by user:** rather than building the legacy schema directly against `PMS_NZ_V2`, user
asked to first extract and document every required table/stored-procedure/parameter into a file,
so a future dedicated database (not `PMS_NZ_V2`, not `ClinicBooking`) can be built from one
authoritative source. Grepped every `CommandType.StoredProcedure` call, `SqlParameter` name, and
`row["ColumnName"]` read across `src/Infrastructure/Legacy/**/*.cs`, and compiled the complete
inventory into `db-scripts/REQUIRED_SCHEMA_INVENTORY.md` — organized by domain group, covering the
`Hiso` concept-engine foundation, all `HSS`-schema KARO/ERMS/COL procedures, and the lower-priority
dormant DMSDA-related procedures. No database was modified in this step - documentation only.

**Key files this step:** `db-scripts/REQUIRED_SCHEMA_INVENTORY.md` (new).

**Next:** awaiting user direction on where/when to build the actual dedicated database from this
inventory.

---

**Redirected by user again:** "currently use the existing database which i provided you connection
strings later we will use new db" — reversed the prior step's deferral; asked to proceed against
`PMS_NZ_V2` now rather than waiting for a future dedicated database. Re-verified current state
before acting: `PMS_NZ_V2` still empty, `pms_nz` login still lacks DDL rights (unchanged from the
earlier migration failure). Was about to generate the full schema-build script for `PMS_NZ_V2` when
the user's next message (mid-turn) changed the target again — see below.

**Redirected by user a third time (mid-turn):** "can we create in local server db instead of
pms_nz_v2 and redirect to pms_nz_v2 for other purposes for example read/update etc?" Confirmed via
AskUserQuestion that this meant: build the full legacy-shaped schema on a **new local database**
(full DDL rights available there), point the test practice's tenant-registry routing at that new
database, and keep `PMS_NZ_V2` configured separately, untouched, for later/other use. User
confirmed: "Yes, exactly that (Recommended)."

- Created a new, empty `HekLegacyPmsDev` database on `localhost` (`CREATE DATABASE`, same trusted
  local connection pattern as `HekTenantRegistry`).
- Wrote `db-scripts/03_LegacySchema_Build.sql` — a full idempotent T-SQL script building every
  schema (`HSS`, `Hiso`, `Task`, `Profile`, `Appointment`, `Prompt`), ~20 backing tables, and ~50
  stored procedures, with every name/parameter/result-column taken directly from
  `REQUIRED_SCHEMA_INVENTORY.md` (i.e., from what the C# code actually calls) rather than invented
  fresh. Ran it against `HekLegacyPmsDev` — 90 batches executed, zero errors.
- Wrote `db-scripts/04_SeedDummyData.sql` — dummy data for patient `1001` / encounter `5001` /
  practice `TEST-PRACTICE-001` across the domain tables (demographics, clinical notes, conditions,
  medications, reports, documents, observations, template schemas, recall categories, screening
  codes, providers, surgery/session data, plus the `Hiso.ProcedureParams` dictionary rows the
  HISO concept-engine (`HisoConceptExecutor`) needs to resolve stored-procedure parameter lists at
  runtime). Ran it against `HekLegacyPmsDev` — confirmed `Patients` row count: 1.
- Updated `HekTenantRegistry.Practices`: `TEST-PRACTICE-001` now routes to `localhost` /
  `HekLegacyPmsDev` instead of `dbserver-local.fff` / `PMS_NZ_V2` (direct `UPDATE`, verified via
  `SELECT` — one row affected). `PMS_NZ_V2`'s credentials remain in the local config, untouched, for
  whatever future purpose the user has in mind for it.
- Added `"Legacy:DbCredentials:localhost": "Trusted_Connection=True"` to
  `appsettings.Development.local.json` so `ILegacyPracticeConnectionResolver` can build a real
  connection string for the new host. Rebuilt the solution — 0 warnings/errors.
- **Surfaced a real gap while preparing to test, not silently worked around:** the real
  `/karo/authenticate` compat endpoint (`HssAuthenticateTranslator`) does not map any `practiceId`
  into the token it issues, because none of the documented legacy KARO Authenticate payload fields
  (username/password/patientId/encounterId/system/pho) include one. A token minted through the real
  flow today would carry an empty `practiceId` claim and fail tenant-registry resolution. Not fixed
  yet — flagged as a new item, and worked around for this verification step only by manually
  crafting a JWT (HMACSHA256-signed with the same dev signing key the app already trusts) carrying
  `hek:patientId=1001`, `hek:encounterId=5001`, `hek:practiceId=TEST-PRACTICE-001`,
  `hek:originScope=Karo`.
- Started the built `Api.dll` in the background (Development environment, from the correct working
  directory) and called `GET /patients/1001/demographics/karo` with the crafted token. **Response:**
  `200 {"patientId":1001,"practiceId":"TEST-PRACTICE-001","firstName":"Amy","lastName":"Mouse","dateOfBirth":"1985-05-05","dateOfEnrolment":"2015-03-01","endEnrolmentDate":null}`
  — an exact match to the seeded row, proving the full chain works end-to-end for the first time:
  JWT auth → `ResourceScopedControllerBase` → MediatR → repository → `ILegacyPracticeConnectionResolver`
  → tenant-registry lookup → `LegacyDbExecutor` → real stored procedure → real row → real response.
  Stopped the background process after confirming.

**Key files this step:** `db-scripts/03_LegacySchema_Build.sql` (new, executed),
`db-scripts/04_SeedDummyData.sql` (new, executed), `src/Api/appsettings.Development.local.json`
(gitignored, updated with the `localhost` credential entry — not reproduced here).

**Important caveat, carried into `PROJECT_STATUS.md`:** this proves the *code* works correctly
against *a* database with the expected shape — it does not confirm the expected shape matches the
real, live Indici PMS/HSS schema, since `HekLegacyPmsDev`'s schema was itself built from the same
inferred procedure/column names flagged in open item 28, not from a live schema dump. Still an
inference, now just a testable one.

**Next:** update `PROJECT_STATUS.md` open items with the practiceId-mapping gap and the refined
status of item 28; awaiting further user direction on `PMS_NZ_V2`'s intended future use and on
whether to fix the compat-auth practiceId gap now or later.

---

**User confirmed:** will fix the practiceId-mapping gap (open item 32) later. Asked what remained
in "building" - answered directly from `PROJECT_STATUS.md`'s open-items table (code still to write:
billing scope granting, `/auth/token` origin-scope decision, Aspose; decisions still owed: KARO
invoice fields, rate-limit numbers, compat-endpoint routing confirmation, live-schema verification;
post-sprint hardening not started at all). No files changed in this step.

**User turned Docker Desktop on** - attempted the `docker compose up` end-to-end verification that
had been flagged as "not yet done" since the Block 0/1 session.

- Created a local, gitignored `.env` (dev-only `SA_PASSWORD`/`AUTH_JWT_SIGNING_KEY` values, matching
  `.env.example`'s documented keys). Confirmed `.env` is in `.gitignore` before writing it.
- `docker compose build api` - succeeded (multi-stage build: SDK restore/publish → aspnet runtime,
  0 errors).
- `docker compose up -d` - `sqlserver` reaches `healthy` reliably every time. `api` crashes on every
  attempt with `SqlException ... TCP Provider, error 40` during its startup tenant-registry
  migration step, unable to reach `sqlserver` at all.
- **Spent significant effort isolating the cause rather than guessing at a fix, since this smelled
  environment-specific rather than a simple config typo:**
  - Confirmed DNS resolution works (`getent hosts sqlserver` → correct IP) from inside a debug
    container built off the same image.
  - Confirmed the exact same target (`sqlserver:1433`, real `sa` login) is reachable and accepts
    logins from **other** containers on the same bridge network: `sqlcmd` from within the
    `sqlserver` container itself (via its own network alias, not just localhost) succeeded, and
    a plain `nc` connectivity test from an unrelated third-party `busybox` container on the same
    network succeeded instantly.
  - Tried `Encrypt=False` (ruling out a TLS/OpenSSL-version handshake mismatch, a real, documented
    issue with newer Debian-based .NET container images and older SQL Server TLS stacks) - no
    change.
  - Tried `IPAddressPreference=IPv4First` (ruling out a known `Microsoft.Data.SqlClient` dual-stack
    socket resolution issue in containers) - no change.
  - Tried connecting by the container's raw IP instead of the `sqlserver` DNS name - no change.
  - Tried connecting via `host.docker.internal` (bypassing the internal compose bridge network
    entirely, going out through the host's published port instead) - no change, identical error.
  - Attempted a same-image control test: installed `nc` inside the actual `api` container (not a
    substitute image) to directly compare a non-.NET tool's behavior in the exact same container -
    blocked, because that container's own `apt-get` also could not reach the internet, while two
    *other* containers' intra-bridge traffic to the same `sqlserver` target worked fine moments
    earlier in the same session.
- **Conclusion, not further guessed at:** every plausible application-level/connection-string cause
  was tested and ruled out one at a time. The consistent pattern (other containers reach
  `sqlserver:1433` instantly with plain tools; the `api` container's .NET process cannot reach it by
  any addressing scheme, and `ClientConnectionId` is always all-zeros meaning TCP itself never
  establishes) points at something specific to the built `api` image/container or a host-level
  Docker network-isolation feature (Docker Desktop's Enhanced Container Isolation, a corporate
  VPN/EDR agent, or a WSL2 vEthernet quirk) - not an application bug, and not something further code
  changes are likely to fix. Flagged as new open item 33 in `PROJECT_STATUS.md` rather than silently
  marked "done" or worked around with an untested guess.
- Reverted the exploratory `Encrypt=False` edit to `docker-compose.yml` (didn't fix anything, kept
  the file clean). Ran `docker compose down` to tear the stack down cleanly rather than leave broken
  containers running.

**Key files this step:** `.env` (new, gitignored, dev-only Docker values - not reproduced here),
`docker-compose.yml` (touched then reverted, net no change).

**Important note carried into `PROJECT_STATUS.md`:** this is specifically a containerized-network
gap, not a regression in the application - the same code was already proven correct against real
data on the host directly (previous entry, `HekLegacyPmsDev`). `docker compose up` end-to-end
verification remains unachieved and needs Zohaib to investigate on the host side (Docker Desktop
settings, AV/EDR/VPN filtering), since the same target is reachable from sibling containers on the
same host using non-.NET tools.

**Next:** awaiting Zohaib's investigation of the host-level Docker networking issue (item 33) before
`docker compose up` can be verified end-to-end.

---

**User asked which API the billing gap belonged to** - answered precisely rather than repeating the
earlier vague framing: it's ERMS's COL/Pegasus `SaveInvoice` endpoint, not KARO's, and confirmed I
did not have `HSSDA.cs`.

**User pointed to `legacy-reference/DAL/HSS/HSSDA.cs`.** Found and read the real `InsertUpdateService`
method - confirmed the real backing stored procedure (`[OnlineClaim].[uspInsertUpdateService]`) and
its full parameter list, which differs meaningfully from what had been guessed (service-name/code
split into master/sub, `fee` not `AmountInclGST`, several fields - `AccountHolderID`, `encounterId`,
`locationId`, `Description`, `ServiceProviderType` - not present in the earlier inferred contract at
all). Reported this as a real, partial answer - it fixes the DAL/procedure half, but the JSON-to-
parameter mapping logic lives in the *controller*, which wasn't supplied yet.

**User pointed to `legacy-reference/controller/COLController.cs`.** Found `SaveInvoice()` and its
`SaveInvoice` request model - this closed the loop completely: the exact JSON field list, the exact
mapping onto `HSSDA.InsertUpdateService`'s parameters, and several legacy-specific quirks (
`masterServiceName` hardcoded to `"COL"`, `locationId` hardcoded empty, practiceId parsed out of a
delimited `EncounterID` string rather than sent as its own field, always-HTTP-200 responses, legacy
session-token auth). Reported all of this plainly before touching any code, and asked whether to
apply just the confirmed field-list/procedure fix (keeping the platform's own already-decided REST/
auth conventions) or also clone the legacy status-code/auth behavior into the canonical endpoint -
user confirmed "yes, go ahead and rebuild it to match."

**Rebuilt the billing/invoices code against the real source**, closing PROJECT_STATUS.md open item 17
(re-labeled from "KARO's SaveInvoice" to "ERMS/COL's SaveInvoice" - the original attribution was
wrong):

- `src/Contracts/Billing/InvoiceInput.cs` / `Invoice.cs` - added `Description`, `AccountHolderId`,
  `ServiceProviderType` (fields the legacy endpoint genuinely accepted but the earlier inferred
  contract omitted), kept the already-locked public field names (`ServiceCode`, `ServiceName`,
  `AmountInclGst` as `decimal`) unchanged since those are the OpenAPI's authoritative wire shape, not
  something this fix was asked to rename.
- `src/Infrastructure/Legacy/Billing/InvoicesRepository.cs` - `SaveAsync` rewritten to call the real
  `[OnlineClaim].[uspInsertUpdateService]` with the real parameter set, including the two confirmed
  hardcoded legacy constants (`masterServiceName = "COL"`, `locationId = DBNull`) preserved exactly as
  the real code does it, not reinvented. The stored procedure's own `-3` "already exists" return code
  is now recognized as a defense-in-depth duplicate signal.
- **Deliberately did not revert the platform's idempotency design**: checked the Contract Design doc
  (Section 12, Decision 3, FR-IDEM-01) first and confirmed the existing natural-key/Idempotency-Key
  approach was already a documented, intentional replacement for the legacy `-3` magic-number
  convention - not a gap to "fix" by reverting to the old behavior. Left it in place.
- **Deliberately did not clone always-200/raw-error-text/session-token behavior** into the canonical
  endpoint either - that's the new platform's own already-decided design (RFC 7807 errors, real REST
  status codes, `billing:write` scope), a separate, already-settled architectural choice (open item
  27) that this specific fix wasn't asked to revisit.
- `SaveInvoiceCommand.cs`/`InvoicesController.cs` updated to thread `CurrentScope.EncounterId` through
  (needed for the real DAL call's `@pAppointmentId` parameter, which the earlier version never
  supplied at all).
- OpenAPI spec (`Unified-Healthcare-API_openapi.yaml`) bumped to 1.1.2 with the three new
  `InvoiceInput` fields and an `x-changelog` entry explaining why.
- Solution rebuilt clean: 0 warnings, 0 errors.
- **Flagged, not silently left inconsistent**: the dev-only `HekLegacyPmsDev` database's schema script
  (`db-scripts/03_LegacySchema_Build.sql`) still defines the old, now-wrong `[HSS].[uspSaveInvoice]`
  procedure name - not updated in this pass since it wasn't asked for, but noted so it doesn't get
  silently forgotten if billing needs testing against that database later.
- **Still open**: KARO's own SaveInvoice implementation (if it has a separate one) remains unsupplied -
  this closes the ERMS/COL half of item 17 only.

**Key files this step:** `src/Contracts/Billing/InvoiceInput.cs`, `src/Contracts/Billing/Invoice.cs`,
`src/Application/Common/Interfaces/IInvoicesRepository.cs`, `src/Infrastructure/Legacy/Billing/InvoicesRepository.cs`,
`src/Application/Features/Billing/Commands/SaveInvoiceCommand.cs`, `src/Api/Features/Billing/Controllers/InvoicesController.cs`,
`hek_analysis/docs/architecture/Unified-Healthcare-API_openapi.yaml` (all edited, not new files).

**Next:** billing:write scope-granting (open item 29) is still unresolved and is now the only thing
standing between this endpoint being fully correct and it actually being callable by anyone.

---

**User pushed back on open item 29**: "implement as it is as legacy system was working i do not
understand why you are asking for billing rights." Did not implement blindly - checked the actual
basis for the `billing:write` requirement first, since removing a security control deserves
verification, not just compliance. Traced it to SRS §12.2, which cites `erms/SecurityAnalysis.md`
SEC-04 as its evidence. Read SEC-04 directly: it's a wildcard-CORS finding, not a missing-scope
finding - the SRS's own citation doesn't actually support the requirement it was used to justify.
Cross-checked the real legacy source (`COLController.cs`, already on hand from the previous step):
`SaveInvoice` used the exact same session-token check as every other endpoint - no distinct billing
permission ever existed in the legacy system. Reported this finding plainly, then applied the change,
since Zohaib's direction turned out to match what the real code shows, not a request to reintroduce
a genuine vulnerability.

- Removed `[Authorize(Policy = BillingWrite)]` from `src/Api/Features/Billing/Controllers/InvoicesController.cs`
  - it now uses the same `ResourceScoped` policy as every other write endpoint. Cleaned up the two
  now-unused `using` directives in that file.
- Left `AuthorizationPolicyNames.BillingWrite` and its `Program.cs` registration in place, unused,
  rather than deleting them - a reasonable middle ground if a real scope-granting design is wanted
  later.
- OpenAPI spec: removed the `bearerAuth: [billing:write]` override on the invoices endpoint (falls
  back to the global default like other endpoints), bumped to 1.1.3 with an `x-changelog` entry.
- Solution rebuilt clean: 0 warnings, 0 errors.
- `PROJECT_STATUS.md` open item 29 closed with the full reasoning trail (not just "done").

**Key files this step:** `src/Api/Features/Billing/Controllers/InvoicesController.cs`,
`hek_analysis/docs/architecture/Unified-Healthcare-API_openapi.yaml`.

**Net effect:** billing/invoices is now both correctly wired to the real legacy procedure (item 17)
and actually callable by any properly-scoped caller (item 29) - no more known blockers on this
endpoint specifically.

---

**User asked how auth worked in legacy, and about ACC45/Aspose and the practiceId gap** - gave a
concrete, verified answer rather than restating the earlier vague framing. Re-read `KARO_HSS_doc.md`
and `ERMS_doc.md`'s Authenticate sections precisely: confirmed neither legacy system's *request*
payload ever includes practiceId, but both systems' *responses* do (server-resolved, not caller-
supplied) - a real, specific finding, not previously stated this precisely. User then gave an example
pair (practiceId `901`, key `FZZ999-B`) and asked what was needed; explained clearly that this looks
like exactly the missing lookup, but that the key format doesn't match KARO's documented `pho` format
and a full mapping list (not just one example) plus confirmation of the key type would be needed to
build it for real.

**User's direction: "leave it configurable and turn off we will check this later it should work as it
was working in legacy."** Interpreted and implemented as: scaffold the resolution mechanism as an
ADR-008-style config toggle (matching the project's own established pattern for exactly this
situation - new capability, ships disabled), rather than leaving practiceId permanently unresolved or
guessing at real mapping data that wasn't supplied.

- New `src/Application/Common/Options/LegacyPracticeResolutionOptions.cs` - `Auth:LegacyPracticeResolution:Enabled`,
  default `false`.
- `AuthenticateCommandHandler` now checks this toggle: if off (default), behavior is unchanged from
  before (practiceId stays empty on compat logins, same as today). If on, resolves
  `Auth:LegacyPracticeMappings:{username}` via the existing `ISecretProvider` abstraction.
- Registered the new options in `Program.cs` alongside the other `IOptions` registrations.
- **Flagged, not guessed past**: keyed by username rather than `pho`, since username is the one field
  confirmed present on both legacy systems' Authenticate payloads - explicitly noted this is an
  assumption, not a confirmed fact, since the user's own example (`FZZ999-B`) doesn't match KARO's
  documented 4-letter `pho` code format (`NBPH`).
- Fixed the existing `AuthenticateCommandHandlerTests.cs` (constructor signature changed - two more
  required dependencies) and added two new tests proving both toggle states behave correctly.
- Ran the full test suite: all green (Application.UnitTests 7/7, every other project's tests
  unaffected).

**Key files this step:** `src/Application/Common/Options/LegacyPracticeResolutionOptions.cs` (new),
`src/Application/Features/Auth/Commands/AuthenticateCommandHandler.cs`, `src/Api/Program.cs`,
`tests/Application.UnitTests/Auth/AuthenticateCommandHandlerTests.cs`.

**Next:** still need real mapping data (a full pho-or-username → practiceId list, not just one
example) and confirmation of which field the old system actually keyed on, before this can safely be
turned on.

---

**User reconfirmed the ACC45 Aspose decision** (open item 30) explicitly: keep the placeholder, do
not substitute another PDF library, keep the save/DMS sequencing intact, wait for a real license, and
mark the rendering step with a clear TODO so it can be finished later without touching the rest of
the workflow.

- Checked the existing code first (`Acc45Repository.SaveFormAsync`) - the sequencing was already
  correct (render-to-DMS, then persist the definition with the DMS GUID, per HISO-BR-12), so nothing
  needed fixing there.
- Added an explicit `// TODO(Aspose): ... // END TODO(Aspose)` bracket directly around the rendering
  call site (bytes/extension/description passed into `DmsDocumentService.AddDocumentAsync`), isolating
  exactly what needs to change later without restructuring anything else.
- Solution rebuilt clean: 0 warnings, 0 errors.

**Key files this step:** `src/Infrastructure/Legacy/Acc45/Acc45Repository.cs`.

---

**User asked to tackle open item 18 (rate-limit thresholds) next.** Investigated before proposing
anything, rather than picking a number: confirmed no source document in the project has quantitative
traffic figures - SRS explicitly flags this as unconfirmable, only a qualitative "10,000 concurrent
users" target exists, with no request-rate breakdown. Rate limiting is net-new; none of the three
legacy systems had any.

Since fabricating a "final" number with zero evidence would violate the standing "never invent
unconfirmed business values, ask if ambiguous" rule, asked Zohaib directly with three concrete
options: formalize the current generous defaults as intentional (matches an already-approved rollout
decision in the Contract Design doc), provide real numbers, or tighten to a conservative default now
without real data. **Zohaib chose to formalize the existing defaults.**

- Updated `src/Api/Configuration/RateLimitOptions.cs`'s doc comment to state plainly that
  10,000 requests/60s per IP is the deliberate Day-1 value (per the Contract Design doc's own
  "generous now, tighten after monitoring" decision), not an unconfirmed placeholder - no functional
  change, since the numbers themselves were already reasonable.
- Solution rebuilt clean: 0 warnings, 0 errors.
- `PROJECT_STATUS.md` open item 18 closed with the full reasoning trail, explicitly noting there's
  still no real number to tighten to since no production monitoring data exists yet - closed as
  "confirmed intentional," not "final values decided."

**Key files this step:** `src/Api/Configuration/RateLimitOptions.cs`.

---

**User asked to tackle the tenant registry schema reconciliation (open items 24/31) next.**
Investigated before touching code, as usual for this project. Re-checked the EAD and ADR-001's full
text directly - reconfirmed no source document gives a literal registry column list, only ADR-001's
prose description. Since there's nothing to confirm against, item 24 closes by explicit new-design
sign-off (Block 1's existing schema stands as the deliberate design), per that item's own stated
resolution path from when it was first opened.

**While reading the actual repository code for item 31, found this was a real bug, not just a
documentation gap.** `PracticeAdminRepository`'s mapping set `DbName` to the *same* value as
`DbServerHost` (both sourced from a single collapsed `databaseServerId` field). Since the connection
resolver builds connection strings as `Server={DbServerHost};Database={DbName};...`, any real practice
registered through `/admin/practices` would get a broken connection string - a database name is never
the same string as its own server's hostname. Undiscovered until now because that endpoint had never
been exercised end-to-end.

Fixed it properly rather than just documenting the mismatch:

- `src/Contracts/Admin/PracticeInput.cs`/`Practice.cs` - replaced the single `databaseServerId` field
  with three explicit ones: `DbServerHost`, `DbName`, `SourceSystem` (typed as the existing
  `OriginScope` enum - reused, not reinvented - instead of the old free-text `"Unknown"` default).
- `src/Infrastructure/Legacy/Admin/PracticeAdminRepository.cs` - `RegisterAsync`/`UpdateAsync`/`GetAsync`
  now map all three fields 1:1, no more duplication.
- `src/Application/Features/Admin/Validators/PracticeAdminValidators.cs` - validates the new fields.
- OpenAPI spec: `PracticeInput` schema updated to match (`dbServerHost`/`dbName`/`sourceSystem`,
  enum-constrained), bumped to 1.1.4 with an `x-changelog` entry.
- Solution rebuilt clean (0 warnings/errors); full test suite green (11/11 across all test projects,
  nothing broke).

**Key files this step:** `src/Contracts/Admin/PracticeInput.cs`, `src/Contracts/Admin/Practice.cs`,
`src/Infrastructure/Legacy/Admin/PracticeAdminRepository.cs`,
`src/Application/Features/Admin/Validators/PracticeAdminValidators.cs`,
`hek_analysis/docs/architecture/Unified-Healthcare-API_openapi.yaml`.

**Net effect:** both items 24 and 31 closed - 24 by sign-off (no real schema existed to check against),
31 by fixing an actual defect rather than just relabeling it as reconciled.

---

**User asked whether project skills, specifically `docs-driven-workflow-skill`, were actually being
used during this session.** Answered honestly: not by formally invoking the `Skill` tool - the
practices matched the skill's letter (read status doc first, ask before inventing business rules,
same-session doc updates, etc.) but from habit/carried-over discipline, not by explicit invocation
each time. User asked to invoke it explicitly going forward - confirmed, and saved as a standing
preference to project memory (`feedback_invoke_skills_explicitly.md`).

**User then confirmed a real fact**: "karo also use same method as erms use" - KARO's SaveInvoice
isn't a separate, still-unconfirmed implementation; it shares ERMS/COL's method (the one already
fixed against real source earlier this session). Attempted to honor the just-made commitment by
calling `Skill` explicitly for this doc-update task - tried both `docs-driven-workflow-skill` and
`docs-driven-workflow` (the name in the file's own frontmatter); both returned "Unknown skill." The
file exists in this repo's `.claude/skills/` but isn't registered as invocable in this environment.
Reported this plainly rather than silently giving up or fabricating a successful call, then followed
the skill's written workflow manually (content already read earlier this session) to apply the actual
update:

- `PROJECT_STATUS.md` open item 17 - removed the "KARO's own SaveInvoice implementation was never
  supplied" caveat, since it no longer applies; item now closes with no remaining thread.
- No code change needed - the existing fix already covers KARO's calling path too.

**Key files this step:** `hek_analysis/PROJECT_STATUS.md` (docs only, no source code touched).

---

**User asked to test against the real `PMS_NZ_V2` database for HISO/KARO/ERMS demographics** ("3
apis"), starting with a real patient ID (2459731) once asked for.

- Checked `PMS_NZ_V2` directly before proceeding - `INFORMATION_SCHEMA.TABLES` still showed zero
  tables under the `pms_nz` login. Flagged this rather than assuming it would just work. User
  clarified the database has real data - the login is scoped to `EXECUTE` on procedures only, no
  `SELECT`/schema-visibility rights (matching exactly how the legacy code itself always calls the
  database). Confirmed this by testing via stored-procedure calls instead of table queries, which
  worked.
- Repointed `TEST-PRACTICE-001`'s registry routing to `PMS_NZ_V2`. Minted three separate JWTs (one
  per originScope - a token only ever grants access to its own matching endpoint) and tested all
  three demographics endpoints against real data.
- **HISO and ERMS**: both got `SqlException` "could not find stored procedure" - genuinely wrong
  procedure names, now confirmed rather than assumed.
- **KARO**: a *different* kind of error - a C#-level `ArgumentException` about a missing column,
  which can only happen after SQL Server already returned a real row. Explained this distinction
  directly to the user when asked "how did you identify this would provide data" - it wasn't a
  guess, it was reading which layer the exception came from (DB-level "doesn't exist" vs. app-level
  "wrong column name").
- Added a temporary diagnostic to `DemographicsRepository.GetKaroAsync` dumping every real column
  name/value instead of guessing - found the procedure returns two parallel sets of similarly-named
  fields: clean ones (`Given`/`Family`/`BirthDate`) and a second set (`FirstName`/`Surname`/`DateOfBirth`)
  holding unrelated composite internal-reference strings (`554:1000111/1000310/1|&|LnB`), not usable
  values.
- **Fixed for real**: switched to the clean field names. Also caught and fixed a related bug found in
  the same real data - `DateOfEnrolment`/`EndEnrolmentDate` come back as empty string, not `DBNull`,
  when unset; the original `is DBNull` check would have thrown on real data. Removed the temporary
  diagnostic once confirmed.
- **Verified**: `GET /patients/2459731/demographics/karo` now returns real `200` data from
  `PMS_NZ_V2` - the first endpoint in this project proven against actual production data, not a dev
  scaffold.
- **Separate bug found and fixed along the way**: user tried Swagger UI and got `401` with no way to
  attach a token - `AddSwaggerGen()` had no security scheme configured, so the Authorize button
  didn't exist at all. Added a proper JWT Bearer definition so Swagger is now actually usable for
  manual testing.
- Full test suite re-run: still green (11/11).

**Key files this step:** `src/Infrastructure/Legacy/Demographics/DemographicsRepository.cs`,
`src/Api/Program.cs` (Swagger security definition).

**Next:** HISO's and ERMS's real procedure names are still unknown - need either live-schema access
or the user's own knowledge of the real names to fix those two the same way KARO was fixed.

---

**User pushed back hard on the architecture**: "i think this will change the whole api my goal was
to never change any method just to make a unified api from all these 3 apis rest of things will call
as it is." Did not proceed defensively or start changing code immediately - explained the real
distinction (database calls/business logic preserved; C# orchestration code necessarily rewritten,
since the legacy stack is .NET Framework/WCF and cannot run inside this .NET 8 codebase) and asked
directly via AskUserQuestion whether the current approach or a true pass-through gateway was actually
wanted. **User confirmed: current approach is correct.** Logged the clarification in
`PROJECT_STATUS.md` so it doesn't resurface as unresolved confusion later.

**User then asked a much more specific, concrete question**: "how can i verify getdata hiso flow in
new code as getdata name is configured at user end why name changed?" This was a real, valid,
specific gap - not resolved by the architecture clarification alone. Investigated properly before
answering: `getData` was a real SOAP operation (`HISO_doc.md` Section 3), and the current codebase's
REST demographics endpoint is a genuinely different name/path/protocol calling into the same
underlying logic - true for auth compat endpoints (preserved exactly) but NOT true for data
operations (unified into new REST paths instead, an earlier Contract Design decision). Asked directly
whether to build a real getData-compatible endpoint now or hold off - **user said build it now.**

- Read the real request/response shape directly from `HISO_doc.md` rather than guessing:
  `{sessionKey, dataContainer}` in, `{GetDataResponseReturn:{dataContainer}}` out, session-GUID-only
  auth.
- While designing this, checked the existing `HisoSessionResolver`/`ResolveHisoSessionQuery`
  (built in Block 1) for reuse - grepped for call sites and found **none**: this session-resolution
  mechanism had never actually been wired to any HTTP endpoint anywhere in the project. A real,
  previously-unflagged gap, not just an "inference."
- Built `POST /hiso/getData` matching the real shape exactly, with no bearer-token requirement
  (session resolved directly from the request body, unlike every other controller in this project).
  Concept dispatch (`dataContainer` keys → `Hiso.uspGetPatient_{ConceptName}`) reuses the
  already-confirmed procedure-naming convention, since the real concept-mapping table was never
  supplied - flagged clearly as the closest faithful reproduction, not a rediscovery.
- Verified end-to-end without touching the user's active `PMS_NZ_V2` test setup: created a separate
  test practice (`TEST-PRACTICE-HISO` → `HekLegacyPmsDev`) and a real session row, added the missing
  `Hiso:ConnectionStrings:localhost:5299` config key (discovered this is a *separate* routing
  mechanism from the tenant registry by reading `HisoSessionRepository` directly, not assuming).
  `POST /hiso/getData` returned real data: `{"getDataResponseReturn":{"dataContainer":{"Demographics":
  {"FirstName":"Amy","LastName":"Mouse","DateOfBirth":"1985-05-05T00:00:00"}}}}`.
- Full test suite re-run: still green (11/11). App left running for the user's own testing.

**Key files this step:** `src/Api/Features/Hiso/Controllers/HisoCompatController.cs` (new),
`src/Adapters.Hiso/GetData/GetDataRequest.cs` (new), `src/Adapters.Hiso/GetData/GetDataResponse.cs`
(new), `src/Application/Features/Hiso/Queries/GetDataQuery.cs` (new),
`src/Api/appsettings.Development.local.json` (added `Hiso:ConnectionStrings:localhost:5299`).

**Next:** KARO's and ERMS's data-retrieval operations have the same wire-compatibility gap `getData`
just closed for HISO - need their real operation names/shapes to build the same treatment.

---

**User pushed further**: "i think this will change the whole api my goal was to never change any
method just to make a unified api from all these 3 apis rest of things will call as it is." Did not
proceed defensively - explained the real distinction (database calls preserved, C# orchestration code
necessarily rewritten since legacy is .NET Framework/WCF and can't run in .NET 8) and asked directly
via AskUserQuestion whether the current approach or a true pass-through gateway was actually wanted.
**User confirmed: current approach is correct.** Logged the clarification so it doesn't resurface as
confusion later - nothing was changed.

**User then asked a much more specific, concrete question**: "how can i verify getdata hiso flow in
new code as getdata name is configured at user end why name changed?" Investigated properly: `getData`
was a real SOAP operation, and the current REST demographics endpoint is a genuinely different name/
path/protocol calling into the same underlying logic. Asked whether to build a real getData-compatible
endpoint now - **user said yes.** Built `POST /hiso/getData` matching the real documented shape,
verified end-to-end.

**User then said "getdata is also not complete"** - correct, important feedback. Enumerated exactly
what was missing against the real business-rule inventory (real concept dictionary, parallel
execution, group cloning, qualifier matching, AWS flow, second-node routing) and asked which pieces to
prioritize, flagging that AWS/second-node needed either real source or a design decision. User then
asked "how can we test it from swagger" (answered, fixed the missing Swagger security scheme along the
way), then pointed to a **massive discovery**: `legacy-reference/Hiso` now contains the ENTIRE real
legacy HISO Visual Studio project - the actual `FormSessionService.svc.cs`, the real `ConceptMapper`,
every builder class, `Task.cs`, `DocumentHandler.cs`, `Mapper.cs`, and the real `Web.config`. Confirmed
`AWSDoc.IndiciDMS` is a compiled DLL, not source - user will map it in later.

- Ran three parallel research agents reading the entire real source in full (all 6 operations' exact
  logic, every appSettings key and its real configured value, every stored procedure name/param list,
  the real concept-mapping algorithm including two genuine legacy bugs found - a dead duplicate PNG
  check, and `saveContainer`'s hardcoded-true response regardless of actual save success), then a
  fourth design pass, before writing any code. Entered plan mode for this given the scope.
- **Surfaced one direct conflict rather than resolving it silently**: real source confirms
  `processAction`'s `addInvoice`/`launchForm` are genuinely empty stubs, but an earlier project
  decision had deliberately implemented them for real instead - asked which should win. **User: "i
  need the legacy api in unified api simple decide on that base"** - legacy fidelity is now the
  standing tiebreaker for every fork in this rebuild.
- Rewrote the plan file around the real source (superseding the documentation-based version) and got
  explicit approval before implementing.
- **Rebuilt `getData` as a genuine port**: real `[Hiso].[UspGetHisoConcepts]`-backed concept
  dictionary with the real 10-minute cache (replacing the earlier name-guessing dispatch entirely),
  real qualifier-ID/conceptName/conceptID priority matching, real group-concept XML cloning, real
  `IsDynamic`/`operationMode` gating (correctly reproducing legacy's own empty-stub paths, not
  building new functionality), and a real MIME/PDF conversion hook (Aspose-blocked for the actual
  byte conversion, same as the already-known ACC45 gap). Built `IAwsDocumentService` and a
  second-database-node connection hook as explicitly deferred, unimplemented placeholders for what
  the user will supply later.
- **Verified against real data, not a stub**: added `db-scripts/05_HisoConceptDictionary.sql` (the
  real concept dictionary table/procedure, genuinely new schema since it was never part of the earlier
  inferred build), seeded 3 real rows mapped to the already-working `Hiso.uspGetPatient_Demographics`,
  and confirmed a real multi-field XML request through `POST /hiso/getData` resolved and filled all
  three fields correctly through the actual ported engine.
- Full test suite re-run after every change: stayed green (11/11) throughout.

**Key files this step:** `src/Application/Common/Models/HisoConceptDetail.cs` (new),
`src/Application/Common/Models/ProcedureResult.cs` (new), `src/Application/Common/Models/HisoRequest.cs`
(extended to match real legacy shape), `src/Application/Common/Interfaces/IHisoConceptDictionary.cs`
(new), `IHisoRequestEngine.cs` (new), `IHisoMimeConverter.cs` (new), `IAwsDocumentService.cs` (new),
`ILegacyPracticeConnectionResolver.cs` (extended), `src/Application/Common/Options/HisoConceptMappingOptions.cs`
(new), `HisoGetDataOptions.cs` (new), `src/Infrastructure/Legacy/Hiso/HisoConceptDictionary.cs` (new),
`HisoRequestEngine.cs` (new), `AsposeUnavailableMimeConverter.cs` (new), `NotConfiguredAwsDocumentService.cs`
(new), `src/Infrastructure/Routing/LegacyPracticeConnectionResolver.cs` (extended),
`src/Adapters.Hiso/GetData/GetDataRequest.cs`/`GetDataResponse.cs` (rewritten), `src/Application/Features/Hiso/Queries/GetDataQuery.cs`
(rewritten), `src/Api/Features/Hiso/Controllers/HisoCompatController.cs` (updated),
`db-scripts/05_HisoConceptDictionary.sql` (new), `src/Api/Program.cs` (new option registrations).

**Next:** `getVersion`, `getDeliveryOptions`, `saveContainer`, `getFormView`, `processAction` all need
the same real-source-grounded rebuild - 8 more builder classes, `Task.cs`, `DocumentHandler.cs`, and
the remaining `Mapper.cs` helpers, all already read in full and detailed in the approved plan file
(`swift-painting-blossom.md`), not yet ported into the codebase. This is a large remaining body of
work, tracked task-by-task.

---

### 2026-07-21 — HISO wire-compat rebuild completed + real-DB verification (all 6 operations)

**Asked:** Continue the pending HISO rebuild work (`"continue"`, `"no need verification right now
just continue pending work"`). Later in the same session, Zohaib supplied real legacy connection
strings for `PMS_NZ_V2`/`Indici_Master`/`DMS_PMS` and asked to connect to the real database directly
rather than fabricate schema, "to avoid issues and reduce token usage."

**Did:**
- Ported the remaining 8 real builder classes: `Acc45DefinitionRepository`, `Acc45DetailRepository`
  (saveContainer's real DMS/ACC45 definition/detail/diagnosis/referral save pipeline), and
  `HisoProcessActionSaveRepository` (all 5 real Patient/PatientConsult/PatientEmployerOrganisation/
  PatientProblem/RegisteredPractitioner builders for `processAction`'s `"save"` branch). All 6 real
  HISO operations are now implemented against ported legacy logic - no `NotImplementedException`
  remains anywhere in the compat surface.
- Ran the full test suite after each addition: stayed green (11/11) throughout.
- Connected directly to the real `dbserver-local`/`PMS_NZ_V2` legacy database using credentials
  Zohaib supplied. Confirmed the real `[Hiso].[UspGetHisoConcepts]` and `[Appointment].
  [usptblHealthLinkSession_GetByGUID]` procedures exist and execute correctly - this is genuinely
  the populated legacy database, not a placeholder.
- **Found and fixed a real bug this real-DB access exposed**: `HisoSessionRepository` did a raw
  `SELECT` referencing `CreatedAtUtc`/`EDIAccount` columns that don't exist on the real
  `tblHealthLinkSession` table (confirmed via `SELECT *`) - it only ever "worked" against this
  project's own earlier fabricated test schema. Rewrote it to call the real session-lookup
  stored procedure instead, plus a minimal separate `InsertedAt` read for the new 12-hour expiry
  rule (a project addition, not part of legacy).
- Re-pointed `TEST-PRACTICE-HISO`'s tenant-registry route from the fabricated `HekLegacyPmsDev` to
  the real `PMS_NZ_V2`, and registered the real numeric practice ID (`933`, discovered from a real
  session row) as its own tenant-registry entry - required because `session.PracticeId` downstream
  is the real legacy numeric ID, not the tenant registry's friendly key; this exposed a structural
  mismatch between those two identifiers that predates this session, flagged in PROJECT_STATUS.md.
- Verified `getVersion` and `getDeliveryOptions` end-to-end against the real database with a
  genuinely fresh session row, including a real `EDIAccount` value flowing through the real
  `PracticeEDI=="1"` conditional branch.
- Advanced `getData` verification to the point of calling the real `Hiso.uspGetPatient` procedure
  with the real concept names (`Patient_FirstName` etc., corrected from earlier placeholder names
  after reading the actual `UspGetHisoConcepts` output) - confirmed the concept dictionary and
  field-resolution engine both function against real data.
- **Hit a hard blocker, not resolved this session**: `[Hiso].[USPGetProcedureParamList]` (the real
  dynamic parameter-discovery procedure `HisoConceptExecutor` depends on for every concept-driven
  call) returns 0 rows for the `pms_nz` login, root-caused to the same restricted metadata-visibility
  pattern seen across `sys.tables`/`sys.columns`/`sys.procedures` (`pms_nz` lacks `VIEW DEFINITION`).
  Confirmed via direct procedure execution (bypassing metadata views entirely) that the underlying
  objects genuinely exist and work - this is a permissions gap, not a missing-schema gap. Zohaib is
  running `GRANT VIEW DEFINITION TO pms_nz;` via an admin login; session paused pending that.
- Still needed once unblocked: real `UDT_tbl*` TVP column lists for the 5 save builders, from
  Zohaib's Web.config `appSettings` or the same permission grant.

**Key files this step:** `src/Application/Common/Interfaces/IHisoProcessActionSaveRepository.cs`
(new), `src/Infrastructure/Legacy/Hiso/HisoProcessActionSaveRepository.cs` (new),
`src/Application/Common/Interfaces/ILegacyPracticeConnectionResolver.cs` (extended -
`ResolveIndiciMasterAsync`), `src/Infrastructure/Routing/LegacyPracticeConnectionResolver.cs`
(extended), `src/Adapters.Hiso/ProcessAction/ProcessActionRequest.cs` (extended -
`ActionContainerXml`), `src/Application/Features/Hiso/Commands/ProcessActionCommand.cs` (rewired
`"save"` branch), `src/Api/Features/Hiso/Controllers/HisoCompatController.cs` (updated),
`src/Infrastructure/Persistence/Hiso/HisoSessionRepository.cs` (rewritten to call the real proc -
bug fix), `src/Api/appsettings.Development.local.json` (real connection strings/credentials added,
gitignored), tenant registry DB (`TEST-PRACTICE-HISO` route updated, `933` practice added).

**Next:** resume real end-to-end verification of `getData`/`saveContainer`/`processAction` once
`VIEW DEFINITION` is granted to `pms_nz`; obtain the real `UDT_tbl*` column lists; update
PROJECT_STATUS.md's open-items section once verification completes.

---

### 2026-07-21 (same day, continued) — VIEW DEFINITION granted, real end-to-end verification, 2 more real bugs found and fixed

**Did:** Zohaib granted `VIEW DEFINITION TO pms_nz` and pasted the real legacy `Web.config`
(confirming `PracticeEDI`/`UserID`/`Password`/`URL`/DMS type IDs/task IDs and all 7 real `UDT_tbl*`
column lists). This unblocked genuine end-to-end verification:

- **`getVersion`/`getDeliveryOptions`/`getData` verified against real production data** - `getData`
  resolved `Patient_FirstName`/`Patient_Surname` to a real patient's actual name via the real
  `Hiso.uspGetPatient` procedure and the real dynamic parameter-discovery engine
  (`[Hiso].[USPGetProcedureParamList]`).
- **Found and fixed `Acc45DetailRepository.BuildReferralTable`**: declared zero columns when no
  `accident.referral` group was present, which SQL Server rejects for any TVP ("Structured types
  must have at least one field") - fixed to always seed a base column.
- **Found and partially fixed a real design gap in `Acc45DetailRepository`**: `BuildDetailTable`
  built its TVP columns dynamically from whatever fields were present in the request, but the real
  `UDT_tblACC45Detail` type has a fixed 54-column shape - SQL Server rejected the mismatch outright.
  Fixed by seeding the full real column list (now known from the Web.config), matching the pattern
  `Acc45DefinitionRepository` already used. Verified `saveContainer`'s definition-save and
  detail-save both now pass through to the real procedure successfully.
  `BuildDiagnosisTable`/`BuildReferralTable` have the identical latent bug (confirmed: diagnosis
  needs 15 real columns via the SQL error) but their `UDT_tblACC45Diagnosis`/`UDT_tblACC45Referral`
  column lists weren't in the Web.config section supplied - Zohaib chose to stop verification here
  for this session rather than keep digging for two more config values.
- Registered the real numeric practice ID (`933`) in the tenant registry, since `session.PracticeId`
  downstream is the real legacy numeric ID, not the tenant registry's friendly key - a structural
  mismatch flagged in PROJECT_STATUS.md, not fixed at the architecture level.
- Full test suite re-run: still green (11/11).

**Key files this step:** `src/Infrastructure/Legacy/Hiso/Acc45DetailRepository.cs` (BuildDetailTable
rewritten to use real `UDT_tblACC45Detail` columns; BuildReferralTable seeded with a base column),
`src/Api/appsettings.Development.local.json` (all real Web.config values added, gitignored), tenant
registry DB (`933` practice registered).

**Not verified this session:** `saveContainer`'s diagnosis/referral sub-tables (missing 2 UDT column
lists), `processAction`'s `"addTask"`/`"save"` branches, `getFormView` (all built and unit-tested,
not yet curl-tested against real data), `Indici_Master` connectivity (unreachable from this session's
network - graceful no-op by design until reachable).

---

### 2026-07-21 (same day, continued) — KARO/HSS wire-compat rebuild started: Ping + Authenticate

**Asked:** Zohaib asked to check current API status, then pivoted to wanting the same wire-compat
treatment applied to KARO/HSS (mirroring HISO), one operation at a time starting with Ping and
Authenticate. Explicitly required: KARO/HSS stays fully separate from HISO (no shared session/auth),
and "do not change any endpoint and any result which was already implemented in legacy - I want the
legacy KARO API in this new API. Implement what is left."

**Did:**
- Explored the codebase (2 parallel Explore agents) and found KARO/ERMS already had partial wire-compat
  (Authenticate only, routed through the generic cross-system `AuthenticateCommand` pipeline rather than
  real legacy logic) plus REST-only "improved" demographics endpoints - same pattern pre-compat HISO had.
- Zohaib then supplied the complete real KARO/HSS legacy source
  (`legacy-reference/hsswebapi/DevLocal/`) - full VS solution including the real controller
  (`APIController.cs`), DAL (`HSSDA.cs`), and encryption layer (`EncryptionManager.cs`). Read the real
  `Ping`/`Authenticate` implementation in full before planning.
- Entered plan mode, wrote a scoped plan for this first slice, got explicit approval (with the two hard
  requirements above folded in after an initial `ExitPlanMode` rejection).
- Built: `IKaroEncryptionService`/`KaroEncryptionService` (Rijndael/AES-256 port, exact hardcoded key,
  exact custom Base64 substitution - portable to .NET 8's `Aes` class with no external dependency),
  `IKaroPracticeConnectionResolver`/`KaroPracticeConnectionResolver` (new routing model - KARO resolves
  connections by `"ConnIndiciDB" + practiceSuffix` parsed from the caller's own `encounterId`, unlike
  HISO's tenant-registry model), `IKaroAuthRepository`/`KaroAuthRepository` (real
  `[HSS].[uspInsertAndValidateToken]` call), `KaroAuthenticateQuery`/Handler (the real encounterId-split/
  decrypt/DB-call orchestration, including legacy's exact message-selection quirk where the DB's own
  `StatusMessage` is discarded in favor of a fixed `"Authentication failed!"` whenever it's non-blank).
  Rewrote `KaroCompatController` to use this real logic for `Authenticate` and added `GET /karo/ping`.
  Removed `HssAuthenticateTranslator.cs` (dead code once the real logic replaced the generic pipeline).
- Added a new unit test (`KaroEncryptionServiceTests`, 3 cases) proving the encryption round-trip.
- **Verified end-to-end against the real production database**: `[HSS].[uspInsertAndValidateToken]`
  confirmed to genuinely exist in `PMS_NZ_V2` (same server as HISO's), and a live test call proved the
  full real pipeline - suffix parsing, connection routing, real proc call, and the exact legacy
  message-selection quirk (DB returned real `"Invalid credentials!"`, app correctly returned
  `"Authentication failed!"` instead, matching legacy exactly).
- Full test suite: 14/14 (11 pre-existing + 3 new).

**Key files this step:** `src/Application/Common/Interfaces/IKaroEncryptionService.cs` (new),
`IKaroPracticeConnectionResolver.cs` (new), `IKaroAuthRepository.cs` (new),
`src/Infrastructure/Legacy/Karo/KaroEncryptionService.cs` (new), `KaroPracticeConnectionResolver.cs`
(new), `KaroAuthRepository.cs` (new), `src/Application/Features/Karo/Queries/KaroAuthenticateQuery.cs`
(new), `src/Api/Features/Auth/Controllers/KaroCompatController.cs` (rewritten),
`src/Adapters.Karo/Auth/HssAuthenticateResponse.cs` (rewritten to real shape),
`src/Adapters.Karo/Auth/HssAuthenticateTranslator.cs` (deleted, dead code),
`tests/Infrastructure.UnitTests/Legacy/KaroEncryptionServiceTests.cs` (new),
`src/Api/appsettings.Development.local.json` (real KARO connection strings added, gitignored).

**Next:** remaining ~13 documented KARO `APIController.cs` operations, one at a time (Zohaib's stated
preference), then ERMS once KARO is done.

---

## 2026-07-21 - ERMS GetPatientData (single-op slice)

**Built:** `GET /erms/GetPatientData` - exact port of legacy `APIController.cs:856`, plus the shared
ERMS Get* building blocks (ErmsTokenValidator, ErmsDataTableMapper = ERMSDataTableToListHiso port with
all quirks, 33 HISO wrapper DTOs + PatientData model, PrepareXml/SetToXml envelope helpers).
`IErmsAuthRepository` gained the legacy `@pToken` parameter.

**Verified:** build clean, 20/20 tests (6 new mapper tests). Live against real `PMS_NZ_V2`
(`ConnIndiciDB_901_FZZ999-B`): malformed token -> real proc SQL error in legacy `<Error>` envelope
(HTTP 200); well-formed invalid GUID token -> exact legacy `<Error><Message>Invalid token
value!</Message></Error>` (HTTP 200). Success round-trip blocked on valid ERMS credentials
(ermsdev rejected in PMS_NZ_V2; default 43.255.162.58 target unreachable) - needs Zohaib.

**Key files:** `src/Application/Common/Interfaces/IErmsTokenValidator.cs` /
`IErmsDemographicsRepository.cs` (new), `src/Infrastructure/Legacy/Erms/ErmsTokenValidator.cs` /
`ErmsDemographicsRepository.cs` (new), `src/Adapters.Erms/Hiso/*` (new),
`src/Application/Features/Erms/Queries/ErmsGetPatientDataQuery.cs` (new),
`ErmsCompatController.cs` (GetPatientData action + envelope helpers),
`tests/Adapters.UnitTests/Erms/ErmsDataTableMapperTests.cs` (new).

**Next:** after Zohaib verifies GetPatientData with real credentials - remaining 18 ERMS Get* ops +
SaveDocument (near-mechanical clones), then Claim Online.

---

## 2026-07-21 - ERMS next-9 Get* slice

**Built:** GetPatientMeasurement, GetSmokingStatus, GetCurrentUser, GetNextOfKin,
GetRegisteredPractitioners, GetAccidents, GetClassifications, GetConsultNotes, GetMedicalAllergies -
exact legacy ports (all quirks: no-null-check NREs, case-sensitive order stamp, ConsultNotes 24-month
default + commented-out min/max stamping, Accidents HTML strip, uspGetAllergies proc name).

**New files:** Adapters.Erms/Hiso/ErmsReadModels.cs (+ ErmsHisoWrappers.cs extension),
IErmsDataRepository + Infrastructure/Legacy/Erms/ErmsDataRepository.cs,
Application/Features/Erms/Queries/ErmsReadQueries.cs (shared ErmsReadPipeline),
9 actions + Render/RenderSingle/StampDated helpers on ErmsCompatController.
Config: Erms:DateFormat = yyyy-MM-dd (local settings, from real Web.config).

**Verified:** build clean, 20/20 tests; all 9 live against real PMS_NZ_V2 - exact legacy
"Invalid token value!" HTTP-200 envelope for invalid tokens. Valid-token data pass = Zohaib via Swagger.

**Next:** GetPrescribed/RegularMedications, 4 report lists + 4 detail ops (needs actualPracticeID
parser extension), SaveDocument.

---

## 2026-07-21 - ERMS final-10 Get* slice (module reads complete)

**Built:** GetPrescribedMedications, GetRegularMedications, GetLaboratoryReportList/Details,
GetRadiologyReportList/Details, GetDischargeSummaryReportList/Details, GetScannedList/Details.
Quirks: uspGetMedications @pIsLongTerm/@pShowStop; LaboratoryReport.Order serializes as element
(legacy missing XmlAttribute); ConvertString2RTF exact port (ErmsRtfConverter) for lab/rad details;
uspGetOtherDocs/uspGetDocResults non-AWS path only (AWSDoc DLL deferral, same as KARO/HISO);
actualPracticeID Convert.ToInt32 before token validation via new pipeline preValidate hook +
IErmsRequestParser RawSecondSegment.

**Verified:** build clean, 20/20 tests; all 10 live vs real PMS_NZ_V2 - legacy invalid-token envelope
HTTP 200; non-numeric segment FormatException envelope verified (1_abc). Real-data pass = Zohaib/Swagger.

**Key files:** ErmsReportModels.cs, ErmsRtfConverter.cs (new); IErmsDataRepository/ErmsDataRepository,
ErmsReadQueries.cs, ErmsRequestParser + IErmsRequestParser, ErmsCompatController (extended).

**Next:** SaveDocument (last ERMS op), then Claim Online (6 ops + SaveInvoice).

---

## 2026-07-21 - ERMS SaveDocument (module complete)

**Built:** POST /erms/SaveDocument - exact port of APIController.cs:1535. ReferralDocument XML model
(real PatiendID typo kept), UpdateExistingDocument (errors swallowed) -> SaveToDMS (uspDocumentSave on
DMS connection, quirky DocumentTypeID lookup, @pDescription=referralId) -> InsertDocument
(@pDataSourceId "23", @pEnounterId misspelling). Error contract: HTTP 400 body "BadRequest"; success:
scrubbed ReferralDocument echo at 200.

**New:** ErmsReferralDocument.cs, IErmsDmsConnectionResolver/ErmsDmsConnectionResolver,
IErmsWriteRepository/ErmsWriteRepository, ErmsSaveDocumentCommand.cs, controller action.
Config: Erms:DMSDocTypes, Erms:DbCredentials:ConnDMSDB_901_FZZ999-B (local settings).

**Verified:** build clean, 20/20 tests; live - invalid token and malformed XML both return legacy-exact
"BadRequest" HTTP 400. Write round-trip = Zohaib via Swagger (real uspDocumentSave has a known
pre-existing ROLLBACK issue in this environment, per KARO session).

**ERMS COMPLETE (22/22 real ops). Next: Claim Online (5 PHCO reads + SaveInvoice).**

---

## 2026-07-21 - Claim Online (COL) module built - all 7 real operations

**Built (from real COLController.cs + DAL/Pegasus/PHCO.cs, now confirmed source):** POST
/erms/col/authenticate (real JSON Credential - prior inferred ColCredential shape confirmed exact;
UserAuthenticate reply with lowercase "error", always 200), GetCurrentPatientData
(OnlineClaim.uspGetPatientData), GetSessionData (**legacy bug preserved: empty proc name - always
fails, SQL error text is the body**), GetProviderData (uspGetProvider), GetSurgeryData
(uspGetSurgeryData), GetDiagnosisData (uspGetConditions, pmsOrder passed raw - no ToUpper),
SaveInvoice (OnlineClaim.uspInsertUpdateService, masterServiceName "COL", sentinels: -3 -> the real
BROKEN JSON {"status":"success","message":" Invoice Already exist.} , >0 -> success id, else
"Invalid values passed!"). COL quirks: NO base64 step; 3rd encounter segment OVERWRITES the practice
suffix; errors returned as RAW text bodies with HTTP 200 + application/json.

**New:** IColRequestParser/ColRequestParser, ColModels.cs + ColDataTableMapper (exact
Utility.DataTableToList port), IColDataRepository/ColDataRepository,
Application/Features/Col/ColQueries.cs; ColCompatController rewritten off the canonical
AuthenticateCommand onto the real pipeline; ColCredentialTranslator deleted (dead).

**Verified:** build clean, 20/20 tests; live vs real PMS DB (suffix _128): reads -> legacy-exact
"Invalid token value!" raw-text 200; authenticate -> real proc rejection {"error":"Authentication
failed!"}; 3-segment encounterId correctly resolves the overwritten (unconfigured) suffix exactly as
legacy would. Real-data pass = Zohaib via Swagger.

**ALL FOUR MODULES NOW COMPLETE: HISO, KARO, ERMS, Claim Online.**

---

## 2026-07-22 - Non-legacy REST surface parked ([NonController])

**Decision (Zohaib):** only the legacy compat APIs stay exposed. Piloted on AuthController
(POST /auth/token -> 404, verified), then rolled out to the other 16 modern controllers
(Demographics, ClinicalNotes, Conditions, Medications, Reports, Documents, Observations, Acc45,
EncounterSummary, Tasks, Recalls, Screening, Providers, PracticeContext, PracticesAdmin, Invoices).
Mechanism: one [NonController] attribute + comment per class - removes routing + Swagger, no code
deleted, remove the attribute to re-enable.

**Verified:** build clean, 20/20 tests; disabled routes return 404 (spot-checked 6); /erms/ping and
/karo/ping still 200; Swagger now lists ONLY /erms (30 paths incl. /erms/col), /karo (18), /hiso (6).
