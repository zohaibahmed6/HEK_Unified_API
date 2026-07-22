namespace HekCoreApi.Application.Common.Interfaces;

/// <summary>
/// Resolves a full ADO.NET connection string for a practice's routed legacy database (ADR-001).
/// Every Block 2 domain repository goes through this rather than building its own connection
/// string, so tenant routing stays centralized in exactly one component (FR-PAT-02).
/// </summary>
public interface ILegacyPracticeConnectionResolver
{
    /// <summary>Throws <see cref="Domain.Exceptions.NotFoundException"/> if <paramref name="practiceId"/> is not a registered practice.</summary>
    Task<string> ResolveAsync(string practiceId, CancellationToken ct = default);

    /// <summary>Resolves by the full (PracticeId, PracticeCode, Environment) triple (KARO/ERMS canonical routing).</summary>
    Task<string> ResolveAsync(Models.RoutingContext context, CancellationToken ct = default);

    /// <summary>
    /// HISO's "second database node" (legacy `ConectionStringPMS_NZ_SecondNode`, confirmed from real
    /// source, legacy-reference/Hiso/DAL/DBMessages.cs) - a single global second connection legacy
    /// routed six specific procedures to (lab/radiology/attachment/incoming-letter/outgoing-letter/
    /// problem), not a per-practice tenant-registry entry like <see cref="ResolveAsync(string, CancellationToken)"/>. Zohaib will
    /// supply the real target later (PROJECT_STATUS.md, HISO wire-compat rebuild) - throws
    /// <see cref="Domain.Exceptions.NotFoundException"/> until the `Hiso:SecondNodeConnectionString`
    /// secret is configured, rather than silently falling back to the primary connection.
    /// </summary>
    Task<string> ResolveSecondNodeAsync(CancellationToken ct = default);

    /// <summary>
    /// HISO's separate "Indici Master" connection (legacy `ConectionStringIndici_Master`, confirmed
    /// from `PatientEmployerOrganisationBuilder.cs`) - used only for Country/City/Suburb/Designation
    /// lookup/upsert calls during `processAction`'s `"save"` branch. Not per-practice, a single global
    /// target like <see cref="ResolveSecondNodeAsync"/>. Throws
    /// <see cref="Domain.Exceptions.NotFoundException"/> until the `Hiso:IndiciMasterConnectionString`
    /// secret is configured.
    /// </summary>
    Task<string> ResolveIndiciMasterAsync(CancellationToken ct = default);
}

/// <summary>
/// Exact 6-procedure predicate (case-insensitive) legacy used to decide primary vs. second-node
/// routing - confirmed from `DBMessages.cs`'s `ExecuteHisoProcedure`. Kept as its own static class so
/// both the connection resolver and any future caller share one source of truth for this list.
/// </summary>
public static class HisoSecondNodeProcedures
{
    private static readonly HashSet<string> Names = new(StringComparer.OrdinalIgnoreCase)
    {
        "hiso.uspgetpatient_laboratoryreport",
        "hiso.uspgetpatient_radiologyreport",
        "hiso.uspgetpatient_attachment",
        "hiso.uspgetpatient_incomingletter",
        "hiso.uspgetpatient_outgoingletter",
        "hiso.uspgetpatient_problem"
    };

    public static bool RequiresSecondNode(string procedureName) => Names.Contains(procedureName);
}
