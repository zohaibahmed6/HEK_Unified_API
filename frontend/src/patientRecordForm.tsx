import { forwardRef, useImperativeHandle, useState } from "react";
import type { EndpointDef } from "./catalog";
import { runEndpoint, type RunResult, type RunStatus } from "./runner";

/**
 * Generic version of HisoPatientForm (hisoView.tsx) for systems that don't return one single
 * multi-section payload like HISO's getData - instead each read endpoint is its own record
 * section. One "Load Patient Record" click runs every listed endpoint in parallel and shows
 * the results as tabs, same left-rail UX as the HISO screen.
 */

interface RecordTabResult {
  endpoint: EndpointDef;
  status: RunStatus;
  result: RunResult | null;
}

export interface PatientRecordFormHandle {
  run: () => Promise<void>;
}

export const PatientRecordForm = forwardRef<
  PatientRecordFormHandle,
  {
    endpoints: EndpointDef[];
    contextValues: Record<string, string>;
    auth: { token?: string };
    onStatus?: (id: string, status: RunStatus) => void;
    renderResult: (endpoint: EndpointDef, raw: string) => React.ReactNode | null;
    isAuthed: boolean;
  }
>(function PatientRecordForm({ endpoints, contextValues, auth, onStatus, renderResult, isAuthed }, ref) {
  const [loading, setLoading] = useState(false);
  const [hasRun, setHasRun] = useState(false);
  const [tabs, setTabs] = useState<Record<string, RecordTabResult>>({});
  const [activeId, setActiveId] = useState(endpoints[0]?.id ?? "");

  const run = async () => {
    setLoading(true);
    setHasRun(true);
    const merged = endpoints.map((ep) => ({ ...ep }));
    await Promise.all(
      merged.map(async (ep) => {
        onStatus?.(ep.id, "loading");
        setTabs((prev) => ({ ...prev, [ep.id]: { endpoint: ep, status: "loading", result: null } }));
        const params = { ...contextValues, ...Object.fromEntries((ep.extraParams ?? []).map((p) => [p.key, p.default])) };
        const r = await runEndpoint(ep, params, auth, ep.method === "POST" ? ep.bodyTemplate?.(params) ?? "" : undefined);
        onStatus?.(ep.id, r.status);
        setTabs((prev) => ({ ...prev, [ep.id]: { endpoint: ep, status: r.status, result: r } }));
      }),
    );
    setLoading(false);
  };

  useImperativeHandle(ref, () => ({ run }));

  if (endpoints.length === 0) return null;
  const active = tabs[activeId];
  const activeEndpoint = endpoints.find((e) => e.id === activeId) ?? endpoints[0];

  return (
    <div className="ep-card hiso-patient-form">
      <div className="hiso-form-head">
        <div className="hiso-form-title">Patient Record</div>
        <button type="button" className="ep-run-btn" onClick={run} disabled={loading || !isAuthed}>
          {loading ? "Loading…" : "Load Patient Record"}
        </button>
      </div>

      {!hasRun && <div className="hiso-empty">Click "Load Patient Record" to fetch the full record.</div>}

      {hasRun && (
        <div className="hiso-record">
          <div className="hiso-record-tabs">
            {endpoints.map((ep) => {
              const t = tabs[ep.id];
              const status = t?.status ?? "idle";
              return (
                <button
                  key={ep.id}
                  type="button"
                  className={`hiso-tab-btn ${ep.id === activeId ? "is-active" : ""}`}
                  onClick={() => setActiveId(ep.id)}
                >
                  <span className="hiso-tab-btn-label">{ep.name}</span>
                  <span className="hiso-tab-btn-summary">
                    {status === "loading" ? "Loading…" : status === "error" ? "Failed" : status === "empty" ? "No data" : status === "success" ? "Has data" : "Not run"}
                  </span>
                </button>
              );
            })}
          </div>
          <div className="hiso-record-panel">
            {active?.result?.routingSummary && (
              <div className="ep-call-flow">
                <span className="ep-call-flow-label">Call Flow</span> {active.result.routingSummary}
              </div>
            )}
            {!active || active.status === "loading" ? (
              <div className="hiso-empty">Loading…</div>
            ) : active.status === "error" ? (
              <div className="hiso-doc-error">{active.result?.summary ?? "Request failed."}</div>
            ) : active.status === "empty" ? (
              <div className="hiso-empty">No data returned.</div>
            ) : active.result ? (
              renderResult(activeEndpoint, active.result.raw) ?? <pre className="ep-result-raw">{active.result.raw}</pre>
            ) : (
              <div className="hiso-empty">No data returned.</div>
            )}
          </div>
        </div>
      )}
    </div>
  );
});
