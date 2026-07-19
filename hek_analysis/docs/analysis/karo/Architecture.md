# KARO — Architecture

**Summary:** KARO is a two-project, two-tier monolith — a single fat Web API controller calling a static, procedure-oriented data-access class directly, with no service layer, no DI, and a shared DAL library that carries substantial code unrelated to KARO itself.

## Findings

### Architecture style
**Fat Controller + Static DAL**, not a layered/onion/clean architecture. There is:
- No interface/abstraction layer between `APIController` and `HSSDA` (`HSSDA` methods are `public static`, called directly: `Controllers\APIController.cs` — e.g. `HSSDA.InsertAndValidateToken(...)`, `HSSDA.GetConsultNotes(...)`).
- No dependency injection container (no `IUnityContainer`, `IDependencyResolver`, `Autofac`, etc. found).
- No repository/unit-of-work pattern — `HSSDA` methods build `SqlParameter` lists and call `DALHelper.ExecuteDataTable`/`ExecuteNonQuery` inline per method (`DAL\South\HSSDA.cs`, e.g. lines 118-146).
- No domain model — all data flows as `DataTable`/`DataSet` from ADO.NET, mapped to plain DTOs in `Models\APIModels.cs` via a generic reflection-based mapper (`Utility.Instance.DataTableToList<T>`, referenced throughout `APIController.cs`, e.g. line 202).
- No unit tests, integration tests, or test project found anywhere in the repository (`find ... -name "*.cs"` across the solution surfaced none).

### Folder structure
```
DevLocal/
  HSSWebAPI/                 <- ASP.NET Web API 2 presentation project
    App_Start/                  WebApiConfig, FilterConfig, RouteConfig, BundleConfig (mostly default-template boilerplate)
    Controllers/                APIController.cs (2047 lines, 24 actions), Enums.cs (unused CallType enum)
    Models/                      APIModels.cs (DTOs), EncryptedInt.cs, EncryptionManager.cs
    Global.asax / Global.asax.cs, Startup.cs (OWIN entry point, empty), Web.config
  DAL/                       <- shared data-access library (separate project, separate .dll)
    South/HSSDA.cs               <- the ONLY module actually called by APIController (1991 lines... wait 1435 — see TechnologyStack)
    BPAC/BPACDA.cs, BPI/BPIDA.cs, DMS/DMSDA.cs, GP2GP/PIDAO.cs, MHNAppointment/MHNAppointmentDA.cs,
    MHNHL7/DBMessages.cs, Pegasus/PHCO.cs, Procare/ProcareApiDA.cs, Procon/ProconApiDA.cs,
    Screening/ScreeningDAL.cs, UI/GUIDA.cs   <- NOT referenced by HSSWebAPI's controller (see Dependency Flow below)
    HelperClasses/DALHelper.cs, DALHelperParameterCache.cs, DbAccess.cs  <- shared ADO.NET plumbing
  Logger/                    <- shared flat-file logging library (separate project, separate .dll)
    Logging.cs, Utility.cs, TypeEnums.cs
```

### Dependency flow
`HSSWebAPI.csproj` references `DAL.dll` and `Logger.dll` as **precompiled binary references** (`HintPath`, not project references): `HSSWebAPI.csproj` lines 53-59 —
```
<Reference Include="DAL, Version=1.0.0.7, ...">
  <HintPath>..\DAL\bin\Debug\DAL.dll</HintPath>
</Reference>
<Reference Include="Logger">
  <HintPath>..\..\MHNPHMP-Integration\Logger\bin\Debug\Logger.dll</HintPath>
</Reference>
```
The `Logger` hint path (`..\..\MHNPHMP-Integration\Logger\bin\Debug\Logger.dll`) points **outside the current repository root**, confirming this repo is a subset/extraction of a larger multi-application solution (`MHNPHMP-Integration`) that shares `DAL` and `Logger` across multiple systems.

Within the traced call graph:
- `APIController.cs` → `DAL.HSSDA` (static calls, ~35 methods) — the only DAL class used.
- `APIController.cs` → `HSSWebAPI.Models.EncryptionManager` / `EncryptedInt` (patient/encounter ID obfuscation).
- `APIController.cs` → `Logger.Logging.Instance` / `TypeEnums.LogType` (event/exception logging).
- `DAL.HSSDA` → `AWSDoc.IndiciDMS` (precompiled external dependency, AWS-backed document store; see `TechnologyStack.md`).
- `DAL.HSSDA` → `DAL.HelperClasses.DALHelper` (ADO.NET execution helper).

No call sites for `BPACDA`, `BPIDA`, `DMSDA`, `PIDAO`, `MHNAppointmentDA`, `DBMessages`, `PHCO`, `ProcareApiDA`, `ProconApiDA`, `ScreeningDAL`, or `GUIDA` were found in `Controllers\APIController.cs` (confirmed via grep for `DMSDA\.|DBMessages\.|BPACDA\.|BPIDA\.|ProcareApiDA\.|ProconApiDA\.|MHNAppointmentDA\.|PHCO\.|PIDAO\.|ScreeningDAL\.|GUIDA\.` — the only hit was a single commented-out line, `Controllers\APIController.cs` line 1342: `//    dsRetinalLookUp = BPACDA.CFRetinalLookUp(connectionString);`). These 11 DAL modules are compiled into the shared `DAL.dll` that KARO references but are not exercised by KARO's own code path.

### Shared libraries
- **DAL.dll** — shared across (at minimum) KARO and other unseen systems in the `MHNPHMP-Integration` family; multi-database (12 distinct connection-string names referenced across the DAL, see `DatabaseAnalysis.md`).
- **Logger.dll** — shared flat-file logger; simple singleton (`Logging.Instance`), file-based, no abstraction/interface, used identically by both `HSSWebAPI` and `DAL` projects (`DAL\South\HSSDA.cs` calls `Logging.Instance.WriteEventLog(...)` directly, e.g. line 264).

### Cross-cutting concerns
| Concern | Implementation | Evidence |
|---|---|---|
| Logging | Static singleton, flat text files per day, no correlation ID | `Logger\Logging.cs` |
| Error handling | Try/catch per action, swallows to `error` string returned in JSON body; global `HandleErrorAttribute` registered for MVC filters only, not Web API exception filters | `App_Start\FilterConfig.cs`; every `APIController` action |
| Authentication | Custom bearer-token string validated inside a stored procedure (`[HSS].[uspInsertAndValidateToken]`), not ASP.NET Identity/OWIN despite those packages being referenced | `Controllers\APIController.cs` lines 1976-1987; `DAL\South\HSSDA.cs` lines 795-859 |
| CORS | Wide open at controller level | `Controllers\APIController.cs` line 22 |
| Configuration | `Web.config` `connectionStrings`/`appSettings`, `ConfigurationManager` used directly throughout DAL (no `IOptions`/config abstraction) | `Web.config`; `DAL\South\HSSDA.cs` (every method builds `connectionString` via `ConfigurationManager.ConnectionStrings[...]`) |
| Multi-tenancy | Ad-hoc: tenant/practice suffix parsed out of the client-supplied `encounterId` string and appended to a connection-string *name* (`"ConnIndiciDB" + practiceid`) | `Controllers\APIController.cs` (every action, e.g. lines 42-60); `DAL\South\HSSDA.cs` (every method, e.g. line 813) |

## Risks
- **No abstraction boundary** between HTTP concerns and data access means any framework migration (e.g., to ASP.NET Core / minimal APIs) requires touching business logic embedded directly in the controller (string parsing of `encounterId`, JSON envelope construction, error-message strings) rather than swapping a clean interface implementation.
- **Binary-reference coupling** (`DAL.dll`/`Logger.dll` via `HintPath`, not project references) means the actual behavior of KARO depends on whatever was last built into those DLLs, and the `Logger` reference path already points outside this repo — the true "source of truth" for Logger may not be fully captured in `E:\NZTFS\hsswebapi`.
- **Bundling 11 unrelated DAL subsystems into one deployable** increases the blast radius of any DAL change (e.g., the SQL-injectable methods in `DMSDA.cs`/`DBMessages.cs`, see `SecurityAnalysis.md`) even though KARO itself never calls them.

## Recommendations
- When decomposing into the unified platform, treat `HSSDA` (South) as KARO's actual data-access surface and treat the other 11 DAL modules as **out of scope** for KARO specifically — confirm with the team whether they belong to HISO/ERMS or a fourth, undocumented system before porting them anywhere.
- Introduce a real service/interface boundary (even a thin one) during rewrite so that the multi-tenant connection-string-name convention and the custom bearer-token validation can be replaced without touching HTTP-layer code.
