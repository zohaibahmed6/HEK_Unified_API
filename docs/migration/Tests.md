# Migration notes: Test projects

Covers `Domain.UnitTests`, `Application.UnitTests`, `Infrastructure.UnitTests`,
`Adapters.UnitTests`, `Api.IntegrationTests`.

- TFM: net8.0 -> net10.0 (via `Directory.Build.props`) for all five.
- Status: Done
- Packages changed: `Microsoft.AspNetCore.Mvc.Testing` 8.0.10 → 10.0.0 (Api.IntegrationTests only —
  must track the ASP.NET Core shared-framework major version). `Microsoft.NET.Test.Sdk`, `xunit`,
  `xunit.runner.visualstudio`, `FluentAssertions`, `NSubstitute`, `coverlet.collector`,
  `Testcontainers.MsSql` all left unchanged — no conflict forced a bump.
- Breaking changes hit: none directly in test code.
- Test results:
  - Domain.UnitTests: 0 tests (project has none currently — not a migration issue).
  - Application.UnitTests: 7/7 pass.
  - Infrastructure.UnitTests: 4/4 pass.
  - Adapters.UnitTests: 6/6 pass.
  - Api.IntegrationTests: 46/49 pass. 3 failures are **pre-existing and unrelated to the .NET 10
    migration** — see `MIGRATION_STATUS.md` change log for detail. All three intentionally assert a
    documented legacy-SQL "known bug" against the live legacy database and now observe different
    live-DB behavior than when the test was written; this is a live-data/legacy-DB-state question,
    not a .NET runtime question. Flagged for a separate follow-up, not blocking this migration.
- Deferred items: none migration-specific. The 3 live-DB test failures above are deferred to a
  separate investigation outside this migration's scope.
