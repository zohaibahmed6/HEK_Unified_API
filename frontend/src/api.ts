export const API_BASE = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:8080";

export interface CallLogEntry {
  system: string;
  timestamp: string;
  correlationId: string;
  text: string;
}

/** Reads the plain-English per-call summary lines (FR-12/FR-6) for the dashboard's "Logs" tab. */
export async function getRecentCallLogs(take = 50): Promise<CallLogEntry[]> {
  const res = await fetch(`${API_BASE}/admin/logs/recent?take=${take}`);
  if (!res.ok) {
    throw new Error(`HTTP ${res.status}`);
  }
  const json = await res.json();
  return (json.entries ?? []) as CallLogEntry[];
}

export interface AuthResult {
  ok: boolean;
  token?: string;
  practiceId?: string;
  raw: unknown;
  error?: string;
  /** FR-12 call-flow traceability: plain-English "what happened, where" sentence from the X-Hek-Routing-Summary response header. */
  routingSummary?: string;
}

/**
 * KARO's real authenticate contract (JSON body, system="hss" required per the real spec) - see
 * KARO_HSS_doc.md. Auth against practice 901/933 currently rejected by the real stored procedure for
 * every credential tried this session (flagged, unresolved as of 2026-07-24 - not a code defect,
 * confirmed by calling the real proc directly and getting the identical result).
 */
export async function karoAuthenticate(
  username: string,
  password: string,
  patientId: string,
  encounterId: string,
  pho: string,
): Promise<AuthResult> {
  const res = await fetch(`${API_BASE}/karo/authenticate`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ username, password, patientId, encounterId, system: "hss", pho }),
  });
  const routingSummary = res.headers.get("X-Hek-Routing-Summary") ?? undefined;
  const json = await res.json();
  return json?.status === "success"
    ? { ok: true, token: json.token, practiceId: json.practiceId, raw: json, routingSummary }
    : { ok: false, raw: json, error: json?.message ?? "Unknown error", routingSummary };
}

/**
 * ERMS's real authenticate contract is XML, not JSON (confirmed live this session) - no base64 step,
 * unlike some legacy variants. Root element must be `<Credential>`, not `<ErmsCredential>` (matches
 * the real `[XmlRoot("Credential")]` contract - an earlier ad-hoc test using the wrong root tag masked
 * a real bug fix as a false failure; corrected here, 2026-07-24).
 *
 * Real bug found and fixed (2026-07-24): the real response is UTF-16-encoded XML
 * (`Content-Type: application/xml; charset=utf-16`, matching real legacy ERMS behavior). The Fetch
 * API's `Response.text()` always decodes as UTF-8 regardless of the declared charset (a genuine
 * WHATWG Fetch spec quirk, not a server bug) - reading these UTF-16 bytes as UTF-8 produced garbled
 * text that never matched the `<Token>` regex, making a real success look like a failure in the UI.
 * Fixed by reading the raw bytes and decoding with the response's own declared charset instead.
 */
export async function ermsAuthenticate(username: string, password: string, patientId: string, encounterId: string): Promise<AuthResult> {
  const xml = `<?xml version="1.0"?><Credential><Username>${username}</Username><Password>${password}</Password><PatientId>${patientId}</PatientId><EncounterId>${encounterId}</EncounterId></Credential>`;
  const res = await fetch(`${API_BASE}/erms/authenticate`, {
    method: "POST",
    headers: { "Content-Type": "text/xml" },
    body: xml,
  });
  const contentType = res.headers.get("content-type") ?? "";
  const charsetMatch = contentType.match(/charset=([^;]+)/i);
  const charset = charsetMatch?.[1]?.trim() ?? "utf-8";
  const routingSummary = res.headers.get("X-Hek-Routing-Summary") ?? undefined;
  const bytes = await res.arrayBuffer();
  const text = new TextDecoder(charset).decode(bytes);
  const tokenMatch = text.match(/<Token>([^<]*)<\/Token>/);
  const practiceMatch = text.match(/<PracticeId>([^<]*)<\/PracticeId>/);
  const errorMatch = text.match(/<Message>([^<]*)<\/Message>/);
  return tokenMatch
    ? { ok: true, token: tokenMatch[1], practiceId: practiceMatch?.[1], raw: text, routingSummary }
    : { ok: false, raw: text, error: errorMatch?.[1] ?? "Unknown error", routingSummary };
}

/** COL's real authenticate contract: JSON, no base64 step, "3rd segment overwrites" encounterId quirk (confirmed live this session). */
export async function colAuthenticate(username: string, password: string, patientId: string, encounterId: string): Promise<AuthResult> {
  const res = await fetch(`${API_BASE}/erms/col/authenticate`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ username, password, patientId, encounterId }),
  });
  const routingSummary = res.headers.get("X-Hek-Routing-Summary") ?? undefined;
  const json = await res.json();
  return json?.Token
    ? { ok: true, token: json.Token, practiceId: json.PracticeId, raw: json, routingSummary }
    : { ok: false, raw: json, error: json?.error ?? "Unknown error", routingSummary };
}
