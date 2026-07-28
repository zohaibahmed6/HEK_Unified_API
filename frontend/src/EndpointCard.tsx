import { forwardRef, useImperativeHandle, useState } from "react";
import type { EndpointDef } from "./catalog";
import { runEndpoint, type RunResult, type RunStatus } from "./runner";
import { renderHisoResult } from "./hisoView";

const STATUS_LABEL: Record<RunStatus, string> = {
  idle: "Not run",
  loading: "Running…",
  success: "Has data",
  empty: "No data",
  error: "Failed",
};

export interface EndpointCardHandle {
  run: () => Promise<void>;
}

export const EndpointCard = forwardRef<
  EndpointCardHandle,
  {
    endpoint: EndpointDef;
    contextValues: Record<string, string>;
    auth: { token?: string; sessionKey?: string };
    onResult: (id: string, status: RunStatus) => void;
  }
>(function EndpointCard({ endpoint, contextValues, auth, onResult }, ref) {
  const isWrite = endpoint.kind === "write";
  const [fields, setFields] = useState<Record<string, string>>(() => Object.fromEntries((endpoint.extraParams ?? []).map((p) => [p.key, p.default])));
  const [rawOverride, setRawOverride] = useState<string | null>(null);
  const [showRaw, setShowRaw] = useState(false);
  const [expanded, setExpanded] = useState(false);
  const [status, setStatus] = useState<RunStatus>("idle");
  const [result, setResult] = useState<RunResult | null>(null);

  const merged = { ...contextValues, ...fields };
  const computedBody = endpoint.method === "POST" ? endpoint.bodyTemplate?.(merged) ?? "" : "";

  const run = async () => {
    setStatus("loading");
    onResult(endpoint.id, "loading");
    const r = await runEndpoint(endpoint, merged, auth, endpoint.method === "POST" ? rawOverride ?? computedBody : undefined);
    setResult(r);
    setStatus(r.status);
    onResult(endpoint.id, r.status);
    setExpanded(true);
  };

  useImperativeHandle(ref, () => ({ run }));

  return (
    <div className={`ep-card ep-card--${status}`}>
      <div className="ep-card-head">
        <div className="ep-card-title">
          <span className={`ep-badge ep-badge--${endpoint.kind}`}>{endpoint.kind}</span>
          <span className="ep-name">{endpoint.name}</span>
        </div>
        <span className={`ep-status ep-status--${status}`}>{STATUS_LABEL[status]}</span>
      </div>
      <div className="ep-meta">
        <code>
          {endpoint.method} {endpoint.path}
        </code>
        {result && <span className="ep-duration">{result.durationMs} ms</span>}
      </div>

      {endpoint.extraParams && endpoint.extraParams.length > 0 && (
        <div className="ep-extra-params">
          <div className="ep-extra-params-label">{isWrite ? "Fields" : "Optional filters"}</div>
          {endpoint.extraParams.map((p) => (
            <label key={p.key} className="ep-extra-field">
              <span>{p.label}</span>
              <input value={fields[p.key] ?? ""} onChange={(e) => setFields((prev) => ({ ...prev, [p.key]: e.target.value }))} placeholder={p.label} />
            </label>
          ))}
        </div>
      )}

      {endpoint.method === "POST" && (
        <div className="ep-body-toggle">
          <button type="button" className="ep-link-btn" onClick={() => setShowRaw((v) => !v)}>
            {showRaw ? "Hide raw request" : "View/edit raw request"}
          </button>
          {showRaw && (
            <textarea
              className="ep-body-editor"
              value={rawOverride ?? computedBody}
              onChange={(e) => setRawOverride(e.target.value)}
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
            const friendly = status === "success" ? renderHisoResult(endpoint, result.raw, auth.sessionKey) : null;
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
