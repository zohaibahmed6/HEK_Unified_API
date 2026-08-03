# Migration notes: Adapters.Hiso

- TFM: net8.0 -> net10.0 (via `Directory.Build.props`).
- Status: Done
- Packages changed: none (no direct `PackageReference`s — only `ProjectReference`s to
  Application + Contracts, which carry their own upgraded transitive packages).
- Breaking changes hit: none.
- Deferred items: none.
