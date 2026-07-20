using System.Data;
using HekCoreApi.Application.Common.Models;

namespace HekCoreApi.Application.Common.Interfaces;

/// <summary>
/// HISO's database-driven concept-mapping execution engine (HISO-BR-03) - resolves a named stored
/// procedure's parameter list from the DB, maps session context onto it, and executes it. This is
/// the actual "getData" mechanism nearly every HISO-sourced Block 2 read endpoint depends on
/// (demographics, notes, conditions, medications, lab/radiology, observations all list "HISO
/// getData" as their legacy source in the Contract Design doc).
/// </summary>
public interface IHisoConceptExecutor
{
    /// <summary>
    /// Executes <paramref name="procedureName"/> for the given session. Returns null if the
    /// procedure failed (matching the legacy engine's own "return null, log, don't throw" pattern
    /// for this specific call).
    /// </summary>
    Task<DataSet?> ExecuteAsync(string procedureName, HealthLinkSession session, IReadOnlyList<HisoRequest> requests, CancellationToken ct = default);
}
