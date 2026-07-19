# KARO — Technology Stack

**Summary:** KARO runs on .NET Framework 4.8 as an ASP.NET Web API 2 application with an OWIN host shim that is never actually configured, backed by ADO.NET/SQL Server stored procedures; the OWIN/Identity/OAuth package set is present but unused.

## Findings

### Runtime / Framework
| Item | Value | Evidence |
|---|---|---|
| Target Framework (HSSWebAPI.csproj) | `v4.8` | `HSSWebAPI.csproj` line 18 |
| Target Framework (DAL.csproj) | `v4.8` | `DAL.csproj` (`grep TargetFrameworkVersion`) |
| Target Framework (Logger.csproj) | `v4.8` | `Logger.csproj` line 12 |
| `httpRuntime targetFramework` | `4.5.2` | `Web.config` line 47 |
| `compilation targetFramework` | `4.8` (debug=`true`) | `Web.config` line 46 |
| Project type | ASP.NET Web Application (Web API 2), `OutputType=Library` | `HSSWebAPI.csproj` lines 13-14 |
| Solution | `HSSWebAPI.sln` referencing HSSWebAPI, DAL(implicit via file reference), Logger(implicit via file reference) | `HSSWebAPI.sln` |

> Note: `<compilation debug="true">` is left enabled in the base `Web.config` (line 46). Debug compilation in a production web app increases attack surface (verbose stack traces) and hurts performance — see `SecurityAnalysis.md`.

### Key NuGet packages (from `packages.config` and `.csproj` references)
| Package | Version | Used? |
|---|---|---|
| Microsoft.AspNet.WebApi / Core / Client / Cors / WebHost / Owin | 5.2.3 / 5.2.6 | Yes — core Web API pipeline |
| Microsoft.Owin, Microsoft.Owin.Host.SystemWeb | 3.0.1 | Host only; `Startup.Configuration()` body is empty (`Startup.cs`) |
| Microsoft.Owin.Security, Security.OAuth, Security.Cookies, Security.Google, Security.Facebook, Security.Twitter, Security.MicrosoftAccount | 3.0.1 | **Referenced but not configured anywhere** — no `UseOAuthBearerAuthentication`, no `UseCookieAuthentication` calls found in `Startup.cs` |
| Microsoft.AspNet.Identity.Core / Owin / EntityFramework | 2.2.1 | **Referenced but no `ApplicationUser`/`UserManager`/`IdentityDbContext` found anywhere in the codebase** — dead reference |
| EntityFramework | 6.1.3 | Referenced (`entityFramework` config section, `DefaultConnectionFactory`) but no `DbContext`/POCO classes were found in the traced code — all data access is raw ADO.NET via `DALHelper`/`DbAccess` |
| Newtonsoft.Json | 13.0.1.0 (binding-redirected from whatever the transitive packages ship) | Yes — all JSON (de)serialization (`APIController.cs`, `EncryptedInt.cs`) |
| Microsoft.AspNet.Mvc, Razor, WebPages, WebPages.Razor, Optimization, WebGrease, Antlr | 5.2.3 / 3.2.3 / 1.1.3 / 1.5.2 / 3.4.1.9004 | Present for `BundleConfig`/legacy MVC scaffolding; no MVC views found — likely default-template leftovers |
| Microsoft.CodeDom.Providers.DotNetCompilerPlatform, Microsoft.Net.Compilers | 1.0.0 | Roslyn compiler shim for older `csc.exe` (Roslyn 1.0-era) — confirms this is an old (~2015-2016) project template |
| bootstrap, jQuery, jQuery.Validation, Modernizr, Respond | 3.0.0 / 1.10.2 / 1.11.1 / 2.6.2 / 1.2.0 | Client-side asset leftovers from default Web API template; no client UI found in this API-only project |

### External/precompiled dependency (no source in repo)
- **AWSDoc.dll / `AWSDoc.IndiciDMS`** — referenced from `DAL\South\HSSDA.cs` (`AWSDoc.IndiciDMS.CheckAWSIsEnabled`, `GetDocumentStatusFromIndici`, `DocumentGetByDocumentKeyJsonResult`). Only the compiled binary (`DevLocal\DAL\bin\Debug\AWSDoc.dll`) is present; no source.
> Unable to verify from available source what AWSDoc does internally (presumably AWS S3-backed document storage for the DMS); its use is confirmed only via call sites in `HSSDA.GetDocuments` (lines 280, 306, 331).

### Data access
- ADO.NET only: `System.Data.SqlClient` (`SqlConnection`, `SqlCommand`, `SqlDataAdapter`, `SqlParameter`) wrapped by a hand-written `DALHelper` (`DAL\HelperClasses\DALHelper.cs`) and `DbAccess` (`DAL\HelperClasses\DbAccess.cs`).
- Database engine: Microsoft SQL Server (connection strings use `Data Source=...\sql2014` and raw IPs — SQL Server 2014-era instance names appear in `Web.config`).

### Build configuration
- Debug and Release `PropertyGroup`s both build to `bin\`; Debug defines `DEBUG;TRACE`, Release defines `TRACE` with `Optimize=true`/`DebugType=pdbonly`. (`HSSWebAPI.csproj` lines 35-51)
- `Web.Debug.config` / `Web.Release.config` present as Web.config transforms (standard XDT transform files) — not fully reviewed line-by-line but confirmed present (`HSSWebAPI.csproj` lines 220-225).
- `packages` restored via classic `packages.config` (not `PackageReference`/SDK-style project) — this is an old-style .csproj, which materially increases migration effort to modern .NET (SDK-style projects, `PackageReference`) since every reference/hint-path is manual XML.

## Risks
- Presence of a full OAuth/Identity/EF stack that is **never invoked** is a strong signal that either (a) an authentication layer was planned and abandoned, or (b) it is inherited from a shared project template across the `MHNPHMP-Integration` solution family and never trimmed. Either way, it is dead weight and a source of false confidence for anyone skimming `packages.config` and assuming auth exists.
- Old-style `.csproj` + `packages.config` + Roslyn 1.0 compiler pin (`Microsoft.Net.Compilers 1.0.0`) indicates the project has not been meaningfully upgraded since its initial creation (~2015/2016 template), despite `TargetFrameworkVersion` being bumped to v4.8 later.

## Recommendations
- When re-platforming, do not carry forward the Identity/OAuth/EF package set unless a genuine business need for it is found elsewhere in the broader solution (outside this repo's scope) — treat it as noise for KARO specifically.
- Confirm whether `AWSDoc.dll` source exists elsewhere in the organization before assuming its behavior; it is a hard external dependency for the AWS-backed document path in `GetDocuments`.
