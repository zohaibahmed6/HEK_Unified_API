namespace HekCoreApi.Application.Common.Models;

/// <summary>Resolved routing target for a <see cref="RoutingContext"/> - the read-path result of the tenant registry (ADR-001).</summary>
public sealed record PracticeRoute(
    string PracticeId,
    string SourceSystem,
    string DbServerHost,
    string DbName,
    bool RowLevelSecurityEnabled);
