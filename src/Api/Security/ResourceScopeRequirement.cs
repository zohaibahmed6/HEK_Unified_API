using Microsoft.AspNetCore.Authorization;

namespace HekCoreApi.Api.Security;

/// <summary>
/// Marker requirement for the ResourceScoped policy (ADR-003). The handler below verifies the
/// token carries a well-formed patientId/encounterId/practiceId/originScope claim set; matching
/// those claims against the *specific* resource being requested (e.g. does this token's patientId
/// equal the {patientId} route value) is a per-endpoint concern applied when Block 2 wires this
/// policy onto real routes.
/// </summary>
public sealed class ResourceScopeRequirement : IAuthorizationRequirement
{
}
