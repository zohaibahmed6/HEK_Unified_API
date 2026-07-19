# ERMS Web API — Technology Stack

## Summary
ERMS is built on .NET Framework 4.8 using ASP.NET Web API 2 with OWIN self-host wiring, ADO.NET/stored-procedure data access, and a mix of XML and JSON serialization across its two controllers.

## Findings

### Target Framework & Runtime
- `ERMSWebAPI.csproj`: `<TargetFrameworkVersion>v4.8</TargetFrameworkVersion>`.
- `Web.config` `<system.web><compilation debug="true" targetFramework="4.8"/><httpRuntime targetFramework="4.6.1" maxRequestLength="10240" executionTimeout="300"/>` — **inconsistency**: `compilation` targets 4.8, `httpRuntime` targets 4.6.1. `debug="true"` is left on (typically a stage/dev artifact that should not ship to production).
- `DAL.csproj` and `Logger.csproj` are separate class-library projects referenced by the Web API project (binary `HintPath` references to `..\..\DAL\bin\Debug\DAL.dll`, i.e., **project output is referenced by path, not by MSBuild project reference** — evidence: `ERMSWebAPI.csproj` `<Reference Include="DAL, Version=1.0.0.7, ...><HintPath>..\..\DAL\bin\Debug\DAL.dll</HintPath>`). This means the web project always consumes whatever DLL happens to be in `DAL\bin\Debug`, a fragile build/deploy setup.
- `maxRequestLength="10240"` (10 MB) caps request size — relevant to `SaveDocument`/`SaveInvoice` payloads (base64 documents).
- `executionTimeout="300"` (5 minutes) — generous timeout, consistent with synchronous stored-procedure-bound calls.

### Web Framework
- ASP.NET Web API 2.2 (`Microsoft.AspNet.WebApi` 5.2.3, `Microsoft.AspNet.WebApi.Core`/`Client`/`Cors`/`Owin`/`WebHost` 5.2.3–5.2.6) — evidence: `packages.config`.
- OWIN self-host wiring present (`Microsoft.Owin.Host.SystemWeb` 3.0.1, `[assembly: OwinStartup(typeof(ERMSWebAPI.Startup))]` in `Startup.cs`) but the `Configuration(IAppBuilder app)` method body is **empty** — no middleware is actually registered (no OAuth, no CORS-via-OWIN, no Identity). This is dead/vestigial scaffolding.
- Routing: attribute routing is enabled (`config.MapHttpAttributeRoutes()`) plus a conventional route `{controller}/{action}/{id}` (`App_Start/WebApiConfig.cs`). Controllers use `[AcceptVerbs("GET"/"POST")]` rather than `[HttpGet]`/`[HttpPost]` attributes.
- A separate legacy MVC-style `RouteConfig`/`FilterConfig` (`System.Web.Mvc`) is also present, pointing at a `Home/Index` controller that does not exist in the read source tree — likely unused scaffolding left over from the Visual Studio Web API project template.

### Data Access
- ADO.NET direct (`System.Data.SqlClient`) via a hand-written data-access helper (`DAL/HelperClasses/DALHelper.cs`, `DbAccess.cs`, `DALHelperParameterCache.cs`) — **not** Entity Framework, despite EF6 being referenced.
- `EntityFramework` 6.1.3 and `EntityFramework.SqlServer` are referenced in both `Web.config` (`<entityFramework>` section, `<defaultConnectionFactory>` pointed at `mssqllocaldb`) and `ERMSWebAPI.csproj`, but no `DbContext`/EF model classes were found in the read scope — this is unused surface area, likely pulled in transitively by `Microsoft.AspNet.Identity.EntityFramework`.
- All data access observed in `HSSDA.cs` (South) and `PHCO.cs` (Pegasus) calls **stored procedures exclusively** via `DALHelper.ExecuteDataTable`/`ExecuteNonQuery`/`ExecuteDataset` with `CommandType.StoredProcedure` and `SqlParameter` objects.

### Serialization
- `Newtonsoft.Json` 13.0.1 (packages.config lists 13.0.1.0; a runtime binding redirect in `Web.config` also targets 13.0.0.0) — used by `COLController` for all requests/responses, and by `Models/EncryptedInt.cs`'s custom `JsonConverter`.
- `System.Xml.Serialization` (`XmlSerializer`) — used by `APIController` for `Credential`, `ReferralDocument`, and all HISO-concept response DTOs in `Models/APIModels.cs`.

### Authentication / Identity packages (referenced but not wired up)
- `Microsoft.AspNet.Identity.Core` / `.Owin` / `.EntityFramework` 2.2.1
- `Microsoft.Owin.Security` / `.Cookies` / `.OAuth` / `.Facebook` / `.Google` / `.MicrosoftAccount` / `.Twitter` 3.0.1
- None of these are configured anywhere in the read source (`Startup.cs` is empty, no `OAuthAuthorizationServerOptions`, no `IdentityDbContext`). See AuthenticationAuthorization.md.

### CORS
- `Microsoft.AspNet.Cors` 5.2.6, `Microsoft.Owin.Cors` 4.0.0, `Microsoft.AspNet.WebApi.Cors` / `System.Web.Http.Cors` 5.2.6 referenced.
- `APIController` has CORS commented out (`//[EnableCors(origins: "*", headers: "*", methods: "*")]`).
- `COLController` has it **active**: `[EnableCors(origins: "*", headers: "*", methods: "*")]`.
- `config.EnableCors()` in `WebApiConfig.cs` is commented out — CORS is controlled per-controller only.

### Other libraries
- `PDFsharp` 1.50.5147 (in `DAL/packages.config`) — used in `DAL/DMS/DMSDA.cs` (imaging/PDF handling for the DMS document module; not reachable from ERMS controllers directly, but ships in the same DAL).
- `AWSDoc` (custom internal assembly, referenced by `HintPath` to `bin\Debug\AWSDoc.dll`) — AWS-backed document storage abstraction used from `HSSDA.cs` (`AWSDoc.IndiciDMS.CheckAWSIsEnabled`) and `DMSAWS/DMSAWS.cs`.
- `NLog` referenced in `DMSAWS/DMSAWS.cs` — a **second, unrelated logging framework** coexisting with the custom `Logger` project; not used elsewhere in the read scope.
- `Antlr` 3.4.1.9004, `bootstrap` 3.0.0, `jQuery` 1.10.2, `Modernizr`, `Respond`, `WebGrease` — all standard ASP.NET Web API project-template front-end/build artifacts (Web API "Help Page" scaffolding), not part of the API surface itself.

### Build configuration
- `Web.Debug.config` / `Web.Release.config` transform files present (standard config transforms).
- `<compilation debug="true">` left enabled in the base `Web.config` (see above) — should be verified per-environment via the transforms; base file being debug-on is a minor operational risk if a transform is missed.

## Evidence
- `E:\NZTFS\ermsapi\DevLocal\ERMSWebAPI\ERMSWebAPI\ERMSWebAPI.csproj`
- `E:\NZTFS\ermsapi\DevLocal\ERMSWebAPI\ERMSWebAPI\packages.config`
- `E:\NZTFS\ermsapi\DevLocal\ERMSWebAPI\ERMSWebAPI\Web.config`
- `E:\NZTFS\ermsapi\DevLocal\ERMSWebAPI\ERMSWebAPI\Startup.cs`
- `E:\NZTFS\ermsapi\DevLocal\ERMSWebAPI\ERMSWebAPI\App_Start\WebApiConfig.cs`, `RouteConfig.cs`, `FilterConfig.cs`
- `E:\NZTFS\ermsapi\DevLocal\DAL\DAL.csproj`, `E:\NZTFS\ermsapi\DevLocal\DAL\packages.config`
- `E:\NZTFS\ermsapi\DevLocal\DAL\HelperClasses\DALHelper.cs`, `DbAccess.cs`
- `E:\NZTFS\ermsapi\DevLocal\DAL\DMSAWS\DMSAWS.cs` (NLog usage)

## Risks
- Referencing DLLs by relative `bin\Debug` `HintPath` instead of MSBuild project references risks building against stale/wrong DAL binaries in CI or on a clean machine.
- Unused EF/Identity/OAuth package surface adds attack surface and confuses readers of the codebase into thinking auth is framework-managed when it is not.
- Mixed targetFramework declarations (4.8 vs 4.6.1) can cause subtle runtime behavior differences and should be reconciled before any further work on the legacy app.
- `debug="true"` compilation flag risks stack traces / debug info leaking in error responses if not overridden per environment.

## Recommendations
- For the unified platform, treat ERMS's actual dependency footprint as: ASP.NET Web API 2 (attribute + convention routing), ADO.NET stored-procedure access, Newtonsoft.Json, XmlSerializer, RijndaelManaged (to be replaced). Ignore the unused EF/Identity/OAuth/MVC/NLog surface as template noise — do not carry it forward.
- Migrate data access to a modern ORM or Dapper-based repository layer with proper connection lifetime management (see Architecture.md, DatabaseAnalysis.md for the `DbAccess` static-state concern).
- Replace custom RijndaelManaged ID obfuscation with either opaque server-side session state or properly managed encryption (KMS/DPAPI-successor) — see SecurityAnalysis.md.
