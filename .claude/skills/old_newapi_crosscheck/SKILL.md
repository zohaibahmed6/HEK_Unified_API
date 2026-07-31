---
name: legacy-api-parity-check
description: Verifies that a new/modernized API returns the same results as the legacy API it's replacing, endpoint by endpoint. Scans both codebases to discover every legacy and new endpoint, maps legacy endpoints to their new equivalents, calls each pair live (via curl), diffs the responses, and writes a Markdown parity report flagging every mismatch with enough detail to locate and fix the bug in the new API. Use this whenever the user is migrating/modernizing an API and wants to confirm the new API behaves identically to the legacy one, mentions "legacy API", "parity check", "regression check between old and new API", "compare old vs new endpoint responses", or asks to validate that a rewritten API hasn't broken anything compared to the original. Also trigger if the user says something like "test my new api against the old one" or "make sure new endpoints match old ones", even without using the word "parity".
---

# Legacy API Parity Check

Confirms a new API is a faithful replacement for a legacy API by calling matching endpoints on both and diffing the real responses. This catches silent regressions that unit tests miss — the new endpoint might return 200 OK with subtly wrong data, and that only shows up by actually calling it and comparing.

This is a **report-only** skill. It never modifies the new API's source code — it only tells you exactly where the two APIs disagree so a human (or a follow-up coding task) can fix it. Never auto-edit code found during this check; that's a separate, deliberate step the user should approve.

## When you don't have enough information yet

Before running anything, make sure you know:

1. **Where both codebases live** (legacy API root, new API root). If either path is missing, ask for it — don't guess a path.
2. **Base URLs** for both running APIs (e.g. `http://localhost:4000` legacy, `http://localhost:8080` new). These must be live/reachable — this skill calls real endpoints, it doesn't mock anything.
3. **Auth**, if any: a static API key/header, or a login flow (username/password endpoint that returns a token). Ask which, and get the credentials/env var names — never hardcode secrets into the skill or the report.
4. **Sample data / path params**: if endpoints take path or query params (e.g. `/orders/:id`), ask the user for a few realistic sample values to test with, or look for fixture/seed data in the repos.

Ask these as normal chat questions before starting — don't leave placeholders in the final report.

## Workflow

### 0. Create a run memory file first

Before calling anything, create `PARITY_MEMORY.md` (or `.json` if the user prefers machine-readable) in the working directory. This is a running log, not the final report — append to it as you go, don't wait until the end to write it. It exists so that if the run is interrupted, or the user asks "what did you actually send/get back," there's a durable record independent of the final report.

For every single call made during step 3, append an entry immediately after the call returns:

```markdown
## <timestamp> — <METHOD> <legacy path> vs <new path>
**Request sent (legacy):** <method, url, headers (redact secrets), body>
**Response (legacy):** <status code, body>
**Request sent (new):** <method, url, headers (redact secrets), body>
**Response (new):** <status code, body>
**Issue found:** <describe the mismatch here if any, or "none">
```

Never write real tokens/API keys/passwords into this file — reference them as `$ENV_VAR_NAME` instead. Keep this file growing throughout the run; `compare_responses.py` and the final `PARITY_REPORT.md` in step 5 are a distilled/organized version of what's already in this memory file, not a replacement for it. Point the user to `PARITY_MEMORY.md` if they want the raw call-by-call trail behind any line in the summary report.

### 1. Discover endpoints in both codebases

Run `scripts/discover_endpoints.py` against each codebase root:

```bash
python3 scripts/discover_endpoints.py --root <path-to-api> --out endpoints.json
```

It regex-scans common framework route patterns (Express/Fastify, Laravel, Django/DRF, Flask, FastAPI, Spring `@RequestMapping`/`@GetMapping` etc., ASP.NET attributes) and emits a JSON list of `{method, path, file, line}`. If the framework isn't recognized, it falls back to searching for path-like string literals near HTTP verb keywords — treat those results as lower-confidence and sanity-check a few by eye.

Run this once for the legacy root and once for the new root.

### 2. Map legacy endpoints to new endpoints

Run `scripts/map_endpoints.py` to pair them up:

```bash
python3 scripts/map_endpoints.py --legacy legacy_endpoints.json --new new_endpoints.json --out mapping.json
```

It matches by HTTP method + normalized path (ignoring version prefixes like `/v1`, `/api`, and param name differences like `:id` vs `{id}` vs `<id>`), then falls back to fuzzy path similarity for anything unmatched. Anything it can't confidently pair goes into an `unmapped` list in the output — review this list with the user rather than guessing; a wrong pairing produces a misleading report.

If the user already knows the mapping isn't 1:1 (e.g. two legacy endpoints merged into one new one), ask them for an explicit mapping file instead (same JSON shape) and skip the auto-mapper.

### 3. Call every pair and capture responses

Run `scripts/run_parity_check.py`:

```bash
python3 scripts/run_parity_check.py \
  --mapping mapping.json \
  --legacy-base-url http://localhost:4000 \
  --new-base-url http://localhost:8080 \
  --auth-header "Authorization: Bearer $TOKEN" \
  --params sample_params.json \
  --out parity_results.json
```

This calls each endpoint pair with curl (same method, same params/body, same auth) against both base URLs, and records status code, headers of interest, and JSON body for each.

Notes:
- If auth requires a login flow, run that first (curl to the login endpoint) and pass the resulting token via `--auth-header`.
- For endpoints requiring path params (`/orders/:id`), supply `sample_params.json` mapping each path template to real sample values — ask the user for these if not already known, or pull IDs from a legacy `GET /list`-style endpoint first.
- Endpoints that mutate state (POST/PUT/DELETE) are risky to fire twice — flag these to the user and confirm before calling them against real environments, or ask for a safe test/staging environment to point at instead.

### 4. Compare and generate the report

Run `scripts/compare_responses.py`:

```bash
python3 scripts/compare_responses.py --results parity_results.json --out PARITY_REPORT.md
```

Comparison logic (apply this thinking, don't just diff blindly):
- **Status code** must match exactly.
- **Structure**: every field present in the legacy response should exist in the new response with the same type, and vice versa for unexpected new fields (flag extras too — they might indicate leaking internal data).
- **Values**: compare actual values, but treat inherently-different fields (timestamps, request IDs, auto-increment IDs, `generated_at`, pagination cursors) as expected-to-differ — don't flag those as mismatches unless their *type* is wrong. Use judgment here; if unsure whether a field is expected to vary, list it under a "needs human review" section rather than silently ignoring or falsely flagging it.
- **Ordering**: for list/array responses, note if item order differs even when the set of items is the same — this is a common, easy-to-miss regression.

### 5. Write the report

Always use this structure for `PARITY_REPORT.md`:

```markdown
# API Parity Report — <date>

## Summary
<N endpoints checked, N matched, N mismatched, N unmapped/skipped>

## Mismatches
### <METHOD> <legacy path> → <new path>
- Legacy: <status code, brief shape>
- New: <status code, brief shape>
- Diff: <concrete field-by-field differences>
- Likely cause / where to look: <file:line in new API if discoverable from step 1's discovery output, otherwise "not localized">

## Needs human review
<fields/endpoints where automated comparison couldn't confidently judge>

## Unmapped legacy endpoints
<legacy endpoints with no confident new-API counterpart — these may be missing in the new API entirely>
```

Save the report next to where the user is working, and tell them the summary numbers directly in chat rather than making them open the file to learn there's a problem.

## After the report

Don't fix the new API's code as part of this skill. Present the mismatches, and if the user then asks you to fix the identified issues, treat that as a new, separate task — read the specific new-API file(s) named in the report and confirm the fix with the user's usual review process rather than batch-editing everything at once.