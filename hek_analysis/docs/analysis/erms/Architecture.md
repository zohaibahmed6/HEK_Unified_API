# ERMS Web API — Architecture

## Summary
ERMS is a monolithic, statically-coupled 3-tier ASP.NET Web API application (Controllers → static DAL classes → SQL Server stored procedures) with no dependency injection, no service/repository abstraction, and cross-cutting concerns (logging, encryption, connection-string selection) implemented as static helper calls duplicated in nearly every controller action.

## Findings

### Architecture style
- **Style:** N-Tier / Transaction Script, not layered-with-abstractions. Controllers call static DAL classes directly (`HSSDA.GetPatientData(...)`, `PHCO.GetCurrentPatientData(...)`); DAL classes call `DALHelper` static ADO.NET wrappers directly. There are no interfaces, no DI container, no repository pattern, no CQRS/mediator.
- Each controller action is a self-contained "transaction script": decode IDs → determine practice id/connection string → validate token → call one DAL method → map DataTable to response DTO → serialize → return. The same ~25-line boilerplate (base64 decode, `"_"`/`"__"` split for practice id, `EnableAzureERMSAPI` proxy check, decrypt, token validation, try/catch, `WriteLog`) is repeated verbatim in all 23 `APIController` actions and, in a slightly reduced form, in all 7 `COLController` actions. Evidence: `Controllers/APIController.cs` lines 125-1666 (repeated pattern), `Controllers/COLController.cs` lines 94-423.

### Folder structure
```
DevLocal/
  DAL/                               (class library: DAL.dll)
    BPAC/BPACDA.cs                   (BPAC integration - not called by ERMS controllers)
    BPI/BPIDA.cs                     (BPI integration - not called by ERMS controllers)
    DMS/DMSDA.cs                     (Document Mgmt System, PDF handling)
    DMSAWS/DMSAWS.cs                 (AWS-backed document storage, namespace MHN.DAL.DMSAWS)
    GP2GP/PIDAO.cs                   (GP2GP - not called by ERMS controllers)
    HelperClasses/                   (DALHelper.cs, DbAccess.cs, DALHelperParameterCache.cs)
    MHNAppointment/MHNAppointmentDA.cs
    MHNHL7/DBMessages.cs             (HL7 messaging - contains string-concatenated SQL, see SecurityAnalysis.md)
    Pegasus/PHCO.cs                  (used by COLController)
    Procare/ProcareApiDA.cs
    Procon/ProconApiDA.cs
    Screening/ScreeningDAL.cs
    South/HSSDA.cs                   (used by APIController - the "HISO/South" data access class)
    UI/GUIDA.cs
  ERMSWebAPI/ERMSWebAPI/             (web project: ERMSWebAPI.dll, IIS-hosted)
    App_Start/                       (WebApiConfig, RouteConfig, FilterConfig, BundleConfig)
    Controllers/APIController.cs     (23 actions, XML, HISO concepts)
    Controllers/COLController.cs     (7 actions, JSON, Pegasus/"COL")
    Helpers/ERMSAPIProxy.cs          (forwards requests to an "Azure ERMS API" mirror)
    Models/APIModels.cs              (1557 lines of XML-serializable DTOs, HISO concept mirror)
    Models/EncryptedInt.cs           (implicit-conversion wrapper type for obfuscated IDs)
    Models/EncryptionManager.cs      (custom Rijndael-based ID obfuscation)
    Pegasus_Models/PegasusAPIModel.cs
    Startup.cs                       (OWIN entry point, empty)
    Web.config                       (connection strings, app settings)
  Logger/                            (class library: Logger.dll)
    Logging.cs, Utility.cs, TypeEnums.cs
```
Evidence: directory listing via `find` on `E:\NZTFS\ermsapi\DevLocal`.

### Dependency flow
`ERMSWebAPI` project → references `DAL.dll` (by `HintPath`, not project reference) and `Logger` (namespace `Logger`, referenced the same way based on `using Logger;` in controllers/helpers). `DAL` project → references `Logger` (`using Logger;` in `South/HSSDA.cs`), an internal `AWSDoc` assembly, `PdfSharp`, and (in `DMSAWS/DMSAWS.cs`) `MHN.DAL.Extentions`/`MHN.Entity.Inbox`/`NLog` — indicating the `DMSAWS` file was copied from, or is shared with, a different application ("MHN") rather than purpose-built for ERMS. `Logger` project has no dependencies on `DAL` or `ERMSWebAPI` (correctly a leaf/cross-cutting library).

There is no dependency **inversion**: `ERMSWebAPI` depends on concrete static classes in `DAL`; nothing is mockable/testable without a real SQL Server connection.

### Shared libraries
- **Logger** (`Logger.Logging`, `Logger.Utility`, `Logger.TypeEnums`) is shared between `DAL` and `ERMSWebAPI` and is the only true cross-cutting library. It is a singleton (`Logging.Instance`) writing to local flat files (see LoggingAnalysis.md).
- **DAL** itself functions as a shared library across (at minimum) ERMS and whatever other application(s) consume `BPACDA`, `BPIDA`, `PIDAO` (GP2GP), `MHNAppointmentDA`, `DBMessages` (MHNHL7), `ProcareApiDA`, `ProconApiDA`, `ScreeningDAL`, `DMSDA`, `DMSAWS` — none of which are invoked from `APIController`/`COLController`. This strongly suggests `DAL` is a **multi-application shared data-access library** (possibly also used by HISO/KARO or other internal apps), which is important context for the unified-platform consolidation: merging ERMS may mean partially merging this shared DAL too.

### Cross-cutting concerns
| Concern | Implementation | Evidence |
|---|---|---|
| Logging | Static `Logging.Instance.WriteEventLog`/`WriteExceptionLog`, called ad-hoc inside each controller action's private `WriteLog` wrapper | `APIController.cs` `WriteLog()`, `COLController.cs` `WriteLog()` |
| Auth/token validation | Re-executed as a stored-procedure call (`HSSDA.InsertAndValidateToken`) on **every** request, not via a Web API `AuthorizeAttribute`/OWIN middleware | `HSSDA.cs` lines 928-992 |
| ID obfuscation | `EncryptionManager`/`EncryptedInt`, called manually per-controller-action | `Models/EncryptionManager.cs`, `Models/EncryptedInt.cs` |
| Practice routing | Manual string-splitting of `EncounterId` on `"_"`/`"__"` to derive `practiceid`, repeated in every action | e.g. `APIController.cs` lines 57-81, 145-164, ... (23 near-identical copies) |
| Error handling | Try/catch per action, converts exceptions to an XML/JSON `error` field, no global exception filter beyond the unused MVC `HandleErrorAttribute` in `FilterConfig.cs` | `APIController.cs`, `FilterConfig.cs` |
| CORS | Per-controller attribute only (`COLController` wildcard-open, `APIController` commented out) | see TechnologyStack.md |

## Evidence
- `E:\NZTFS\ermsapi\DevLocal\ERMSWebAPI\ERMSWebAPI\Controllers\APIController.cs`
- `E:\NZTFS\ermsapi\DevLocal\ERMSWebAPI\ERMSWebAPI\Controllers\COLController.cs`
- `E:\NZTFS\ermsapi\DevLocal\DAL\South\HSSDA.cs`, `E:\NZTFS\ermsapi\DevLocal\DAL\Pegasus\PHCO.cs`
- `E:\NZTFS\ermsapi\DevLocal\DAL\DMSAWS\DMSAWS.cs` (cross-application namespace `MHN.DAL.DMSAWS`)
- `E:\NZTFS\ermsapi\DevLocal\Logger\Logging.cs`

## Risks
- **Duplication as architecture**: the ~25-line request-handling boilerplate copy-pasted 30 times means any fix (e.g., a security patch to the ID-decryption logic) must be manually replicated across every action, and several actions already show subtle differences (e.g., `COLController.Authenticate` does not derive `pho`/Azure-forwarding at all, unlike `APIController.Authenticate`) — evidence of drift from copy-paste.
- **No dependency inversion** makes the codebase impossible to unit test without a live database; any automated regression testing during migration will require either a) reverse-engineering stored procedure contracts into fixtures, or b) an integration-test environment against real SQL Server.
- **Shared DAL project scope creep**: because `DAL.dll` bundles many unrelated integrations (GP2GP, MHNHL7, Procare, Procon, BPAC, BPI, Screening, DMS/DMSAWS) that ERMS does not use, the migration team must confirm with stakeholders whether `DAL` is genuinely shared with other in-scope systems (HISO/KARO) before deciding what subset to carry into the unified platform.

## Recommendations
- In the unified platform, extract a single shared "request context" (auth token validation, practice/tenant resolution, ID obfuscation/decoding) into middleware/filters instead of per-action boilerplate.
- Introduce a repository/service layer with interfaces so business logic can be unit tested independently of SQL Server.
- Clarify DAL project ownership/boundaries with the client before assuming which DAL subfolders are in scope for the ERMS migration vs. belong to sibling systems.
