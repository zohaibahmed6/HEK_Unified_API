using HekCoreApi.Contracts.Security;

namespace HekCoreApi.Contracts.Admin;

/// <summary>dbServerHost/dbName/sourceSystem resolve via the tenant registry (ADR-001) and are never exposed to non-admin callers - internal routing detail only. This endpoint IS the admin-scoped caller.</summary>
public sealed record Practice(string PracticeId, string Name, string? PhoCode, string DbServerHost, string DbName, OriginScope SourceSystem)
{
    public static Practice FromInput(string practiceId, PracticeInput input) => new(practiceId, input.Name, input.PhoCode, input.DbServerHost, input.DbName, input.SourceSystem);
}
