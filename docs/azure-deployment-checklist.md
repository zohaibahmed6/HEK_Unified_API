# Azure Deployment Checklist

Use this checklist live during the actual deployment session — check items off in order. See
`docs/deployment.md` for full technical background on each piece.

## 1. Pre-deployment hardening
- [ ] Replace `ConnectionStrings:TenantRegistry` password (currently `CHANGE_ME_DEV_ONLY` in `src/Api/appsettings.json`) with a real value, set only as an Azure Container Apps secret/env var — never commit it.
- [ ] Replace `Auth:JwtSigningKey` (currently `DEV-ONLY-INSECURE-SIGNING-KEY-...`) the same way.
- [ ] Replace `Legacy:ConnMHNPMS` / `Legacy:ConnMHNDMS` / `DbCredentials:localhost` (all `CHANGE_ME_DEV_ONLY`) the same way.
- [ ] Confirm `Auth:Enabled` is set to `false` for this deploy (Entra ID isn't ready yet — flip on later as a config-only change, no code change needed).

## 2. Provision Azure resources (one-time)
- [ ] Create resource group.
- [ ] Create Azure Container Registry (ACR).
- [ ] Create Azure Container Apps environment.
- [ ] Create the Container App (image comes from step 3).
- [ ] Confirm/provision the SQL database for `HekTenantRegistry`.
- [ ] Confirm network reachability from Container Apps to wherever the legacy practice DBs live (VPN / private endpoint / firewall rule as needed).

## 3. Build & push image
- [ ] `docker build` from `src/Api/Dockerfile`.
- [ ] Tag the image.
- [ ] Push to ACR.

## 4. Deploy to Container Apps
- [ ] Create/update the Container App from the pushed image.
- [ ] Set env vars/secrets from step 1.
- [ ] Set `ASPNETCORE_ENVIRONMENT=Production`.
- [ ] Configure ingress on port 8080.
- [ ] Set scaling rules (min/max replicas).

## 5. Legacy host-domain cutover (KARO / ERMS / COL / HISO)
*(reused verbatim from `docs/deployment.md` §"Deployment checklist")*
- [ ] **DNS**: for each real hostname (`hss.itsmyhealth.nz`, `southerms.indici.nz`, `hiso.itsmyhealth.nz`, and their `dev*` equivalents), point the DNS record at this app's Azure hostname.
- [ ] **Azure custom domain binding**: add each hostname as a custom domain on this app (Azure verifies ownership via TXT/CNAME).
- [ ] **TLS certificate per hostname**: bind a cert for each custom domain (Azure App Service Managed Certificate is simplest — free, auto-renewing).
- [ ] **Confirm `LegacyHostRouting:Rules`** in `appsettings.json` still has the right `HostContains` substrings for the real hostnames in this environment.
- [ ] **Smoke test after cutover**: call each real hostname's real legacy path (`/api/Ping`, `/api/Authenticate`, `/COL/Authenticate`) from *outside* Azure and confirm a 200-with-real-body response, not a 404.

## 6. Post-deploy verification
- [ ] `GET /health` returns OK from outside Azure.
- [ ] `GET /health/ready` returns OK from outside Azure (includes SQL dependency check).
- [ ] Point `crosscheck/HEK_4APIs.postman_environment.json`'s `newBase` at the real Azure hostname and re-run a subset of `crosscheck/HEK_Complete_Verified.postman_collection.json` to confirm parity holds in the real environment.
- [ ] Confirm logging/telemetry is flowing in Azure (Application Insights or equivalent — not yet wired for Azure specifically, decide approach here).

## 7. Known gaps carried into this deploy (not blockers, just tracked)
- [ ] No compose-level healthcheck for `api` service — doesn't block Azure deploy (Container Apps has its own health probes).
- [ ] No scale-out documentation beyond "app is stateless" — fine at low replica count for first deploy.
- [ ] ERMS Azure-forwarding path gap (see `docs/PROJECT_MASTER.md`) — small, scoped, low priority.
- [ ] Entra ID auth stays off for this deploy — revisit once a Microsoft/Azure account is confirmed and an App Registration exists.
