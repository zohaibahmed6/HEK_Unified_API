---
name: legacy-parity-checker
description: >
  Compares the 4 legacy reference APIs (HISO, ERMS, KARO/HSS, COL) against
  the new unified HEK API's legacy-compat endpoints to find what the legacy
  systems do that the unified API is missing or doing differently — field
  by field, operation by operation. Use when the user asks to "check legacy
  parity", "compare legacy vs unified", "find what's missing vs the real
  API", or references HISO/ERMS/KARO/COL alongside the unified API. Starts
  from documentation, only falls back to running the real reference APIs
  and the unified API live when docs are ambiguous or missing. Maintains a
  standalone LEGACY_PARITY_VALIDATOR.md tracking what's confirmed matching,
  what's confirmed missing/different, and what's still unchecked — so work
  can resume across sessions without re-checking what's already validated.
---

# Legacy Parity Checker

## Goal

For each of the 4 legacy systems (HISO, ERMS, KARO/HSS, COL), find every
operation/endpoint the real legacy system supports, and confirm whether the
unified API's corresponding legacy-compat endpoint replicates it correctly:
same operations present, same request/response fields, same error/edge-case
behavior. Report gaps. Do not fix code unless asked — this is an audit.

## Default locations (ask if not found)

- Legacy docs + prior analysis: `E:\claude_projects\HEK Core API\hek_analysis`
- Legacy reference API source/specs: `E:\claude_projects\HEK Core API\legacy-reference`
- Unified API source: wherever the current project repo is (confirm with user
  if not obvious from the working directory).

If these paths don't exist in the current environment, ask Zohaib rather
than guessing at alternate locations.

## Hard rule: minimize tokens

- Start from docs, not code, and not live systems. Only escalate a single
  operation to "read code" or "call live system" when the doc doesn't
  settle the question.
- Never `Read`/`cat` a whole spec doc or source file. `grep -n` for the
  specific operation name / field name / class name you need.
- One escalation at a time, smallest possible: a single `curl` call with a
  minimal payload, not a full test suite run.
- Never re-read a file in this session for information already extracted
  and written to the validator file — the validator file IS the memory.
- Batch independent greps into one shell command per system where possible.

## Workflow

### 1. Load or create the validator file

Look for `LEGACY_PARITY_VALIDATOR.md` in the unified API repo root. If it
exists, read only its status table (not full history) to see what's already
✅ Confirmed / ❌ Gap / ⬜ Unchecked, and resume from ⬜ items only — never
re-validate a ✅ or ❌ row unless the user explicitly asks for a re-check.

If it doesn't exist, create it with this structure:

```markdown
# Legacy Parity Validator

Tracks operation-by-operation parity between the 4 legacy reference APIs
(HISO, ERMS, KARO/HSS, COL) and the unified API's legacy-compat endpoints.
Statuses: ⬜ Unchecked · ✅ Confirmed match · ❌ Gap/mismatch found

## HISO
| Operation | Legacy behavior (source) | Unified endpoint | Status | Notes |
|---|---|---|---|---|

## ERMS
| Operation | Legacy behavior (source) | Unified endpoint | Status | Notes |
|---|---|---|---|---|

## KARO/HSS
| Operation | Legacy behavior (source) | Unified endpoint | Status | Notes |
|---|---|---|---|---|

## COL
| Operation | Legacy behavior (source) | Unified endpoint | Status | Notes |
|---|---|---|---|---|

## Gaps found (summary)
(only ❌ rows, one line each, populated as found)

## Last updated
```

### 2. Enumerate legacy operations per system, from docs first

For each system, grep the legacy doc/spec for operation names (e.g.
`getVersion`, `getData`, `saveContainer`, `Authenticate`, `SaveDocument`,
`SaveInvoice`, etc.) and their documented request/response shape. Add one
row per operation to the validator file's ⬜ list before checking anything.

### 3. For each ⬜ operation, check the unified API

- First: `grep -rn "<OperationName>"` in the unified API source to find the
  handler/controller. Confirm it exists at all — if not, that's an
  immediate ❌ Gap ("not implemented"), no further check needed.
- If it exists: grep the handler for the fields/params it reads and returns,
  compare against the doc's documented shape. Note any field present in
  legacy but absent in unified, or vice versa, or behaving differently
  (e.g. validation rule, error code, default value).
- Only if the doc + code aren't enough to settle it: run ONE live call
  against the real legacy reference API and ONE against the unified API
  with matching input, diff the two responses directly (don't paraphrase —
  show the actual diff).

### 4. Update the validator file after each operation, not at the end

Write the row's status (✅/❌) and a one-line note immediately after each
check, so a crash/interruption or a parallel terminal doesn't lose progress
or duplicate work. Update "Last updated" timestamp each time.

### 5. Multi-session / multi-terminal use

Each terminal can claim one system (HISO in terminal A, ERMS in terminal B,
etc.) since the validator file is the shared source of truth. Before
starting, re-read only that system's table section to avoid collisions.

### 6. Final report

When asked for a summary, read only the "Gaps found" section of the
validator file (not the full tables) and present it as the answer — don't
regenerate it from scratch by rescanning code.

## What NOT to do

- Don't fix any gap found unless explicitly asked — report it in the
  validator file and summary only.
- Don't re-validate ✅ rows without being asked.
- Don't dump full legacy doc or source file contents into the response —
  cite operation name + one-line evidence only.
- Don't guess at legacy behavior when docs are silent — mark ⬜ with a note
  on what's needed (e.g. "needs live legacy call, credentials required")
  rather than assuming.