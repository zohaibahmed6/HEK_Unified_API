# Migration notes: Infrastructure

- TFM: net8.0 -> net10.0 (via `Directory.Build.props`).
- Status: Done
- Packages changed:
  - `Microsoft.EntityFrameworkCore.SqlServer` / `.Design` / `.Tools` 8.0.10 → 10.0.0 (provider
    exists and resolved cleanly for net10 — no lag).
  - `Microsoft.Data.SqlClient` 6.0.0 → 6.1.1 — **forced**, not optional: EF Core SqlServer 10.0.0
    requires `Microsoft.Data.SqlClient >= 6.1.1` transitively (NU1605 package-downgrade otherwise).
    Risk: `AWSDocCore.dll` (vendored, plain `<Reference>`) is strong-name-bound to exactly
    `Microsoft.Data.SqlClient 6.0.0.0`. Verified this does **not** break in practice — .NET's
    assembly loader for `<Reference>` DLLs resolved against 6.1.1 without a `FileNotFoundException`,
    and the Docker-based smoke test (HISO SOAP path, which is the AWSDocCore-eligible code path)
    came up healthy. Re-verify explicitly if any AWS-eligible HISO document call misbehaves in
    real use — this was a documented incident before (see the pin's original comment history in
    `Directory.Packages.props`).
  - `Microsoft.Identity.Client` 4.66.2 → 4.73.1 — forced transitively (`Microsoft.Data.SqlClient
    6.1.1 -> Azure.Identity 1.14.2 -> Microsoft.Identity.Client >= 4.73.1`).
  - `Microsoft.Extensions.Caching.Memory` 8.0.1 → 10.0.0 — forced transitively
    (`Microsoft.Data.SqlClient 6.1.1` requires `>= 9.0.0`; bumped to 10.0.0 to match the runtime).
  - `System.Security.Cryptography.Xml` — added as an explicit `PackageReference` (previously only
    transitive) to force resolution to 10.0.0, the latest available. NuGet's audit DB still flags
    it (NU1903, GHSA-23rf-6693-g89p and related) at every version tried — no fixed release exists
    yet. Suppressed solution-wide via `Directory.Build.props`'s `NoWarn`. **Accepted, documented
    risk** — revisit when a newer patch ships.
  - `System.IdentityModel.Tokens.Jwt` / `Microsoft.IdentityModel.Tokens` — left at 8.14.0
    (unchanged). Pinned for `AWSDocCore.dll`'s strong-name binding; no dependency in the graph
    forced a bump, so left untouched rather than risk breaking that binding unnecessarily.
  - `System.Text.Encoding.CodePages` — **removed** 2026-08-03 (was only needed by the old vendored
    net40 `Aspose.Words.dll`, no longer referenced at all).
  - `SixLabors.ImageSharp` 3.1.11 — added 2026-08-03 for `AsposeMimeConverter.ConvertImageToPdfAsync`
    (see below). Open-source, cross-platform image decode paired with the already-present
    `PdfSharp` to draw images onto PDF pages. No license/watermark concern.
  - `Aspose.Words` 24.11.0 — added 2026-08-03, the **official NuGet package**, replacing the old
    vendored net40 `Aspose.Words.dll` `<Reference>` entirely, for
    `AsposeMimeConverter.ConvertHtmlToPdfAsync`. Confirmed via an isolated scratch probe to build
    and run cleanly under net10 (the old DLL's FormatException does not reproduce). Currently
    **unlicensed/evaluation** - the existing `.lic` file (expiry 2017-04-20) is rejected by this
    newer package version (confirmed live error message, see `MIGRATION_STATUS.md`); Zohaib's
    explicit call (2026-08-03) is to run unlicensed for now and add a current license later. Output
    PDFs carry Aspose's evaluation watermark until that happens - **not acceptable for real patient
    documents in production as-is**.
- Breaking changes hit: none in code from the TFM/package bump itself (all resolved via package
  version bumps above). **Separately found post-migration**: `AsposeMimeConverter`'s (old, vendored
  DLL) Aspose.Words license failed to load under net10 (root cause traced to the old net40 DLL
  itself, not reproducible with the newer NuGet package - see `MIGRATION_STATUS.md`), and because
  the license was loaded eagerly in the constructor, this crashed the whole class (not just the
  Aspose-dependent calls) with a 500 on every use. Fixed 2026-08-03: license loading no longer
  throws from the constructor (captured as `Lazy<Exception?>` instead of `Lazy<bool>`),
  `ConvertImageToPdfAsync` was rewritten on ImageSharp + PdfSharp (no Aspose dependency at all, no
  license concern), and `ConvertHtmlToPdfAsync` was moved onto the official Aspose.Words NuGet
  package (currently unlicensed/evaluation per Zohaib - see above) instead of PuppeteerSharp
  (tried first, reverted - Docker build in this environment couldn't reach `archive.ubuntu.com` to
  install Chromium's system libraries, a network restriction specific to this dev machine, not a
  code issue).
- Deferred items: a current Aspose.Words license needs to be purchased/renewed and dropped in at
  `Legacy/Hiso/vendor/Aspose.Words.lic` before HTML-to-PDF output ships to real users (no code
  change needed when that happens).
- Tests: `Infrastructure.UnitTests` — 4/4 pass. `Api.IntegrationTests` — 49/49 pass (full solution).
  Postman `crosscheck` collection New API folders — 59/59 pass.
