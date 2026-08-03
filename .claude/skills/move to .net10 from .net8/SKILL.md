---
name: hek-dotnet8-to-dotnet10-migration
description: Guides the migration of the HEK Core API from .NET 8 to .NET 10 — auditing project files, upgrading target frameworks and NuGet packages, fixing breaking changes, and re-verifying build/tests. Use whenever Zohaib asks to move, upgrade, port, or migrate the HEK API (or any of its modules) from .NET 8 to .NET 10, or mentions ".NET upgrade", "framework migration", "TFM bump", or wants to plan/track that migration. Every run MUST also update the project's separate migration documentation files (MIGRATION_STATUS.md master log + per-module notes) — never do the code/conversion work without updating docs in the same session.
---

# HEK API: .NET 8 → .NET 10 Migration

This skill covers two things that must always happen together: doing the actual migration work, and recording it in the project's documentation. Don't do one without the other — an undocumented migration step is, for this project, an unfinished one.

## Why documentation is non-negotiable here

This is a multi-session, multi-module migration. Neither Zohaib nor a future Claude session will remember what got upgraded, what broke, or why a workaround was chosen. The docs are the only thing carrying that context forward. Treat every conversion action (upgrading a project file, fixing a breaking change, re-pointing a package) as incomplete until it's written down.

## Documentation structure

Two layers, kept separate on purpose — a fast overview, and detail you only open when you need it:

1. **Master log**: `MIGRATION_STATUS.md` at the project root (`E:\claude_projects\HEK Core API\MIGRATION_STATUS.md`), alongside any existing `PROJECT_STATUS.md`. Append-only change log — never rewrite history, only add entries. Contains:
   - Current phase (Audit / Upgrade / Fix Breaking Changes / Test / Done)
   - Table of modules/projects with status (Not started / In progress / Blocked / Done)
   - Append-only Change Log section: one dated entry per session — what was done, what broke, what's next

2. **Per-module notes**: `docs/migration/<module-name>.md` for each project/module touched (e.g. `docs/migration/Auth.md`, `docs/migration/DataAccess.md`, matching the existing project layout: Adapters, Application, Domain, Infrastructure, Api). Contains:
   - Target framework moniker before/after
   - NuGet packages that needed version bumps or replacement, and why
   - Breaking changes hit and how each was resolved (with code references, not just "fixed it")
   - Anything intentionally deferred, and why

If neither file exists yet, create them on first run — don't wait to be asked. If `MIGRATION_STATUS.md` already exists, read it first to pick up where the last session left off; never guess at prior state.

## Workflow

1. **Read state first.** Before touching code, read `MIGRATION_STATUS.md` and the relevant per-module file(s). Report to Zohaib in chat what's already done and what you're about to work on — don't silently re-do or skip work.

2. **Audit before changing.** For the module in scope, identify: current TFM (`net8.0`), all NuGet package references and their .NET 10 compatibility status, and any use of APIs/behaviors removed or changed in .NET 9/10 (see the checklist below). Summarize findings in chat as a short table before making changes — this is a decision point, not busywork, so give Zohaib the chance to flag anything before code moves.

3. **Upgrade incrementally, one module at a time.** Bump the TFM, update packages, fix compile errors, run tests. Don't batch multiple unrelated modules into one pass — each module gets audited, converted, tested, and documented before moving to the next. This project already has separate test projects per layer (Adapters.UnitTests, Application.UnitTests, Domain.UnitTests, Infrastructure.UnitTests, Api.IntegrationTests) — run the matching test project after each module upgrade to catch regressions early.

4. **Update the docs in the same session as the code change** — not as a follow-up. After finishing a module (or a meaningful chunk of one):
   - Add a dated entry to the `MIGRATION_STATUS.md` change log
   - Update that module's status in the status table
   - Write or update `docs/migration/<module-name>.md` with what actually happened (including dead ends — those save the next session time)

5. **Flag risk plainly.** If a package has no .NET 10-compatible version, or a breaking change requires a real design decision (not just a mechanical fix), stop and raise it in chat as a short multiple-choice decision rather than picking an approach silently.

6. **Legacy reference code stays out of scope.** This project has a `legacy-reference/` folder (old .NET Framework webapi projects like hsswebapi, ermsapi). Don't attempt to migrate anything under `legacy-reference/` as part of this skill unless Zohaib explicitly says otherwise — it's reference material, not part of the live API.

## Known .NET 9/10 breaking-change areas to check during audit (Web API projects)

- Minimal API / routing behavior changes and any renamed/removed extension methods
- ASP.NET Core auth middleware changes (JWT handler defaults, identity API changes)
- EF Core provider version compatibility with .NET 10 (many providers lag behind runtime releases — verify before committing to a TFM bump)
- System.Text.Json default serialization behavior changes between versions
- Kestrel server default configuration changes
- Any NuGet packages with no .NET 10-targeting release yet — these block the upgrade for that module and must be flagged, not silently pinned to an old TFM

This list is a starting checklist, not exhaustive — verify against the current official Microsoft .NET 8→9→10 breaking-changes documentation (web search) rather than relying on memory, since that list gets updated as issues are found.

## Output format for chat updates

When reporting progress, use a short table, not prose:

| Module | TFM | Status | Notes |
|---|---|---|---|
| Adapters | net10.0 | Done | No breaking changes hit |
| Application | net8.0 | In progress | EF Core provider pending .NET 10 support |