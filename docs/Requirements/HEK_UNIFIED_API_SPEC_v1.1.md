# HEK Unified Healthcare API Hub — Specification Addendum

**Project:** HEK Unified Healthcare API (API Hub / Integration Gateway)
**Source:** Dr. Ahmed Javad's spoken requirements, continuation of the "AI Transition Platform" meeting transcript (Hindi/Urdu, machine-transcribed; translated to English below)
**Author:** Zohaib Ahmed
**Date:** 2026-07-23
**Status:** Draft v1.1 — Addendum to v1.0

This document extracts **only Dr. Javad's statements** from the newly supplied transcript segment (~18:04–22:40), translates them to English, and folds them into the existing spec as new/updated requirements. Original v1.0 requirements are unchanged; new items are appended below and should be read together with `HEK_UNIFIED_API_SPEC.md`.

---

## 1. Dr. Javad's Statements — English Translation

Translated in speaking order. Fragments are merged where they clearly continue one thought; asides directed at other call participants (camera, greetings) are omitted as non-requirements.

1. "The system is running — this is the API, you can't [just change it live]. Anyway, mature this a bit first."
2. "No — mature it."
3. "Mature this further and show me what you've done. Because what you've built now — the real question is: are we not asking them [the existing consumers] to change anything?"
4. "We become unified on the back end."
5. "No, no, that's fine — but the consumers say: we won't change anything on our side. Call us the same way, give us the data the same way. So what do we do about that?"
6. "We need this data delivered in exactly the same format going forward — do whatever you want on the back end, we don't need to know — because that is our agreement with you."
7. "Everyone listen — talk less, hear me out. I want the [existing consumers] to not have to change anything at all."
8. "The ERMS side should also not have to change anything — no changes. Whoever is calling us today keeps calling the same way; the unification work happens behind that, on our side."
9. "So it becomes unified [on our end]."
10. "Okay — go ahead, take this forward, and show me a proper demo. Don't do this token/mock kind of thing. Build an interface as part of the demo — the interface is easy to build."
11. "Inside that interface, show how it's actually happening."
12. "Listen to my requirement again, because listening is very important — you cannot work with AI unless your listening skills are good."
13. "On the back end we've unified everything — our side is fine."
14. "We are basically exposing [a single API] so that consumers come in."
15. "They should all come to one single place and call from there, so we get their data in the calls they already make. HISO comes in, Karo comes in, ERMS comes in. Whoever else — because we also have several other integrations running (e.g. a system referred to as 'Florence' in the transcript — name unclear, needs confirmation) — somehow we need to bring all of them onto a single place. I understand what you've built so far, but take it one step further."
16. "Next time you demo this, I need to see actual systems — not this token/mock business — because I'm going to have you present this to other people [stakeholders]."
17. "Show me/tell me what people say — how does the rest of the company take this forward — because we need to carry this ahead."
18. "The whole industry right now says you cannot have multiple servers [per integration] — that's all been running as patchwork/ad-hoc add-ons. We need to finish [consolidating] this. Next week, show me how you're doing it — and also show me a diagram of where a call comes from and where it goes. The industry reportedly has around 18 different environments (number as stated in transcript, needs verification) of this kind."
19. "So — from which environment was this pulled, and how?"
20. "Explain it clearly — do you understand or not?"
21. "It's necessary — AI won't do the understanding for you. You have to understand what is required. There's no time going forward to bring more people/systems in without that understanding."

---

## 2. New / Updated Requirements Derived from the Above

### 2.1 Functional Requirements (additions to Section 3 of v1.0)

| ID | Requirement | Source |
|----|-------------|--------|
| FR-10 | **Zero change for existing consumers.** HISO, Karo, ERMS, and any other current caller must keep calling exactly as they do today — same request shape, same response format. All unification happens strictly behind the gateway; consumers must not be asked to change anything. | Items 3, 5, 6, 7, 8, 15 |
| FR-11 | **Bring all running integrations into one place**, not just the original three. The transcript references additional systems already running in parallel (one referred to as "Florence" — name to be confirmed) that must also be routed through the same hub. | Item 15 |
| FR-12 | **Call-flow traceability.** The system/demo must be able to show, per request, which environment the call originated from and which environment/system it was routed to and fulfilled from. | Items 18, 19 |
| FR-13 | **Working demo interface.** The next demo must include an actual interface (UI) built around the API — not a bare token/mock exchange — so the flow can be shown visually end-to-end. | Items 10, 11, 16 |

### 2.2 Process Requirements (additions to Section 5 of v1.0)

| ID | Requirement | Source |
|----|-------------|--------|
| PR-7 | Next checkpoint: **one week out**, with (a) a working demo using **actual systems**, not mocked/token data, and (b) an **architecture/call-flow diagram** showing request origin → routing → fulfillment. | Items 16, 18 |
| PR-8 | The developer (not just the AI) must fully understand the requirement before building — Dr. Javad expects the requirement to be understood and repeated back correctly, not delegated blindly to AI. | Items 12, 21 |
| PR-9 | This work will eventually be presented to other company stakeholders once satisfactory — the bar for polish and correctness should account for that downstream audience. | Item 16 |
| PR-10 | Consolidation is industry-driven: the stated rationale is that running many separate per-integration servers (multiple environments, ad-hoc/patchwork add-ons) is no longer acceptable practice — this justifies the single-hub direction. | Item 18 |

### 2.3 Open Items / Needs Verification

- **"Florence"** — name of an additional running integration mentioned in the transcript; transcription is uncertain and needs confirmation with Dr. Javad or the source team.
- **"~18 environments"** — figure stated in the transcript for how many environments this class of system runs across industry-wide; needs verification, as transcription quality is imperfect throughout this segment.
- Full list of "several other" integrations currently running, beyond HISO/Karo/ERMS/Florence, that need to be folded into the hub.

---

## 3. Net Effect on the v1.0 Spec

- **FR-5** (per-consumer field scoping) and the "no extra data to anyone" rule are reinforced and sharpened: not only must consumers get only their own fields, they must get them through their *existing, unchanged* call pattern — the gateway must accept legacy request shapes and adapt internally rather than requiring consumer-side migration.
- **Section 7 (Acceptance Criteria)** should be updated for the next demo to require: real systems/data (no mocks), a working demo UI, and a call-flow diagram per environment — in addition to the original criteria.
- **Section 8 (Open Items)** gains the three items listed in 2.3 above.

---

*Change Log*
- v1.0 (2026-07-22): Initial spec extracted from meeting transcript.
- v1.1 (2026-07-23): Addendum — Dr. Javad's requirements extracted and translated from continuation transcript segment (18:04–22:40); added FR-10–FR-13, PR-7–PR-10, and open items.
