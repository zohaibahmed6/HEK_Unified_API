# HISO — Risk Assessment (Migration Focus)

**Summary:** The dominant migration risks for HISO are (1) losing undocumented,
config-driven, and dead/unreachable business logic silently, (2) the WCF/.NET Framework 4.8
hosting model not translating cleanly to modern .NET, and (3) tight coupling to opaque
external/commercial dependencies (Aspose, DMSProxy, AWSDoc) whose contracts cannot be fully
verified from this source tree alone.

## Findings / Risk Register

| ID | Risk | Likelihood | Impact | Evidence | Mitigation |
|---|---|---|---|---|---|
| R-01 | **Legacy framework/hosting risk**: WCF (`System.ServiceModel`) has no first-class host in modern .NET (CoreWCF exists but is a community project with narrower feature support); a straight lift-and-shift of the `.svc` model is not viable. | High | High | `Hiso.csproj` `TargetFrameworkVersion=v4.8`; `Web.config` `<system.serviceModel>` | Decide early whether to (a) keep a thin WCF/CoreWCF compatibility shim for existing HealthLink clients, or (b) require all clients to move to a new REST/JSON contract, and plan a client-migration window accordingly. |
| R-02 | **Thread-unsafe shared DAL state cannot scale** to the 10,000-concurrent-user target as-is. | High | High | `DAL/DbAccess.cs` static `SqlConnection`/`SqlCommand`/`DataSet` fields | Full DAL rewrite is mandatory, not optional, for the unified platform — do not port `DbAccess.cs` forward. |
| R-03 | **Dead/unreachable code hides an ambiguous business decision** — the unreachable ACC45-save branch in `saveProcessAction` (`FormSessionService.svc.cs` line 535 onward) could represent either an intentional simplification or an accidentally disabled feature. | Medium | High (clinical/claims data correctness) | `FormSessionService.svc.cs` lines 502-593 | Get explicit business/product sign-off before deciding to drop or reinstate this logic; do not let a developer infer intent from dead code alone. |
| R-04 | **Config-driven business rules (`Web.config` appSettings) are easy to lose silently** because they look like deployment config, not business logic, to someone unfamiliar with the codebase. | High | High | `Web.config` `UDT_*`, `QualifierList`, `DMS*TypeId`, `Task*Id` keys; `Utitlity.GetColumnNameByTableName` | Explicitly migrate every appSetting-driven rule into `BusinessRules.md`-style documented, versioned configuration or code in the unified platform (already begun in this report — see BR-16, BR-17, BR-20, BR-21, BR-22). |
| R-05 | **Tight coupling to commercial Aspose licenses** (Words/Cells/Pdf, pinned to a ~2016-era Aspose.Words build) for document generation; losing license access or Aspose API compatibility breaks document rendering entirely. | Medium | Medium-High | `Hiso.csproj` Aspose references; `ConceptMapper/TypeConverter.cs` | Confirm current Aspose licensing terms and whether the unified platform will continue with Aspose or move to an open alternative (e.g., a headless browser PDF renderer) — plan a document-generation abstraction layer either way. |
| R-06 | **Opaque external dependencies** (`DMSProxy`, `AWSDoc`, `MHNEntity`) whose source was not available for this review — their internal contracts, error handling, and data formats cannot be fully assessed, meaning migration effort for document storage/retrieval is likely underestimated until those are reviewed. | High | Medium | `Hiso.csproj` `HintPath` references; usage in `DocumentHandler.cs`, `DAL/DBMessages.cs`, `Acc45DefinitionBuilder.cs` | Schedule a dedicated review pass (Phase 2/3) once source or API contracts for `DMSProxy`/`AWSDoc`/`MHNEntity` are available. |
| R-07 | **No automated tests found** anywhere in this project — any refactor/migration has no regression safety net beyond manual QA. | High | High | No `*Test*.cs`/test project referenced in `Hiso.sln`/`Hiso.csproj` | Build characterization tests against current stored-procedure contracts and XML mapping behavior *before* refactoring, using this report's `BusinessRules.md` as the test-case source. |
| R-08 | **Security posture (no auth, hardcoded secrets, fault-detail leakage) cannot be carried forward** into a platform serving 10,000 concurrent users across multiple locations without becoming a serious breach risk. | High | Critical | See `SecurityAnalysis.md`, `AuthenticationAuthorization.md` | Treat all HISO security findings as blocking issues for the unified platform's design, not cosmetic cleanup. |
| R-09 | **Dynamic, string-literal-driven routing logic** (second-node DB routing, AWS-enabled procedure allow-lists) is fragile to change — adding/removing a report type requires editing hardcoded `HashSet`/`if` chains in `DAL/DBMessages.cs`, with no config-driven or data-driven alternative. | Medium | Medium | `DAL/DBMessages.cs` `ExecuteHisoProcedure`, `awsEnabledSPs` | Replace hardcoded procedure-name allow-lists with a data-driven routing table (e.g., a DB reference table flagging which procedures need second-node/AWS routing) as part of the rewrite. |
| R-10 | **Performance risk from parallel stored-procedure execution without bounded concurrency** — `Parallel.ForEach` over the distinct procedure list (`ConceptMapper/HisoConceptDetail.cs` `GetProcedureList`) has no `MaxDegreeOfParallelism` limit, which could exhaust SQL Server connection pool capacity under heavy concurrent form-load traffic at 10,000-user scale. | Medium | Medium | `ConceptMapper/HisoConceptDetail.cs` lines 85-105 | Bound concurrency explicitly and load-test the concept-resolution pipeline before assuming it scales. |

## Risks (narrative)
Taken together, the highest-value risk to manage during migration is not any single line of
code but the **combination of undocumented, config-resident, and partially-dead business
logic** discovered throughout this review (see `BusinessRules.md` and
`DocumentationGap.md`). A naive "rewrite from reading the code" approach is likely to
silently drop several real behaviors (resume-only-refreshes-currentuser, second-node
routing, AWS fallback, ACC45 msdwcref special-casing) because they are easy to miss without
deliberately cataloguing them, as this report has attempted to do.

## Recommendations
1. Use `BusinessRules.md` as the authoritative checklist during redesign; require explicit
   sign-off (kept/changed/dropped) for every rule before implementation.
2. Do not port forward `DAL/DbAccess.cs`'s shared-state pattern under any circumstances.
3. Schedule dedicated technical spikes for the Aspose document pipeline and the
   `DMSProxy`/`AWSDoc` integrations before committing to a migration timeline, since their
   internals are currently opaque to this review.
4. Build characterization/regression tests against current stored-procedure I/O contracts
   before refactoring any Builder/Mapper class, given the complete absence of existing tests.
5. Treat all findings in `SecurityAnalysis.md` and `AuthenticationAuthorization.md` as
   release-blocking for the unified platform, not optional hardening.
