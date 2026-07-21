using System.Data;

namespace HekCoreApi.Application.Common.Models;

/// <summary>One executed HISO concept procedure's raw result, keyed by procedure name for later field lookup (`Hiso.ConceptMapper.ProcedureResult`, confirmed from legacy source).</summary>
public sealed class ProcedureResult
{
    public required string ProcedureName { get; set; }

    public DataSet? DsResult { get; set; }
}
