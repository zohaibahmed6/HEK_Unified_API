import { useMemo, useRef, useState } from "react";
import type { SystemId } from "./systems";
import { endpointsForSystem, sharedParams } from "./catalog";
import { EndpointCard, type EndpointCardHandle } from "./EndpointCard";
import type { RunStatus } from "./runner";
import type { SystemAuthState } from "./store";
import { karoAuthenticate, ermsAuthenticate, colAuthenticate } from "./api";

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
  onSetAuth: (patch: Partial<Pick<SystemAuthState, "token" | "sessionKey" | "practiceId">>) => void;
}) {
  const endpoints = useMemo(() => endpointsForSystem(system), [system]);
  const reads = endpoints.filter((e) => e.kind === "read");
  const writes = endpoints.filter((e) => e.kind === "write");

  const [statuses, setStatuses] = useState<Record<string, RunStatus>>({});
  const [authing, setAuthing] = useState(false);
  const [authError, setAuthError] = useState<string | null>(null);
  const [username, setUsername] = useState(system === "hiso" ? "" : AUTH_DEFAULTS[system].username);
  const [password, setPassword] = useState(system === "hiso" ? "" : AUTH_DEFAULTS[system].password);
  const [runningAll, setRunningAll] = useState(false);

  const cardRefs = useRef<Record<string, EndpointCardHandle | null>>({});

  const handleResult = (id: string, status: RunStatus) => {
    setStatuses((prev) => ({ ...prev, [id]: status }));
  };

  const doAuthenticate = async () => {
    setAuthing(true);
    setAuthError(null);
    try {
      if (system === "karo") {
        const patientId = auth.values.patientId ?? "";
        const encounterId = auth.values.encounterId ?? "";
        const r = await karoAuthenticate(username, password, patientId, encounterId, auth.values.pho ?? "");
        if (r.ok) onSetAuth({ token: r.token, practiceId: r.practiceId });
        else setAuthError(r.error ?? "Authentication failed");
      } else if (system === "erms") {
        const patientId = auth.values.pmsPatientId ?? "";
        const encounterId = auth.values.pmsEncounterId ?? "";
        const r = await ermsAuthenticate(username, password, patientId, encounterId);
        if (r.ok) onSetAuth({ token: r.token, practiceId: r.practiceId });
        else setAuthError(r.error ?? "Authentication failed");
      } else if (system === "col") {
        const patientId = auth.values.pmsPatientId ?? "";
        const encounterId = auth.values.pmsEncounterId ?? "";
        const r = await colAuthenticate(username, password, patientId, encounterId);
        if (r.ok) onSetAuth({ token: r.token, practiceId: r.practiceId });
        else setAuthError(r.error ?? "Authentication failed");
      }
    } finally {
      setAuthing(false);
    }
  };

  const runAllReads = async () => {
    setRunningAll(true);
    for (const ep of reads) {
      await cardRefs.current[ep.id]?.run();
    }
    setRunningAll(false);
  };

  const counts = reads.reduce(
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
    </div>
  );
}
