# Requirement Document — AI Transition Platform / Unified API Gateway
Source: "AI Transition Platform - Task Update.vtt"
Speaker: Dr. Ahmed Javad

## Overview
Zohaib walked Dr. Javad through the current state of the unified API gateway (consolidating ERMS, Cairo, and Claims Online behind one API, with a connection-string registry replacing per-server web.config entries). Dr. Javad reviewed the demo, confirmed the design is on the right track, and assigned research tasks for authentication and auto-scaling ahead of a wider review next week.

## Requirements

### Research: Authentication
1. Research the latest/modern authentication methodologies for APIs — specifically what is meant by running APIs through "modern OAuth" (as opposed to the current username/password-based authentication).
2. Evaluate whether/how to move the gateway's auth model from plain username/password to a modern OAuth-based approach.

### Research: Auto-Scaling
3. Research the latest auto-scaling methodologies applicable to the .NET Core–based gateway.
4. Plan to containerize the API in Docker so it can scale up/down automatically based on load.
5. Plan to publish/deploy the service in a way (Dr. Javad specifically mentioned "publish in Europe") that supports automatic scaling with no manual intervention ("no drama").

### Deliverable / Timeline
6. Complete the above research by Friday ("juma").
7. Following the research, prepare a proper test/challenge of the gateway.
8. The following week, call in stakeholders/reviewers to demo what has been built and gather feedback/critique.

### Confirmed / Approved (no action needed, noted for context)
9. The unified API approach — one gateway API internally routing to ERMS, Cairo, and Claims Online, with existing external clients seeing zero difference in behavior or data format — was reviewed and approved by Dr. Javad ("zero difference to them, absolutely zero").
10. The connection-string registry design (replacing web.config-based server connection strings, enabling new practices/environments to be added without redeployment) was reviewed and approved.
11. The middle-layer/gateway concept — a single entry point that knows which backend servers exist, takes the incoming call, fetches data from the right server, and returns it in the expected (e.g. ERMS) format — was confirmed as correctly understood by Dr. Javad.
12. Telemetry/logging has already been added per Dr. Javad's earlier request and was acknowledged as done.

## Open Questions / Ambiguities
- Dr. Javad referenced "modern OAuth" but was unsure of the exact terminology himself ("I don't know what it's called") — the research task should clarify and confirm the correct standard/approach (e.g., OAuth 2.0 client credentials, OpenID Connect, etc.).
- "Publish in Europe" — unclear if this refers to a specific Azure/AWS region, a specific hosting requirement, or was said informally; worth confirming with Dr. Javad.
- The full scope of "logging on every endpoint" (mentioned earlier by Zohaib as pending) is not explicitly re-confirmed as a requirement by Dr. Javad in this transcript, but was referenced as already in progress.

## Notes
- Current status per Zohaib: backend/API consolidation, UI, AWS integration, and .NET Core DLL integration are complete and tested (using local/test data, not production patient data). Logging implementation is the one item still pending execution.
- Next milestone: research done by Friday → proper test built → demo to stakeholders the following week.
