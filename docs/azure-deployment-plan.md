# Azure Deployment Plan — HEK Core API

## Context

The API is functionally done (60/60 legacy operations verified) and the user wants to move to
deployment next, without waiting on the two open decisions (Azure region, Microsoft Entra ID
account) — those are being treated as "switch later" items, not blockers. The goal of this task is
a written deployment plan **and** a separate checklist to run through step-by-step at actual
deploy time. The repo already has most of the groundwork done (Dockerfile, docker-compose,
DB-driven connection registry, `docs/deployment.md` with a host-routing checklist) — this plan
builds on that rather than redesigning it.

## What already exists (reuse, don't rebuild)

- **Dockerfile**: `src/Api/Dockerfile` — multi-stage (SDK 8.0 → aspnet 8.0), non-root user, `EXPOSE 8080`, entrypoint `dotnet HekCoreApi.Api.dll`. Deploy-ready as-is.
- **docker-compose.yml** / **docker-compose.override.yml** (root) — proven working end-to-end as of 2026-07-30 (`docker compose up -d --build`).
- **Connection-string registry**: DB-driven per-practice connections (`src/Infrastructure/Routing/TenantRegistryService.cs` + `TenantRegistryDbContext`) — already reviewed/approved, no redeploy needed to add a practice.
- **Secrets abstraction**: `ISecretProvider` / `EnvironmentVariableSecretProvider` (`src/Infrastructure/Secrets/`) — secrets come from env vars, not hardcoded (except one documented legacy Rijndael key exception).
- **Host-based routing for 4 legacy domains on 1 Azure app**: `LegacyHostRoutingMiddleware` — fully documented in `docs/deployment.md` §"Legacy host-based routing" with its own 5-step DNS/custom-domain/TLS/rule-check/smoke-test checklist. This plan's checklist absorbs that section rather than duplicating it differently.
- **Health checks**: `GET /health` (liveness), `GET /health/ready` (readiness incl. SQL dependency).
- **CI**: `.github/workflows/ci.yml` — build+test only, **no CD/deploy pipeline exists yet**.

## Target: Azure Container Apps

Recommended (matches prior guidance already given to the user) because: the app is stateless
(confirmed in `docs/deployment.md` — in-memory idempotency/cache is per-instance but swappable),
already containerized, and Container Apps gives auto-scaling without managing VMs. Region left as
a placeholder value (`<TBD>`) since the user said it's not a blocker — plan uses whatever default
region is chosen at deploy time and documents it as a one-line config change to move later.

## Plan of work

### 1. Pre-deployment hardening (must happen before any real traffic hits it)
- Replace the three dev-only placeholder secrets currently in committed `src/Api/appsettings.json`
  (`ConnectionStrings:TenantRegistry` password, `Auth:JwtSigningKey`, `Legacy:ConnMHNPMS`/`ConnMHNDMS`/`DbCredentials:localhost` — all currently `CHANGE_ME_DEV_ONLY`) with real values supplied **only** as Azure Container Apps secrets/env vars, never committed.
- Decide `Auth:Enabled` value for this deploy (currently defaults `false` — legacy-equivalent auth mode). Since Entra isn't ready, plan assumes it stays `false` for this deploy and gets flipped on later as a config-only change once the Entra App Registration exists — no code change needed then.

### 2. Provision Azure resources (one-time setup)
- Resource group, Azure Container Registry (to push the built image), Azure Container Apps environment, and the Container App itself.
- Azure SQL (or existing SQL Server reachable from Azure) for the `HekTenantRegistry` DB — confirm network reachability (VPN/private endpoint/firewall rule) from Container Apps to wherever the legacy practice DBs live, since the app makes outbound SQL calls per practice at request time.

### 3. Build & push image
- `docker build` from `src/Api/Dockerfile`, tag, push to Azure Container Registry.
- No Dockerfile changes needed — reuse as-is.

### 4. Deploy to Container Apps
- Create/update the Container App from the pushed image, wire in env vars/secrets from step 1, set `ASPNETCORE_ENVIRONMENT=Production`.
- Configure ingress on port 8080 (matches `EXPOSE 8080`).
- Set scaling rules (min/max replicas — since stateless, safe to scale >1).

### 5. Legacy host-domain cutover (KARO/ERMS/COL/HISO)
- Follow the existing 5-step checklist already written in `docs/deployment.md` §"Deployment checklist (do this once per environment)" verbatim: DNS repoint → Azure custom domain binding → TLS cert per hostname → confirm `LegacyHostRouting:Rules` substrings → smoke test each real hostname from outside Azure.
- This is the actual go-live moment — until DNS is repointed, old traffic keeps hitting the old legacy servers, so this step is reversible/safe to rehearse.

### 6. Post-deploy verification
- Hit `/health` and `/health/ready` from outside Azure.
- Run a subset of the 60 verified operations from `crosscheck/HEK_Complete_Verified.postman_collection.json` against the real deployed URL (swap `newBase` in the Postman environment to the Azure hostname) to confirm parity holds in the real environment, not just locally.
- Confirm OpenTelemetry/logging is flowing (either wire an `aspire-dashboard` equivalent in Azure, e.g. Application Insights, or confirm Serilog file/console output is being captured by Azure's log stream — this is a gap not yet resolved in current docs, flagged as a decision point in the checklist).

### 7. Known open gaps to carry into this deploy (not blockers, but should be tracked)
- No compose-level healthcheck for `api` service (local-only gap, doesn't block Azure deploy since Container Apps has its own health probes).
- No scale-out documentation beyond "the app is stateless" — fine for first deploy at low replica count, revisit if scaling issues appear.
- ERMS Azure-forwarding path gap (mentioned in `docs/PROJECT_MASTER.md`) — small, scoped, low priority, doesn't block this deployment.
- Entra ID auth stays off for this deploy (see step 1) — flip on later, config-only.

## Deliverables of this planning task
1. This plan file.
2. A separate deployment-day checklist file (checkboxes, sequential) — to be created at
   `docs/azure-deployment-checklist.md` in the repo so it's checked into version control and usable
   live during the actual deployment session. Content = steps 1–6 above condensed into literal
   check-items, plus the existing host-routing 5-step checklist folded in as its own section
   (reused verbatim from `docs/deployment.md`, not rewritten).

## Verification
- No code changes in this task — it's planning only. "Verification" here means: the checklist file
  is reviewed against `docs/deployment.md` and `docker-compose.yml`/`src/Api/Dockerfile` for
  accuracy before being treated as authoritative, and the user confirms the plan matches their
  intent before actual deployment work begins.
