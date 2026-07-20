namespace HekCoreApi.Application.Common.Models;

/// <summary>
/// Per-concept filter/paging/sort criteria HISO's concept-mapping engine applies when executing a
/// procedure (HISO-BR-08/09/10 - dynamic field/group matching). Ported directly from the property
/// usage observed in the supplied `DBMessages.cs` (`ExecuteHisoProcedure`). The original request-
/// parsing logic that builds a full list of these from an incoming getData request (the
/// `Hiso.ConceptMapper` code referenced by `DBMessages.cs`'s `using` but not itself supplied) is
/// out of scope here - domain-group repositories construct one directly with only the fields their
/// specific endpoint needs (e.g. a demographics lookup needs none of the date-range fields).
/// </summary>
public sealed class HisoRequest
{
    public required string ProcedureName { get; set; }

    public string? GroupmaxVal { get; set; }
    public string? FieldmaxVal { get; set; }
    public string? GroupminVal { get; set; }
    public string? FieldminVal { get; set; }
    public string? GroupminDateTime { get; set; }
    public string? FieldminDateTime { get; set; }
    public string? GroupmaxDateTime { get; set; }
    public string? FieldmaxDateTime { get; set; }
    public int GroupStartRowIndex { get; set; }
    public int GroupMaximumRows { get; set; }
    public string? FieldnumRows { get; set; }
    public string? GroupsearchString { get; set; }
    public string? FieldsearchString { get; set; }
    public string? Grouporder { get; set; }
    public string? Fieldorder { get; set; }
    public string? FieldQualifierID { get; set; }
    public string? GroupreferenceID { get; set; }
}
