# ERMS Web API — Executive Summary

## System Overview

ERMS Web API (namespace `ERMSWebAPI`, solution `ERMSWebAPI.sln`) is a legacy ASP.NET Web API 2 (OWIN self-host over IIS, .NET Framework 4.8) service that exposes patient clinical data from the "indici" Practice Management System (PMS) to two consumers:

1. **ERMS** (Electronic Request Management System — South Island NZ eReferrals platform) via `Controllers/APIController.cs`, an XML-based API (23 actions) that mirrors the HISO Concepts data-exchange standard.
2. **A second, entirely undocumented integration** (internally called "COL" / Pegasus / "PHCO") via `Controllers/COLController.cs`, a JSON-based API (7 actions) that exposes patient/session/provider/surgery/diagnosis data and an invoicing write-back (`SaveInvoice`) to what the code strongly implies is a Pegasus Health / claiming system.

Both controllers sit on top of a shared `DAL` project (stored-procedure-only data access against SQL Server, split by `practiceid`-suffixed connection strings) and a shared flat-file `Logger` project. The DAL also contains dormant integration code for GP2GP, MHNHL7 (HL7 messaging), MHNAppointment, Procare, Procon, BPAC, BPI, Screening, and DMS/DMSAWS (AWS-backed document storage) — none of which are called from the two ERMS controllers, but all of which ship in the same deployable and will need a decision (retain/retire) during consolidation.

Authentication is a bespoke scheme: username/password (or patient/encounter context) is POSTed to `/api/authenticate` or `/col/authenticate`, validated against a stored procedure (`uspInsertAndValidateToken`), and a GUID token + expiry is returned; the token is then passed as a raw Bearer-style header on every subsequent call and re-validated against the same stored procedure on every request. Patient/Encounter identifiers are additionally obfuscated in the URL using a hand-rolled AES(Rijndael)-based encoder with a **key hard-coded in source** (`Models/EncryptionManager.cs`).

## Technology Stack Summary

- .NET Framework 4.8 (project targets both `v4.8` and `4.6.1` inconsistently — see TechnologyStack.md), ASP.NET Web API 2.2 / OWIN self-host, System.Web (IIS-hosted, not self-contained).
- Data access: ADO.NET + stored procedures only (no LINQ-to-SQL/EF runtime usage in the request path, despite EF6/Identity/OAuth packages being referenced but unused).
- Serialization: `System.Xml.Serialization` (APIController) and `Newtonsoft.Json` 13.0.1 (COLController).
- Custom crypto: `System.Security.Cryptography.RijndaelManaged` with a hard-coded 256-bit key.
- Logging: bespoke flat-file logger (`Logger` project), no structured logging, no correlation IDs.

## Architecture Style

Monolithic 3-tier ASP.NET Web API application: **Controllers → DAL (static classes + stored procs) → SQL Server**, with a thin cross-cutting `Logger` project. Not layered with interfaces/DI; everything is static classes and static method calls (`HSSDA.GetX(...)`, `PHCO.GetX(...)`, `DALHelper.ExecuteX(...)`). No repository/service abstraction, no unit tests found in the read scope.

## Counts (from evidence gathered)

| Metric | Count | Evidence |
|---|---|---|
| Controllers | 2 | `APIController.cs`, `COLController.cs` |
| HTTP endpoints (actions) | 30 | 23 in APIController + 7 in COLController (see EndpointInventory.md) |
| Business rules extracted | 20 (catalogued) | See BusinessRules.md |
| Distinct DAL classes touched by ERMS controllers | 2 directly (`HSSDA`, `PHCO`/`PHCO.cs`→`PHCO` class in `DAL.Pegasus`), 12 more present in DAL project but not called by these controllers | See DatabaseAnalysis.md, DependencyAnalysis.md |
| Distinct stored procedures called by `HSSDA`/`PHCO` | ~35 (`uspGet...`, `uspInsert...`, `uspUpdate...`) | grep of `South/HSSDA.cs`, `Pegasus/PHCO.cs` |
| Connection strings defined | 15 (paired Indici/DMS per practice id) | `Web.config` |

## Top 5 Security Issues

1. **Hard-coded plaintext database credentials in `Web.config`** for 15 connection strings, several pointing at a public IP (`43.255.162.58`), with authentication mode `None`. (Critical)
2. **Hard-coded, static AES key in `Models/EncryptionManager.cs`** used to obfuscate Patient/Encounter IDs carried in URLs — reversible by anyone with the compiled assembly; the encoding is used as a pseudo-access-control mechanism, not real encryption. (Critical)
3. **No real authentication/authorization framework** — despite referencing Microsoft.AspNet.Identity/OWIN OAuth packages, `Startup.Configuration()` is empty; auth is a custom GUID-token-per-stored-procedure scheme with no scopes/roles/claims. (High)
4. **Wildcard CORS** (`[EnableCors(origins: "*", headers: "*", methods: "*")]`) on `COLController`, which also handles a financial write endpoint (`SaveInvoice`). (High)
5. **Sensitive data (Bearer tokens, patient/encounter identifiers) written to plaintext log files** with no redaction, no rotation/retention policy, no access control on the `Logs\` directory. (High)

## Top 5 Risks (for migration)

1. **.NET Framework 4.8 / ASP.NET Web API 2 / OWIN** is a dead-end stack — full rewrite (not "port") is required to reach modern .NET.
2. **Undocumented API surface**: the entire `COLController` (7 endpoints, including a financial `SaveInvoice` write) has zero coverage in `ERMS_doc.md` — behavior must be reverse-engineered from code and validated with the business before migration.
3. **Practice routing logic is entangled in the controller layer** via manual string-splitting of `EncounterId` (`"_"`/`"__"` delimiters) to recover practice id and select a connection string — this is undocumented, fragile, and duplicated near-identically in all 30 actions.
4. **Custom encryption is used as an access-control primitive** for patient/encounter IDs; if it is dropped or changed incompatibly during migration, existing ERMS/Pegasus front-end integrations will break silently.
5. **Reflection-heavy, convention-based DataTable→object mapping** (`ERMSDataTableToListHiso<T>`, using a `"|&|"` / `"|?|"` delimited string protocol inside SQL result columns) is a brittle, undocumented contract between stored procedures and the API layer.

## Overall Recommendation

ERMS should be treated as **the smallest and most self-contained of the three legacy APIs** in terms of endpoint count, but it carries disproportionate security debt (hard-coded secrets, home-grown crypto, no framework-level auth) and hides a second, undocumented API surface (COL/Pegasus) that must be scoped with the business before consolidation. For the unified platform, ERMS's *data contract* (the HISO-concept XML shapes and the JSON Pegasus shapes) should be preserved as external-facing schemas, but its *implementation* (auth, encryption, connection-string-per-practice routing, static DAL) should not be ported — it should be rebuilt against the unified platform's real auth (OAuth2/OIDC), secrets management, and multi-tenant data access patterns identified in the cross-API Phase 4 comparison.
