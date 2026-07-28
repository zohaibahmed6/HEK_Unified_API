import { forwardRef, useImperativeHandle, useState } from "react";
import type { EndpointDef } from "./catalog";
import { runEndpoint, type RunResult, type RunStatus } from "./runner";

const STATUS_LABEL: Record<RunStatus, string> = {
  idle: "Not run",
  loading: "Running…",
  success: "Has data",
  empty: "No data",
  error: "Failed",
};

export interface UnifiedActionCardHandle {
  runAll: () => Promise<void>;
}

/**
 * One dashboard card for an entire system: pick the action on the left (getVersion,
 * demographics, GetPatientData, ...), run it, see the result below - instead of one
 * small card per endpoint cluttering the screen.
 */
export const UnifiedActionCard = forwardRef<
  UnifiedActionCardHandle,
  {
    endpoints: EndpointDef[];
    contextValues: Record<string, string>;
    auth: { token?: string; sessionKey?: string };
    onResult: (id: string, status: RunStatus) => void;
    renderResult?: (endpoint: EndpointDef, raw: string, auth: { token?: string; sessionKey?: string }) => React.ReactNode | null;
  }
>(function UnifiedActionCard({ endpoints, contextValues, auth, onResult, renderResult }, ref) {
  const [activeId, setActiveId] = useState(endpoints[0]?.id ?? "");
  const active = endpoints.find((e) => e.id === activeId) ?? endpoints[0];

  const [fieldsById, setFieldsById] = useState<Record<string, Record<string, string>>>({});
  const [rawOverrideById, setRawOverrideById] = useState<Record<string, string | null>>({});
  const [statusById, setStatusById] = useState<Record<string, RunStatus>>({});
  const [resultById, setResultById] = useState<Record<string, RunResult | null>>({});
  const [showRaw, setShowRaw] = useState(false);
  const [expanded, setExpanded] = useState(true);

  if (!active) return null;

  const isWrite = active.kind === "write";
  const fields = fieldsById[active.id] ?? Object.fromEntries((active.extraParams ?? []).map((p) => [p.key, p.default]));
  const merged = { ...contextValues, ...fields };
  const computedBody = active.method === "POST" ? active.bodyTemplate?.(merged) ?? "" : "";
  const rawOverride = rawOverrideById[active.id] ?? null;
  const status = statusById[active.id] ?? "idle";
  const result = resultById[active.id] ?? null;

  const setField = (key: string, value: string) => {
    setFieldsById((prev) => ({ ...prev, [active.id]: { ...fields, [key]: value } }));
  };

  const runOne = async (ep: EndpointDef) => {
    setStatusById((prev) => ({ ...prev, [ep.id]: "loading" }));
    onResult(ep.id, "loading");
    const epFields = fieldsById[ep.id] ?? Object.fromEntries((ep.extraParams ?? []).map((p) => [p.key, p.default]));
    const epMerged = { ...contextValues, ...epFields };
    const epRawOverride = rawOverrideById[ep.id] ?? null;
    const r = await runEndpoint(ep, epMerged, auth, ep.method === "POST" ? epRawOverride ?? ep.bodyTemplate?.(epMerged) ?? "" : undefined);
    setResultById((prev) => ({ ...prev, [ep.id]: r }));
    setStatusById((prev) => ({ ...prev, [ep.id]: r.status }));
    onResult(ep.id, r.status);
    return r;
  };

  const run = async () => {
    await runOne(active);
    setExpanded(true);
  };

  useImperativeHandle(ref, () => ({
    runAll: async () => {
      for (const ep of endpoints.filter((e) => e.kind === "read")) {
        await runOne(ep);
      }
    },
  }));

  return (
    <div className={`ep-card hiso-unified-card ep-card--${status}`}>
      <div className="hiso-unified-tabs">
        {endpoints.map((ep) => (
          <button
            key={ep.id}
            type="button"
            className={`hiso-unified-tab ${ep.id === active.id ? "is-active" : ""}`}
            onClick={() => setActiveId(ep.id)}
          >
            <span className={`ep-badge ep-badge--${ep.kind}`}>{ep.kind}</span>
            <span>{ep.name}</span>
            <span className={`ep-status ep-status--${statusById[ep.id] ?? "idle"}`}>{STATUS_LABEL[statusById[ep.id] ?? "idle"]}</span>
          </button>
        ))}
      </div>

      <div className="ep-meta">
        <code>
          {active.method} {active.path}
        </code>
        {result && <span className="ep-duration">{result.durationMs} ms</span>}
      </div>

      {active.extraParams && active.extraParams.length > 0 && (
        <div className="ep-extra-params">
          <div className="ep-extra-params-label">{isWrite ? "Fields" : "Optional filters"}</div>
          {active.extraParams.map((p) => (
            <label key={p.key} className="ep-extra-field">
              <span>{p.label}</span>
              <input value={fields[p.key] ?? ""} onChange={(e) => setField(p.key, e.target.value)} placeholder={p.label} />
            </label>
          ))}
        </div>
      )}

      {active.method === "POST" && (
        <div className="ep-body-toggle">
          <button type="button" className="ep-link-btn" onClick={() => setShowRaw((v) => !v)}>
            {showRaw ? "Hide raw request" : "View/edit raw request"}
          </button>
          {showRaw && (
            <textarea
              className="ep-body-editor"
              value={rawOverride ?? computedBody}
              onChange={(e) => setRawOverrideById((prev) => ({ ...prev, [active.id]: e.target.value }))}
              rows={8}
              spellCheck={false}
            />
          )}
        </div>
      )}

      <div className="ep-actions">
        <button type="button" className={isWrite ? "ep-save-btn" : "ep-run-btn"} onClick={run} disabled={status === "loading"}>
          {status === "loading" ? "Running…" : isWrite ? "Save" : "Run"}
        </button>
        {result && (
          <button type="button" className="ep-link-btn" onClick={() => setExpanded((v) => !v)}>
            {expanded ? "Hide response" : "Show response"}
          </button>
        )}
      </div>

      {result && expanded && (
        <div className="ep-result">
          <div className="ep-result-summary">{result.summary}</div>
          {(() => {
            const friendly = status === "success" ? renderResult?.(active, result.raw, auth) : null;
            if (friendly) {
              return (
                <>
                  {friendly}
                  <details className="ep-raw-toggle">
                    <summary>Raw response</summary>
                    <pre className="ep-result-raw">{result.raw.length > 4000 ? result.raw.slice(0, 4000) + "\n… (truncated)" : result.raw}</pre>
                  </details>
                </>
              );
            }
            return <pre className="ep-result-raw">{result.raw.length > 4000 ? result.raw.slice(0, 4000) + "\n… (truncated)" : result.raw}</pre>;
          })()}
        </div>
      )}
    </div>
  );
});
