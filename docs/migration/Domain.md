# Migration notes: Domain

- TFM: net8.0 -> net10.0 (was redundantly explicit in the .csproj — removed, now inherited from
  `Directory.Build.props` like every other project).
- Status: Done
- Packages changed: none (this project has zero package references).
- Breaking changes hit: none.
- Deferred items: none.
