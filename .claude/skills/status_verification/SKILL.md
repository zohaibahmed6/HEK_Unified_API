---
name: plan-status-verifier
description: >
  Cheaply verify that a project's status-tracker file (e.g. *-plan-status.md,
  PROJECT_STATUS.md) matches reality in the actual codebase/running system —
  without reading whole files or burning tokens. Use when the user asks to
  "verify status", "check if the plan is really done", "confirm the status
  file is accurate", "audit progress", or points at a plan + status file pair
  (e.g. *-full-plan.md / *-plan-status.md). Designed to be run independently
  in multiple terminals/sessions at once, each auditing a different slice of
  steps, so no single session needs the whole codebase in context.
---

# Plan Status Verifier

## Goal

Confirm — cheaply, with evidence — that each step marked ✅/🟡/🔴 in a status
file is actually true in the code / running system. Catch stale or
optimistic status entries. Do NOT re-implement, re-plan, or re-explain the
project. This is an audit, not a design review.

## Hard rule: minimize tokens

- NEVER `Read`/`cat` a whole source file. Use `grep -n`, `rg`, `wc -l`,
  `head -n 5`, or targeted line ranges only.
- NEVER re-derive or re-explain what a step *should* do from first
  principles — the status file already states the claim; just test it.
- One command per check where possible. Chain independent checks with `&&`
  or `;` in a single shell call instead of separate tool calls.
- If a check requires a live system (endpoint, container), use the smallest
  possible probe: `curl -s -o /dev/null -w '%{http_code}'`, `docker compose
  ps --status running`, `docker compose logs --tail=20 <service>` — never
  full logs, never full response bodies unless the check literally requires
  the payload.
- Stop as soon as a check has a clear PASS/FAIL. Don't gold-plate.

## Workflow

1. **Find the pair of files.** Look for `*-full-plan.md` (or similarly named
   plan doc) and `*-plan-status.md` (or `PROJECT_STATUS.md`) in the repo
   root or docs folder. If ambiguous, ask the user which files to use — do
   not guess silently on scope.

2. **Extract testable claims only.** For each row/entry in the status file,
   reduce it to one concrete, checkable fact. Ignore narrative/prose in the
   "Notes" column except to pull out the specific file names, endpoints,
   class/method names, or config keys it mentions. Examples:
   - "Middleware X wired in" → `grep -rn "class X" --include=*.cs`
   - "Migration applied" → check migration file exists + `dotnet ef
     migrations list` or equivalent, not a full DB dump.
   - "Endpoint returns 200" → one `curl -s -o /dev/null -w '%{http_code}'`
     call against the real route.
   - "Bug fixed in file Y" → `grep -n "<the fixed symbol>" Y` to confirm the
     fix is present, not absent.
   - "Container builds clean" → `docker compose build <service> 2>&1 | tail
     -n 15` (tail only, never full build log).

3. **Batch by phase/section for parallel terminals.** If the user is running
   this in multiple VS Code terminals at once, split the status file's steps
   into independent phases/ranges (e.g. "steps 1-5 in terminal A, 6-11 in
   terminal B") so each terminal only loads the subset of code it needs to
   check. Tell the user which range you're covering if asked to split.

4. **Run checks, collect evidence.** For each claim: run the smallest
   command that proves or disproves it. Record the exact command and its
   one-line-or-so output as evidence — do not paraphrase away the proof.

5. **Report a compact table.** Output only:

   | # | Step | Status claimed | Verified? | Evidence |
   |---|------|-----------------|-----------|----------|

   Verified column: ✅ Confirmed / ❌ Mismatch / ⚠️ Could not check (say why
   — e.g. needs a live remote system not available here).

   Below the table, list only the ❌ and ⚠️ rows with one line each on what's
   actually wrong or missing. Do not restate the ✅ rows.

6. **Never silently "fix" anything found broken.** Report it. Ask before
   changing code, unless the user has explicitly said "and fix anything you
   find" up front.

## What NOT to do

- Don't open the plan/status files' entire content into a long response —
  the user already has them.
- Don't summarize the whole project history from the Notes column.
- Don't spin up new features or infer next steps unless asked.
- Don't guess at business rules to make a check pass — if a check is
  ambiguous, mark it ⚠️ and say what's needed to verify it properly.