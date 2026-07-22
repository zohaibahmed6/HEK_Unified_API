# As-Built Architecture

See `docs/DOCUMENT_INDEX.md` for the full doc map. This describes the code as it actually exists,
derived by reading `src/*` (not the spec's aspirational description).

## Layers (Clean Architecture)

| Project | Role | Depends on |
|---|---|---|
| `Domain` | Entities (e.g. `PracticeRegistryEntry`), domain exceptions. No dependencies. | — |
| `Contracts` | Wire/DTO shapes shared across layers: `Auth`, `Security`, `Demographics` (canonical + per-system), etc. | Domain |
| `Application` | CQRS: MediatR commands/queries + handlers, FluentValidation pipeline, interface-only repository/service contracts (`Common/Interfaces`), options classes. No infrastructure/DB code. | Domain, Contracts |
| `Infrastructure` | Concrete implementations: ADO.NET legacy repositories (`Infrastructure/Legacy/*`), EF Core `TenantRegistryDbContext`, `ISecretProvider` implementation, `IJwtTokenIssuer` (`JwtTokenIssuer`), convention-scanned DI registration (`AddInfrastructureRepositories()`). | Application, Domain |
| `Adapters.Hiso` / `Adapters.Karo` / `Adapters.Erms` | Per-legacy-system wire-format DTO libraries (request/response shapes matching each legacy system's real JSON/XML contract), e.g. `KaroDemographicsResponse.cs`, `ErmsReferralDocument.cs`. Pure data-shape libraries, no business logic. | Application (for shared interfaces) |
| `Api` | ASP.NET Core host. Thin controllers under `Api/Features/<Area>/Controllers`, vertical-slice folders. `Program.cs` wires DI, health checks, auth, CORS, exception handling. | All of the above |

Dependency flow is strictly inward: `Api` → `Adapters.*`/`Infrastructure` → `Application` → `Contracts`/`Domain`.
Application never references Infrastructure or Api; it depends only on its own interfaces.

## CQRS / MediatR

Every controller action sends a MediatR `IRequest` (query or command) and returns the handler's
result; controllers contain no business logic. Handlers live under
`Application/Features/<Area>/{Commands,Queries}` and depend only on `Application/Common/Interfaces`
abstractions (e.g. `IDemographicsRepository`, `IHisoSessionRepository`), implemented in
`Infrastructure`. A FluentValidation pipeline behavior runs before handlers.

## Legacy-compat module isolation

Four legacy-compat controllers reproduce each source system byte-for-byte:

- `HisoCompatController` (`Route("hiso")`) — 6 ops: getData, getVersion, getDeliveryOptions, processAction, saveContainer, getFormView.
- `KaroCompatController` (`Route("karo")`) — ping/authenticate (GET+POST) + ~19 more ops (demographics, clinicalnotes, conditions, documents, labresults, medications, observations, provider, recallcategories, encountersummary, recalls, screeningcodes, patientattachment, + POST variants).
- `ErmsCompatController` (`Route("erms")`) — ping/authenticate + ~20 GET ops (GetPatientData, GetSmokingStatus, GetCurrentUser, GetNextOfKin, GetAccidents, GetClassifications, GetConsultNotes, GetMedicalAllergies, lab/radiology/discharge/scanned report list+detail pairs) + `SaveDocument`.
- `ColCompatController` (`Route("erms/col")`) — authenticate, GetCurrentPatientData, GetSessionData, GetProviderData, GetSurgeryData, GetDiagnosisData, SaveInvoice.

Per `docs/assessment-2026-07-22.md` §1: "Module isolation between HISO/KARO/ERMS/COL is enforced
structurally (separate parsers, token validators, connection resolvers; zero shared session
state)" — deliberately duplicated, not shared, so a change to one legacy system's quirks can never
leak into another's byte-exact response.

## The canonical `/v1` surface

Per git status and `PROJECT_STATUS.md`, most of the ~17 originally-parked canonical controllers
have since been re-enabled (`CanonicalDemographicsController`, `ClinicalNotesController`,
`ConditionsController`, `DocumentsController`, `EncounterSummaryController`,
`MedicationsController`, `ObservationsController`, `PracticeContextController`,
`ProvidersController`, `RecallsController`, `ReportsController`, `ScreeningController`,
`TasksController`, `InvoicesController`, `Acc45Controller`, `PracticesAdminController`, `AuthController`).
Only `CanonicalDemographicsController` still carries a `[NonController]`-era design note; verify
current `[NonController]` status per-controller before assuming any one is live — this doc does not
re-derive that per-file (see Drift note in `docs/CHANGELOG.md`).

`CanonicalDemographicsController` (`src/Api/Features/Canonical/Controllers/CanonicalDemographicsController.cs`)
is a single merged endpoint at `v1/patients/{patientId}/demographics` (route inferred from the JWT's
`OriginScope` claim, not a URL segment per system) — this is a **known drift** from the OpenAPI yaml,
which still documents four separate paths (`/patients/{patientId}/demographics/{hiso,karo,erms,col}`).
See Drift section below.

## Request-flow example: KARO demographics

```
GET /karo/demographics?system=...&patientId=...&encounterId=...
  -> KaroCompatController.GetDemographics (Api/Features/Auth/Controllers/KaroCompatController.cs)
  -> IMediator.Send(KaroDemographicsQuery)              (MediatR dispatch)
  -> Application/Features/Karo/Queries handler           (business rules, validation)
  -> IDemographicsRepository / IKaroRepository           (Application interface)
  -> Infrastructure/Legacy/Demographics/DemographicsRepository.GetKaroAsync
       -> ILegacyPracticeConnectionResolver.ResolveAsync (tenant/practice -> connection string)
       -> LegacyDbExecutor.ExecuteDataTableAsync("[HSS].[uspGetDemographics]", @pPatientID)
  -> real KARO/HSS SQL Server stored procedure, PMS_NZ_V2
  <- DataTable row (Given/Family/BirthDate/DateOfEnrolment/EndEnrolmentDate columns)
  <- mapped to KaroDemographicsResponse / DemographicsKaro, same envelope/status code as legacy
```

The canonical path (`v1/patients/{id}/demographics`) reuses the *same* `IDemographicsRepository`
(zero new DB code) and instead maps the result onto `DemographicsCanonical`, applying per-origin
field scoping (`FieldSelector.Project`) before serializing.

## Known drift (yaml vs. real controllers)

The OpenAPI yaml (v1.1.4) documents four separate canonical demographics paths
(`/patients/{patientId}/demographics/hiso|karo|erms|col`). The real
`CanonicalDemographicsController` instead exposes one merged, versioned endpoint
(`v1/patients/{patientId}/demographics`) that infers the source system from the caller's JWT
`OriginScope` claim and accepts a `?fields=` query parameter for sparse fieldsets. This is not a
trivial missing-route fix — it's a structural difference — so the yaml was left untouched per
instructions; logged in `docs/CHANGELOG.md` instead.
