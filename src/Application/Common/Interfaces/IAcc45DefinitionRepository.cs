using HekCoreApi.Application.Common.Models;

namespace HekCoreApi.Application.Common.Interfaces;

/// <summary>Ported from legacy-reference/Hiso/Acc45DefinitionBuilder.cs (`GenerateTable`/`SaveAcc45Definition`/`GetACC45Definition`).</summary>
public interface IAcc45DefinitionRepository
{
    Task<bool> SaveDefinitionAsync(Acc45DefinitionInput input, HealthLinkSession session, CancellationToken ct = default);

    /// <summary>DB-only part of legacy's `GetACC45Definition` - `ResumePath`/`ViewType` come from here for real; the actual `View` bytes come via <see cref="IDmsProxyService"/> (real direct-DB implementation as of 2026-07-31).</summary>
    Task<Acc45DefinitionRow?> GetDefinitionAsync(HealthLinkSession session, CancellationToken ct = default);
}

public sealed record Acc45DefinitionInput(
    string? FormInstanceId, string? FormInstanceVersion, string? FormEngineId, string? FormInstanceOperationMode,
    string? FormDefinitionId, string? FormDefinitionVersion, string? FormDefinitionTitle,
    string? ViewType, string? ViewSignature, string? ResumePath, string DmsDocumentId,
    string FormXml, string? FormComments);

public sealed record Acc45DefinitionRow(string? ResumePath, string? ViewType, Guid? DmsDocumentId, string? FormXml);
