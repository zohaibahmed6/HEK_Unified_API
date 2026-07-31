# Client Delivery Readiness Report

Produced 2026-07-28. Every claim below was checked this session (test run, live call, source grep, or docker-compose read) — not copied from another status file. Where an existing status file disagrees with what was actually found, that's called out explicitly.

## 1. Where you actually stand

Against **spec v1.1's bar** (`docs/HEK_UNIFIED_API_SPEC_v1.1.md` — an addendum to v1.0, not a replacement):

| Spec requirement | Status |
|---|---|
| FR-1–FR-9 (v1.0 core: unify HISO/Karo/ERMS behind one hub, canonical model, field scoping, audit trail) | **Substantially built.** All 60 legacy operations across HISO/ERMS/KARO/COL confirmed present and field-verified (`LEGACY_PARITY_VALIDATOR.md`). Audit logging confirmed live (`CanonicalDemographicsAccess consumer=... practiceId=... fieldsReturned=...`). |
| FR-10 (zero change for existing consumers) | **Met** — legacy-compat controllers preserve exact existing call shapes (JSON/XML/SOAP as each system originally used). |
| FR-11 (bring in *all* integrations — "Florence" + unnamed others) | **Not started.** "Florence" is unconfirmed/unidentified; no work has begun here because the system isn't even named yet. |
| FR-12 (call-flow traceability — show per-request which environment it came from / was routed to) | **Gap, verified just now.** Checked `src/Api/Telemetry/LegacyOperationObserver.cs` — audit logs carry `consumer`/`practiceId`/`endpoint`/`fieldsReturned`, but **not** which DB server/environment actually fulfilled the request. The frontend dashboard (`SystemDashboard.tsx`) has no environment/routing display either (grepped, zero matches). The static diagrams in `docs/flow-diagrams/` show the *general* architecture, not a *per-request* trace. This is a real, unmet requirement, not just undocumented. |
| FR-13 + PR-7 (working demo UI, real systems not mocks, architecture/call-flow diagram) | **Partial.** Frontend dashboard exists and drives real endpoints (confirmed live this session: HISO getData returned real NHI `ZZZ0083`/`HAMZA ARSHAD`/`1995-08-31`). `docs/flow-diagrams/*.html` diagrams exist for HISO getData, ERMS authenticate + GetScannedList, KARO authenticate — **these were silently broken until fixed earlier this session** (missing the Mermaid.js library entirely; would have rendered as raw text in front of a client). Now fixed and should be re-checked in an actual browser before any demo. |

## 2. What's pending — ranked by what actually blocks delivery

### A. Needs Zohaib specifically (external/decision-blocked, not something more code can fix)
1. **Docker networking — actually already resolved**, contradicting `PROJECT_STATUS.md` item #33 which still describes it as an open blocker (`docker compose up` can't reach `sqlserver` container, host-level networking issue, 2026-07-20 entry). **Verified today**: current `docker-compose.yml`'s `api` service no longer depends on the `sqlserver` container at all — it points `ConnectionStrings__TenantRegistry` at `host.docker.internal`, i.e., the host's own SQL Server. `docker ps` confirms no `sqlserver` container is even running, and `hekcoreapi-api-1` has been up 3 days serving real data successfully. **This item should be closed, not left open** — the workaround already shipped.
2. **"Florence" integration name + the "several other" integrations (FR-11)** — nothing can start here until Zohaib identifies what these actually are.
3. **SOUTH environment (practice 518) DB connection details** — blocks closing v1.1-plan Step 8's remaining KARO/ERMS/COL read-endpoint pass.
4. **Aspose license status** — worth explicitly re-confirming with Zohaib now, because it may already be resolved (see §3 below) and the open item should be closed if so.
5. **`AWSDoc.IndiciDMS` DLL mapping / second DB node connection string** — Zohaib said he'd supply later; check if this promise has been fulfilled since.
6. **practiceId lookup key for legacy compat auth translators (#32)** — username vs. pho, still unconfirmed, currently shipped OFF by default.

### B. Real, confirmed code gaps (not documentation drift — genuinely unbuilt)
7. **ERMS Authenticate's Azure-forwarding proxy is entirely unported** (confirmed via full-repo grep for "Azure" — zero hits outside secret-provider files). Any practice whose EncounterId suffix contains "azure" would silently be served locally instead of proxied, a behavior change from legacy. Needs a decision: port it, or deliberately retire it (and document that decision).
8. **FR-12 traceability is unbuilt**, per §1 above — the audit log doesn't carry environment/DB-server routing info.
9. **`/auth/token`'s `originScope` for a direct (non-legacy) caller (#26)** — unresolved, endpoint returns `501`.
10. **CORRECTED 2026-07-28 — item #28's "~34 of ~35 unverified" framing was itself stale, same as #30/#33/#34.** That framing is dated 2026-07-20, right after the *first* live test (KARO demographics — which did catch a real column-name bug, `FirstName`/`LastName` existing but holding unrelated data). It was never updated after what happened next: 2026-07-20/21, Zohaib supplied the **complete real legacy source code** for KARO (`APIController.cs`, `HSSDA.cs`), HISO (`FormSessionService.svc.cs`), and COL (`PHCO.cs`) — so procedure names stopped being guessed and started being read directly from real source. Then, 2026-07-21 through 2026-07-23, nearly every KARO/ERMS/HISO/COL operation was rebuilt against that real source and **live-verified against real production data** (patient 2459731, practice 901) — `PROJECT_STATUS.md`'s own entries (lines 261-290, 642-676) document this in detail, often querying raw columns via `sqlcmd` *before* writing code. Covered: all KARO operations (Ping/Authenticate/9+9+2 read+write ops), ERMS Clinical Notes/Lab Results/Medications/Allergies/CurrentProvider/Practitioners/Radiology/Smoking/NextOfKin, COL Conditions. Real mismatches (e.g. pipe-delimited `ReferenceId` columns) were found and fixed live, not assumed away. **What genuinely remains unverified/unbuilt today is a short, specific list, not a vague "34 unknown"**: Observations (no confident HISO concept match), Measurements (delimiter shape `"{ref}|&|{value}|?|{type}|?|{code}|?|{label}|?|{date}"` not yet decoded), Encounter Summary (confirmed real legacy stub, correctly left as-is rather than faked), COL `GetSessionData` (deliberately preserves a real legacy bug — calls an empty stored-procedure name). This is a materially smaller risk than originally reported here — apologies for repeating the stale framing without cross-checking it against the rest of the same file first.

### C. Formal phases not started at all
11. **Phases 13–17** (Testing, Performance Testing, Security Review, Deployment, Production Readiness) are **all ⬜ Not Started** per `PROJECT_STATUS.md`, and nothing found this session contradicts that. Concretely:
    - No load/performance test exists against the stated 10,000-concurrent-user target — no SLA/latency numbers exist to hold the system to.
    - No formal security review has been run (beyond the Day-1 build-time hardening already in the code).
    - No deployment runbook / production readiness checklist exists.
    - `Domain.UnitTests` has **zero tests** (confirmed by running `dotnet test` just now) — an empty test project, not a coverage gap that's merely thin.

### D. Housekeeping (low urgency but should not ship silently)
12. **`PROJECT_STATUS.md` is stale in at least 3 places** (Aspose/#30, AWS branches/#34, Docker networking/#33 — see §3). If this file is ever shown to the client as "current state," it will misrepresent progress in your favor on some items and against you on others. Needs a sync pass.
13. **Contract Review (Phase 7)** scored 4 PASS/3 WARNING pre-v1.1 and was never re-run against the v1.1 addendum.
14. **Rate-limit thresholds** are "generous defaults" — explicitly flagged to revisit once real production traffic data exists (it doesn't yet).
15. **`v1.1-plan-status.md` has an internal inconsistency**: its own summary line claims "all 17 steps done and live-verified," but Steps 4, 5, and 8's row-level status markers say 🟡, not ✅. Whoever reads just the summary would get a falsely rosy picture.

## 3. Documentation-vs-reality drift found this session (the "what could be better" headline finding)

`PROJECT_STATUS.md` was last updated on these specific items around 2026-07-19/20. `LEGACY_PARITY_VALIDATOR.md` (a different, more recently-touched tracker in the same repo) shows real fixes landed on 2026-07-25/26 that were never reflected back:

- **Item #30 (Aspose rendering)**: `PROJECT_STATUS.md` still frames this as blocked on a missing commercial license. `LEGACY_PARITY_VALIDATOR.md` records this as **resolved 2026-07-26** — the "no license available" framing was itself wrong; Zohaib's real `Aspose.Words.dll`/`.lic` were already sitting in `legacy-reference/Hiso/bin/`, vendored into `src/Infrastructure/Legacy/Hiso/vendor/`, and verified working under .NET 8.
- **Item #34 (AWS document flow)**: `PROJECT_STATUS.md` describes ERMS `SaveDocument`/`GetOtherDocs`/`GetDocResults` and KARO `GetDocuments` as deferred pending a "non-portable DLL." `LEGACY_PARITY_VALIDATOR.md` shows all 4 were fixed 2026-07-25/26 using the real, already-available `IAwsDocumentService`/`AWSDocCore.dll` — the "non-portable" framing was stale.
- **Item #33 (Docker networking)**: as covered in §2A above, resolved via architecture change (point at host DB directly), not by fixing the container-to-container path — but the open item was never closed.
- **Item #28 (Block-2 stored procedures)**: written 2026-07-20 after the *first* live verification, never updated after the much larger 2026-07-21–23 wave of real-source-code rebuilds + live verification across nearly every KARO/ERMS/HISO/COL operation (see §2B.10, corrected). This is a 4th instance of the same pattern, and the most consequential one — it made the single biggest remaining technical risk look far larger than it actually is.

**This is the single most valuable thing to fix before showing `PROJECT_STATUS.md` to a client**: three of four drifted items make the project look *less* done than it is (Aspose, AWS docs, Block-2 procedures), and one makes it look like there's still an unresolved networking blackbox (Docker) when there isn't. A client or stakeholder reading this file cold would form a materially wrong picture in both directions — and given this is now a *4th* recurring instance, `PROJECT_STATUS.md` needs a proper close-out pass, not one-off spot fixes, before it's ever shown externally.

## 4. My pre-delivery verification checklist

What I'd actually do before handing this to a client, and what I did/didn't get to this session:

| # | Check | Status |
|---|---|---|
| 1 | Run the full automated test suite | ✅ Done — 66/66 pass (7 Application, 4 Infrastructure, 6 Adapters, 49 API integration). `Domain.UnitTests`: 0 tests (empty project, not a failure but worth noting to a client asking about coverage). |
| 2 | `docker compose up` and confirm the container path actually works clean | ✅ Confirmed indirectly — `api` container already up 3 days, serving real live data (I called it directly for the getData test); compose file shows the container no longer depends on the old broken `sqlserver` path. Recommend one explicit `docker compose down && docker compose up -d --build` dry run before a client demo, just to see it from cold. |
| 3 | Live-call each of the 4 legacy-compat surfaces with real credentials | Partially done — HISO getData spot-checked live this session with real data. ERMS/KARO/COL not re-spot-checked in this pass (already covered extensively in earlier `LEGACY_PARITY_VALIDATOR.md` work); recommend one fresh live call per system right before the actual demo, since data/session validity can drift day to day. |
| 4 | Check `LEGACY_PARITY_VALIDATOR.md`'s open gaps | ✅ Done — one open gap: ERMS Azure-forwarding (see §2B.7). |
| 5 | Confirm frontend dashboard drives at least one real end-to-end call per system | Partial — HISO confirmed this session. ERMS/KARO/COL panels exist per earlier status notes but weren't re-clicked-through today. |
| 6 | Confirm `docs/flow-diagrams/*.html` actually render | Fixed this session (all 5 were missing the Mermaid.js library, would have shown raw diagram-syntax text to a client). **Not yet re-opened in an actual browser to visually confirm the fix** — do this before any client-facing use. |
| 7 | Flag everything Phases 13–17 haven't covered | ✅ Done — see §2C. No load test, no formal security review, no deployment runbook. This is the honest answer to "is this production-ready": no, it's demo-ready with real data, not load/security/ops-hardened yet. |

## Bottom line

You're at: **legacy parity is genuinely strong (60/60 operations, field-verified, real live data confirmed, and — corrected in this update — the large majority of underlying stored procedures are source-derived and live-verified too, not guessed)**, the demo surface (dashboard + diagrams) is close but had a silent rendering bug just fixed and needs a fresh look, and the honest gap is that **hardening never started** — no load test, no security review, no deployment process. The real remaining unverified-procedure list is short and named (Observations, Measurements, Encounter Summary scope, COL GetSessionData's known bug), not a large unknown. Before showing this to the client: sync `PROJECT_STATUS.md`'s stale items (now 4 confirmed instances, including this one), decide on the Azure-forward gap, re-render-check the diagrams in a browser, and be upfront that Phases 13–17 are the next phase of work, not already done.
