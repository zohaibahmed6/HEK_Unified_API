namespace HekCoreApi.Application.Common.Models;

/// <summary>
/// One row from HISO's database-driven procedure-parameter dictionary (HISO-BR-03 -
/// "[Hiso].[USPGetProcedureParamList]"), ported directly from the supplied `DBMessages.cs`.
/// Property names preserve the original DB column names exactly (not PascalCase) because the
/// generic DataTable-to-object mapper (Infrastructure's DataTableMapper) binds by column name.
/// </summary>
public sealed class DynamicParam
{
    // ReSharper disable once InconsistentNaming - matches the source stored procedure's column name.
    public string Parameter_name { get; set; } = string.Empty;

    public object? ParamValue { get; set; }
}
