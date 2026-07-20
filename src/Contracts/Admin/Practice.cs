namespace HekCoreApi.Contracts.Admin;

/// <summary>databaseServerId resolves via the tenant registry (ADR-001) and is never exposed to non-admin callers - internal routing detail only. This endpoint IS the admin-scoped caller.</summary>
public sealed record Practice(string PracticeId, string Name, string? PhoCode, string DatabaseServerId)
{
    public static Practice FromInput(string practiceId, PracticeInput input) => new(practiceId, input.Name, input.PhoCode, input.DatabaseServerId);
}
