# Handoff Summary — HEK Core API (for Fable session)

## Standing rules (do not violate)
- Rebuild legacy HISO/KARO/ERMS/Claim-Online endpoints **exactly** as legacy behaves — same routes,
  request/response shapes, quirks/bugs included. Do NOT "improve" or fix legacy bugs unless told.
- HISO, KARO, ERMS, Claim Online must stay **fully isolated** — no shared session/token/DB routing
  between them, even though they live in one API host.
- All real connection strings/secrets go through `ISecretProvider`, stored in gitignored
  `src/Api/appsettings.Development.local.json` — never hardcode.
- Convention-based DI: any Infrastructure class implementing one Application interface
  auto-registers via `AddInfrastructureRepositories()` — no manual DI wiring needed.
- Work efficiently, minimal token usage, don't leave things half-wired if budget runs low.

## Status by module
- **HISO** — complete (all 6 real operations), verified.
- **KARO** — complete (all 21 real operations), verified live against real DB
  (`PMS_NZ_V2`/`dbserver-local`). Controller: `src/Api/Features/Auth/Controllers/KaroCompatController.cs`.
- **ERMS** — only `Ping` + `Authenticate` done and verified live (real creds `ermsdev`/`eRMsd3V`).
  Remaining: 19 `Get*` operations + `SaveDocument`, not started. `GetPatientData` was scoped only
  (complex wrapped-XML model, needs new `ERMSDataTableToListHiso<T>` mapper + new
  `IErmsTokenValidator`, clone of `KaroTokenValidator`) — no code written yet.
  Reference source: `legacy-reference/ermsapi/DevLocal/ERMSWebAPI/ERMSWebAPI/Controllers/APIController.cs`
  (1900 lines; `GetPatientData` body at line 856).
- **Claim Online (`ColCompatController`)** — only `Authenticate` exists. 6 more real ops confirmed
  from legacy `COLController.cs`: `GetCurrentPatientData`, `GetSessionData`, `GetProviderData`,
  `GetSurgeryData`, `GetDiagnosisData` (all backed by a `PHCO` class, confirmed present in the
  supplied ERMS source tree) + `SaveInvoice` (reuses same real proc as KARO:
  `HSSDA.InsertUpdateService` / `[HSS].[uspInsertUpdateService]`). Not started.

## Reusable building blocks already built (don't rebuild)
- `KaroEncryptionService` (`src/Infrastructure/Legacy/Karo/KaroEncryptionService.cs`) — exact
  Rijndael/AES-256 port, reused by ERMS too via `IKaroEncryptionService`.
- `KaroAuthResult` record (in `IKaroAuthRepository.cs`) — shared by KARO and ERMS auth results.
- Practice-suffix / encounterId parsing pattern — see `KaroRequestParser`/`ErmsRequestParser`
  (split on `"__"` else `"_"`; 4th segment overwrites, doesn't append — a real legacy quirk).
- `PlainTextInputFormatter` (`src/Api/Formatters/PlainTextInputFormatter.cs`) — lets Swagger show
  a raw-body box for XML/plain-text legacy endpoints; registered in `Program.cs`.
- `KaroRootResponse<T>` envelope (`src/Adapters.Karo/Demographics/KaroDemographicsResponse.cs`) —
  reused across all KARO Get* responses.

## Immediate next task (recommended starting point for Fable)
Continue ERMS: build `GetPatientData` first (closest analog to KARO's `GetDemographics`), then work
through the remaining `Get*` ops following the same Karo Get*-operation pattern (repository interface
+ implementation calling the real stored proc, query/handler, controller action, adapter response
DTO). After ERMS is complete, move to Claim Online's remaining 6 ops + `SaveInvoice`.

## Where to look for more detail
- `hek_analysis/PROJECT_STATUS.md` — full dated change log of everything built so far.
- `AI_USAGE_LOG.md` — parallel log.
- Plan file (analysis only, not yet acted on): `C:\Users\zohaib.ahmed\.claude\plans\swift-painting-blossom.md`
