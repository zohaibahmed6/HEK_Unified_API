namespace HekCoreApi.Application.Common.Models;

/// <summary>
/// One row from `[Hiso].[UspGetHisoConcepts]` - HISO's real concept dictionary, confirmed directly
/// from legacy-reference/Hiso/ConceptMapper/HisoConceptDetail.cs (previously unavailable to this
/// project - only referenced by name in DBMessages.cs's `using Hiso.ConceptMapper;`). Property names
/// match the legacy class exactly; Infrastructure's DataTableMapper binds by column name,
/// case-insensitive.
/// </summary>
public sealed class HisoConceptDetail
{
    public string HisoConceptID { get; set; } = string.Empty;
    public string ConceptName { get; set; } = string.Empty;
    public string HisoGroupID { get; set; } = string.Empty;
    public string HisoConceptDetailName { get; set; } = string.Empty;
    public string HisoCategory { get; set; } = string.Empty;
    public string HisoID { get; set; } = string.Empty;
    public string Defination { get; set; } = string.Empty;
    public string HisoDataType { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string TableAlias { get; set; } = string.Empty;
    public string TableColumn { get; set; } = string.Empty;
    public string ColumnAlias { get; set; } = string.Empty;
    public string IsActive { get; set; } = string.Empty;
    public string CodingSystem { get; set; } = string.Empty;
    public string QualifierID { get; set; } = string.Empty;
    public string QualifierName { get; set; } = string.Empty;
    public string ProcedureName { get; set; } = string.Empty;
    public bool IsFixed { get; set; }
    public string Sort { get; set; } = string.Empty;

    /// <summary>Confirmed hardcoded in legacy source (not config-driven, unlike QualifierList).</summary>
    public static readonly string[] MeasurementGroup =
    {
        "Patient_Measurement", "Patient_TestResult", "Patient_Condition_Exists",
        "Patient_DateLastImmunised", "Patient_FullyImmunised"
    };
}
