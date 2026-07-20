# ADR-012: Solution Structure Deviations and Block 1 Implementation Inferences

**Status:** Approved (Day 1 build sprint, Block 0 + Block 1)
**Deciders:** Implementation carried out per `hek_analysis/PROJECT_STATUS.md`-approved plan, 2026-07-20

## Context

The Implementation Plan (`hek_analysis/docs/Unified-Healthcare-API_ImplementationPlan.md`) names
seven solution projects: `Api, Application, Domain, Infrastructure, Adapters.Hiso, Adapters.Karo,
Adapters.Erms`. Building Block 0 (scaffolding) and Block 1 (security core) against strict Clean
Architecture rules (architecture-rules skill) surfaced a small number of structural decisions and
several genuine gaps where no source document (SRS/EAD/ADR-001-011/API Contract Design/OpenAPI
spec) states an exact answer. Per the project's standing rule ("never invent a business rule,
endpoint shape, or field that isn't in the docs"), each is recorded here rather than silently
assumed, consistent with every other ADR/decision in this project's log.

## Decision 1: Added an eighth project, `Contracts`

A zero-dependency shared kernel holding wire-shape DTOs (`Error`, `TokenRequest`/`TokenResponse`,
`ResourceScope`, `OriginScope`, claim/header name constants) referenced by both `Api` and all three
`Adapters.*` projects. These types could legally live in `Application` (Adapters already reference
Application), but doing so would turn Application's feature folders into a dumping ground for pure
DTOs with zero orchestration logic, muddying "Application = orchestration only." `Domain` does not
reference `Contracts` - Domain remains at zero dependencies.

## Decision 2: COL/Pegasus lives in `Adapters.Erms/Col/`, not a separate `Adapters.Col` project

ADR-003's follow-up decision log confirms COL needs its own distinct `originScope`, separate from
ERMS's other functions. Rather than add an unrequested eighth Adapters project, COL's translator
lives in a `Col/` subfolder inside `Adapters.Erms` and is coded to always assign `OriginScope.Col`,
never `OriginScope.Erms` - the origin-scope separation is enforced in code, not by project boundary.

## Decision 3: Single Dockerfile, not "one per service"

The Implementation Plan's Block 0 row says "Dockerfile per service." Today there is exactly one
deployable (`Api` hosts everything; `Infrastructure`/`Adapters.*` are class libraries, not separate
processes) - consistent with the EAD's stated consolidation goal. One Dockerfile is the correct
reading of "per service" given there is currently one service. Revisit if a future phase splits a
component into its own process.

## Decision 4: Canonical `POST /auth/token` returns 501, not a guessed origin scope

The OpenAPI `TokenResponse.originScope` enum only defines `Hiso|Karo|Erms|Col`. No source document
states what origin scope a *direct* (non-legacy) caller of the canonical endpoint should receive,
and every real caller today goes through a legacy compat endpoint instead. Fabricating a fifth
enum value not in the spec would violate the project's "never invent a contract field" rule.
Returns `501 Not Implemented` until this is resolved with the stakeholder.

## Decision 5: Legacy compat auth endpoints preserve legacy status-code/body behavior, not FR-HTTP-01's new semantics

FR-HTTP-01 requires meaningful HTTP status codes platform-wide, replacing KARO/ERMS's "always 200"
pattern. But ADR-002/004/007's "zero consumer-side change" principle, applied to the *compat*
endpoints specifically, means HSS Portal/ERMS/COL's existing client code (which reads a `status`/
`Message` field from a 200 body, not the HTTP status line) must keep working unmodified. Resolution:
FR-HTTP-01 governs the *canonical* contract (`/auth/token`, and all Block 2 domain endpoints);
the three legacy compat endpoints (`/karo/authenticate`, `/erms/authenticate`, `/erms/col/authenticate`)
deliberately preserve the exact legacy wire behavior, including always-200, since that behavior is
part of the "exact existing response shape" the zero-change principle protects. Flagged for
stakeholder confirmation - the alternative reading (FR-HTTP-01 applies even to compat endpoints)
would break existing consumers if adopted, so this reading was chosen as the lower-risk default.

## Decision 6: Compat endpoint paths are namespaced (`/karo/authenticate`, `/erms/authenticate`), not literally bare `/authenticate`

Per `KARO_HSS_doc.md` and `ERMS_doc.md`, both legacy systems exposed their Authenticate endpoint at
the literal bare path `/authenticate` - each on its own dedicated legacy server/host. In this
unified single-host Block 0/1 deployment, both paths would collide. Namespaced by system prefix as
an interim measure. **Flagged**: ADR-011 already establishes that ERMS/HSS run on a designated
active/standby server pair, distinct from the general fleet - host-based routing (distinct hostnames
resolving to the correct backend) may be the intended real zero-path-change mechanism instead of a
path prefix. Needs confirmation before this is relied upon as final.

## Decision 7: Tenant registry schema, HISO server-address map, and HISO session-expiry column are inferred, not confirmed

- **Tenant registry columns** (`PracticeRegistryEntry`): inferred from ADR-001's prose ("one row per
  practice with which physical database server it lives on"). No literal schema exists in any
  source document.
- **`HisoServerAddressMapOptions`**: ADR-007 states routing is decided by "which address was
  called," but no document gives the concrete list of HISO server addresses or how many exist.
  Shipped as an empty, config-driven placeholder.
- **`HisoSessionContext.SessionCreatedAtUtc`** (via a `CreatedAtUtc` column read in
  `HisoSessionRepository`): required to enforce the new 12-hour expiry (ADR-004), but only
  `ProviderID/PatientID/AppointmentID/PracticeID` are confirmed present on
  `[Appointment].[tblHealthLinkSession]` in any source document. The column name/existence needs
  verification against the live schema.

## Decision 8: Dormant DAL modules - deferred entirely, then `DMSDA.cs` ported once source was supplied

Per direct confirmation during Block 1 planning: `DMSDA.cs`, `DBMessages.cs`, and the other
8-10 retained-but-dormant DAL modules (BPAC, BPI, GP2GP, MHNAppointment, MHNHL7, Procare, Procon,
Screening, UI, DMSAWS) had no legacy C# source anywhere in this repo - only documentation
describing the confirmed SQL-injection vulnerability, not full method signatures. Writing stand-in
code would have meant inventing an API shape beyond what any source document specifies. **Initial
decision: defer entirely** - no code written for these modules in Block 0/1.

**Update, same day**: Zohaib supplied the real source for the two confirmed-injection files
(`legacy-reference/DAL/DMS/DMSDA.cs`, `legacy-reference/DAL/MHNHL7/DBMessages.cs`, outside `src/`,
reference-only). On reading them:

- **`DMSDA.cs`**: the confirmed injection was real and precisely located - `UpdateInboxFolderDocuments`,
  a `private` method never called by anything else in the file (dead code within its own class, not
  just dormant at the module level). Ported to `src/Infrastructure/Legacy/Dormant/Dmsda/DmsDocumentService.cs`
  with the injection fixed via full parameterization; the rest of the file's already-parameterized
  methods ported faithfully. This closes the `DMSDA.cs` half of SRS Phase A hard-blocker item 4.
- **`DBMessages.cs`**: reading the actual file **contradicted the expected framing** - no
  string-concatenated SQL exists in it; every call is already parameterized via a `DALHelper` class
  whose own source wasn't supplied. The file is also tightly coupled to HISO's live concept-mapping
  engine via types not present anywhere in this repo (`HealthLinkSession`, `HisoRequest`,
  `DynamicParam`, `AWSDoc.IndiciDMS`, `Hiso.ConceptMapper`) - Block 2 ACC45/HISO territory, not
  something to invent stub shapes for now. Flagged back to Zohaib rather than either silently
  skipped or silently fabricated. **Decision (2026-07-20): defer to Block 2** - when the ACC45/HISO
  domain group is built, `DBMessages.cs`'s `ExecuteHisoProcedure`/`GetParamList`/`MapParamList`
  logic ports naturally alongside the real supporting types Block 2 defines for that group, instead
  of inventing placeholder types now. This half of SRS Phase A hard-blocker item 4 is intentionally
  carried forward, not closed, and not silently dropped.

The other 8-10 dormant modules remain fully deferred (no source supplied for them).
`PROJECT_STATUS.md` §3 item 23 tracks the current split state.

**Update, Block 2 (2026-07-20)**: building Block 2's first domain group (Patient Demographics)
surfaced that `DBMessages.cs`'s `ExecuteHisoProcedure` engine is not ACC45-specific niche
plumbing - it's the actual mechanism behind "HISO getData," which the Contract Design doc lists
as the legacy source for the majority of HISO-sourced read endpoints (demographics, notes,
conditions, medications, lab results, observations). Deferring it further would have blocked
most of Block 2, not just the ACC45 group. Ported the core engine into
`src/Infrastructure/Legacy/Hiso/HisoConceptExecutor.cs` as live, shared infrastructure (not
dormant), reconstructing `HealthLinkSession`/`HisoRequest`/`DynamicParam` directly from their
observable property usage in the supplied `DBMessages.cs` source - extracted from real code, not
invented from nothing. The AWS-enrichment branch is ported but throws/logs rather than executing
(`AWSDoc.IndiciDMS`'s source was never available even to the original Phase 1 analysis, per SRS
Section 16). See `PROJECT_STATUS.md` open item 28 for the consolidated flagged-inference note
covering every Block 2 repository's procedure/column-name assumptions, including this engine's.

## Decision 9: `ILogger<T>` permitted in the Application layer

architecture-rules states Application "contains orchestration only... no infrastructure
implementation." `ResolveHisoSessionQueryHandler` uses `Microsoft.Extensions.Logging.ILogger<T>`
(an abstraction, not a concrete Serilog dependency) to emit the new security-event logging ADR-004
requires. Interpreted as permitted - `ILogger<T>`/`IOptions<T>` are framework *abstractions*
already conventional in Clean Architecture Application layers, not infrastructure implementations
(EF Core/SQL/HTTP calls, which Application still has zero of). Flagged as an interpretation, not a
certainty, since the skill's wording could be read more strictly.

## Consequences

None of these decisions block Block 0/1's acceptance criteria. Decisions 4, 5, 6, and 7 carry real
open questions that should be resolved with the stakeholder before Block 2 builds endpoints that
depend on them (particularly Decision 6's routing mechanism, since ADR-007/ADR-011 already
establish per-system addressing patterns that this decision only partially reconciles). Decision 8
leaves an SRS Phase A hard-blocker genuinely unresolved - not fixed, not silently skipped, tracked
as an explicit open item.
