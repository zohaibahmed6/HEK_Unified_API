// Generates the remaining per-endpoint Mermaid flow-diagram pages in this folder, using the exact
// template/CSS/JS already hand-authored in karo-authenticate-flow.html / col-saveinvoice-flow.html.
// Run: node gen-flows.js
const fs = require("fs");
const path = require("path");

function page({ file, title, h1, dek, mermaid, minWidth = 1400, cards = [], sourceRows = [], companions = "" }) {
  const cardHtml = cards
    .map(
      (c) =>
        `      <div class="legend-card">\n        <span class="tag">${esc(c.tag)}</span>\n        <h3>${esc(c.h3)}</h3>\n        <p>${c.p}</p>\n      </div>`
    )
    .join("\n");
  const rowsHtml = sourceRows
    .map((r, i) => `          <tr><td class="n">${i + 1}</td><td>${esc(r[0])}</td><td class="src">${r[1]}</td></tr>`)
    .join("\n");

  return `<title>${esc(title)}</title>
<script src="https://cdn.jsdelivr.net/npm/mermaid@10.9.1/dist/mermaid.min.js"></script>
<script>
  mermaid.initialize({ startOnLoad: true, theme: window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'default' });
</script>
<style>
  :root {
    --ink: #161a1d; --paper: #f3f1ea; --panel: #ffffff; --line: #dcd7c9; --muted: #64655c;
    --accent: #2f6f6d; --accent-strong: #1c4b49; --accent-soft: #dfeae8;
    --amber: #a8621a; --amber-soft: #f4e6d4; --code-bg: #eee9db;
    --shadow: 0 1px 2px rgba(22,26,29,0.06), 0 8px 24px rgba(22,26,29,0.05);
  }
  :root[data-theme="dark"] {
    --ink: #eceae3; --paper: #14171a; --panel: #1b1f22; --line: #2c3033; --muted: #9a9c93;
    --accent: #63b8b3; --accent-strong: #8fd0cb; --accent-soft: #1f3634;
    --amber: #e2984f; --amber-soft: #3a2c1a; --code-bg: #23272a;
    --shadow: 0 1px 2px rgba(0,0,0,0.4), 0 8px 24px rgba(0,0,0,0.35);
  }
  @media (prefers-color-scheme: dark) {
    :root:not([data-theme="light"]) {
      --ink: #eceae3; --paper: #14171a; --panel: #1b1f22; --line: #2c3033; --muted: #9a9c93;
      --accent: #63b8b3; --accent-strong: #8fd0cb; --accent-soft: #1f3634; --amber: #e2984f;
      --amber-soft: #3a2c1a; --code-bg: #23272a; --shadow: 0 1px 2px rgba(0,0,0,0.4), 0 8px 24px rgba(0,0,0,0.35);
    }
  }
  * { box-sizing: border-box; }
  html, body { background: var(--paper); color: var(--ink); margin: 0; font-family: -apple-system, "Segoe UI", "Helvetica Neue", Arial, sans-serif; }
  body { padding: 48px 24px 96px; }
  .wrap { max-width: 980px; margin: 0 auto; }
  .eyebrow { font-family: ui-monospace, "SFMono-Regular", Menlo, Consolas, monospace; font-size: 12.5px; letter-spacing: 0.08em; text-transform: uppercase; color: var(--accent-strong); margin: 0 0 10px; }
  h1 { font-family: ui-serif, "Iowan Old Style", "Palatino Linotype", Georgia, serif; font-weight: 600; font-size: clamp(28px, 4vw, 38px); line-height: 1.12; letter-spacing: -0.01em; margin: 0 0 12px; text-wrap: balance; }
  .dek { font-size: 16px; line-height: 1.55; color: var(--muted); max-width: 62ch; margin: 0 0 36px; }
  .dek code, p code, li code, td code { font-family: ui-monospace, "SFMono-Regular", Menlo, Consolas, monospace; font-size: 0.92em; background: var(--code-bg); padding: 0.08em 0.4em; border-radius: 4px; }
  section { margin-bottom: 44px; }
  h2 { font-family: ui-serif, "Iowan Old Style", "Palatino Linotype", Georgia, serif; font-size: 20px; font-weight: 600; margin: 0 0 4px; letter-spacing: -0.005em; }
  .section-note { font-size: 13.5px; color: var(--muted); margin: 0 0 18px; }
  .diagram-frame { background: var(--panel); border: 1px solid var(--line); border-radius: 10px; box-shadow: var(--shadow); padding: 20px 18px; }
  .zoom-bar { display: flex; align-items: center; gap: 10px; margin-bottom: 14px; }
  .zoom-bar-bottom { margin-bottom: 0; margin-top: 14px; }
  .zoom-bar button { font-family: ui-monospace, "SFMono-Regular", Menlo, Consolas, monospace; font-size: 15px; line-height: 1; width: 34px; height: 34px; border-radius: 8px; border: 1px solid var(--line); background: var(--paper); color: var(--ink); cursor: pointer; }
  .zoom-bar button:hover { border-color: var(--accent); color: var(--accent-strong); }
  .zoom-bar button:focus-visible { outline: 2px solid var(--accent); outline-offset: 2px; }
  .zoom-bar .pct { font-family: ui-monospace, "SFMono-Regular", Menlo, Consolas, monospace; font-size: 13px; font-variant-numeric: tabular-nums; color: var(--muted); min-width: 46px; text-align: center; }
  .zoom-bar .hint { font-size: 12.5px; color: var(--muted); margin-left: auto; }
  .diagram-scroll { overflow: auto; max-height: 78vh; padding-bottom: 4px; border-radius: 6px; }
  .diagram-scroll .mermaid { min-width: ${minWidth}px; }
  .diagram-scroll svg { display: block; }
  .diagram-scroll .mermaid text, .diagram-scroll svg text { font-size: 16px !important; }
  .diagram-scroll .mermaid .messageText, .diagram-scroll svg .messageText { font-size: 16px !important; }
  .diagram-scroll .mermaid .labelText, .diagram-scroll .mermaid .loopText, .diagram-scroll svg .labelText, .diagram-scroll svg .loopText { font-size: 15px !important; }
  .legend-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(280px, 1fr)); gap: 16px; }
  .legend-card { background: var(--panel); border: 1px solid var(--line); border-radius: 10px; padding: 18px 20px; box-shadow: var(--shadow); }
  .legend-card .tag { display: inline-block; font-family: ui-monospace, "SFMono-Regular", Menlo, Consolas, monospace; font-size: 11.5px; letter-spacing: 0.04em; text-transform: uppercase; padding: 2px 8px; border-radius: 999px; background: var(--amber-soft); color: var(--amber); margin-bottom: 10px; }
  .legend-card h3 { font-size: 15.5px; margin: 0 0 8px; font-weight: 650; }
  .legend-card p { font-size: 13.8px; line-height: 1.55; color: var(--muted); margin: 0 0 8px; }
  .legend-card p:last-child { margin-bottom: 0; }
  table.steps { width: 100%; border-collapse: collapse; font-size: 13.5px; }
  table.steps th, table.steps td { text-align: left; padding: 9px 12px; border-bottom: 1px solid var(--line); vertical-align: top; }
  table.steps th { font-family: ui-monospace, "SFMono-Regular", Menlo, Consolas, monospace; font-size: 11px; letter-spacing: 0.06em; text-transform: uppercase; color: var(--muted); font-weight: 600; }
  table.steps td.n { font-variant-numeric: tabular-nums; color: var(--accent-strong); font-weight: 700; width: 28px; }
  table.steps td.src code { font-size: 11.6px; white-space: nowrap; }
  .table-wrap { overflow-x: auto; border: 1px solid var(--line); border-radius: 10px; background: var(--panel); box-shadow: var(--shadow); }
  .table-wrap table.steps { border: none; box-shadow: none; }
  .table-wrap td, .table-wrap th { border-color: var(--line); }
  footer { margin-top: 56px; padding-top: 18px; border-top: 1px solid var(--line); font-size: 12.5px; color: var(--muted); }
</style>

<div class="wrap">
  <p class="eyebrow">HEK Core API — legacy-compat wire flow</p>
  <h1>${h1}</h1>
  <p class="dek">${dek}</p>

  <section>
    <h2>Sequence</h2>
    <p class="section-note">Use the zoom controls below if the text reads small — the diagram itself scrolls both ways once zoomed in.</p>
    <div class="diagram-frame">
      <div class="zoom-bar">
        <button type="button" id="zoomOut" aria-label="Zoom out">−</button>
        <span class="pct" id="zoomPct">150%</span>
        <button type="button" id="zoomIn" aria-label="Zoom in">+</button>
        <button type="button" id="zoomReset" aria-label="Reset zoom" style="width:auto;padding:0 12px;">Reset</button>
        <span class="hint">Drag the scrollbars, or scroll while hovering, to pan around</span>
      </div>
      <div class="diagram-scroll" id="diagramScroll">
        <pre class="mermaid">
${mermaid.trim()}
        </pre>
      </div>
      <div class="zoom-bar zoom-bar-bottom">
        <button type="button" id="zoomOutBottom" aria-label="Zoom out">−</button>
        <span class="pct" id="zoomPctBottom">150%</span>
        <button type="button" id="zoomInBottom" aria-label="Zoom in">+</button>
        <button type="button" id="zoomResetBottom" aria-label="Reset zoom" style="width:auto;padding:0 12px;">Reset</button>
        <span class="hint">Drag the scrollbars, or scroll while hovering, to pan around</span>
      </div>
    </div>
  </section>

${
  cards.length
    ? `  <section>
    <h2>Worth knowing</h2>
    <p class="section-note">The diagram above is drawn as the everything-succeeds path.</p>
    <div class="legend-grid">
${cardHtml}
    </div>
  </section>

`
    : ""
}  <section>
    <h2>Step-by-step source index</h2>
    <p class="section-note">Where each stage of the diagram actually lives in the codebase.</p>
    <div class="table-wrap">
      <table class="steps">
        <thead><tr><th>#</th><th>Stage</th><th>Real source</th></tr></thead>
        <tbody>
${rowsHtml}
        </tbody>
      </table>
    </div>
  </section>

  <footer>
    Built from the verified source in <code>HEK Core API</code> — no procedure name, field, or table
    above is inferred; each is confirmed against the real controller/repository source.${companions ? " Companion diagrams in this same folder: " + companions + "." : ""}
  </footer>
</div>

<script>
  (function () {
    var scrollBox = document.getElementById('diagramScroll');
    var pctLabels = [document.getElementById('zoomPct'), document.getElementById('zoomPctBottom')];
    var BASE_WIDTH = ${minWidth};
    var scale = 1.5;
    var svgEl = null;
    function apply() {
      if (!svgEl) return;
      svgEl.style.width = (BASE_WIDTH * scale) + 'px';
      svgEl.style.height = 'auto';
      svgEl.style.maxWidth = 'none';
      pctLabels.forEach(function (el) { el.textContent = Math.round(scale * 100) + '%'; });
    }
    function findSvgAndInit() {
      var svg = scrollBox.querySelector('svg');
      if (!svg || svg === svgEl) return;
      svgEl = svg;
      apply();
    }
    var observer = new MutationObserver(findSvgAndInit);
    observer.observe(scrollBox, { childList: true, subtree: true });
    findSvgAndInit();
    setTimeout(findSvgAndInit, 300);
    setTimeout(findSvgAndInit, 1000);
    function zoomIn() { scale = Math.min(scale + 0.25, 4); apply(); }
    function zoomOut() { scale = Math.max(scale - 0.25, 0.5); apply(); }
    function zoomReset() { scale = 1.5; apply(); }
    ['zoomIn', 'zoomInBottom'].forEach(function (id) { document.getElementById(id).addEventListener('click', zoomIn); });
    ['zoomOut', 'zoomOutBottom'].forEach(function (id) { document.getElementById(id).addEventListener('click', zoomOut); });
    ['zoomReset', 'zoomResetBottom'].forEach(function (id) { document.getElementById(id).addEventListener('click', zoomReset); });
  })();
</script>
`;
}

function esc(s) {
  return String(s).replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
}

// ---------- shared building blocks ----------

const KARO_HEAD = `
    actor Client
    participant Ctrl as KaroCompatController
    participant Hnd as {{HANDLER}}
    participant Parser as KaroRequestParser
    participant Route as KaroRoutingResolver
    participant TokenVal as KaroTokenValidator
    participant Repo as {{REPO}}
    participant Conn as KaroPracticeConnectionResolver
    participant Tenant as TenantRegistryService
    participant Secret as ISecretProvider
    participant PracticeDb as Practice DB (PMS_NZ_V2)`;

function karoReadDiagram({ method, request, handler, repo, repoMethod, proc, params, mapNote }) {
  return `
sequenceDiagram
    autonumber${KARO_HEAD.replace("{{HANDLER}}", handler).replace("{{REPO}}", repo)}

    Client->>Ctrl: GET ${method} { patientId, encounterId, system, pho${params ? ", " + params : ""} }
    Ctrl->>Hnd: ${request}

    Hnd->>Parser: ParseEncounterId / read bearer token
    Parser-->>Hnd: patientId, encounterId, practiceSuffix
    Hnd->>Route: Resolve EncounterId
    Route-->>Hnd: RoutingContext
    Hnd->>TokenVal: ValidateAsync ...
    TokenVal-->>Hnd: valid

    Hnd->>Repo: ${repoMethod}
    Repo->>Conn: ResolveAsync routingContext
    Conn->>Tenant: ResolveRouteAsync routingContext
    Tenant-->>Conn: PracticeRoute with DbServerHost, DbName
    Conn->>Secret: GetRequiredSecretAsync for DbServerHost
    Secret-->>Conn: credential
    Conn-->>Repo: connection string

    Repo->>PracticeDb: EXEC ${proc}
    PracticeDb-->>Repo: DataTable / DataSet
    Repo-->>Hnd: mapped rows
    ${mapNote ? "Note over Hnd: " + mapNote + "\n    " : ""}Hnd-->>Ctrl: KaroListResult succeeded, entries
    Ctrl-->>Client: 200, patientId/resourceType/system/entry envelope`;
}

function karoWriteDiagram({ method, request, handler, repoMethod, proc, outParam, successNote, failNote }) {
  return `
sequenceDiagram
    autonumber${KARO_HEAD.replace("{{HANDLER}}", handler).replace("{{REPO}}", "KaroWriteRepository")}

    Client->>Ctrl: POST ${method} (JSON body)
    Ctrl->>Hnd: ${request}

    Hnd->>Parser: ParseEncounterId / Decrypt
    Parser-->>Hnd: patientId, encounterId, practiceSuffix
    Hnd->>Route: Resolve EncounterId
    Route-->>Hnd: RoutingContext
    Hnd->>TokenVal: ValidateAsync ...
    TokenVal-->>Hnd: valid

    Hnd->>Repo: ${repoMethod}
    Repo->>Conn: ResolveAsync routingContext
    Conn->>Tenant: ResolveRouteAsync routingContext
    Tenant-->>Conn: PracticeRoute
    Conn->>Secret: GetRequiredSecretAsync
    Secret-->>Conn: credential
    Conn-->>Repo: connection string

    Repo->>PracticeDb: EXEC ${proc}
    PracticeDb-->>Repo: ${outParam}

    alt ${outParam} greater than 0
        Repo-->>Hnd: success
        Hnd-->>Ctrl: KaroWriteResult true
        Ctrl-->>Client: 200, status success${successNote ? "\n        Note over Ctrl: " + successNote : ""}
    else ${outParam} 0 or less
        Repo-->>Hnd: failure
        Hnd-->>Ctrl: KaroWriteResult false
        Ctrl-->>Client: 200, status fail${failNote ? "\n        Note over Ctrl: " + failNote : ""}
    end`;
}

const ERMS_HEAD = `
    actor Client
    participant Ctrl as ErmsCompatController
    participant Hnd as {{HANDLER}}
    participant Parser as ErmsRequestParser
    participant Route as ErmsRoutingResolver
    participant TokenVal as ErmsTokenValidator
    participant Repo as {{REPO}}
    participant Conn as ErmsPracticeConnectionResolver
    participant Tenant as TenantRegistryService
    participant Secret as ISecretProvider
    participant PracticeDb as Practice DB (PMS_NZ_V2)`;

function ermsReadDiagram({ method, request, handler, repo, repoMethod, proc, params, renderNote }) {
  return `
sequenceDiagram
    autonumber${ERMS_HEAD.replace("{{HANDLER}}", handler).replace("{{REPO}}", repo)}

    Client->>Ctrl: GET ${method}?pmsPatientId=...&pmsEncounterId=...${params ? "&" + params : ""}
    Ctrl->>Hnd: ${request}

    Hnd->>Parser: ParseEncounterId / Decrypt DecodeBase64 ...
    Parser-->>Hnd: patientId, encounterId, practiceSuffix
    Hnd->>Route: Resolve EncounterId
    Route-->>Hnd: RoutingContext
    Hnd->>TokenVal: ValidateAsync ...
    TokenVal-->>Hnd: valid

    Hnd->>Repo: ${repoMethod}
    Repo->>Conn: ResolveAsync routingContext
    Conn->>Tenant: ResolveRouteAsync routingContext
    Tenant-->>Conn: PracticeRoute with DbServerHost, DbName
    Conn->>Secret: GetRequiredSecretAsync for DbServerHost
    Secret-->>Conn: credential
    Conn-->>Repo: connection string

    Repo->>PracticeDb: EXEC ${proc}
    PracticeDb-->>Repo: DataTable
    Repo-->>Hnd: ErmsReadResult with Table
    Ctrl->>Ctrl: ${renderNote || "Render(...) maps DataTable to the real XML envelope"}
    Ctrl-->>Client: 200, application/xml (error, if any, as &lt;Error&gt;&lt;Message&gt;, still HTTP 200)`;
}

const COL_HEAD = `
    actor Client
    participant Ctrl as ColCompatController
    participant Hnd as {{HANDLER}}
    participant Parser as ColRequestParser
    participant Route as ErmsRoutingResolver
    participant TokenVal as ErmsTokenValidator
    participant Repo as ColDataRepository
    participant Conn as ErmsPracticeConnectionResolver
    participant Tenant as TenantRegistryService
    participant Secret as ISecretProvider
    participant PracticeDb as Practice DB (PMS_NZ_V2)`;

function colReadDiagram({ method, request, handler, repoMethod, proc, params, note }) {
  return `
sequenceDiagram
    autonumber${COL_HEAD.replace("{{HANDLER}}", handler)}

    Client->>Ctrl: GET ${method}?pmsPatientId=...&pmsEncounterId=...${params ? "&" + params : ""}
    Ctrl->>Hnd: ${request}

    Hnd->>Parser: ParseEncounterId / Decrypt
    Parser-->>Hnd: patientId, encounterId, practiceSuffix
    Hnd->>Route: Resolve EncounterId
    Note over Route: COL has no routing resolver of its own — IErmsRoutingResolver, shared verbatim
    Route-->>Hnd: RoutingContext
    Hnd->>TokenVal: ValidateAsync ...
    Note over TokenVal: also IErmsTokenValidator — same shared instance
    TokenVal-->>Hnd: valid

    Hnd->>Repo: ${repoMethod}
    Repo->>Conn: ResolveAsync routingContext
    Conn->>Tenant: ResolveRouteAsync routingContext
    Tenant-->>Conn: PracticeRoute
    Conn->>Secret: GetRequiredSecretAsync
    Secret-->>Conn: credential
    Conn-->>Repo: connection string

    Repo->>PracticeDb: EXEC ${proc}
    PracticeDb-->>Repo: DataTable${note ? "\n    Note over Repo: " + note : ""}
    Repo-->>Hnd: ColReadResult with Table
    Hnd-->>Ctrl: succeeded
    Ctrl-->>Client: 200, JSON list (or one empty object if no rows)`;
}

// ---------- endpoint data ----------

const pages = [];

// KARO reads
pages.push({
  file: "karo-demographics-flow.html",
  title: "KARO/HSS demographics — Request Flow",
  h1: 'KARO/HSS <code>demographics</code>: patient + card rows',
  dek: "Traced from the real, verified source in this repo. Uses its own dedicated repository, not the shared multi-resource one the other reads use.",
  mermaid: karoReadDiagram({
    method: "demographics",
    request: "KaroDemographicsQuery(system, pho, patientId, encounterId, token)",
    handler: "KaroDemographicsQueryHandler",
    repo: "KaroDemographicsRepository",
    repoMethod: "GetAsync(practiceSuffix, routingContext, patientId, ct)",
    proc: "HSS.uspGetDemographics pPatientId",
    mapNote: "returns a 2-table DataSet — patient row + up to 2 card rows",
  }),
  cards: [
    {
      tag: "Confirmed live",
      h3: "Real fields, not the naive ones",
      p: "The usable name/DOB columns are <code>Given</code>/<code>Family</code>/<code>BirthDate</code>, not <code>FirstName</code>/<code>LastName</code>/<code>DateOfBirth</code> — confirmed against real <code>PMS_NZ_V2</code> data (patient 2459731) this session.",
    },
  ],
  sourceRows: [
    ["REST entry point", "<code>KaroCompatController.cs</code>"],
    ["Query handler", "<code>KaroDemographicsQuery.cs</code>"],
    ["Dedicated repository", "<code>KaroDemographicsRepository.cs</code>"],
    ["Connection + tenant routing", "<code>KaroPracticeConnectionResolver.cs</code>, <code>TenantRegistryService.cs</code>"],
  ],
  companions: "karo-authenticate-flow.html, karo-clinicalnotes-flow.html",
});

const karoSimpleReads = [
  ["clinicalnotes-read", "KaroClinicalNotesQuery", "KaroClinicalNotesQueryHandler", "GetConsultNotesAsync(practiceSuffix, routingContext, patientId, ct)", "HSS.uspGetConsultNotes pPatientId"],
  ["conditions", "KaroConditionsQuery", "KaroConditionsQueryHandler", "GetConditionsAsync(practiceSuffix, routingContext, patientId, ct)", "HSS.uspGetConditions pPatientId"],
  ["labresults", "KaroLabResultsQuery", "KaroLabResultsQueryHandler", "GetLabResultsAsync(practiceSuffix, routingContext, patientId, ct)", "HSS.uspGetLabResults pPatientId"],
  ["medications", "KaroMedicationsQuery", "KaroMedicationsQueryHandler", "GetMedicationsAsync(practiceSuffix, routingContext, patientId, ct)", "HSS.uspGetMedications pPatientId"],
  ["recalls", "KaroRecallsQuery", "KaroRecallsQueryHandler", "GetRecallsAsync(practiceSuffix, routingContext, patientId, ct)", "HSS.uspGetRecalls pPatientId"],
  ["screeningcodes", "KaroScreeningCodesQuery", "KaroScreeningCodesQueryHandler", "GetScreeningCodesAsync(practiceSuffix, routingContext, ct)", "HSS.uspGetScreeningCodes pPracticeId=\"6\" (real legacy constant)"],
];
for (const [route, req, hnd, repoMethod, proc] of karoSimpleReads) {
  const routeLabel = route.replace("-read", "");
  pages.push({
    file: `karo-${route}-flow.html`,
    title: `KARO/HSS ${routeLabel} — Request Flow`,
    h1: `KARO/HSS <code>${routeLabel}</code>: one stored procedure`,
    dek: "Traced from the real, verified source in this repo. Same authenticate-time routing/token components as <code>karo-authenticate-flow.html</code>, one read, one procedure.",
    mermaid: karoReadDiagram({ method: routeLabel, request: `${req}(system, pho, patientId, encounterId, token)`, handler: hnd, repo: "KaroDataRepository", repoMethod: `${repoMethod}`, proc }),
    sourceRows: [
      ["REST entry point", "<code>KaroCompatController.cs</code>"],
      ["Query handler", `<code>${req}.cs</code>`],
      ["Shared multi-resource repository", "<code>KaroDataRepository.cs</code>"],
      ["Connection + tenant routing", "<code>KaroPracticeConnectionResolver.cs</code>, <code>TenantRegistryService.cs</code>"],
    ],
    companions: "karo-demographics-flow.html, karo-clinicalnotes-flow.html",
  });
}

pages.push({
  file: "karo-documents-flow.html",
  title: "KARO/HSS documents — Request Flow",
  h1: "KARO/HSS <code>documents</code>: branches to AWS on some practices",
  dek: "Traced from the real, verified source in this repo. Same shape as ERMS's discharge/scanned reads — the practice's own AWS-enabled flag decides which of two real procedures runs.",
  minWidth: 1500,
  mermaid: `
sequenceDiagram
    autonumber${KARO_HEAD.replace("{{HANDLER}}", "KaroDocumentsQueryHandler").replace("{{REPO}}", "KaroDataRepository")}
    participant Aws as AwsDocumentService

    Client->>Ctrl: GET documents { patientId, encounterId, identifier }
    Ctrl->>Hnd: KaroDocumentsQuery(system, pho, patientId, encounterId, identifier, token)

    Hnd->>Parser: ParseEncounterId / read token
    Parser-->>Hnd: patientId, encounterId, practiceSuffix, practiceSuffixNumeric
    Hnd->>Route: Resolve EncounterId
    Route-->>Hnd: RoutingContext
    Hnd->>TokenVal: ValidateAsync ...
    TokenVal-->>Hnd: valid

    Hnd->>Repo: GetDocumentsAsync practiceSuffix, practiceSuffixNumeric,<br/>routingContext, patientId, identifier
    Repo->>Conn: ResolveAsync routingContext
    Conn-->>Repo: connection string
    Repo->>Aws: CheckAwsIsEnabledAsync practiceIdInt, connectionString
    Aws-->>Repo: awsEnabled

    alt awsEnabled is false
        Repo->>PracticeDb: EXEC HSS.uspGetDocuments pPatientId
    else awsEnabled is true
        Repo->>PracticeDb: EXEC HSS.uspGetDocuments_AWS pPatientId
        opt identifier supplied
            Repo->>Aws: DocumentGetByDocumentKeyJsonResultAsync identifier,<br/>practiceIdInt, dmsConnectionString, connectionString
            Aws-->>Repo: single document JSON (base64 content)
        end
    end
    PracticeDb-->>Repo: DataTable
    Repo-->>Hnd: KaroDocumentInfo list
    Hnd-->>Ctrl: succeeded
    Ctrl-->>Client: 200, patientId/resourceType/system/entry envelope`,
  cards: [
    {
      tag: "Conditional",
      h3: "Real AWS branch, checked per practice",
      p: "Not a global switch — <code>CheckAwsIsEnabledAsync</code> queries the practice's own DB live. When enabled and a specific <code>identifier</code> is requested, a second AWS call fetches the real document bytes.",
    },
  ],
  sourceRows: [
    ["REST entry point", "<code>KaroCompatController.cs</code>"],
    ["Query handler", "<code>KaroDocumentsQuery.cs</code>"],
    ["AWS-branching repository", "<code>KaroDataRepository.cs</code> (<code>GetDocumentsAsync</code>)"],
    ["AWS document service", "<code>AwsDocumentService.cs</code>"],
  ],
  companions: "karo-patientattachment-flow.html, karo-document-flow.html",
});

pages.push({
  file: "karo-observations-flow.html",
  title: "KARO/HSS observations (read) — Request Flow",
  h1: "KARO/HSS <code>observations</code>: optional concept filter",
  dek: "Traced from the real, verified source in this repo.",
  mermaid: karoReadDiagram({
    method: "observations",
    request: "KaroObservationsQuery(system, pho, patientId, encounterId, conceptId, token)",
    handler: "KaroObservationsQueryHandler",
    repo: "KaroDataRepository",
    repoMethod: "GetObservationsAsync(practiceSuffix, routingContext, patientId, conceptId, ct)",
    proc: "HSS.uspGetObservations pPatientId, pConceptId",
    params: "conceptId",
  }),
  sourceRows: [
    ["REST entry point", "<code>KaroCompatController.cs</code>"],
    ["Query handler", "<code>KaroObservationsQuery.cs</code>"],
    ["Repository", "<code>KaroDataRepository.cs</code> (<code>GetObservationsAsync</code>)"],
  ],
  companions: "karo-observations-write-flow.html",
});

pages.push({
  file: "karo-provider-flow.html",
  title: "KARO/HSS provider — Request Flow",
  h1: "KARO/HSS <code>provider</code>: needs a real userId",
  dek: "Traced from the real, verified source in this repo. Confirmed live this session with userId=1.",
  mermaid: karoReadDiagram({
    method: "provider",
    request: "KaroProviderQuery(system, pho, patientId, encounterId, userId, token)",
    handler: "KaroProviderQueryHandler",
    repo: "KaroDataRepository",
    repoMethod: "GetProviderAsync(practiceSuffix, routingContext, patientId, userId, ct)",
    proc: "HSS.uspGetProvider pUserId",
    params: "userId",
  }),
  sourceRows: [
    ["REST entry point", "<code>KaroCompatController.cs</code>"],
    ["Query handler", "<code>KaroProviderQuery.cs</code>"],
    ["Repository", "<code>KaroDataRepository.cs</code> (<code>GetProviderAsync</code>)"],
  ],
});

pages.push({
  file: "karo-recallcategories-flow.html",
  title: "KARO/HSS recallcategories — Request Flow",
  h1: "KARO/HSS <code>recallcategories</code>: filtered by group name",
  dek: "Traced from the real, verified source in this repo. Confirmed live this session — real, but returns an empty list for groups with no seeded RecallCategory row on this practice.",
  mermaid: karoReadDiagram({
    method: "recallcategories",
    request: "KaroRecallCategoriesQuery(system, pho, patientId, encounterId, group, token)",
    handler: "KaroRecallCategoriesQueryHandler",
    repo: "KaroDataRepository",
    repoMethod: "GetRecallCategoriesAsync(practiceSuffix, routingContext, group, ct)",
    proc: "HSS.uspGetRecallCategories pGroup",
    params: "group (required)",
  }),
  cards: [
    {
      tag: "Confirmed live",
      h3: "Empty result is a real data gap, not a bug",
      p: 'Ran with <code>group="Immunisation"</code> and <code>group="Recall"</code> this session — both returned an empty <code>entry:[]</code>, which is also why the write endpoint (<code>karo-recalls-write-flow.html</code>) rejects <code>categoryId=1</code>: no such category is seeded for this practice.',
    },
  ],
  sourceRows: [
    ["REST entry point", "<code>KaroCompatController.cs</code>"],
    ["Query handler", "<code>KaroRecallCategoriesQuery.cs</code>"],
    ["Repository", "<code>KaroDataRepository.cs</code> (<code>GetRecallCategoriesAsync</code>)"],
  ],
  companions: "karo-recalls-write-flow.html",
});

pages.push({
  file: "karo-encountersummary-flow.html",
  title: "KARO/HSS encountersummary — Request Flow",
  h1: "KARO/HSS <code>encountersummary</code>: a genuine hardcoded stub",
  dek: "Traced from the real, verified source in this repo. No database call at all — the real legacy endpoint returns fixed JSON, reproduced verbatim rather than \"implemented for real.\"",
  minWidth: 1200,
  mermaid: `
sequenceDiagram
    autonumber
    actor Client
    participant Ctrl as KaroCompatController
    participant Hnd as KaroEncounterSummaryQueryHandler

    Client->>Ctrl: GET encountersummary { patientId, encounterId, identifier }
    Ctrl->>Hnd: KaroEncounterSummaryQuery(system, pho, patientId, encounterId, identifier, token)
    Note over Hnd: no parser, no routing, no DB call —<br/>legacy's real GetEncounterSummary genuinely returns<br/>a fixed JSON stub, not a live query
    Hnd-->>Ctrl: fixed JSON result
    Ctrl-->>Client: 200, application/json (the stub body)`,
  cards: [
    {
      tag: "Real legacy behaviour",
      h3: "Not a gap this project introduced",
      p: "Confirmed from the real legacy source — <code>GetEncounterSummary</code> genuinely never touches the database. Reproducing it as a real stub (rather than \"finishing\" it) is the whole point of this compat layer.",
    },
  ],
  sourceRows: [
    ["REST entry point", "<code>KaroCompatController.cs</code>"],
    ["Query handler (hardcoded stub)", "<code>KaroEncounterSummaryQuery.cs</code>"],
  ],
});

pages.push({
  file: "karo-patientattachment-flow.html",
  title: "KARO/HSS patientattachment — Request Flow",
  h1: "KARO/HSS <code>patientattachment</code>: the widest filter set, same AWS branch",
  dek: "Traced from the real, verified source in this repo. Most-parameterized KARO read — reference/sort/subject/date filters all optional, same AWS-enabled branch as <code>documents</code>.",
  mermaid: karoReadDiagram({
    method: "patientattachment",
    request: "KaroPatientAttachmentQuery(system, pho, patientId, encounterId, referenceID,<br/>sortOrder, subject, dateFrom, dateTo, token)",
    handler: "KaroPatientAttachmentQueryHandler",
    repo: "KaroDataRepository",
    repoMethod: "GetPatientAttachmentAsync(practiceSuffix, practiceSuffixNumeric,<br/>routingContext, patientId, referenceId, sortOrder, subject, dateFrom, dateTo, ct)",
    proc: "HSS.uspGetPatientDMS pPatientId, ...(optional filters)",
    params: "referenceID, sortOrder, subject, dateFrom, dateTo",
    mapNote: "confirmed live this session — call succeeds, empty result for this test patient",
  }),
  sourceRows: [
    ["REST entry point", "<code>KaroCompatController.cs</code>"],
    ["Query handler", "<code>KaroPatientAttachmentQuery.cs</code>"],
    ["Repository", "<code>KaroDataRepository.cs</code> (<code>GetPatientAttachmentAsync</code>)"],
  ],
  companions: "karo-documents-flow.html",
});

// KARO writes
pages.push({
  file: "karo-conditions-write-flow.html",
  title: "KARO/HSS conditions (write) — Request Flow",
  h1: "KARO/HSS <code>conditions</code> write: diagnosis with a duplicate sentinel",
  dek: "Traced from the real, verified source in this repo. <code>-5</code> is a real, specific 'already exists' sentinel the real stored procedure returns.",
  mermaid: karoWriteDiagram({
    method: "conditions",
    request: "KaroSaveConditionCommand(PatientId, EncounterId, UserId, Type, OnSetDate,<br/>Summary, IsLongTerm, ConceptId, Name, FSN, BearerToken)",
    handler: "KaroSaveConditionCommandHandler",
    repoMethod: "SaveConditionAsync(practiceSuffix, routingContext, patientId, appointmentId,<br/>userId, diagnosisType, onsetDate, summary, isLongTerm, conceptId, diseaseName, fsn, ct)",
    proc: "HSS.uspInsertUpdateDiagnosis pPatientId, pAppointmentId, pUserId,<br/>pDiagnosisType, pOnsetDate, pSummary, pConceptId, pDiseaseName, pFSN, pIsLongTerm",
    outParam: "pOutputParam",
    successNote: "confirmed live this session (Type 2 diabetes mellitus, concept 44054006)",
    failNote: "-5 = real 'diagnosis already exists' sentinel",
  }),
  sourceRows: [
    ["REST entry point", "<code>KaroCompatController.cs</code>"],
    ["Command handler", "<code>KaroWriteCommands.cs</code> (<code>KaroSaveConditionCommandHandler</code>)"],
    ["Write repository", "<code>KaroWriteRepository.cs</code> (<code>SaveConditionAsync</code>)"],
  ],
  companions: "karo-clinicalnotes-flow.html",
});

pages.push({
  file: "karo-observations-write-flow.html",
  title: "KARO/HSS observations (write) — Request Flow",
  h1: "KARO/HSS <code>observations</code> write: vitals in one call",
  dek: "Traced from the real, verified source in this repo. Confirmed live this session, then read back via the observations read to verify the round trip.",
  mermaid: karoWriteDiagram({
    method: "observations",
    request: "KaroSaveObservationsCommand(PatientId, EncounterId, UserId, Temperature,<br/>WaistCircumference, Height, Weight, BPSys, BPDia, HeartRate, Notes, Risk, Framingham, BearerToken)",
    handler: "KaroSaveObservationsCommandHandler",
    repoMethod: "SaveObservationsAsync(practiceSuffix, routingContext, patientId, appointmentId,<br/>userId, temperature, waist, height, weight, bpSys, bpDia, heartRate, notes, risk, framingham, ct)",
    proc: "HSS.uspInsertUpdateObservation pPatientId, pAppointmentId, ...(optional vitals)",
    outParam: "pOutputParam",
    successNote: "confirmed live this session (36.8°C, HR 72, BP 120/80)",
  }),
  sourceRows: [
    ["REST entry point", "<code>KaroCompatController.cs</code>"],
    ["Command handler", "<code>KaroWriteCommands.cs</code> (<code>KaroSaveObservationsCommandHandler</code>)"],
    ["Write repository", "<code>KaroWriteRepository.cs</code> (<code>SaveObservationsAsync</code>)"],
  ],
  companions: "karo-observations-flow.html",
});

pages.push({
  file: "karo-recalls-write-flow.html",
  title: "KARO/HSS recalls (write) — Request Flow",
  h1: "KARO/HSS <code>recalls</code> write: silent rejection, no exception",
  dek: "Traced from the real, verified source in this repo. Confirmed live this session — the real procedure returns a non-positive output with no error text when the category doesn't exist for this practice.",
  mermaid: karoWriteDiagram({
    method: "recalls",
    request: "KaroSaveRecallCommand(PatientId, EncounterId, UserId, Priority, Group,<br/>DueDate, Notes, CategoryId, BearerToken)",
    handler: "KaroSaveRecallCommandHandler",
    repoMethod: "SaveRecallAsync(practiceSuffix, routingContext, patientId, appointmentId,<br/>userId, priority, group, dueDate, notes, categoryId, ct)",
    proc: "HSS.uspInsertUpdateRecall pPatientId, pAppointmentId, pPriority,<br/>pGroup, pDueDate, pNotes, pUserId, pRecallCategoryId",
    outParam: "pOutputParam",
    failNote: "\"Unable to Save Recall\" — real, confirmed this session with categoryId=1 (not seeded for this practice)",
  }),
  cards: [
    {
      tag: "Confirmed live, real limitation",
      h3: "No valid RecallCategory seeded for this practice",
      p: 'The <code>recallcategories</code> read for <code>group="Immunisation"</code>/<code>"Recall"</code> returns empty for this practice — so no <code>categoryId</code> value can currently succeed here. Not a code bug; a test-data gap.',
    },
  ],
  sourceRows: [
    ["REST entry point", "<code>KaroCompatController.cs</code>"],
    ["Command handler", "<code>KaroWriteCommands.cs</code> (<code>KaroSaveRecallCommandHandler</code>)"],
    ["Write repository", "<code>KaroWriteRepository.cs</code> (<code>SaveRecallAsync</code>)"],
  ],
  companions: "karo-recallcategories-flow.html",
});

pages.push({
  file: "karo-invoice-flow.html",
  title: "KARO/HSS invoice (write) — Request Flow",
  h1: "KARO/HSS <code>invoice</code> write: a real, currently-unstable procedure",
  dek: "Traced from the real, verified source in this repo. Succeeded once by hand this session with a real ServiceMappingId, then failed on every repeat with a parameter-count mismatch — a live, environment-side issue outside this project's code.",
  mermaid: karoWriteDiagram({
    method: "invoice",
    request: "KaroSaveInvoiceCommand(PatientId, EncounterId, UserId, Name, Code, Fee, Payee, BearerToken)",
    handler: "KaroSaveInvoiceCommandHandler",
    repoMethod: "SaveInvoiceAsync(practiceSuffix, routingContext, patientId, encounterId,<br/>name, code, fee, userId, payee, ct)",
    proc: "HSS.uspInsertUpdateService pPatientID, pAppointmentId, pMasterServiceName=\"HSS Service\",<br/>pSubServiceName, pSubServiceCode, pFee, pLocationId=\"167\" (hardcoded), pUserId, pPayee",
    outParam: "pOutputParam",
    failNote: "\"Procedure or function uspInsertUpdateService has too many arguments specified\" — real SQL-side error, confirmed reproducible this session",
  }),
  cards: [
    {
      tag: "Real bug, not fixed here",
      h3: "Deployed SP no longer matches this parameter set",
      p: "This exact call (<code>@pPatientID</code>, <code>@pAppointmentId</code>, <code>@pMasterServiceName</code>, <code>@pSubServiceName</code>, <code>@pSubServiceCode</code>, <code>@pFee</code>, <code>@pLocationId</code>, <code>@pUserId</code>, <code>@pPayee</code>, output) succeeded once, then failed identically on every retry including with a brand-new, non-duplicate <code>@pSubServiceCode</code> — ruling out a duplicate-row branch. The real fix is on the SQL side.</p>",
    },
  ],
  sourceRows: [
    ["REST entry point", "<code>KaroCompatController.cs</code>"],
    ["Command handler", "<code>KaroWriteCommands.cs</code> (<code>KaroSaveInvoiceCommandHandler</code>)"],
    ["Write repository", "<code>KaroWriteRepository.cs</code> (<code>SaveInvoiceAsync</code>)"],
  ],
  companions: "col-saveinvoice-flow.html",
});

pages.push({
  file: "karo-summary-flow.html",
  title: "KARO/HSS summary (write) — Request Flow",
  h1: "KARO/HSS <code>summary</code>: a dynamic, schema-driven save",
  dek: "Traced from the real, verified source in this repo. Unlike every other KARO write, this one has no fixed parameter list — it's a real, schema-driven encounter-summary pipeline (diabetes / diabetic-foot-exam / retinopathy screening forms).",
  minWidth: 1500,
  mermaid: `
sequenceDiagram
    autonumber
    actor Client
    participant Ctrl as KaroCompatController
    participant Hnd as KaroSaveSummaryCommandHandler
    participant Repo as KaroWriteRepository

    Client->>Ctrl: POST summary { patientId, encounterID, system,<br/>identifier, providerID, dateTimeRecorded, entry: [...] }
    Note over Ctrl: legacy's own manual JObject property scan is<br/>reproduced field-by-field, case-insensitively
    Ctrl->>Hnd: KaroSaveSummaryCommand(...)

    Hnd->>Repo: SaveSummaryAsync(practiceSuffix, routingContext, patientId,<br/>encounterId, providerId, identifier, dateTimeRecorded, entriesJson, ct)
    Note over Repo: real pipeline — GetTemplateSchema, FillSummaryData,<br/>BuildJsonTimeLineData, then InsertSummary

    alt identifier matches no real schema
        Repo-->>Hnd: -4 (real, specific "invalid identifier" sentinel)
    else outcome invalid
        Repo-->>Hnd: -5 (real, specific "invalid outcome" sentinel)
    else schema found, outcome valid
        Repo->>Repo: InsertSummary(...)
        Repo-->>Hnd: real insertion id, or 0 on generic failure
    end

    Hnd-->>Ctrl: KaroWriteResult
    Ctrl-->>Client: 200, status success or fail`,
  cards: [
    {
      tag: "Design gap noticed this session",
      h3: "No UI field exposes real summary content",
      p: 'The dashboard sends <code>entry: []</code> since no form field maps to it — the real procedure rejects that with "Invalid values passed!" This isn\'t a backend bug; the dashboard simply has no input for the dynamic schema payload yet.',
    },
  ],
  sourceRows: [
    ["REST entry point (manual field scan)", "<code>KaroCompatController.cs</code>"],
    ["Command handler", "<code>KaroWriteCommands.cs</code> (<code>KaroSaveSummaryCommandHandler</code>)"],
    ["Dynamic-schema write pipeline", "<code>KaroWriteRepository.cs</code> (<code>SaveSummaryAsync</code>)"],
  ],
});

// ERMS reads
pages.push({
  file: "erms-getpatientdata-flow.html",
  title: "ERMS GetPatientData — Request Flow",
  h1: "ERMS <code>GetPatientData</code>: same demographics procedure as KARO",
  dek: "Traced from the real, verified source in this repo. Confirms KARO and ERMS's demographics reads hit the exact same real stored procedure.",
  mermaid: ermsReadDiagram({
    method: "GetPatientData",
    request: "ErmsGetPatientDataQuery(pmsPatientId, pmsEncounterId, BearerToken)",
    handler: "ErmsGetPatientDataQueryHandler",
    repo: "ErmsDemographicsRepository",
    repoMethod: "GetDemographicsAsync(practiceSuffix, routingContext, patientId, ct)",
    proc: "HSS.uspGetDemographics pPatientId",
    renderNote: "first mapped row (or an empty PatientData) serialized — no null check, a legacy NRE quirk on mapper failure is reproduced exactly",
  }),
  cards: [
    {
      tag: "Shared procedure",
      h3: "Identical SP to KARO's demographics",
      p: "<code>ErmsDemographicsRepository.GetDemographicsAsync</code> and <code>KaroDemographicsRepository.GetAsync</code> both call <code>[HSS].[uspGetDemographics]</code> — the same real legacy data, reached from two different compat surfaces.",
    },
  ],
  sourceRows: [
    ["REST entry point", "<code>ErmsCompatController.cs</code>"],
    ["Query handler", "<code>ErmsGetPatientDataQuery.cs</code>"],
    ["Repository (shared with GetCurrentUser's demographic half)", "<code>ErmsDemographicsRepository.cs</code>"],
  ],
  companions: "karo-demographics-flow.html, erms-authenticate-flow.html",
});

const ermsSimpleReads = [
  ["GetPatientMeasurement", "ErmsGetPatientMeasurementQuery", "ErmsGetPatientMeasurementQueryHandler", "GetMeasurementAsync(practiceSuffix, routingContext, patientId, ct)", "HSS.uspGetMeasurement pPatientId", null],
  ["GetSmokingStatus", "ErmsGetSmokingStatusQuery", "ErmsGetSmokingStatusQueryHandler", "GetSmokingStatusAsync(practiceSuffix, routingContext, patientId, ct)", "HSS.uspGetSmokingStatus pPatientId", null],
  ["GetCurrentUser", "ErmsGetCurrentUserQuery", "ErmsGetCurrentUserQueryHandler", "GetProviderAsync(practiceSuffix, routingContext, patientId, userId, locationId, encounterId, ct)", "HSS.uspGetProvider pUserId, pLocationId", "LocationId, pmsUserId"],
  ["GetNextOfKin", "ErmsGetNextOfKinQuery", "ErmsGetNextOfKinQueryHandler", "GetNextOfKinAsync(practiceSuffix, routingContext, patientId, ct)", "HSS.uspGetNextOfKin pPatientId", null],
  ["GetRegisteredPractitioners", "ErmsGetRegisteredPractitionersQuery", "ErmsGetRegisteredPractitionersQueryHandler", "GetRegisteredPractitionersAsync(practiceSuffix, routingContext, patientId, locationId, ct)", "HSS.uspGetRegisteredPractitioners pLocationId", "pmsLocationId"],
  ["GetAccidents", "ErmsGetAccidentsQuery", "ErmsGetAccidentsQueryHandler", "GetAcc45Async(practiceSuffix, routingContext, patientId, sortOrder, minDate, maxDate, ct)", "HSS.uspGetACC45 pPatientId, pSortOrder, pMinDate, pMaxDate", "pmsOrder, pmsMinDateTime, pmsMaxDateTime"],
  ["GetClassifications", "ErmsGetClassificationsQuery", "ErmsGetClassificationsQueryHandler", "GetConditionsAsync(practiceSuffix, routingContext, patientId, sortOrder, minDate, maxDate, ct)", "HSS.uspGetConditions pPatientId, pSortOrder, pMinDate, pMaxDate", "pmsOrder, pmsMinDateTime, pmsMaxDateTime"],
  ["GetConsultNotes", "ErmsGetConsultNotesQuery", "ErmsGetConsultNotesQueryHandler", "GetConsultNotesAsync(practiceSuffix, routingContext, patientId, sortOrder, minDate, maxDate, ct)", "HSS.uspGetConsultNotes pPatientId, pSortOrder, pMinDate, pMaxDate", "pmsOrder, pmsMinDateTime, pmsMaxDateTime"],
  ["GetMedicalAllergies", "ErmsGetMedicalAllergiesQuery", "ErmsGetMedicalAllergiesQueryHandler", "GetMedicalAllergiesAsync(practiceSuffix, routingContext, patientId, sortOrder, minDate, maxDate, ct)", "HSS.uspGetAllergies pPatientId, pSortOrder, pMinDate, pMaxDate", "pmsOrder, pmsMinDateTime, pmsMaxDateTime"],
  ["GetPrescribedMedications", "ErmsGetPrescribedMedicationsQuery", "ErmsGetPrescribedMedicationsQueryHandler", "GetMedicationsAsync(practiceSuffix, routingContext, patientId, sortOrder, minDate, maxDate, isLongTerm=false, ct)", "HSS.uspGetMedications pPatientId, pIsLongTerm=false", "pmsOrder, pmsMinDateTime, pmsMaxDateTime"],
  ["GetRegularMedications", "ErmsGetRegularMedicationsQuery", "ErmsGetRegularMedicationsQueryHandler", "GetMedicationsAsync(practiceSuffix, routingContext, patientId, sortOrder, minDate, maxDate, isLongTerm=true, ct)", "HSS.uspGetMedications pPatientId, pIsLongTerm=true", "pmsOrder, pmsMinDateTime, pmsMaxDateTime"],
  ["GetLaboratoryReportList", "ErmsGetLaboratoryReportListQuery", "ErmsGetLaboratoryReportListQueryHandler", "GetLabsAsync(practiceSuffix, routingContext, patientId, sortOrder, minDate, maxDate, ct)", "HSS.uspGetLabs pPatientId, pSortOrder, pMinDate, pMaxDate", "pmsOrder, pmsMinDateTime, pmsMaxDateTime"],
  ["GetRadiologyReportList", "ErmsGetRadiologyReportListQuery", "ErmsGetRadiologyReportListQueryHandler", "GetRadsAsync(practiceSuffix, routingContext, patientId, sortOrder, minDate, maxDate, ct)", "HSS.uspGetRads pPatientId, pSortOrder, pMinDate, pMaxDate", "pmsOrder, pmsMinDateTime, pmsMaxDateTime"],
  ["GetLaboratoryReportDetails", "ErmsGetLaboratoryReportDetailsQuery", "ErmsGetLaboratoryReportDetailsQueryHandler", "GetLabResultsAsync(practiceSuffix, routingContext, patientId, referenceId, ct)", "HSS.uspGetLabResults pPatientId, pReferenceId", "pmsReferenceId"],
  ["GetRadiologyReportDetails", "ErmsGetRadiologyReportDetailsQuery", "ErmsGetRadiologyReportDetailsQueryHandler", "GetRadResultsAsync(practiceSuffix, routingContext, patientId, referenceId, ct)", "HSS.uspGetRadResults pPatientId, pReferenceId", "pmsReferenceId"],
];
for (const [route, req, hnd, repoMethod, proc, params] of ermsSimpleReads) {
  const rtfNote = route.includes("Details") ? " Content comes back RTF-escaped and Base64'd (<code>ErmsRtfConverter.ConvertString2Rtf</code>), reproduced exactly." : "";
  pages.push({
    file: `erms-${route.toLowerCase()}-flow.html`,
    title: `ERMS ${route} — Request Flow`,
    h1: `ERMS <code>${route}</code>: one stored procedure`,
    dek: `Traced from the real, verified source in this repo.${rtfNote}`,
    minWidth: params ? 1500 : 1400,
    mermaid: ermsReadDiagram({ method: route, request: `${req}(pmsPatientId, pmsEncounterId${params ? ", " + params.split(", ").join(", ") : ""}, BearerToken)`, handler: hnd, repo: "ErmsDataRepository", repoMethod, proc, params }),
    sourceRows: [
      ["REST entry point", "<code>ErmsCompatController.cs</code>"],
      ["Query handler", `<code>${req}.cs</code>`],
      ["Shared multi-resource repository", "<code>ErmsDataRepository.cs</code>"],
    ],
    companions: "erms-getpatientdata-flow.html, erms-getscannedlist-flow.html",
  });
}

pages.push({
  file: "erms-getdischargesummaryreportlist-flow.html",
  title: "ERMS GetDischargeSummaryReportList — Request Flow",
  h1: "ERMS <code>GetDischargeSummaryReportList</code>: AWS-branch list",
  dek: "Traced from the real, verified source in this repo. Same shared AWS-branching method the scanned-documents read uses, filtered to discharge summaries.",
  minWidth: 1500,
  mermaid: `
sequenceDiagram
    autonumber${ERMS_HEAD.replace("{{HANDLER}}", "ErmsGetDischargeSummaryReportListQueryHandler").replace("{{REPO}}", "ErmsDataRepository")}
    participant Aws as AwsDocumentService

    Client->>Ctrl: GET GetDischargeSummaryReportList?pmsPatientId=...&pmsEncounterId=...&pmsOrder=...
    Ctrl->>Hnd: ErmsGetDischargeSummaryReportListQuery(...)

    Hnd->>Parser: ParseEncounterId / Decrypt
    Parser-->>Hnd: patientId, encounterId, practiceSuffix, practiceSuffixNumeric
    Hnd->>Route: Resolve EncounterId
    Route-->>Hnd: RoutingContext
    Hnd->>TokenVal: ValidateAsync ...
    TokenVal-->>Hnd: valid

    Hnd->>Repo: GetOtherDocsAsync practiceSuffix, practiceSuffixNumeric,<br/>routingContext, patientId, sortOrder, minDate, maxDate, isReferral=true
    Repo->>Conn: ResolveAsync routingContext
    Conn-->>Repo: connection string

    alt AWS not enabled for this practice
        Repo->>PracticeDb: EXEC HSS.uspGetOtherDocs pType="Discharge Summary"
    else AWS enabled
        Repo->>PracticeDb: EXEC HSS.uspGetOtherDocs_AWS pType="Discharge Summary"
        Repo->>Aws: GetDocumentStatusFromIndiciAsync per row's DMSID
        Aws-->>Repo: DocumentType, stamped onto each row's DataType
    end
    PracticeDb-->>Repo: DataTable
    Repo-->>Hnd: ErmsReadResult with Table
    Ctrl->>Ctrl: Render(...) into the DischargeReports XML envelope
    Ctrl-->>Client: 200, application/xml`,
  cards: [
    {
      tag: "Conditional",
      h3: "Same AWS branch as GetScannedList",
      p: "<code>GetOtherDocsAsync</code> is the one repository method behind <code>GetDischargeSummaryReportList</code>, <code>GetScannedList</code>, and (with <code>isReferral</code> reversed) discharge/scanned detail reads — filtered only by the <code>@pType</code> parameter.",
    },
  ],
  sourceRows: [
    ["REST entry point", "<code>ErmsCompatController.cs</code>"],
    ["Query handler", "<code>ErmsGetDischargeSummaryReportListQuery.cs</code>"],
    ["Shared AWS-branching repository", "<code>ErmsDataRepository.cs</code> (<code>GetOtherDocsAsync</code>)"],
  ],
  companions: "erms-getscannedlist-flow.html, erms-getdischargesummarydetails-flow.html",
});

pages.push({
  file: "erms-getdischargesummarydetails-flow.html",
  title: "ERMS GetDischargeSummaryDetails — Request Flow",
  h1: "ERMS <code>GetDischargeSummaryDetails</code>: same AWS branch, single row",
  dek: "Traced from the real, verified source in this repo.",
  minWidth: 1500,
  mermaid: `
sequenceDiagram
    autonumber${ERMS_HEAD.replace("{{HANDLER}}", "ErmsGetDischargeSummaryDetailsQueryHandler").replace("{{REPO}}", "ErmsDataRepository")}
    participant Aws as AwsDocumentService

    Client->>Ctrl: GET GetDischargeSummaryDetails?pmsPatientId=...&pmsEncounterId=...&pmsReferenceId=...
    Ctrl->>Hnd: ErmsGetDischargeSummaryDetailsQuery(pmsPatientId, pmsEncounterId, pmsReferenceId, BearerToken)

    Hnd->>Parser: ParseEncounterId / Decrypt
    Parser-->>Hnd: patientId, encounterId, practiceSuffix, practiceSuffixNumeric
    Hnd->>Route: Resolve EncounterId
    Route-->>Hnd: RoutingContext
    Hnd->>TokenVal: ValidateAsync ...
    TokenVal-->>Hnd: valid

    Hnd->>Repo: GetDocResultsAsync practiceSuffix, practiceSuffixNumeric,<br/>routingContext, referenceId, isDischarge=true

    alt AWS not enabled
        Repo->>PracticeDb: EXEC HSS.uspGetDocResults pIsDischarge=true
    else AWS enabled
        Repo->>PracticeDb: EXEC HSS.uspGetDocResults_AWS pIsDischarge=true
        opt referenceId supplied and rows exist
            Repo->>Aws: DocumentGetByDocumentKeyJsonResultAsync referenceId,<br/>practiceIdInt, dmsConnectionString, connectionString
            Aws-->>Repo: single document JSON (base64 content)
        end
    end
    PracticeDb-->>Repo: DataTable
    Repo-->>Hnd: ErmsReadResult with Table
    Ctrl->>Ctrl: Render(...) into DischargeSummaryContents<br/>(no RTF conversion, unlike lab/radiology details)
    Ctrl-->>Client: 200, application/xml`,
  sourceRows: [
    ["REST entry point", "<code>ErmsCompatController.cs</code>"],
    ["Query handler", "<code>ErmsGetDischargeSummaryDetailsQuery.cs</code>"],
    ["Shared AWS-branching repository", "<code>ErmsDataRepository.cs</code> (<code>GetDocResultsAsync</code>)"],
  ],
  companions: "erms-getscanneddetails-flow.html, karo-document-flow.html",
});

pages.push({
  file: "erms-getscanneddetails-flow.html",
  title: "ERMS GetScannedDetails — Request Flow",
  h1: "ERMS <code>GetScannedDetails</code>: single scanned document",
  dek: "Traced from the real, verified source in this repo. Confirmed live this session with a real referenceID pulled from GetScannedList.",
  minWidth: 1500,
  mermaid: `
sequenceDiagram
    autonumber${ERMS_HEAD.replace("{{HANDLER}}", "ErmsGetScannedDetailsQueryHandler").replace("{{REPO}}", "ErmsDataRepository")}
    participant Aws as AwsDocumentService

    Client->>Ctrl: GET GetScannedDetails?pmsPatientId=...&pmsEncounterId=...&pmsReferenceId=...
    Ctrl->>Hnd: ErmsGetScannedDetailsQuery(pmsPatientId, pmsEncounterId, pmsReferenceId, BearerToken)

    Hnd->>Parser: ParseEncounterId / Decrypt
    Parser-->>Hnd: patientId, encounterId, practiceSuffix, practiceSuffixNumeric
    Hnd->>Route: Resolve EncounterId
    Route-->>Hnd: RoutingContext
    Hnd->>TokenVal: ValidateAsync ...
    TokenVal-->>Hnd: valid

    Hnd->>Repo: GetDocResultsAsync practiceSuffix, practiceSuffixNumeric,<br/>routingContext, referenceId, isDischarge=false
    Repo->>Conn: ResolveAsync routingContext
    Conn-->>Repo: connection string

    alt AWS not enabled
        Repo->>PracticeDb: EXEC HSS.uspGetDocResults pIsDischarge=false
    else AWS enabled
        Repo->>PracticeDb: EXEC HSS.uspGetDocResults_AWS pIsDischarge=false
        Repo->>Aws: DocumentGetByDocumentKeyJsonResultAsync referenceId,<br/>practiceIdInt, dmsConnectionString, connectionString
        Aws-->>Repo: real document JSON (base64 content)
    end
    PracticeDb-->>Repo: DataTable
    Repo-->>Hnd: ErmsReadResult with Table
    Ctrl->>Ctrl: Render(...) into ScanReportContent (no RTF conversion)
    Ctrl-->>Client: 200, application/xml with real document content`,
  cards: [
    {
      tag: "Confirmed live",
      h3: "Real content, verified end to end this session",
      p: "Called with a real <code>referenceID</code> pulled from a live <code>GetScannedList</code> response and got back real base64 document content — not just a code-reading exercise.",
    },
  ],
  sourceRows: [
    ["REST entry point", "<code>ErmsCompatController.cs</code>"],
    ["Query handler", "<code>ErmsGetScannedDetailsQuery.cs</code>"],
    ["Shared AWS-branching repository", "<code>ErmsDataRepository.cs</code> (<code>GetDocResultsAsync</code>)"],
  ],
  companions: "erms-getscannedlist-flow.html",
});

// COL
pages.push({
  file: "col-authenticate-flow.html",
  title: "COL Authenticate — Request Flow",
  h1: "COL <code>Authenticate</code>: shares ERMS's own auth repository",
  dek: "Traced from the real, verified source in this repo. No base64 step (unlike ERMS itself) — confirmed live this session.",
  minWidth: 1450,
  mermaid: `
sequenceDiagram
    autonumber
    actor Client
    participant Ctrl as ColCompatController
    participant Hnd as ColAuthenticateQueryHandler
    participant Parser as ColRequestParser
    participant AuthRepo as ErmsAuthRepository
    participant Conn as ErmsPracticeConnectionResolver
    participant Tenant as TenantRegistryService
    participant Secret as ISecretProvider
    participant PracticeDb as Practice DB (PMS_NZ_V2)

    Client->>Ctrl: POST authenticate { Username, Password, PatientId, EncounterId }
    Ctrl->>Hnd: ColAuthenticateQuery(Username, Password, PatientId, EncounterId)
    Note over Hnd: uses IErmsAuthRepository directly —<br/>COL has no ColAuthRepository of its own

    Hnd->>Parser: ParseEncounterId Decrypt PatientId ...
    Note over Parser: no base64 decode step, unlike ERMS's own authenticate —<br/>a real, confirmed COL-specific quirk
    Parser-->>Hnd: patientId, encounterId, practiceSuffix

    Hnd->>AuthRepo: InsertAndValidateTokenAsync practiceSuffix, routingContext,<br/>username, password, patientId, encounterId
    AuthRepo->>Conn: ResolveAsync routingContext
    Conn->>Tenant: ResolveRouteAsync routingContext
    Tenant-->>Conn: PracticeRoute
    Conn->>Secret: GetRequiredSecretAsync
    Secret-->>Conn: credential
    Conn-->>AuthRepo: connection string

    AuthRepo->>PracticeDb: EXEC HSS.uspInsertAndValidateToken pUsername, pPassword,<br/>pPatientID, pAppointmentID
    PracticeDb-->>AuthRepo: Token, Expiry, PracticeId or error

    alt credentials rejected
        AuthRepo-->>Hnd: error set
        Hnd-->>Ctrl: ColAuthenticateResult with Error
        Ctrl-->>Client: 200, JSON with non-empty error field
    else valid
        AuthRepo-->>Hnd: Token, Expiry, PracticeId
        Hnd-->>Ctrl: ColAuthenticateResult success
        Ctrl-->>Client: 200, JSON Token/Expiry/PracticeId, error empty
    end`,
  cards: [
    {
      tag: "Architecture",
      h3: "No separate COL auth repository",
      p: "<code>ColAuthenticateQueryHandler</code>'s constructor takes <code>IErmsAuthRepository</code> directly — confirmed in code, the same real component ERMS's own authenticate flow uses.",
    },
  ],
  sourceRows: [
    ["REST entry point", "<code>ColCompatController.cs</code>"],
    ["Query handler", "<code>ColQueries.cs</code> (<code>ColAuthenticateQueryHandler</code>)"],
    ["Encounter-ID parsing (no base64 step)", "<code>ColRequestParser.cs</code>"],
    ["Auth repository (shared with ERMS)", "<code>ErmsAuthRepository.cs</code>"],
  ],
  companions: "erms-authenticate-flow.html, col-saveinvoice-flow.html",
});

pages.push({
  file: "col-getcurrentpatientdata-flow.html",
  title: "COL GetCurrentPatientData — Request Flow",
  h1: "COL <code>GetCurrentPatientData</code>: [OnlineClaim] schema, ERMS routing",
  dek: "Traced from the real, verified source in this repo. Confirmed live this session with real patient 2459731.",
  mermaid: colReadDiagram({
    method: "GetCurrentPatientData",
    request: "ColGetCurrentPatientDataQuery(PatientId, EncounterId, BearerToken)",
    handler: "ColGetCurrentPatientDataQueryHandler",
    repoMethod: "GetCurrentPatientDataAsync(practiceSuffix, routingContext, patientId, ct)",
    proc: "OnlineClaim.uspGetPatientData pPatientId",
  }),
  sourceRows: [
    ["REST entry point", "<code>ColCompatController.cs</code>"],
    ["Query handler", "<code>ColQueries.cs</code> (<code>ColGetCurrentPatientDataQueryHandler</code>)"],
    ["Repository", "<code>ColDataRepository.cs</code> (<code>GetCurrentPatientDataAsync</code>)"],
  ],
  companions: "col-saveinvoice-flow.html, col-authenticate-flow.html",
});

pages.push({
  file: "col-getsessiondata-flow.html",
  title: "COL GetSessionData — Request Flow",
  h1: "COL <code>GetSessionData</code>: a real, preserved legacy bug",
  dek: "Traced from the real, verified source in this repo. The real legacy PHCO.GetSessionData executes an EMPTY stored-procedure name — reproduced exactly, not fixed, since the whole point of this compat layer is byte-exact parity.",
  mermaid: colReadDiagram({
    method: "GetSessionData",
    request: "ColGetSessionDataQuery(PatientId, EncounterId, BearerToken)",
    handler: "ColGetSessionDataQueryHandler",
    repoMethod: "GetSessionDataAsync(practiceSuffix, routingContext, patientId, ct)",
    proc: '"" (empty string — real legacy bug, not a placeholder)',
    note: 'SQL Server rejects an empty procedure name — the real error text becomes the response body, exactly as real legacy COL behaves',
  }),
  cards: [
    {
      tag: "Real bug, preserved on purpose",
      h3: "Confirmed intermittent this session",
      p: 'Observed both a real error ("BeginExecuteReader: CommandText property has not been initialized") and, on other runs, a normal-looking result — consistent with an empty procedure name being a genuine, unstable legacy defect, not something to silently correct here.',
    },
  ],
  sourceRows: [
    ["REST entry point", "<code>ColCompatController.cs</code>"],
    ["Query handler", "<code>ColQueries.cs</code> (<code>ColGetSessionDataQueryHandler</code>)"],
    ["Repository (empty proc name, real bug)", "<code>ColDataRepository.cs</code> (<code>GetSessionDataAsync</code>)"],
  ],
});

pages.push({
  file: "col-getproviderdata-flow.html",
  title: "COL GetProviderData — Request Flow",
  h1: "COL <code>GetProviderData</code>: full provider list",
  dek: "Traced from the real, verified source in this repo. Confirmed live this session — real provider list for the resolved practice.",
  mermaid: colReadDiagram({
    method: "GetProviderData",
    request: "ColGetProviderDataQuery(PatientId, EncounterId, BearerToken)",
    handler: "ColGetProviderDataQueryHandler",
    repoMethod: "GetProviderDataAsync(practiceSuffix, routingContext, patientId, ct)",
    proc: "OnlineClaim.uspGetProvider pPatientId",
  }),
  sourceRows: [
    ["REST entry point", "<code>ColCompatController.cs</code>"],
    ["Query handler", "<code>ColQueries.cs</code> (<code>ColGetProviderDataQueryHandler</code>)"],
    ["Repository", "<code>ColDataRepository.cs</code> (<code>GetProviderDataAsync</code>)"],
  ],
});

pages.push({
  file: "col-getsurgerydata-flow.html",
  title: "COL GetSurgeryData — Request Flow",
  h1: "COL <code>GetSurgeryData</code>: raw, unvalidated LocationId",
  dek: "Traced from the real, verified source in this repo. LocationId is passed straight through, never decoded or decrypted — a real, confirmed legacy quirk.",
  mermaid: colReadDiagram({
    method: "GetSurgeryData",
    request: "ColGetSurgeryDataQuery(PatientId, EncounterId, LocationId, BearerToken)",
    handler: "ColGetSurgeryDataQueryHandler",
    repoMethod: "GetSurgeryDataAsync(practiceSuffix, routingContext, patientId, locationId, encounterId, ct)",
    proc: "OnlineClaim.uspGetSurgeryData pLocationId (raw, never decoded/decrypted)",
    params: "LocationId",
  }),
  sourceRows: [
    ["REST entry point", "<code>ColCompatController.cs</code>"],
    ["Query handler", "<code>ColQueries.cs</code> (<code>ColGetSurgeryDataQueryHandler</code>)"],
    ["Repository", "<code>ColDataRepository.cs</code> (<code>GetSurgeryDataAsync</code>)"],
  ],
});

pages.push({
  file: "col-getdiagnosisdata-flow.html",
  title: "COL GetDiagnosisData — Request Flow",
  h1: "COL <code>GetDiagnosisData</code>: date-ranged conditions list",
  dek: "Traced from the real, verified source in this repo. pmsOrder is passed raw (no ToUpper), unlike ERMS's equivalent date-range operations — a real, confirmed COL-specific quirk.",
  mermaid: colReadDiagram({
    method: "GetDiagnosisData",
    request: "ColGetDiagnosisDataQuery(PatientId, EncounterId, Order, MinDateTime, MaxDateTime, BearerToken)",
    handler: "ColGetDiagnosisDataQueryHandler",
    repoMethod: "GetDiagnosisDataAsync(practiceSuffix, routingContext, patientId, sortOrder, minDate, maxDate, ct)",
    proc: "OnlineClaim.uspGetConditions pPatientId, pSortOrder (raw, no ToUpper), pMinDate, pMaxDate",
    params: "pmsOrder, pmsMinDateTime, pmsMaxDateTime",
  }),
  sourceRows: [
    ["REST entry point", "<code>ColCompatController.cs</code>"],
    ["Query handler", "<code>ColQueries.cs</code> (<code>ColGetDiagnosisDataQueryHandler</code>)"],
    ["Repository", "<code>ColDataRepository.cs</code> (<code>GetDiagnosisDataAsync</code>)"],
  ],
});

// ---------- write ----------

const outDir = __dirname;
let count = 0;
for (const p of pages) {
  const html = page(p);
  fs.writeFileSync(path.join(outDir, p.file), html);
  count++;
}
console.log(`Wrote ${count} flow-diagram pages.`);
