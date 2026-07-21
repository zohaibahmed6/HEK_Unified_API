using System.Xml;
using HekCoreApi.Application.Common.Models;

namespace HekCoreApi.Application.Common.Interfaces;

/// <summary>
/// HISO's real per-field concept-resolution engine, ported from
/// legacy-reference/Hiso/ConceptMapper/HisoConceptDetail.cs's `HisoRequest` class
/// (`GetHisoRequest`/`getNodeType`/`NodeValueByNode`/`NodeValue`/`FillXMLDetails`) - the actual
/// `getData` "dynamic mode" pipeline: parse the request XML into per-field criteria, resolve each
/// field's backing procedure via qualifier-ID/conceptName/conceptID priority matching, then fill the
/// XML template with real result values (including group-concept cloning and MIME-type routing).
/// </summary>
public interface IHisoRequestEngine
{
    /// <summary>Legacy: `HisoRequest.GetHisoRequest(XmlDocument)` - parses every `&lt;field&gt;` node's ancestry/attributes into a flat request list.</summary>
    List<HisoRequest> ParseRequest(XmlDocument requestDocument);

    /// <summary>
    /// Legacy: `Mapper.PrepareConceptsFromRequest` - resolves each parsed field's backing procedure
    /// name (qualifier-ID match with the measurements/dynamicConcept exception, else conceptName,
    /// else the "empty group query" expansion which re-parses <paramref name="requestDocument"/>
    /// after cloning template group children per matching concept). Returns the updated request list
    /// (may be a wholly new list, matching legacy's own `ref` reassignment) and the distinct,
    /// non-empty procedure names to execute - filtered to "currentuser"-only procedures when
    /// <paramref name="formStatus"/> isn't `"N"` (resumed/parked forms, HISO-BR-08).
    /// </summary>
    (List<HisoRequest> Requests, List<string> ProcedureNames) PrepareConcepts(
        XmlDocument requestDocument,
        IReadOnlyList<HisoConceptDetail> concepts,
        List<HisoRequest> requests,
        string formStatus);

    /// <summary>Legacy: `HisoRequest.FillXMLDetails` - mutates <paramref name="requestDocument"/> in place with real result values.</summary>
    Task FillXmlDetailsAsync(
        IReadOnlyList<ProcedureResult> procedures,
        XmlDocument requestDocument,
        IReadOnlyList<HisoConceptDetail> concepts,
        List<HisoRequest> requests,
        CancellationToken ct = default);
}
