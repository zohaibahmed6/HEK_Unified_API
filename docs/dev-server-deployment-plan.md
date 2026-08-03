# Dev/Training Server Deployment Plan

Simpler than the Azure plan (`docs/azure-deployment-plan.md`) — no cloud account, DNS, or
region decisions needed. Just a server that can run Docker.

## Steps

### 1. Prepare the server
- Install Docker (Docker Desktop on Windows, Docker Engine on Linux) on the target server.
- Nothing else is needed — `src/Api/Dockerfile` and the root `docker-compose.yml` /
  `docker-compose.override.yml` already handle the rest.

### 2. Get the code onto the server
- `git clone` the repo onto the server, or copy the already-built Docker image over.

### 3. Provide real settings (server-local, never committed)
- Create a `.env` file on the server (gitignored, stays local to that machine) with real
  dev/training-level values, replacing the placeholder `CHANGE_ME_DEV_ONLY` values currently in
  `src/Api/appsettings.json`:
  - `SA_PASSWORD` (SQL Server sa password)
  - `ConnectionStrings__TenantRegistry`
  - `Auth__JwtSigningKey`
  - Any `Legacy:Conn*` / `DbCredentials:*` values needed for whichever practices are being tested
- `Auth__Enabled` stays `false` for this test deploy (same as the Azure plan — Entra ID isn't
  wired up yet).

### 4. Start it
- Run: `docker compose up -d --build`
- This brings up `sqlserver`, `api` (port 8080), `frontend`, and `aspire-dashboard` together.

### 5. Verify
- Open `http://<server-address>:8080/health` — should return OK.
- Open `http://<server-address>:8080/health/ready` — should return OK (includes SQL check).
- Point `crosscheck/HEK_4APIs.postman_environment.json`'s `newBase` at
  `http://<server-address>:8080` and run a subset of
  `crosscheck/HEK_Complete_Verified.postman_collection.json` to confirm responses match what was
  verified locally.

### 6. Share access
- Give testers the server's address (e.g. `http://192.168.x.x:8080` or an internal DNS name like
  `http://dev-api.company.local`) so they can hit it directly.

## Differences from the Azure plan
- No Azure account, billing, or region decision.
- No DNS/custom-domain/TLS cutover for the 4 legacy hostnames (KARO/ERMS/COL/HISO) — that host-based
  routing (`LegacyHostRoutingMiddleware`) is only needed once real external client traffic must hit
  real legacy hostnames; for internal dev/training testing, calling the server's own address
  directly (or adding a `Host` header, or a hosts-file entry — see `docs/deployment.md`
  §"Local/Postman testing without a real DNS record") is enough.
- No secret hardening at production strength required — just replace the `CHANGE_ME_DEV_ONLY`
  placeholders with values appropriate for a dev/training environment.
