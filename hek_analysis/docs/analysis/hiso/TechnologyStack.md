# HISO — Technology Stack

**Summary:** HISO is a .NET Framework 4.8 WCF Web Application with no modern package
management beyond one NuGet package (Newtonsoft.Json); nearly all other third-party
dependencies are referenced as loose DLLs via `HintPath`, making the build environment
fragile and hard to reproduce.

## Findings

### Runtime / Framework
| Item | Value | Evidence |
|---|---|---|
| Target Framework | .NET Framework 4.8 | `Hiso.csproj` line 16: `<TargetFrameworkVersion>v4.8</TargetFrameworkVersion>` |
| Compilation target framework | 4.8 (compile), `httpRuntime` set to 4.6 | `Web.config` lines 17-18: `<compilation debug="true" targetFramework="4.8"/>`, `<httpRuntime targetFramework="4.6"/>` — **inconsistent**, see `DocumentationGap.md` |
| Project type | ASP.NET Web Application (Library output) hosting a WCF service | `Hiso.csproj` `<OutputType>Library</OutputType>`, `ProjectTypeGuids` includes the ASP.NET web application GUID `{349c5851-...}` |
| Language | C# (implicit, no explicit `LangVersion`) | `.cs` files throughout |
| Solution | `Hiso.sln` references the `Hiso` project only in the file provided | `Hiso.sln` |
| WCF service | `FormSessionService.svc` / contract `FormSessionPortType` | `FormSessionService.svc`, `FormSessionPortType.cs`, `FormSessionService.svc.cs` |
| Build config | Debug/Release, `AnyCPU`, DebugSymbols full in Debug, `Optimize=false` in Debug / `true` in Release | `Hiso.csproj` lines 32-49 |
| IIS hosting | IISExpress dev server config present; no explicit deployment/hosting model beyond IIS/ASP.NET | `Hiso.csproj` lines 19-30 |
| Source control | TFS (Team Foundation Server) | `Hiso.csproj` `SccProvider`, `SccAuxPath` referencing `vtdotfssvr01.vitonta.com:8080/tfs` |

### NuGet-managed packages
| Package | Version | Evidence |
|---|---|---|
| Newtonsoft.Json | 13.0.3 | `packages.config`; used in `DAL/DBMessages.cs` (`JsonConvert.DeserializeObject<DocumentFile>`) |

### Loose-DLL / HintPath referenced third-party libraries (not NuGet-managed)
| Library | Notes / Evidence |
|---|---|
| Aspose.Cells | Excel manipulation; `HintPath=..\..\PMSNZ Project\PMSdll\Aspose.Cells.dll`. No direct usage found in the files read (may be used transitively or by MHNEntity). |
| Aspose.Pdf | PDF generation/licensing; `HintPath=..\..\PMSNZ Project\PMSdll\Aspose.Pdf.dll`. Used in `FormSessionService.svc.cs` `getDeliveryOptions` (`Aspose.Pdf.License`). |
| Aspose.Words | Version 16.1.0.0 (very old, ~2016-era) pinned via `SpecificVersion=False`; `HintPath=..\..\PMSNZ Project\PMSdll\Aspose.Words.dll`. Used for HTML→PDF and image→PDF conversion in `ConceptMapper/TypeConverter.cs`. |
| AWSDoc | Proprietary in-house library for AWS-backed document status/retrieval; `HintPath=bin\AWSDoc.dll`. Used in `DAL/DBMessages.cs` (`AWSDoc.IndiciDMS.CheckAWSIsEnabled`, `DocumentGetByDocumentKeyJsonResult`, `GetDocumentStatusFromIndici`). Source not included in this tree — **Unable to verify internal behavior from available source.** |
| DMSProxy | Proprietary in-house Document Management System client (looks like an ASMX/SOAP proxy — namespace `DMSProxy.DMSService`); `HintPath=..\..\PMSNZ Project\PMSdll\DMSProxy.dll`. Used in `DocumentHandler.cs`, `Acc45DefinitionBuilder.cs`. Source not included — **Unable to verify from available source.** |
| Logger | Proprietary logging library; `HintPath=..\..\PMSdll\Logger.dll`. Used pervasively via `Logger.Logging.Instance.WriteEventLog(...)` / `WriteExceptionLog(...)`. See `LoggingAnalysis.md`. |
| Microsoft.IdentityModel.JsonWebTokens / .Logging / .Tokens, System.IdentityModel.Tokens.Jwt (v6.5.1.0) | Referenced with `HintPath=bin\...dll` (i.e., copied build output, not restored via NuGet). **No usage of JWT/token validation found in any `.cs` file read** — appears to be a leftover/unused reference. See `AuthenticationAuthorization.md` and `DocumentationGap.md`. |

### Framework assemblies referenced
`System.ServiceModel`, `System.ServiceModel.Web` (WCF), `System.Web.Services` (legacy ASMX-era, likely used by `DMSProxy`/`AWSDoc` consumers), `System.Runtime.Caching` (`MemoryCache`, used for in-process concept-list caching), `System.Data`, `System.Drawing`, `System.EnterpriseServices`, `System.Web.Entity`, `System.Web.DynamicData`, `System.Web.ApplicationServices` (evidence: `Hiso.csproj` lines 50-110).

### Project references
- `MHNEntity.csproj` (`..\MHNEntity\MHNEntity.csproj`) — external project not included in the provided source tree. `HealthLinkSession`-adjacent domain types may partially originate there, but the `HealthLinkSession` class itself is defined locally in `Mapper.cs` (see `Architecture.md`). **Unable to verify MHNEntity's contents from available source.**

### Database engine
Microsoft SQL Server via `System.Data.SqlClient` (`SqlConnection`, `SqlCommand`, `SqlDataAdapter`), connecting to SQL Server named instances (`dbserver-local`, `192.168.0.6\sql2014`). See `DatabaseAnalysis.md`.

## Risks
- Loose-DLL dependencies (`Aspose.*`, `DMSProxy`, `AWSDoc`, `Logger`) sourced from sibling folders outside this project (`..\..\PMSNZ Project\PMSdll\`, `..\..\PMSdll\`) mean the build is not self-contained and cannot be reproduced without access to those sibling folders — a significant migration/CI risk.
- Aspose.Words pinned at a ~2016-era version (16.1.0.0) is far behind current Aspose releases; licensing and security-patch status of that specific build is unknown.
- Unused-looking JWT/IdentityModel references suggest an abandoned or half-finished attempt to add token-based auth — worth confirming with the client whether this was intentional groundwork.

## Recommendations
- For the unified platform, do not attempt to reuse the WCF hosting model; treat Aspose document generation and the DMS/AWS document integrations as isolated external dependencies behind explicit interfaces.
- Confirm Aspose licensing terms/version compatibility before any migration that continues to use Aspose.
- Establish whether `MHNEntity`, `DMSProxy`, and `AWSDoc` source is available for review in Phase 2, since large parts of session/document behavior are opaque without them.
