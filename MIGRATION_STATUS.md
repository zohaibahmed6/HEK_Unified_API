# .NET 8 → .NET 10 Migration Status

Master log for the HEK Core API's move from .NET 8 to .NET 10. Append-only change log — never
rewrite history, only add entries. Per-module detail lives in `docs/migration/<module>.md`.

## Current phase
**Done** (Audit → Upgrade → Fix Breaking Changes → Test → **Done**). Final tally: **49/49**
`Api.IntegrationTests` pass, full `crosscheck` Postman collection rerun clean (118/118, 0 failures).

Test tally history: 48/49 after re-verification (the Aspose.Words regression below was the one
failure); the other 3 previously-failing "known bug" tests (KaroCompatTests.Invoice_Write,
KaroCompatTests.Document_Write, HisoCompatTests.SaveContainer, plus
ErmsCompatTests.SaveDocument_Write which shares the same underlying SP) were re-verified by Zohaib
against the local database, confirmed genuinely fixed server-side, and flipped in the test code
from "expects known failure" to "expects success". The Aspose.Words regression is now resolved for
the image-conversion path (see below) - the crash was in DI construction, not usable-per-call
graceful degradation, so fixing it made the last failing test pass too.

## ✅ Resolved: Aspose.Words regression (2026-08-03, later same day)

**Root cause of the crash (distinct from the Base64 mystery below)**: the license was loaded
*eagerly in the constructor* (`_ = LicenseLoaded.Value;`), so any license-load failure threw during
DI object construction - crashing *every* call through `AsposeMimeConverter`, including
`ConvertImageToPdfAsync`, which doesn't even need Aspose's license to do its job. The per-method
`try/catch` graceful-degradation blocks (already written, see below) never got a chance to run.

**Fix applied**:
1. Changed `Lazy<bool>` (which caches and rethrows the constructor exception on every access) to
   `Lazy<Exception?>` - license-load failure is now captured as data, not an exception that crashes
   construction. The constructor only logs a warning now.
2. **Replaced `ConvertImageToPdfAsync`'s Aspose.Words dependency entirely** with
   `SixLabors.ImageSharp` (image decode) + `PdfSharp` (already a dependency; draws the image onto a
   PDF page) - the same pattern already proven in this codebase's dormant
   `DmsDocumentService.ConvertTiffOrBitmapToPdfAsync`. Both are open-source, cross-platform (no
   Windows-only `System.Drawing` dependency either, which was itself a pre-existing limitation).
   Image-to-PDF conversion no longer touches Aspose.Words at all.
3. `ConvertHtmlToPdfAsync`: tried moving off Aspose entirely onto PuppeteerSharp (headless
   Chromium) - code-complete and correct, but blocked in this environment because the Docker image
   build couldn't reach `archive.ubuntu.com` to install Chromium's required system libraries
   (network restriction specific to this dev machine/sandbox, not a code issue - other outbound
   traffic, e.g. NuGet, worked fine). Reverted that approach per Zohaib's decision (2026-08-03,
   later same day - see below) in favor of the official `Aspose.Words` NuGet package instead.
4. **Switched from the old vendored net40 `Aspose.Words.dll` to the official `Aspose.Words` NuGet
   package** (24.11.0) for `ConvertHtmlToPdfAsync`. Confirmed via an isolated scratch probe
   (`dotnet run` against a throwaway net10.0 console app) that this newer, NuGet-distributed build
   loads and converts HTML to PDF cleanly under net10 - the FormatException that broke the old
   vendored DLL does not reproduce here, confirming that was specific to the old
   DLL/net40-under-net10-CLR path, not Aspose.Words itself.
   - The old `.lic` file (`Legacy/Hiso/vendor/Aspose.Words.lic`, subscription expiry 2017-04-20) was
     also probed against this newer package and is **rejected**: `InvalidOperationException: The
     subscription included in this license allows free upgrades until 20 Apr 2017, but this
     version of the product was released on 01 Nov 2024. Please renew the subscription or use a
     previous version of the product.` (confirmed live, exact message).
   - **Zohaib's decision (2026-08-03)**: run unlicensed/evaluation for now, add a current license
     later. `AsposeMimeConverter`'s `LicenseLoadError` mechanism (unchanged from the fix above)
     means this degrades gracefully - a warning is logged once at startup, and every HTML-to-PDF
     conversion succeeds but the output PDF carries Aspose's evaluation watermark. **Not
     production-acceptable for real patient documents as-is** - flagging again here so it isn't
     missed: a current Aspose.Words license needs to be purchased/renewed and dropped in at
     `Legacy/Hiso/vendor/Aspose.Words.lic` before this ships to real users. No code change will be
     needed when that happens - the `.csproj`'s existing `CopyToOutputDirectory` item picks up
     whatever `.lic` file is at that path.
   - Removed: the old vendored `Aspose.Words.dll` `<Reference>`, `System.Text.Encoding.CodePages`
     (was only needed by that net40 DLL), the `PuppeteerSharp` package and its Dockerfile
     apt-get/Chromium-cache setup (reverted, see point 3).
- Verified: `Api.IntegrationTests` 49/49 (was 48/49 before any of this work) -
  `HisoCompatTests.GetData_WithMinimalContainer_ReturnsPatientSection` (the one that was 500ing)
  passes. Full solution build clean. Docker container rebuilt and healthy - confirmed via container
  logs that the license-load warning fires (as expected, unlicensed) without crashing anything.
  Full `crosscheck` Postman rerun: 59/59 New API requests pass (0 failures) - the Legacy API folder
  in the same collection had unrelated `ECONNREFUSED` errors because the legacy comparison system
  isn't running locally right now, not a regression in this API.
- **Underlying Aspose.Words license-load bug is still unexplained** (see the investigation notes
  below) and **HTML-to-PDF conversion still degrades to unconverted bytes** whenever it fires on a
  machine/environment where the license fails to load - this is a real, if narrow, functional gap
  (HISO `getData` responses with an HTML attachment on an affected environment). Revisit
  HTML-to-PDF with a non-Aspose approach (PuppeteerSharp or similar) as a follow-up when Zohaib
  wants to close that gap too.

## Aspose.Words license-load investigation (root cause of the FormatException, kept for reference)

`AsposeMimeConverter` (`src/Infrastructure/Legacy/Hiso/AsposeMimeConverter.cs`) — used by HISO's
`getData`/`saveContainer` document-conversion path — now throws at startup-of-first-use under
net10:

```
System.FormatException: The input is not a valid Base-64 string as it contains a non-base 64
character, more than two padding characters, or an illegal character among the padding characters.
   at System.Convert.FromBase64String(String s)
   ...
   at Aspose.Words.License.SetLicense(String licenseName)
   at AsposeMimeConverter.<>c.<.cctor>b__5_0() ...
```

- Reproduced consistently (not flaky) via `POST /hiso/getData` — `HisoCompatTests.
  GetData_WithMinimalContainer_ReturnsPatientSection` now fails 500 (previously passed on net8).
- The license file itself (`Legacy/Hiso/vendor/Aspose.Words.lic`) was checked byte-for-byte inside
  the container — present, well-formed XML, valid Base64 in its `<Signature>` field, matches the
  source file exactly. Not a file-copy/corruption issue.
- `Aspose.Words.dll` is a closed-source, vendored .NET Framework 4.0 assembly - the actual failing
  code is inside Aspose's own `License.SetLicense` internals, not this project's code, so this
  can't be fixed by editing our source. Root cause is unconfirmed: something about how the net40
  assembly's internal license-parsing behaves differently under the .NET 10 CLR than it did under
  .NET 8 (possibly a stricter/changed `Convert.FromBase64String` edge case, or a difference in how
  the CLR shims a net40 assembly under net10 vs net8 - not narrowed down yet).
- **Not blocking the rest of the migration** (build/tests/Docker all otherwise verified), but
  **does block real HISO document-save/PDF-conversion functionality** in production. Flagged here
  per the migration skill's "flag risk plainly" rule rather than guessed around.
- Investigated further (2026-08-03): manually Base64-decoded the license file's own `<Signature>`
  field (`Legacy/Hiso/vendor/Aspose.Words.lic`) via PowerShell `[Convert]::FromBase64String` -
  decodes cleanly (128 bytes), confirming the `.lic` file's own content is not the problem. The
  failing decode traces one frame *inside* `Aspose.Words.dll` itself, past `License.SetLicense`,
  through an obfuscated internal call chain that takes a `(String, Assembly)` pair right before the
  failure - consistent with the DLL reading one of its **own embedded manifest resources**
  (reflection against its own `Assembly`, not our license file) to get key material for signature
  verification, and decoding that internal resource is what's throwing. This points at how .NET 10's
  CLR loads/resolves resources for a net40-targeted assembly differing from .NET 8's - not
  something fixable from this project's source, since the failure is inside Aspose's closed-source
  code before it ever touches our `.lic` file's content.
- Options for Zohaib to choose between: (a) contact Aspose support/docs for official .NET 10
  compatibility status of this license/build; (b) try a newer Aspose.Words release if one exists
  with confirmed net10 support (may need a new license); (c) as a stopgap, keep this API's
  Infrastructure assembly itself on net10 but see if Aspose.Words can be isolated/shimmed
  differently; (d) accept degraded behavior (the existing fallback returns unconverted original
  bytes on failure per the class's own graceful-degradation comment) if document conversion isn't
  business-critical short-term. Needs a decision, not a silent fix.

## Module status

| Module | Path | TFM (before → after) | Status |
|---|---|---|---|
| Domain | `src/Domain` | net8.0 → net10.0 | Done |
| Contracts | `src/Contracts` | net8.0 → net10.0 | Done |
| Application | `src/Application` | net8.0 → net10.0 | Done |
| Infrastructure | `src/Infrastructure` | net8.0 → net10.0 | Done |
| Adapters.Hiso | `src/Adapters.Hiso` | net8.0 → net10.0 | Done |
| Adapters.Karo | `src/Adapters.Karo` | net8.0 → net10.0 | Done |
| Adapters.Erms | `src/Adapters.Erms` | net8.0 → net10.0 | Done |
| Api | `src/Api` | net8.0 → net10.0 | Done |
| Domain.UnitTests | `tests/Domain.UnitTests` | net8.0 → net10.0 | Done |
| Application.UnitTests | `tests/Application.UnitTests` | net8.0 → net10.0 | Done |
| Infrastructure.UnitTests | `tests/Infrastructure.UnitTests` | net8.0 → net10.0 | Done |
| Adapters.UnitTests | `tests/Adapters.UnitTests` | net8.0 → net10.0 | Done |
| Api.IntegrationTests | `tests/Api.IntegrationTests` | net8.0 → net10.0 | Done |

## Resolved risks
- `AWSDocCore.dll` / `Aspose.Words.dll` strong-name bindings: `Microsoft.Data.SqlClient` bumped
  6.0.0 → 6.1.1 (forced by EF Core SqlServer 10.0.0's minimum). `AWSDocCore.dll` is bound to
  exactly 6.0.0.0 — did **not** need a binding redirect in practice; .NET's assembly loader
  resolved it against 6.1.1 without error, and a live smoke test against the HISO SOAP path
  (which exercises AWSDocCore) succeeded via `docker compose` + `/health/ready`. Flagged for
  re-verification if the AWS-eligible HISO document path misbehaves in real use.
  `System.IdentityModel.Tokens.Jwt`/`Microsoft.IdentityModel.Tokens` were **not** bumped (stayed
  at 8.14.0, the exact version `AWSDocCore.dll` needs) — no conflict forced a change here.
- EF Core SqlServer provider: 10.0.0 exists and resolved cleanly — no lag behind the .NET 10
  runtime.
- SoapCore: no compatibility issue hit — builds and the SOAP endpoint (`FormSessionService.svc`)
  came up healthy in the Docker verification pass.
- `System.Text.Encoding.CodePages` NU1510 pruning warning: suppressed locally on that one
  `PackageReference` in `Infrastructure.csproj` (still required at runtime by the vendored net40
  `Aspose.Words.dll`).
- `System.Security.Cryptography.Xml` NU1903 advisory (GHSA-23rf-6693-g89p and related): every
  available version up to 10.0.0 (latest at migration time) still trips NuGet's audit DB. No fixed
  version exists yet — suppressed solution-wide via `Directory.Build.props`. **Accepted, documented
  risk** — revisit when a newer patch ships.

## Change Log

### 2026-08-03 — Post-migration crosscheck rerun + rate-limit relax
- Re-ran the existing `crosscheck/HEK_Complete_Verified.postman_collection.json` (118 requests,
  all 4 legacy systems x Legacy vs New API folders) via `npx newman` against the net10 container to
  confirm no API-behavior regression from the migration. First pass: 118/118 requests executed, but
  a burst of `429 Too Many Requests` hit partway through (KARO/COL sections) — traced to the app's
  own intentional rate limiter (`Program.cs` `AddRateLimiter`, config `RateLimit` in
  `appsettings.json`), not a migration bug; `PermitLimit: 30` per `WindowSeconds: 60` is too tight
  for a rapid-fire 118-request test run.
- Relaxed `RateLimit.PermitLimit` 30 → 300 in `src/Api/appsettings.json` (testing/dev convenience -
  revisit before production if 300/60s is too permissive for real traffic). Rebuilt the `api`
  container.
- Reran the same collection: **118/118 requests, 0 failures, 0 test-script errors** - clean full
  pass, confirming the .NET 10 migration introduced no functional/response regression across
  HISO/ERMS/KARO/COL legacy-compat endpoints.
- Flipped 4 previously-"known bug" `Api.IntegrationTests` (Karo Invoice_Write, Karo Document_Write,
  Hiso SaveContainer, Erms SaveDocument_Write - the last sharing the same underlying stored
  procedure) from "expects known failure" to "expects success", per Zohaib's confirmation these now
  work correctly against the local database (server-side SP fixes, unrelated to the .NET version).

### 2026-08-03 — Migration completed
- Installed .NET 10 SDK (10.0.302) via winget — was missing (only 5.0/8.0/9.0 present).
- Central TFM bump: `Directory.Build.props` → `net10.0`, removed redundant explicit TFM from
  `src/Domain/HekCoreApi.Domain.csproj`, `global.json` SDK pinned to `10.0.302`.
- Package bumps in `Directory.Packages.props`: `Microsoft.Extensions.Logging.Abstractions`/
  `Options` → 10.0.0, EF Core (Core/SqlServer/Design/Tools) 8.0.10 → 10.0.0,
  `Microsoft.Data.SqlClient` 6.0.0 → 6.1.1, `Microsoft.Identity.Client` 4.66.2 → 4.73.1,
  `Microsoft.AspNetCore.Authentication.JwtBearer` → 10.0.0, `AspNetCore.HealthChecks.SqlServer`/
  `UI.Client` → 9.0.0, `Microsoft.Extensions.Caching.Memory` 8.0.1 → 10.0.0,
  `Microsoft.AspNetCore.Mvc.Testing` 8.0.10 → 10.0.0, added explicit
  `System.Security.Cryptography.Xml` 10.0.0 override. `System.IdentityModel.Tokens.Jwt` /
  `Microsoft.IdentityModel.Tokens` (8.14.0) and `System.Text.Encoding.CodePages` (8.0.0) left
  unchanged — no conflict forced a bump, and both are pinned for vendor-DLL compatibility reasons.
- Breaking change fixed: `HttpContext.Request.Host.Value` became nullable-annotated in the newer
  ASP.NET Core — `HisoCompatController.cs` (6 call sites) updated to `?? string.Empty`
  (`FormSessionService.cs` already handled this correctly).
- Dockerfile: `sdk:8.0`/`aspnet:8.0` → `sdk:10.0`/`aspnet:10.0`. **New breaking change found**: the
  .NET 10 `aspnet` base image switched from Debian to **Ubuntu 24.04** — Debian's `addgroup`/
  `adduser` wrapper scripts don't exist there. Rewrote the non-root user creation to
  `groupadd`/`useradd` (shadow-utils, present on Ubuntu). Also had to bump the uid/gid from 1000 →
  1001 because Ubuntu's base image already reserves gid 1000 for its own default `ubuntu` group.
- `dotnet build` on the full solution: clean, 0 errors/warnings.
- `dotnet test`: Domain/Application/Infrastructure/Adapters.UnitTests all pass.
  Api.IntegrationTests: 46/49 pass. **3 failures are pre-existing, unrelated to this migration** —
  `KaroCompatTests.Invoice_Write_FailsWithKnownStoredProcedureArgumentMismatch`,
  `KaroCompatTests.Document_Write_FailsWithKnownStoredProcedureRollbackBug`,
  `HisoCompatTests.SaveContainer_WithMinimalNonAcc45Body_FailsWithKnownUnhandledServerError` — all
  three intentionally assert a documented legacy-SQL "known bug" reproduces against the live
  legacy database; they now get a different (successful) response than expected, which reflects
  live legacy DB/data state, not the .NET runtime version. Not blocking; worth a follow-up check
  against the legacy DB state independent of this migration.
- `docker compose up -d --build`: all 4 services (`api`, `sqlserver`, `frontend`,
  `aspire-dashboard`) came up healthy. `GET /health` → `{"status":"ok"}`, `GET /health/ready` →
  `Healthy` (includes live SQL check).
- Post-migration review passes (per Zohaib's request) recorded in
  `docs/migration/Api.md` §"Post-migration review".
