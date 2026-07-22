# Model Handoff — HEK Core API (Unified Healthcare API Hub)

**Purpose of this file:** if a new AI model (or developer) picks up this project, read THIS first.
It captures the standing rules, the exact current state, how to verify things, and the approved
road ahead. Last updated: 2026-07-22 (all four legacy modules complete).

---

## 1. Standing rules (NEVER violate)

1. **Legacy fidelity wins.** The four legacy compat APIs (HISO, KARO/HSS, ERMS, Claim Online) are
   rebuilt **exactly** as legacy behaves — same routes, request/response shapes, status codes,
   error text, and bugs/quirks included. Do NOT "improve" or fix legacy behavior unless Zohaib
   explicitly says so. Real clients consume these surfaces.
2. **Module isolation.** HISO, KARO, ERMS, COL share no session/token/middleware/DB routing with
   each other, even though they live in one API host. (Exception, by design: COL reuses ERMS's
   token validator/auth repo/connection resolver because the legacy COLController literally lives
   in-process with ERMS's APIController and shares its HSSDA DAL.)
3. **Secrets** only via `ISecretProvider`, stored in gitignored
   `src/Api/appsettings.Development.local.json`. Never hardcode; never commit.
4. **Convention DI:** any Infrastructure class implementing one Application interface
   auto-registers via `AddInfrastructureRepositories()` — no manual wiring.
5. **Docs-driven workflow:** update `hek_analysis/PROJECT_STATUS.md` (append-only change log) and
   `AI_USAGE_LOG.md` in the same session as any change. Plan first, get approval, then code.
6. Work in small approved slices; never leave code half-wired; build + test after every batch.

## 2. Architecture (as built)

Clean Architecture: `Domain` → `Application` (MediatR CQRS + FluentValidation; interfaces in
`Application/Common/Interfaces`) → `Infrastructure` (legacy DAL ports under
`Infrastructure/Legacy/{Hiso,Karo,Erms}`) → `Api` (thin controllers under `Api/Features/*`) +
wire-format DTO projects `Adapters.Hiso`, `Adapters.Karo`, `Adapters.Erms` (COL lives in
`Adapters.Erms/Col`). Serilog (console + rolling file `src/Api/logs/`), `CorrelationIdMiddleware`,
`GlobalExceptionHandler` (never leaks internals), health checks `/health` + `/health/ready`,
JWT auth + rate limiter exist but are flag-disabled, Dockerfile exists, target: net8.0.

## 3. Module status — ALL FOUR LEGACY MODULES COMPLETE (live-verified)

| Module | Route | Ops | Controller |
|---|---|---|---|
| HISO | `/hiso/*` | 6 | `Api/Features/Hiso/Controllers/HisoCompatController.cs` |
| KARO/HSS | `/karo/*` | 21 | `Api/Features/Auth/Controllers/KaroCompatController.cs` |
| ERMS | `/erms/*` | 22 (Ping, Authenticate, 19 Get*, SaveDocument) | `ErmsCompatController.cs` |
| Claim Online | `/erms/col/*` | 7 (authenticate, 5 reads, SaveInvoice) | `ColCompatController.cs` |

Standing deferrals (documented, intentional): legacy AWS document branches (`AWSDoc.IndiciDMS`
DLL not portable), Azure proxy branches (`EnableAzureERMSAPI`), and the HISO
PracticeId-registry-vs-numeric-ID mismatch (flagged, unresolved).

**The 17 modern REST controllers are PARKED** (decision 2026-07-22): each carries
`[NonController]` + comment — removed from routing and Swagger, code kept. Only the four legacy
surfaces are exposed. Remove the attribute to re-enable a controller.

## 4. Key reusable building blocks (do not rebuild)

- `KaroEncryptionService` — exact Rijndael/AES-256 legacy port (shared key across KARO/ERMS/COL),
  reused via `IKaroEncryptionService`.
- Parsers: `KaroRequestParser`, `ErmsRequestParser` (base64 + `"__"/"_"` split + pho + raw 2nd
  segment), `ColRequestParser` (no base64; 3rd segment OVERWRITES suffix). Each module its own.
- Token validators: `KaroTokenValidator`, `ErmsTokenValidator` (COL reuses ERMS's) — all wrap the
  real `[HSS].[uspInsertAndValidateToken]`.
- Mappers: `ErmsDataTableMapper` (`ERMSDataTableToListHiso<T>` port: `|&|`/`|?|` splits, null on
  failure), `ColDataTableMapper` (`DataTableToList<T>` port), `ErmsRtfConverter`
  (`ConvertString2RTF` port). Quirks are load-bearing — callers deliberately NRE on null mappers.
- Envelope helpers on `ErmsCompatController`: `PrepareXml`/`SetToXml` (utf-16 declaration, always
  HTTP 200, error → `<Error><Message>`); COL returns raw error text as JSON body, always 200.
- Connection routing: `"ConnIndiciDB"+suffix` / `"ConnDMSDB"+suffix` via per-module resolvers,
  keys `Karo:DbCredentials:*` / `Erms:DbCredentials:*` in the local settings file.
- `LegacyDbExecutor` for all proc calls; `PlainTextInputFormatter` for raw-body endpoints.

## 5. How to verify anything (the working live-test recipe)

- Run: `dotnet run --project src/Api` → Swagger at `http://localhost:5238/swagger`.
- Real local DB: `dbserver-local` / `PMS_NZ_V2` via suffix `_901_FZZ999-B` (KARO/ERMS) or `_128`
  (COL-reachable, since COL's parser can't produce compound suffixes). The default
  `43.255.162.58` targets are unreachable from this machine.
- KARO real success creds exist (`hsslive`, see PROJECT_STATUS 2026-07-21 KARO entry, patient
  2459731, encounterId `19592581__901__FZZ999-B`). ERMS `ermsdev`/`eRMsd3V` is real-rejected in
  PMS_NZ_V2; Zohaib does valid-token passes via Swagger.
- Invalid-token smoke test: any Get* with `Authorization: Bearer 00000000-0000-0000-0000-000000000001`
  must return the module's exact legacy envelope with HTTP 200 (ERMS: XML `<Error>`; COL: raw text).
- Known environment quirks: real `uspDocumentSave` throws an internal ROLLBACK here (pre-existing,
  not a port bug); stop stale `HekCoreApi.Api` processes before building (file locks).

## 6. What was assessed and what comes next (2026-07-22 assessment)

Spec: `docs/HEK_UNIFIED_API_SPEC.md` (hub/gateway with canonical model, field-level scoping,
audit, telemetry). Implementation skill: `.claude/skills/implementation` (phase-gated; approval
required before each phase; minimal code; no spelling errors).

**Gaps found:** no OpenTelemetry at all; no per-request/response logging (successful calls leave
NO trace in `src/Api/logs/` — only warnings/errors/exceptions are logged); no audit trail
(FR-6); no unified `/v1` surface with sparse fieldsets + per-consumer field scopes (FR-3/4/5);
no dataset/unified-model/architecture docs; no docker-compose; no golden regression tests over
the compat envelopes.

**Approved roadmap (each phase gated on Zohaib's explicit approval; zero functional change to
legacy surfaces in phases 1–6):**
1. **Compat safety net** — golden-response integration tests freezing byte-exact behavior
   (status, content-type, body) for representative endpoints of all four modules. DO THIS FIRST —
   later phases add middleware and the utf-16/broken-JSON envelopes are fragile.
2. **Observability core** — `RequestResponseLoggingMiddleware` (full bodies per Zohaib's explicit
   choice, 32 KB truncation, masking hooks, `RequestLogging` options), `UseSerilogRequestLogging()`,
   MediatR `RequestLoggingBehavior`, OpenTelemetry (ASP.NET Core + SqlClient instrumentation,
   traces + metrics, OTLP + console exporters).
3. **Audit logging** — `IAuditService`: consumer, timestamp, endpoint, fields returned (sink
   choice = decision point at phase start).
4. **Security hardening** — headers middleware, request size limits, enable rate limiter, OWASP
   checklist. Legacy behavior re-verified via phase-1 suite.
5. **Hub packaging** — per-module DI extension methods, docker-compose, scale-out + module
   onboarding docs.
6. **Documentation set** — `docs/architecture.md`, `docs/datasets/{hiso,karo,erms}.md` +
   `unified-model.md`, diagrams, consumer auth guide, doc index.
7. **Unified canonical `/v1` API** (largest, LAST, additive only) — sparse fieldsets,
   per-consumer field-scope profiles (response = requested ∩ allowed), OAuth2 client credentials,
   consumer simulators proving parity. Design presented as its own plan at phase start.

**Full assessment text:** see the 2026-07-22 entries in `PROJECT_STATUS.md` and the plan file
history; risks: middleware corrupting legacy envelopes (mitigated by phase 1), PHI in logs
(Zohaib accepted full-body logging; masking still required), spec-vs-parked-surface tension
(resolved: `/v1` will be a NEW module; parked controllers stay as reference).

## 7. Where to look for detail

- `hek_analysis/PROJECT_STATUS.md` — authoritative append-only history of everything built.
- `AI_USAGE_LOG.md` — parallel session log.
- `legacy-reference/` — the REAL legacy source (ermsapi, hsswebapi, Hiso, controller/) — always
  port from here, never from memory or docs alone.
- `docs/HEK_UNIFIED_API_SPEC.md` — the hub spec; `.claude/skills/` — project skills (invoke
  `implementation` and `docs-driven-workflow` explicitly when relevant).
