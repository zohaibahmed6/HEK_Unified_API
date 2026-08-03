# Migration notes: Application

- TFM: net8.0 -> net10.0 (via `Directory.Build.props`).
- Status: Done
- Packages changed: `Microsoft.Extensions.Logging.Abstractions` 8.0.2 → 10.0.0,
  `Microsoft.Extensions.Options` 8.0.2 → 10.0.0 (kept in step with the net10 runtime).
  FluentValidation/FluentValidation.DependencyInjectionExtensions (11.10.0) and MediatR (12.4.1)
  left unchanged — already net-version-agnostic, no conflict forced a bump.
- Breaking changes hit: none.
- Deferred items: none.
- Tests: `Application.UnitTests` — 7/7 pass.
