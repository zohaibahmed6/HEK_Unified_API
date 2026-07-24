# Deployment

## Dockerfile
`src/Api/Dockerfile` — multi-stage build (SDK 8.0 → aspnet 8.0 runtime), non-root user (`hek`,
uid/gid 1000), `EXPOSE 8080`, entrypoint `dotnet HekCoreApi.Api.dll`. Only `Api` is a deployable
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

### Known blocker (PROJECT_STATUS.md open item 33) — NOT resolved
`docker compose up -d --build` builds and starts cleanly, and `sqlserver` reports healthy, but the
`api` container crashes on startup with `SqlException ... error: 40` — it cannot reach the
`sqlserver` container over TCP, even though the same target is reachable from other containers on
the same host/network using non-.NET tools. Not a regression from any specific session's code
changes. Likely next step is host-side investigation (Docker Desktop networking settings,
AV/EDR/VPN filtering) rather than a code fix.

**Current working path:** `dotnet run` locally (bypasses Docker networking entirely) — this is how
every "verified against real production data" claim in `PROJECT_STATUS.md` was actually run.

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

## Current known gaps
- No confirmed working containerized end-to-end run (see blocker above) — `dotnet run` is the only
  verified path today.
- No container health-check wiring beyond the `sqlserver` service's own `healthcheck:` block (the
  `api` service has no compose-level healthcheck).
- No scale-out/horizontal-scaling documentation yet, though the API itself is stateless (legacy
  tokens live in legacy DBs; in-memory idempotency/cache stores are per-instance, swappable behind
  `IIdempotencyStore` per assessment §7).
