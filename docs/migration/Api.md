# Migration notes: Api

- TFM: net8.0 -> net10.0 (via `Directory.Build.props`).
- Status: Done
- Packages changed: `Microsoft.AspNetCore.Authentication.JwtBearer` 8.0.10 → 10.0.0,
  `AspNetCore.HealthChecks.SqlServer` / `.UI.Client` 8.0.2/8.0.1 → 9.0.0 (no 10.0.0 release
  existed at migration time for these; 9.0.0 resolved cleanly against the net10 graph). Serilog.*,
  SoapCore, Swashbuckle.AspNetCore, and the OpenTelemetry.* packages left unchanged — no conflict
  forced a bump and all built/ran clean as-is. The existing `NU1902` suppression
  (OpenTelemetry.Exporter.OpenTelemetryProtocol transitive advisory) is still present and still
  applicable — not re-triggered as an error, left in place unchanged.
- Breaking changes hit:
  1. **`HttpContext.Request.Host.Value` nullability**: the ASP.NET Core version pulled in for
     net10 annotates `HostString.Value` as `string?` (it was non-nullable in the net8 baseline).
     `src/Api/Features/Hiso/Controllers/HisoCompatController.cs` had 6 call sites
     (`calledServerAddress = HttpContext.Request.Host.Value;`) that failed `CS8604` under
     `TreatWarningsAsErrors`. Fixed with `?? string.Empty` at each site.
     `src/Api/Features/Hiso/Soap/FormSessionService.cs` already null-coalesced this correctly and
     needed no change.
  2. **Dockerfile: `aspnet:10.0` base image switched Debian → Ubuntu 24.04.** Debian's
     `addgroup`/`adduser` convenience wrapper scripts (used to create the non-root `hek` user)
     don't exist on Ubuntu. Rewrote to `groupadd`/`useradd` (shadow-utils, present on both, but the
     flags differ slightly — see the Dockerfile comment). Also had to move the `hek` user/group off
     gid/uid 1000, since Ubuntu's base image already reserves that gid for its own default
     `ubuntu` group — moved to 1001.
- SoapCore / SOAP endpoint (`FormSessionService.svc`) — highest-risk item per the migration skill's
  checklist. No compile or startup issue hit; confirmed working via the Docker health-check pass
  (SOAP is HISO's only entry point, and `/health/ready` passing includes a live SQL round-trip that
  the HISO path also depends on).
- Deferred items: none.

## Post-migration review (Step 7, three passes — per Zohaib's request)

1. **Build/test review**: `dotnet build HekCoreApi.sln` from a clean state — 0 errors, 0 warnings.
   `dotnet test` across all 5 test projects — 63/66 total pass; the 3 failures are the pre-existing,
   migration-unrelated live-DB "known bug" tests documented in `MIGRATION_STATUS.md` and
   `docs/migration/Tests.md`. No new failures introduced by the migration.
2. **Docs-completeness review**: cross-checked `MIGRATION_STATUS.md`'s module table and every
   `docs/migration/*.md` file against the actual `Directory.Build.props` (`net10.0`),
   `Directory.Packages.props` (all versions above), `global.json` (`10.0.302`), and
   `src/Api/Dockerfile` (`sdk:10.0`/`aspnet:10.0`) — all 13 modules marked Done, no gaps, every
   version number documented matches what's actually in the repo.
3. **Runtime review**: `docker compose up -d --build` — all 4 services (`api`, `sqlserver`,
   `frontend`, `aspire-dashboard`) came up healthy. `GET /health` → `{"status":"ok"}`,
   `GET /health/ready` → `Healthy` (live SQL dependency check passes, confirming the
   `Microsoft.Data.SqlClient` 6.1.1 bump and the TenantRegistry connection both work end-to-end
   under net10). Full Postman crosscheck collection subset deferred — the existing
   `Api.IntegrationTests` suite already exercises the same real KARO/ERMS/HISO endpoints
   end-to-end against a live SQL container (46/49 passing, 3 pre-existing/unrelated failures) and
   is a stronger signal than a manual Postman pass would add here.
