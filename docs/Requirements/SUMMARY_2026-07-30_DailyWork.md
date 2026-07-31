# Daily Work Summary — July 30, 2026

**Scope:** Research + implementation follow-up on Dr. Javad's two assigned items — modern authentication and auto-scaling — ahead of the Friday review.

## 1. Research Delivered

Two presentation-ready documents were produced under `docs/Requirements/`:

- **`RESEARCH_Auth_AutoScaling.md`** — plain-language research covering OAuth 2.0 (specifically the Client Credentials flow, since our callers are always systems, not humans), OpenID Connect, Entra ID, and Azure auto-scaling options (Azure Container Apps recommended over App Service or Kubernetes/AKS at our current scale). Written so it can be lifted directly into slides.
- **`SUGGESTIONS_Gateway_NextSteps.md`** — concrete, prioritized next steps: rate limiting, the Entra ID fix (below), standing up a real Entra tenant, and the Container Apps deployment path, plus an anticipated-questions section for the live demo.

Both documents were corrected through several rounds of review against the actual codebase (not just the source requirement transcript), including removing an incorrect "publish in Europe = Azure/GDPR" assumption that was not something Dr. Javad actually said, and correcting an earlier claim that legacy endpoints call canonical endpoints internally (verified false — they are fully separate code paths).

## 2. Code Fixes Implemented

**Rate limiting** — enabled a two-tier policy using the existing `RateLimitOptions` infrastructure:
- Login/authenticate endpoints (ERMS, KARO, COL, canonical `/auth/token`): **10 requests/minute**
- All other endpoints: **30 requests/minute**
- Files: `RateLimitOptions.cs`, `RateLimitPolicyNames.cs` (new), `Program.cs`, and the four auth controllers.

**Entra ID authentication gap fixed** — found and corrected a real implementation bug in `EntraIdIdentityValidator.cs`: the code was verifying the *gateway's own* app credentials with Entra ID instead of the *caller's* credentials, meaning any non-empty username/password would have silently passed once enabled. Fixed by:
- Making the flawed username/password path fail closed (reject) instead of falsely succeeding, since that pattern (ROPC) is deprecated and wrong for our system-to-system use case.
- Adding the correct inbound token validation path: a new `EntraBearer` authentication scheme in `Program.cs` that validates tokens *presented by calling applications* (which authenticate directly with their own Entra App Registration) against Entra ID's public keys — activates automatically only once a real tenant is configured, with zero effect until then.

## 3. Verification

- Full solution build: **0 errors, 0 warnings**.
- Full test suite: **66/66 tests passing** (Domain, Application, Infrastructure, Adapters, and API integration tests) — no regressions from either change.

## 4. Open Items Requiring Dr. Javad / Org-Level Input

- Azure region for deployment (explicitly undecided — not "Europe" as earlier assumed).
- Whether a real Azure/Entra ID tenant already exists, or needs to be created, and who owns that setup.
- Confirmation that the "prepare a proper test/challenge of the gateway" line in the original requirement doc was a transcription artifact, not something Dr. Javad actually said — flagged so it isn't presented as a commitment.

## 5. Not Done Today (Deliberately Out of Scope)

- Legacy endpoint JWT translation — investigated, but confirmed to have no functional benefit today (legacy controllers don't call canonical endpoints), so deprioritized to an optional future cleanup rather than implemented.
- Actual load test / auto-scaling demo build-out — a next step, not part of today's research pass.
