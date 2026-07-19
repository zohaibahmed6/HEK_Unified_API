using HekCoreApi.Contracts.Auth;
using HekCoreApi.Contracts.Security;

namespace HekCoreApi.Application.Common.Interfaces;

/// <summary>
/// Mints HEK Core API's own resource-scoped JWT (ADR-003) carrying patientId/encounterId/
/// practiceId/originScope claims. Distinct from Entra ID's own tokens - Entra ID validates the
/// caller's credential (see IIdentityValidator); it has no concept of patient/encounter scoping, so
/// the platform issues its own token for that purpose. Flagged as an inference from reading
/// ADR-002 and ADR-003 together - see PROJECT_STATUS.md.
/// </summary>
public interface IJwtTokenIssuer
{
    Task<TokenResponse> IssueAsync(ResourceScope scope, CancellationToken ct = default);
}
