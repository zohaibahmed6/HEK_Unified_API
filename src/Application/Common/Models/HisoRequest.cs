namespace HekCoreApi.Application.Common.Models;

/// <summary>
/// Per-concept filter/paging/sort criteria HISO's concept-mapping engine applies when executing a
/// procedure (HISO-BR-08/09/10 - dynamic field/group matching). Originally reconstructed from
/// `DBMessages.cs`'s property usage alone (the real `Hiso.ConceptMapper.HisoRequest` wasn't supplied
/// yet); now extended to match that real class exactly, confirmed from
/// legacy-reference/Hiso/ConceptMapper/HisoConceptDetail.cs. Field names/casing preserved as-is
/// (legacy's own inconsistent casing, e.g. `Grouporder` not `GroupOrder`) rather than normalized,
/// since the ported XML-parsing logic depends on these exact names.
/// </summary>
public sealed class HisoRequest
{
    public string ProcedureName { get; set; } = string.Empty;

    public string? SectionName { get; set; }

    public string? GroupName { get; set; }
    public string? GroupConceptID { get; set; }
    public string? GroupConceptName { get; set; }
    public string? GroupLaboratoryReport { get; set; }
    public int GroupMaximumRows { get; set; } = 50000;
    public string? Grouporder { get; set; }
    public string? GroupreferenceID { get; set; }
    public string? GroupQualifierName { get; set; }
    public string? GroupQualifierID { get; set; }
    public string? GroupCodingSystem { get; set; }
    public string? Grouprefresh { get; set; }
    public int GroupStartRowIndex { get; set; }
    public string? GroupminDateTime { get; set; }
    public string? GroupmaxDateTime { get; set; }
    public string? GroupsearchString { get; set; }
    public string? GroupminVal { get; set; }
    public string? GroupmaxVal { get; set; }

    public string? FieldName { get; set; }
    public string? FieldConceptID { get; set; }
    public string? FieldConceptName { get; set; }
    public string? FieldQualifierName { get; set; }
    public string? FieldQualifierID { get; set; }
    public string? FieldCodingSystem { get; set; }
    public string? Fieldrefresh { get; set; }
    public string? Fieldorder { get; set; }
    public string? FieldstartPosition { get; set; }
    public string? FieldnumRows { get; set; }
    public string? FieldminDateTime { get; set; }
    public string? FieldmaxDateTime { get; set; }
    public string? FieldsearchString { get; set; }
    public string? FieldminVal { get; set; }
    public string? FieldmaxVal { get; set; }
    public string? FieldreferenceID { get; set; }
}
