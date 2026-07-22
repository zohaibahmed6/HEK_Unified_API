namespace HekCoreApi.Application.Common.Models;

/// <summary>
/// Read-path result from the central HISO session registry (HekTenantRegistry.HisoSessions) - a
/// SessionGuid's own routing details. Carries DbServerHost/DbName directly (not just PracticeId/
/// Environment) since each HisoSessions row is self-sufficient - no secondary Practices lookup.
/// </summary>
public sealed record HisoSessionRoute(string PracticeId, string Environment, string DbServerHost, string DbName, DateTimeOffset CreatedAt);
