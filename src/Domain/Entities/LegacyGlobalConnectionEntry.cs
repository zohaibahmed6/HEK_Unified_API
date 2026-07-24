namespace HekCoreApi.Domain.Entities;

/// <summary>
/// v1.1 spec follow-through, Step 7 (2026-07-24): replaces the single global env-var secrets
/// (`Hiso:SecondNodeConnectionString`, `Hiso:IndiciMasterConnectionString`) with real registry rows.
/// Confirmed from real legacy source (`DBMessages.cs`'s `ExecuteHisoProcedure`) that both the HISO
/// second-node and Indici-master connections are genuinely global-per-environment, not per-practice -
/// second-node is chosen by which stored procedure is called (see
/// <c>HisoSecondNodeProcedures</c> in the Application layer), master is used at one fixed ACC45-save
/// call site. One row per environment-scoped connection <see cref="Key"/> (e.g. "Hiso:SecondNode",
/// "Hiso:IndiciMaster"), swappable per deployed environment without a redeploy - unlike a single
/// hardcoded secret that had to be identical across every environment.
/// </summary>
public sealed class LegacyGlobalConnectionEntry
{
    public int Id { get; set; }

    /// <summary>e.g. "Hiso:SecondNode", "Hiso:IndiciMaster" - one row per real global connection target.</summary>
    public required string Key { get; set; }

    public required string DbServerHost { get; set; }

    public required string DbName { get; set; }

    /// <summary>Same <c>Legacy:DbCredentials:{host}</c> secret-key pattern every other connection resolver already uses - credentials never stored in this table.</summary>
    public required string CredentialSecretKey { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
