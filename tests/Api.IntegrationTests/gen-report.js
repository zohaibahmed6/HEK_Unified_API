const fs = require("fs");
const path = require("path");

const trxPath = path.join(__dirname, "TestResults", "live-integration-results.trx");
const xml = fs.readFileSync(trxPath, "utf-8");

function unescapeXml(s) {
  return s
    .replace(/&lt;/g, "<")
    .replace(/&gt;/g, ">")
    .replace(/&quot;/g, '"')
    .replace(/&apos;/g, "'")
    .replace(/&amp;/g, "&");
}

const results = [];
const resultRe = /<UnitTestResult\b([^>]*)>([\s\S]*?)<\/UnitTestResult>/g;
let m;
while ((m = resultRe.exec(xml))) {
  const attrs = m[1];
  const body = m[2];
  const getAttr = (name) => {
    const am = attrs.match(new RegExp(name + '="([^"]*)"'));
    return am ? unescapeXml(am[1]) : "";
  };
  const stdOutMatch = body.match(/<StdOut>([\s\S]*?)<\/StdOut>/);
  const errMsgMatch = body.match(/<Message>([\s\S]*?)<\/Message>/);
  results.push({
    name: getAttr("testName"),
    outcome: getAttr("outcome"),
    duration: getAttr("duration"),
    stdOut: stdOutMatch ? unescapeXml(stdOutMatch[1]) : "",
    errorMessage: errMsgMatch ? unescapeXml(errMsgMatch[1]) : "",
  });
}

// Group by class (part before the last dot before method name)
function classOf(name) {
  const noParen = name.split("(")[0];
  const parts = noParen.split(".");
  parts.pop();
  return parts.pop();
}

const groups = {};
for (const r of results) {
  const c = classOf(r.name);
  groups[c] = groups[c] || [];
  groups[c].push(r);
}

const total = results.length;
const passed = results.filter((r) => r.outcome === "Passed").length;
const failed = total - passed;

function esc(s) {
  return s.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
}

function methodName(fullName) {
  const noParen = fullName.split("(")[0];
  return noParen.split(".").pop() + (fullName.includes("(") ? "(" + fullName.split("(").slice(1).join("(") : "");
}

function splitStdOut(stdOut) {
  const lines = stdOut.split("\n");
  const first = lines[0] || "";
  const rest = lines.slice(1).join("\n");
  const reqMatch = rest.match(/^request: ([\s\S]*?)\nresponse \((\d+)\): ([\s\S]*)$/);
  if (reqMatch) {
    return { call: first, request: reqMatch[1], status: reqMatch[2], response: reqMatch[3] };
  }
  const respMatch = rest.match(/^response \((\d+)\): ([\s\S]*)$/);
  if (respMatch) {
    return { call: first, request: null, status: respMatch[1], response: respMatch[2] };
  }
  return { call: first, request: null, status: null, response: rest };
}

let sectionsHtml = "";
for (const [cls, items] of Object.entries(groups)) {
  const clsPassed = items.filter((i) => i.outcome === "Passed").length;
  sectionsHtml += `<section class="group">
    <h2>${esc(cls)} <span class="group-count">${clsPassed}/${items.length}</span></h2>
    <div class="cards">`;
  for (const r of items) {
    const parsed = splitStdOut(r.stdOut);
    const ok = r.outcome === "Passed";
    sectionsHtml += `
      <details class="card ${ok ? "pass" : "fail"}">
        <summary>
          <span class="badge ${ok ? "badge-pass" : "badge-fail"}">${ok ? "PASS" : "FAIL"}</span>
          <span class="method">${esc(methodName(r.name))}</span>
          <span class="duration">${esc(r.duration)}</span>
        </summary>
        <div class="card-body">
          ${parsed.call ? `<div class="call"><code>${esc(parsed.call)}</code></div>` : ""}
          ${parsed.request ? `<div class="block"><div class="block-label">Request</div><pre>${esc(parsed.request)}</pre></div>` : ""}
          ${parsed.status ? `<div class="block"><div class="block-label">Response (HTTP ${esc(parsed.status)})</div><pre>${esc(parsed.response)}</pre></div>` : ""}
          ${!parsed.status && r.stdOut ? `<div class="block"><div class="block-label">Output</div><pre>${esc(r.stdOut)}</pre></div>` : ""}
          ${r.errorMessage ? `<div class="block error"><div class="block-label">Failure reason</div><pre>${esc(r.errorMessage)}</pre></div>` : ""}
        </div>
      </details>`;
  }
  sectionsHtml += `</div></section>`;
}

const html = `<!doctype html>
<html>
<head>
<meta charset="utf-8" />
<title>HEK Core API — Live Integration Test Evidence</title>
<style>
  :root {
    color-scheme: light dark;
    --bg: #f4f5f3; --surface: #ffffff; --ink: #1c211e; --ink-dim: #5c645d;
    --border: #dde0db; --accent: #2f5d4f; --accent-soft: #e4ede9;
    --pass: #1a7a4c; --pass-bg: #e2f3e7; --fail: #b3402a; --fail-bg: #fbe8e3;
    --code-bg: #eef0ec; --shadow: 0 1px 2px rgba(28,33,30,0.06);
  }
  @media (prefers-color-scheme: dark) {
    :root {
      --bg: #14180f; --surface: #1c221a; --ink: #e7ebe3; --ink-dim: #98a293;
      --border: #2c342a; --accent: #7dbf9e; --accent-soft: #223129;
      --pass: #6fd39a; --pass-bg: #1a3324; --fail: #ea8f79; --fail-bg: #3a1f18;
      --code-bg: #232a20; --shadow: 0 1px 2px rgba(0,0,0,0.3);
    }
  }
  :root[data-theme="dark"] {
    --bg: #14180f; --surface: #1c221a; --ink: #e7ebe3; --ink-dim: #98a293;
    --border: #2c342a; --accent: #7dbf9e; --accent-soft: #223129;
    --pass: #6fd39a; --pass-bg: #1a3324; --fail: #ea8f79; --fail-bg: #3a1f18;
    --code-bg: #232a20; --shadow: 0 1px 2px rgba(0,0,0,0.3);
  }
  :root[data-theme="light"] {
    --bg: #f4f5f3; --surface: #ffffff; --ink: #1c211e; --ink-dim: #5c645d;
    --border: #dde0db; --accent: #2f5d4f; --accent-soft: #e4ede9;
    --pass: #1a7a4c; --pass-bg: #e2f3e7; --fail: #b3402a; --fail-bg: #fbe8e3;
    --code-bg: #eef0ec; --shadow: 0 1px 2px rgba(28,33,30,0.06);
  }
  * { box-sizing: border-box; }
  body {
    font-family: "Iowan Old Style", "Palatino Linotype", Palatino, Georgia, serif;
    margin: 0; padding: 2.5rem 1.5rem; background: var(--bg); color: var(--ink); line-height: 1.5;
  }
  .page { max-width: 980px; margin: 0 auto; }
  h1 {
    font-family: -apple-system, "Segoe UI", Roboto, sans-serif;
    font-size: 1.65rem; font-weight: 700; margin: 0 0 0.3rem; text-wrap: balance; letter-spacing: -0.01em;
  }
  .subtitle { font-size: 0.92rem; color: var(--ink-dim); margin-bottom: 1.75rem; }
  .subtitle code { font-family: ui-monospace, SFMono-Regular, Menlo, monospace; font-size: 0.85em; }
  .summary { display: flex; gap: 0.75rem; margin-bottom: 2.25rem; flex-wrap: wrap; }
  .stat {
    background: var(--surface); border: 1px solid var(--border); border-radius: 10px;
    padding: 0.9rem 1.4rem; box-shadow: var(--shadow); min-width: 110px;
  }
  .stat .num { font-family: -apple-system, "Segoe UI", Roboto, sans-serif; font-size: 1.7rem; font-weight: 700; font-variant-numeric: tabular-nums; }
  .stat .label { font-size: 0.72rem; color: var(--ink-dim); text-transform: uppercase; letter-spacing: 0.06em; margin-top: 0.15rem; }
  .stat.total .num { color: var(--accent); }
  .stat.pass .num { color: var(--pass); }
  .stat.fail .num { color: var(--fail); }
  .group { margin-bottom: 2rem; }
  .group h2 {
    font-family: -apple-system, "Segoe UI", Roboto, sans-serif;
    font-size: 1rem; font-weight: 600; border-bottom: 1px solid var(--border);
    padding-bottom: 0.5rem; margin-bottom: 0.75rem; display: flex; align-items: baseline; gap: 0.6rem;
  }
  .group-count {
    font-weight: 500; font-size: 0.78rem; color: var(--ink-dim);
    font-variant-numeric: tabular-nums; background: var(--accent-soft); padding: 0.1rem 0.5rem; border-radius: 999px;
  }
  .cards { display: flex; flex-direction: column; gap: 0.45rem; }
  .card { background: var(--surface); border: 1px solid var(--border); border-radius: 8px; box-shadow: var(--shadow); overflow: hidden; }
  .card.fail { border-color: color-mix(in srgb, var(--fail) 45%, var(--border)); }
  summary {
    cursor: pointer; padding: 0.6rem 1rem; display: flex; align-items: center; gap: 0.75rem; list-style: none;
  }
  summary:focus-visible { outline: 2px solid var(--accent); outline-offset: -2px; }
  summary::-webkit-details-marker { display: none; }
  summary::before { content: "▸"; color: var(--ink-dim); font-size: 0.7rem; transition: transform 0.15s ease; }
  details[open] summary::before { transform: rotate(90deg); }
  @media (prefers-reduced-motion: reduce) { details[open] summary::before { transition: none; } }
  .badge {
    font-family: -apple-system, "Segoe UI", Roboto, sans-serif;
    font-size: 0.66rem; font-weight: 700; padding: 0.18rem 0.5rem; border-radius: 4px; letter-spacing: 0.04em;
    flex-shrink: 0;
  }
  .badge-pass { background: var(--pass-bg); color: var(--pass); }
  .badge-fail { background: var(--fail-bg); color: var(--fail); }
  .method { font-family: ui-monospace, SFMono-Regular, Menlo, monospace; font-size: 0.85rem; flex: 1; word-break: break-word; }
  .duration {
    font-family: ui-monospace, SFMono-Regular, Menlo, monospace;
    font-size: 0.72rem; color: var(--ink-dim); white-space: nowrap; font-variant-numeric: tabular-nums;
  }
  .card-body { padding: 0 1rem 1rem 1rem; }
  .call { margin-bottom: 0.5rem; }
  .call code {
    font-family: ui-monospace, SFMono-Regular, Menlo, monospace;
    font-size: 0.8rem; background: var(--code-bg); padding: 0.2rem 0.45rem; border-radius: 4px;
  }
  .block { margin-top: 0.55rem; }
  .block-label {
    font-family: -apple-system, "Segoe UI", Roboto, sans-serif;
    font-size: 0.68rem; font-weight: 700; text-transform: uppercase; letter-spacing: 0.05em; color: var(--ink-dim); margin-bottom: 0.3rem;
  }
  .block.error .block-label { color: var(--fail); }
  .wide-scroll { overflow-x: auto; }
  pre {
    font-family: ui-monospace, SFMono-Regular, Menlo, monospace;
    background: var(--code-bg); padding: 0.6rem 0.75rem; border-radius: 6px; overflow-x: auto;
    font-size: 0.76rem; white-space: pre-wrap; word-break: break-word; margin: 0; max-height: 260px; overflow-y: auto;
  }
</style>
</head>
<body>
  <div class="page">
    <h1>HEK Core API — Live Integration Test Evidence</h1>
    <div class="subtitle">Real HTTP calls against the live docker stack (<code>dbserver-local</code> → <code>PMS_NZ_V2</code> / <code>DMS_PMS</code>) — 2026-07-28</div>
    <div class="summary">
      <div class="stat total"><div class="num">${total}</div><div class="label">Total</div></div>
      <div class="stat pass"><div class="num">${passed}</div><div class="label">Passed</div></div>
      <div class="stat fail"><div class="num">${failed}</div><div class="label">Failed</div></div>
    </div>
    ${sectionsHtml}
  </div>
</body>
</html>`;

fs.writeFileSync(path.join(__dirname, "TestResults", "live-integration-report.html"), html);
console.log("Wrote", results.length, "results to TestResults/live-integration-report.html");
