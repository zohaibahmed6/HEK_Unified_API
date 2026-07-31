import { useMemo, useRef, useState } from "react";
import type { SystemId } from "./systems";
import { endpointsForSystem, sharedParams } from "./catalog";
import { EndpointCard, type EndpointCardHandle } from "./EndpointCard";
import { UnifiedActionCard, type UnifiedActionCardHandle } from "./UnifiedActionCard";
import { HisoPatientForm, type HisoPatientFormHandle, renderHisoResult } from "./hisoView";
import { renderKaroResult } from "./karoView";
import { renderErmsResult } from "./ermsView";
import { renderColResult } from "./colView";
import { PatientRecordForm, type PatientRecordFormHandle } from "./patientRecordForm";
import type { EndpointDef } from "./catalog";
import type { RunStatus } from "./runner";
import type { SystemAuthState } from "./store";
import { karoAuthenticate, ermsAuthenticate, colAuthenticate } from "./api";

// KARO(HSS)/ERMS/COL now all get the same HISO-style tabbed patient-record form (see
// PatientRecordForm) - one "Load Patient Record" click runs every plain read in parallel, shown as
// tabs. Endpoints that need a real ID from a prior response (drill-down "Details" calls,
// provider/document/location lookups) plus all writes stay in a collapsed "Advanced" panel
// underneath, same placement as HISO's Advanced panel.
const RECORD_SYSTEMS: SystemId[] = ["karo", "erms", "col"];

// Reads that need an ID pulled from another response first (referenceID, identifier, userId, etc.)
// can't be usefully auto-run with blank defaults - they live in the Advanced panel instead.
const DRILLDOWN_READ_IDS = new Set([
  "karo-documents",
  "karo-observations-get",
  "karo-provider",
  "karo-recallcategories",
  "karo-encountersummary",
  "karo-patientattachment",
  "erms-GetCurrentUser",
  "erms-GetRegisteredPractitioners",
  "erms-GetLaboratoryReportDetails",
  "erms-GetRadiologyReportDetails",
  "erms-GetDischargeSummaryDetails",
  "erms-GetScannedDetails",
  "col-GetSurgeryData",
]);

const UNIFIED_RENDERERS: Partial<Record<SystemId, (ep: EndpointDef, raw: string, auth: { token?: string; sessionKey?: string }) => React.ReactNode | null>> = {
  karo: (ep, raw) => renderKaroResult(ep, raw),
  erms: (ep, raw) => renderErmsResult(ep, raw),
  col: (ep, raw) => renderColResult(ep, raw),
};

// The remaining HISO calls (session/version/administrative) aren't part of the patient record -
// tucked away in a collapsed "advanced" panel instead of cluttering the main form.
const HISO_ADVANCED_IDS = new Set(["hiso-getVersion", "hiso-getDeliveryOptions", "hiso-getFormView", "hiso-processAction", "hiso-saveContainer"]);

const AUTH_DEFAULTS: Record<Exclude<SystemId, "hiso">, { username: string; password: string }> = {
  karo: { username: "hsslive", password: "H$$L1v3005" },
  erms: { username: "ermsdev", password: "eRMsd3V" },
  col: { username: "indiCOLProd", password: "C@L321$Prod!" },
};

export function SystemDashboard({
  system,
  auth,
  onSetValue,
  onSetAuth,
}: {
  system: SystemId;
  auth: SystemAuthState;
  onSetValue: (key: string, value: string) => void;
  onSetAuth: (patch: Partial<Pick<SystemAuthState, "token" | "sessionKey" | "practiceId" | "username" | "password">>) => void;
}) {
  const endpoints = useMemo(() => endpointsForSystem(system), [system]);
  const allReads = endpoints.filter((e) => e.kind === "read");
  const writes = endpoints.filter((e) => e.kind === "write");
  const reads = allReads;

  const [statuses, setStatuses] = useState<Record<string, RunStatus>>({});
  const [authing, setAuthing] = useState(false);
  const [authError, setAuthError] = useState<string | null>(null);
  // FR-12 call-flow traceability: the plain-English "what happened, where" sentence from the auth
  // call's X-Hek-Routing-Summary response header, shown right under the auth status.
  const [authRoutingSummary, setAuthRoutingSummary] = useState<string | null>(null);
  // Persisted via the dashboard store (localStorage), same as patientId/encounterId - falls back to
  // the real known-good default credential only the first time, before the user has ever saved one.
  const username = system === "hiso" ? "" : (auth.username ?? AUTH_DEFAULTS[system].username);
  const password = system === "hiso" ? "" : (auth.password ?? AUTH_DEFAULTS[system].password);
  const setUsername = (value: string) => onSetAuth({ username: value });
  const setPassword = (value: string) => onSetAuth({ password: value });
  const [runningAll, setRunningAll] = useState(false);

  const isHiso = system === "hiso";
  const isRecord = RECORD_SYSTEMS.includes(system);
  const cardRefs = useRef<Record<string, EndpointCardHandle | null>>({});
  const unifiedCardRef = useRef<UnifiedActionCardHandle | null>(null);
  const hisoFormRef = useRef<HisoPatientFormHandle | null>(null);
  const recordFormRef = useRef<PatientRecordFormHandle | null>(null);
  const hisoAdvancedEndpoints = useMemo(() => endpoints.filter((e) => HISO_ADVANCED_IDS.has(e.id)), [endpoints]);
  const recordEndpoints = useMemo(
    () => (isRecord ? reads.filter((e) => !DRILLDOWN_READ_IDS.has(e.id) && !e.id.endsWith("-ping")) : []),
    [isRecord, reads],
  );
  const recordAdvancedEndpoints = useMemo(
    () => (isRecord ? endpoints.filter((e) => DRILLDOWN_READ_IDS.has(e.id) || e.kind === "write") : []),
    [isRecord, endpoints],
  );

  const handleResult = (id: string, status: RunStatus) => {
    setStatuses((prev) => ({ ...prev, [id]: status }));
  };

  const doAuthenticate = async () => {
    setAuthing(true);
    setAuthError(null);
    setAuthRoutingSummary(null);
    try {
      if (system === "karo") {
        const patientId = auth.values.patientId ?? "";
        const encounterId = auth.values.encounterId ?? "";
        const r = await karoAuthenticate(username, password, patientId, encounterId, auth.values.pho ?? "");
        if (r.ok) onSetAuth({ token: r.token, practiceId: r.practiceId });
        else setAuthError(r.error ?? "Authentication failed");
        setAuthRoutingSummary(r.routingSummary ?? null);
      } else if (system === "erms") {
        const patientId = auth.values.pmsPatientId ?? "";
        const encounterId = auth.values.pmsEncounterId ?? "";
        const r = await ermsAuthenticate(username, password, patientId, encounterId);
        if (r.ok) onSetAuth({ token: r.token, practiceId: r.practiceId });
        else setAuthError(r.error ?? "Authentication failed");
        setAuthRoutingSummary(r.routingSummary ?? null);
      } else if (system === "col") {
        const patientId = auth.values.pmsPatientId ?? "";
        const encounterId = auth.values.pmsEncounterId ?? "";
        const r = await colAuthenticate(username, password, patientId, encounterId);
        if (r.ok) onSetAuth({ token: r.token, practiceId: r.practiceId });
        else setAuthError(r.error ?? "Authentication failed");
        setAuthRoutingSummary(r.routingSummary ?? null);
      }
    } finally {
      setAuthing(false);
    }
  };

  const runAllReads = async () => {
    setRunningAll(true);
    if (isHiso) {
      await hisoFormRef.current?.run();
    } else if (isRecord) {
      await recordFormRef.current?.run();
    } else {
      for (const ep of allReads) {
        await cardRefs.current[ep.id]?.run();
      }
    }
    setRunningAll(false);
  };

  const counts = allReads.reduce(
    (acc, ep) => {
      const s = statuses[ep.id] ?? "idle";
      acc[s] = (acc[s] ?? 0) + 1;
      return acc;
    },
    { idle: 0, loading: 0, success: 0, empty: 0, error: 0 } as Record<RunStatus, number>,
  );

  const contextValues = auth.values;
  const authContext = { token: auth.token, sessionKey: system === "hiso" ? contextValues.sessionKey : undefined };
  const isAuthed = system === "hiso" ? Boolean(contextValues.sessionKey) : Boolean(auth.token);

  return (
    <div className="dash">
      <section className="dash-auth">
        {system === "hiso" ? (
          <div className="dash-auth-row">
            <label className="dash-field">
              <span>Session Key (GUID)</span>
              <input
                value={contextValues.sessionKey ?? ""}
                onChange={(e) => onSetValue("sessionKey", e.target.value)}
                placeholder="e.g. F4FA4398-3906-4A91-AE8E-70AA044E1672"
              />
            </label>
          </div>
        ) : (
          <>
            <div className="dash-auth-row">
              <label className="dash-field">
                <span>Username</span>
                <input value={username} onChange={(e) => setUsername(e.target.value)} />
              </label>
              <label className="dash-field">
                <span>Password</span>
                <input type="password" value={password} onChange={(e) => setPassword(e.target.value)} />
              </label>
              <button type="button" className="dash-auth-btn" onClick={doAuthenticate} disabled={authing}>
                {authing ? "Authenticating…" : "Authenticate"}
              </button>
            </div>
            {auth.token && (
              <div className="dash-auth-status dash-auth-status--ok">
                Token acquired{auth.practiceId ? ` · practice: ${auth.practiceId}` : ""}
              </div>
            )}
            {authError && <div className="dash-auth-status dash-auth-status--error">{authError}</div>}
            {authRoutingSummary && (
              <div className="dash-call-flow">
                <span className="dash-call-flow-label">Call Flow</span> {authRoutingSummary}
              </div>
            )}
          </>
        )}

        <div className="dash-context-row">
          {sharedParams[system]
            .filter((p) => p.key !== "sessionKey")
            .map((p) => (
              <label key={p.key} className="dash-field dash-field--compact">
                <span>{p.label}</span>
                <input value={contextValues[p.key] ?? ""} onChange={(e) => onSetValue(p.key, e.target.value)} />
              </label>
            ))}
        </div>
      </section>

      <section className="dash-summary">
        <div className="dash-summary-counts">
          <span className="count count--success">{counts.success} has data</span>
          <span className="count count--empty">{counts.empty} no data</span>
          <span className="count count--error">{counts.error} failed</span>
          <span className="count count--idle">{counts.idle} not run</span>
        </div>
        <button type="button" className="dash-run-all-btn" onClick={runAllReads} disabled={runningAll || !isAuthed}>
          {runningAll ? "Running all…" : "Run all reads"}
        </button>
      </section>

      {!isAuthed && (
        <div className="dash-hint">{system === "hiso" ? "Enter a session key above to enable calls." : "Authenticate above to enable calls."}</div>
      )}

      {isHiso ? (
        <section className="dash-record-full">
          <HisoPatientForm
            ref={hisoFormRef}
            contextValues={contextValues}
            sessionKey={contextValues.sessionKey}
            onStatus={(s) => handleResult("hiso-getData-record", s)}
          />
          {hisoAdvancedEndpoints.length > 0 && (
            <details className="hiso-advanced">
              <summary>Advanced: session/version/administrative calls</summary>
              <UnifiedActionCard
                endpoints={hisoAdvancedEndpoints}
                contextValues={contextValues}
                auth={authContext}
                onResult={handleResult}
                renderResult={(ep, raw, a) => renderHisoResult(ep, raw, a.sessionKey)}
              />
            </details>
          )}
        </section>
      ) : isRecord ? (
        <section className="dash-record-full">
          <PatientRecordForm
            key={system}
            ref={recordFormRef}
            endpoints={recordEndpoints}
            contextValues={contextValues}
            auth={authContext}
            onStatus={handleResult}
            renderResult={(ep, raw) => UNIFIED_RENDERERS[system]!(ep, raw, authContext)}
            isAuthed={isAuthed}
          />
          {recordAdvancedEndpoints.length > 0 && (
            <details className="hiso-advanced">
              <summary>Advanced: lookups needing an ID, and writes</summary>
              <UnifiedActionCard
                key={system}
                ref={unifiedCardRef}
                endpoints={recordAdvancedEndpoints}
                contextValues={contextValues}
                auth={authContext}
                onResult={handleResult}
                renderResult={UNIFIED_RENDERERS[system]}
              />
            </details>
          )}
        </section>
      ) : (
        <>
          <section className="dash-grid">
            {reads.map((ep) => (
              <EndpointCard
                key={ep.id}
                ref={(el) => {
                  cardRefs.current[ep.id] = el;
                }}
                endpoint={ep}
                contextValues={contextValues}
                auth={authContext}
                onResult={handleResult}
              />
            ))}
          </section>

          {writes.length > 0 && (
            <>
              <h2 className="dash-section-title">Writes</h2>
              <p className="dash-section-hint">These call real write endpoints against real data - fill in fields and Save individually, not included in "Run all reads".</p>
              <section className="dash-grid">
                {writes.map((ep) => (
                  <EndpointCard key={ep.id} endpoint={ep} contextValues={contextValues} auth={authContext} onResult={handleResult} />
                ))}
              </section>
            </>
          )}
        </>
      )}
    </div>
  );
}
