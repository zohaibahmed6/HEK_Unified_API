import { useState } from "react";
import { systems, type SystemId } from "./systems";
import { HisoPanel } from "./HisoPanel";
import { AuthPanel } from "./AuthPanel";
import { karoAuthenticate, ermsAuthenticate, colAuthenticate } from "./api";
import "./App.css";

function App() {
  const [active, setActive] = useState<SystemId>("hiso");
  const current = systems.find((s) => s.id === active)!;

  return (
    <div className="shell" data-theme={active}>
      <aside className="sidebar">
        <div className="brand">
          <span className="brand-mark">HEK</span>
          <span className="brand-sub">Legacy System Client</span>
        </div>
        <nav>
          {systems.map((s) => (
            <button
              key={s.id}
              className={`nav-item nav-item--${s.id} ${s.id === active ? "is-active" : ""}`}
              onClick={() => setActive(s.id)}
            >
              <span className="nav-dot" />
              {s.label}
            </button>
          ))}
        </nav>
      </aside>

      <main className="content">
        <header className="content-header">
          <h1>{current.label}</h1>
          <p className="content-subtitle">{current.fullName}</p>
        </header>

        <section className="panel">
          <div className="panel-row">
            <span className="panel-label">Auth</span>
            <span className="panel-value">{current.authKind}</span>
          </div>
          <p className="panel-description">{current.description}</p>
        </section>

        {current.id === "hiso" && <HisoPanel />}

        {current.id === "karo" && (
          <AuthPanel
            actionLabel="Authenticate (KARO)"
            fields={[
              { key: "username", label: "Username", default: "hsslive" },
              { key: "password", label: "Password", default: "H$$L1v3005", type: "password" },
              { key: "patientId", label: "Patient ID", default: "2459731" },
              { key: "encounterId", label: "Encounter ID", default: "2147488418__901__FZZ999-B" },
              { key: "pho", label: "PHO", default: "NBPH0" },
            ]}
            onAuthenticate={(v) => karoAuthenticate(v.username, v.password, v.patientId, v.encounterId, v.pho)}
          />
        )}

        {current.id === "erms" && (
          <AuthPanel
            actionLabel="Authenticate (ERMS)"
            fields={[
              { key: "username", label: "Username", default: "ermsdev" },
              { key: "password", label: "Password", default: "eRMsd3V", type: "password" },
              { key: "patientId", label: "Patient ID", default: "2459731" },
              { key: "encounterId", label: "Encounter ID", default: "2147488418__901__FZZ999-B" },
            ]}
            onAuthenticate={(v) => ermsAuthenticate(v.username, v.password, v.patientId, v.encounterId)}
          />
        )}

        {current.id === "col" && (
          <AuthPanel
            actionLabel="Authenticate (COL)"
            fields={[
              { key: "username", label: "Username", default: "indiCOLProd" },
              { key: "password", label: "Password", default: "C@L321$Prod!", type: "password" },
              { key: "patientId", label: "Patient ID", default: "2459731" },
              { key: "encounterId", label: "Encounter ID", default: "2147488418__901__FZZ999-B" },
            ]}
            onAuthenticate={(v) => colAuthenticate(v.username, v.password, v.patientId, v.encounterId)}
          />
        )}

        <section className="panel panel--callflow">
          <div className="panel-row">
            <span className="panel-label">Call flow</span>
            <span className="panel-value">Real request tracing</span>
          </div>
          <p className="panel-description">
            Every call above is a real request through this hub - traced end to end (origin → routing →
            fulfillment) via OpenTelemetry, already wired for all four systems including this SOAP
            facade. View the live trace for the request you just made:
          </p>
          <a className="callflow-link" href="http://localhost:18888/traces" target="_blank" rel="noreferrer">
            Open live traces →
          </a>
        </section>
      </main>
    </div>
  );
}

export default App;
