# Deployment

## Dockerfile
`src/Api/Dockerfile` — multi-stage build (SDK 10.0 → aspnet 10.0 runtime, migrated from 8.0 on
2026-08-03, see `MIGRATION_STATUS.md`), non-root user (`hek`, uid/gid 1001 — bumped from 1000
because the net10 aspnet base image switched Debian → Ubuntu, which reserves gid 1000 for its own
default `ubuntu` group), `EXPOSE 8080`, entrypoint `dotnet HekCoreApi.Api.dll`. Only `Api` is a deployable
image — `Adapters.*`/`Infrastructure` are class libraries it hosts, not separate processes (see
`docs/adr/ADR-012-solution-structure-and-block1-inferences.md` for why this is one Dockerfile, not
"one per service").

## docker-compose
`docker-compose.yml` (root) and `docker-compose.override.yml` **do exist** in the repo (contrary to
older assumptions that no compose file was present):

- `sqlserver` service: `mcr.microsoft.com/mssql/server:2022-latest`, port 1433, healthcheck via
  `sqlcmd`, requires `SA_PASSWORD` env var.
- `api` service: builds from `src/Api/Dockerfile`, port 8080 (5080 in the dev override), depends on
  `sqlserver` being healthy, reads `ConnectionStrings__TenantRegistry`, `Auth__Enabled`,
  `Auth__JwtSigningKey` from environment.
- `docker-compose.override.yml`: dev-only overrides (`ASPNETCORE_ENVIRONMENT=Development`, port
  5080, read-only source mount).
- `aspire-dashboard` service (added 2026-07-23): `mcr.microsoft.com/dotnet/aspire-dashboard:8.0`,
  receives OpenTelemetry traces/metrics from `api` over OTLP, UI at `http://localhost:18888`
  (unauthenticated in this compose file — demo/dev only). `api` points at it via
  `Otel__OtlpEndpoint=http://aspire-dashboard:18889`.

### Container networking — resolved
Earlier sessions saw the `api` container fail to reach `sqlserver` over TCP (`SqlException ... error:
40`) on some dev machines. As of 2026-07-30, `docker compose up -d --build` runs cleanly end-to-end on
the current dev machine — `api`, `frontend`, and `aspire-dashboard` all come up healthy, and real
legacy-compat calls (ERMS `Authenticate`, host-routed — see below) succeed against the containerized
`api` on port 8080. If this resurfaces on a different machine, treat it as local Docker
Desktop networking/AV/VPN config, not a code issue — `dotnet run` remains a valid fallback.

## Config / secrets
`ISecretProvider` (`src/Application/Common/Interfaces/ISecretProvider.cs`) abstracts secret
resolution; `EnvironmentVariableSecretProvider` (`src/Infrastructure/Secrets/`) is the current
implementation — secrets resolved from environment variables, never hardcoded literals (except the
legacy Rijndael key, ported verbatim for byte-compatibility — an accepted, documented risk per
`docs/assessment-2026-07-22.md` §3). `JwtTokenIssuer` resolves its signing key via
`ISecretProvider.GetRequiredSecretAsync(_options.SigningKeySecretName)` — never a literal in code.

## Environment variables (from docker-compose.yml)
| Variable | Purpose |
|---|---|
| `SA_PASSWORD` | SQL Server sa password (required, no default) |
| `ASPNETCORE_ENVIRONMENT` | Production (default) / Development (override) |
| `ConnectionStrings__TenantRegistry` | EF Core tenant registry DB connection |
| `Auth__Enabled` | JWT bearer auth toggle (default false — flag-disabled per assessment §3) |
| `Auth__JwtSigningKey` | Symmetric key name/value resolved via ISecretProvider (required, no default) |

## Telemetry (NFR-5, resolved 2026-07-23)
OpenTelemetry is wired into `src/Api/Program.cs` — traces (ASP.NET Core requests, HTTP client calls,
SQL Server dependency calls) and metrics (the same instrumentation plus .NET runtime GC/thread-pool
metrics, plus a custom `HekTelemetry` meter for FR-5 field-scoping counters, currently wired into
`CanonicalDemographicsController`). Additive to Serilog — logs are unchanged.

- **Via `docker compose up`**: traces/metrics flow to the `aspire-dashboard` container automatically
  (`Otel__OtlpEndpoint` is set in `docker-compose.yml`). Open `http://localhost:18888`.
- **Via `dotnet run`** (the currently-verified path, see blocker above): `Otel:OtlpEndpoint` is unset
  by default in `appsettings.Development.json`, so traces/metrics print to console instead. To see
  them in the same dashboard UI without full compose, run the dashboard container standalone —
  `docker run --rm -it -p 18888:18888 -p 18889:18889 -e DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS=true mcr.microsoft.com/dotnet/aspire-dashboard:8.0`
  — then set `Otel:OtlpEndpoint` to `http://localhost:18889` in `appsettings.Development.local.json`.
- Confirmed: full solution build + all 23 tests pass with telemetry wired in; app boots cleanly under
  `dotnet run` with the console exporter active (smoke-tested 2026-07-23).

## Health check endpoints
Wired in `src/Api/Program.cs`:
- `GET /health` — liveness, `MapHealthChecks("/health", ...)`, matches OpenAPI spec's
  `{"status":"ok"}` shape (KARO Ping / ERMS Ping equivalent).
- `GET /health/ready` — readiness, includes a SQL dependency check (`AddHealthChecks()` registers a
  SQL check per Program.cs).

## Legacy host-based routing (KARO/ERMS/COL) — how one Azure app serves 4 real URLs

**The question this answers:** the app only gets one Azure URL (e.g.
`hekcoreapi.azurewebsites.net`). When a real client calls `hss.itsmyhealth.nz`, how does that request
end up at the KARO controller and not somewhere else?

**Simple answer:** it's not the *app* that has one URL each for KARO/ERMS/COL/HISO — DNS points all of
those real hostnames at the *same* Azure app. Azure lets one App Service / Container App answer to
many "custom domains" at once (this is a standard Azure feature, not something built for this
project). When `hss.itsmyhealth.nz` resolves via DNS to this app, the incoming request still carries
`Host: hss.itsmyhealth.nz` exactly as the client sent it — Azure doesn't rewrite it. Inside the app,
`LegacyHostRoutingMiddleware` (`src/Api/Middleware/LegacyHostRoutingMiddleware.cs`) reads that `Host`
header, matches it against `LegacyHostRouting:Rules` in `appsettings.json`
(`"hss" -> Karo`, `"erms" -> Erms`/`Col`), and rewrites the request path onto this hub's internal
route (`/karo/*`, `/erms/*`, `/erms/col/*`) *before* MVC picks a controller. So the request that left
the client as `POST hss.itsmyhealth.nz/api/Authenticate` lands, unmodified from the client's point of
view, on `KaroCompatController.Authenticate`.

HISO doesn't need any of this — `hiso.itsmyhealth.nz` only ever serves the SOAP endpoint
(`/FormSessionService.svc`), so there's no other system's path for it to collide with.

### Deployment checklist (do this once per environment — dev/staging/prod)

1. **DNS**: for each real hostname (`hss.itsmyhealth.nz`, `southerms.indici.nz`,
   `hiso.itsmyhealth.nz`, and their `dev*` equivalents), point the DNS record (CNAME, or A record per
   Azure's custom-domain instructions) at this app's Azure hostname instead of the old legacy server's
   IP. This is the actual cutover step — until DNS is repointed, old traffic keeps hitting the old
   legacy server.
2. **Azure custom domain binding**: in the Azure Portal (App Service/Container App → Custom domains),
   add each of those same hostnames as a custom domain on this one app. Azure verifies domain
   ownership (TXT/CNAME record) before it'll accept the binding.
3. **TLS certificate per hostname**: bind a certificate for each custom domain (Azure App Service
   Managed Certificate is the simplest — free, auto-renewing, one per custom domain).
4. **Confirm `appsettings.json`'s `LegacyHostRouting:Rules`** still contains the right
   `HostContains` substrings for whatever the real hostnames are in that environment (they don't need
   to be exact matches — `"hss"` matches both `hss.itsmyhealth.nz` and `devhss.itsmyhealth.nz`).
5. **Smoke test after cutover**: call each real hostname's real legacy path
   (`/api/Ping`, `/api/Authenticate`, `/COL/Authenticate`) from *outside* Azure (not from inside the
   container, to prove DNS + custom domain + TLS + routing all actually work together) and confirm a
   200-with-real-body response, not a 404.

### Local/Postman testing without a real DNS record

Locally there's no DNS entry for `hss.itsmyhealth.nz`, so `Host` defaults to `localhost:8080` and the
routing rule never matches (404, same shape as the KARO/ERMS collision this middleware exists to
solve). Two ways to test against the real hostnames locally, without touching any code:

- **One-off in Postman**: add a `Host` header manually (`Host: southerms.indici.nz`), keep the URL as
  `http://localhost:8080/...`. Works immediately but has to be re-added per request/collection.
- **Permanent fix, closer to real behavior — edit the Windows hosts file** (`C:\Windows\System32\drivers\etc\hosts`, needs admin) and add:
  ```
  127.0.0.1 hss.local
  127.0.0.1 erms.local
  ```
  Then call `http://hss.local:8080/api/Ping` / `http://erms.local:8080/API/Authenticate` directly —
  no manual header needed, Postman/curl/browser all send `Host: hss.local`/`erms.local` automatically,
  which still matches the `"hss"`/`"erms"` substring rules. This is the closer-to-production way to
  test locally since it exercises hostname-based dispatch exactly like Azure will, just via a hosts
  file entry instead of real DNS.

## Current known gaps
- No confirmed working containerized end-to-end run (see blocker above) — `dotnet run` is the only
  verified path today.
- No container health-check wiring beyond the `sqlserver` service's own `healthcheck:` block (the
  `api` service has no compose-level healthcheck).
- No scale-out/horizontal-scaling documentation yet, though the API itself is stateless (legacy
  tokens live in legacy DBs; in-memory idempotency/cache stores are per-instance, swappable behind
  `IIdempotencyStore` per assessment §7).
