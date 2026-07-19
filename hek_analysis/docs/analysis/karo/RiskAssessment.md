# KARO — Migration Risk Assessment

**Summary:** The dominant migration risks are not framework-porting effort (the endpoint surface is small and mechanically simple) but rather (a) faithfully-reproducible-but-wrong behaviors that look intentional, (b) an opaque authorization/tenant-routing contract that lives mostly in undocumented stored procedures, and (c) a shared DAL library carrying both dead code and live security vulnerabilities that must not be blindly carried into the unified platform.

## Findings

### Legacy framework risk
- **.NET Framework 4.8 → modern .NET.** Web API 2 (`System.Web.Http`) has no direct equivalent in ASP.NET Core; every controller action, the OWIN pipeline (currently unused anyway), routing, and CORS configuration will need to be rewritten against ASP.NET Core's model, not merely recompiled. Old-style `.csproj` + `packages.config` adds mechanical conversion overhead (SDK-style project conversion) on top of the framework port. **Severity: Medium** (small codebase — 2,047-line controller, ~1,400-line reachable DAL — makes the mechanical porting effort itself modest; the risk is in behavior-preservation, not line count).
- ADO.NET/stored-procedure data access transfers conceptually cleanly to Dapper/EF Core raw SQL in a modern stack, but the ~35 stored procedures themselves (and their T-SQL bodies) are outside this repository and must be sourced from the database directly before any reimplementation. **Severity: Medium-High**, contingent on DB access.

### Fragile / undocumented business logic
- **`GetEncounterSummary`'s hardcoded mock responses (BR-09)** and **`SaveScreeningCode`'s unauthenticated no-op (BR-06)** are the two highest-risk "silent failure" behaviors — a naive migration that faithfully reproduces current behavior ships broken functionality into the new platform, while a migration that "fixes" them without stakeholder sign-off risks breaking whatever (if anything) currently depends on the existing behavior. **Severity: High.** Recommendation: explicit stakeholder decision required before touching either.
- **Multi-tenant routing via string-concatenated connection-string names** (`"ConnIndiciDB" + practiceid`, derived from parsing a client-supplied `encounterId`) is fragile: a malformed or unrecognized practice suffix produces either a `NullReferenceException` (missing config entry) or silently queries the wrong tenant's database if suffix parsing is ambiguous. There is no central tenant registry to validate against. **Severity: High** — this pattern is the single biggest structural risk to carry forward unmodified; it should be replaced with an explicit, validated tenant-resolution service in the unified platform.
- **Practice-specific hardcoded overrides** (e.g., `302_F3H045` → `pho = "SCDHB"`, BR-04) are easy to miss during a line-by-line rewrite since they are scattered across five different Save* actions rather than centralized. **Severity: Medium.**
- **Magic-number return codes** from stored procedures (`-3`, `-4`, `-5` meaning different "already exists"/"invalid" conditions per procedure, BR-12/BR-13) are undocumented and procedure-specific — losing track of even one during a rewrite changes user-facing behavior (e.g., "duplicate diagnosis" silently becomes a hard failure instead of a friendly message). **Severity: Medium.**

### Tight coupling to third-party/sibling integrations
- **Shared DAL library** (`DAL.dll`) is referenced by KARO but contains 11 other subsystem modules (BPAC, BPI, DMS, GP2GP, MHNAppointment, MHNHL7, Pegasus, Procare, Procon, Screening, UI) not called by KARO's own controller — some of which are **near-duplicates of KARO's own functionality** (Procare, Procon, Pegasus reuse KARO's `[HSS]`/related schema procedures). Any schema change made "for KARO" during migration risks breaking these unseen sibling systems, and there is no way to detect that risk from within this repository alone. **Severity: High** — requires coordination with whoever owns those other integrations (possibly out of scope for this Phase 1 analysis, but must be flagged to the team doing HISO/ERMS analysis and the eventual comparison phase).
- **`AWSDoc.dll`** external dependency (source unavailable) is load-bearing for AWS-enabled practices' document retrieval (`GetDocuments`). Its behavior can currently only be characterized by its call signature, not its implementation. **Severity: Medium**, contingent on locating source or documentation elsewhere in the organization.
- **Shared `Logger.dll`** referenced via a hint path pointing outside this repository (`..\..\MHNPHMP-Integration\Logger\bin\Debug\Logger.dll`) confirms KARO is extracted from a larger solution; the true current source of `Logger.cs` may differ from what's in this repo. **Severity: Low-Medium** (the analyzed `Logging.cs` is simple enough that behavior is unlikely to differ materially, but this should be verified before assuming completeness).

### Things dangerous to silently drop during rewrite
1. **Per-request, per-patient token re-validation** (BR-05) — dropping this in favor of a coarser "is the user logged in" check would be a security regression (currently every call re-checks token validity against the specific patient/encounter/practice triple).
2. **The `302_F3H045` → `SCDHB` PHO override** and any other undiscovered practice-specific special cases — these are invisible until a specific tenant's behavior changes; a full audit of all `if (practiceid.Contains(...))`-style special cases (and their stored-procedure-side equivalents, not visible here) should be done against production traffic/logs before cutover, not just against source code.
3. **The `-3`/`-4`/`-5` "already exists" success-path handling** in `SaveCondition`/`SaveInvoice`/`SaveSummary` — if dropped, previously-idempotent client behavior (retry-safe saves) becomes error-prone.
4. **The optional-parameter pattern in `HSSDA`** (many DAL methods only add a `SqlParameter` if the corresponding value is non-empty) — this implies the underlying stored procedures have meaningful default-parameter behavior when a parameter is omitted vs. explicitly null/empty; a naive "always pass every parameter" reimplementation could change stored-procedure behavior if the procedures branch on `IS NULL` vs. missing-parameter-uses-default differently. Needs DB-side verification.
5. **The AWS-enabled/not-enabled document retrieval branch** (BR-18) — silently defaulting all practices to one path during migration could break document retrieval for whichever practice population is on the other path today.

## Evidence
See `BusinessRules.md`, `Architecture.md`, `DependencyAnalysis.md`, and `DatabaseAnalysis.md` for full citations underlying each risk above.

## Risks (consolidated ranking)
| Risk | Severity | Migration Impact |
|---|---|---|
| Multi-tenant connection-string-name routing pattern | High | Structural — must be redesigned, not ported |
| Ambiguous/broken endpoints (`GetEncounterSummary`, `SaveScreeningCode`) | High | Requires stakeholder decision before implementation |
| Shared DAL coupling to sibling systems (Procare/Procon/Pegasus etc.) | High | Requires cross-team coordination beyond this repo's scope |
| Undocumented stored-procedure contracts (return codes, optional-parameter defaults) | Medium-High | Requires DB access to fully de-risk |
| Hardcoded practice-specific special cases | Medium | Requires audit against production data/logs |
| `AWSDoc.dll` opaque dependency | Medium | Requires locating source or reverse-engineering via testing |
| Legacy framework/project format porting | Medium | Mechanically bounded (small codebase) but requires careful behavior-preservation testing |
| Shared `Logger.dll` outside repo scope | Low-Medium | Verify current source matches what was analyzed |

## Recommendations
1. Treat this Phase 1 report as necessary but not sufficient — before Phase 4 (comparison/migration recommendations) locks in a design, obtain direct database access to review the ~35+ stored procedures underpinning KARO's business logic, since a large share of the actual business rules live there, not in the C# reviewed here.
2. Flag the shared-DAL coupling risk explicitly to whoever is analyzing HISO and ERMS — if either of those systems turns out to use the same `DAL.dll` (e.g., via the BPAC/BPI/Pegasus/Procare/Procon modules), the unified-platform design must account for that shared blast radius rather than treating KARO as fully independent.
3. Sequence the migration to isolate and resolve the two "silently wrong" endpoints and the tenant-routing pattern early, since these carry disproportionate risk relative to their code size.
