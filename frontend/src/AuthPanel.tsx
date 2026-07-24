import { useState } from "react";
import type { AuthResult } from "./api";

interface Field {
  key: string;
  label: string;
  default: string;
  type?: string;
}

interface AuthPanelProps {
  fields: Field[];
  onAuthenticate: (values: Record<string, string>) => Promise<AuthResult>;
  actionLabel: string;
}

export function AuthPanel({ fields, onAuthenticate, actionLabel }: AuthPanelProps) {
  const [values, setValues] = useState<Record<string, string>>(
    Object.fromEntries(fields.map((f) => [f.key, f.default])),
  );
  const [loading, setLoading] = useState(false);
  const [result, setResult] = useState<AuthResult | null>(null);

  async function handleSubmit() {
    setLoading(true);
    setResult(null);
    try {
      setResult(await onAuthenticate(values));
    } finally {
      setLoading(false);
    }
  }

  return (
    <section className="panel">
      {fields.map((f) => (
        <div className="field-row" key={f.key}>
          <label htmlFor={`f-${f.key}`}>{f.label}</label>
          <input
            id={`f-${f.key}`}
            type={f.type ?? "text"}
            value={values[f.key]}
            onChange={(e) => setValues((v) => ({ ...v, [f.key]: e.target.value }))}
          />
        </div>
      ))}

      <button className="action-btn" onClick={handleSubmit} disabled={loading}>
        {loading ? "Authenticating…" : actionLabel}
      </button>

      {result && (
        <div className={`result ${result.ok ? "result--ok" : "result--error"}`}>
          {result.ok ? (
            <p>
              Token: <code>{result.token}</code>
              {result.practiceId && <> — Practice: <code>{result.practiceId}</code></>}
            </p>
          ) : (
            <p>{result.error}</p>
          )}
        </div>
      )}
    </section>
  );
}
