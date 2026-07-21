using System.Globalization;
using System.Xml;
using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Models;
using HekCoreApi.Application.Common.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HekCoreApi.Infrastructure.Legacy.Hiso;

/// <summary>
/// Ported from legacy-reference/Hiso/ConceptMapper/HisoConceptDetail.cs's `HisoRequest` class.
/// Every branch below matches the real source exactly, including its quirks (e.g. the
/// `getNodeType`/`NodeValue` duplicate PNG-check dead branch is collapsed here since it's genuinely
/// unreachable, not a behavior to preserve - both conditions produce the identical result).
/// </summary>
public sealed class HisoRequestEngine : IHisoRequestEngine
{
    private readonly IHisoMimeConverter _mimeConverter;
    private readonly HisoConceptMappingOptions _options;
    private readonly ILogger<HisoRequestEngine> _logger;

    public HisoRequestEngine(IHisoMimeConverter mimeConverter, IOptions<HisoConceptMappingOptions> options, ILogger<HisoRequestEngine> logger)
    {
        _mimeConverter = mimeConverter;
        _options = options.Value;
        _logger = logger;
    }

    public List<HisoRequest> ParseRequest(XmlDocument requestDocument)
    {
        var results = new List<HisoRequest>();
        var fieldNodes = requestDocument.DocumentElement?.SelectNodes("//field") ?? requestDocument.SelectNodes("//field");
        if (fieldNodes is null)
        {
            return results;
        }

        foreach (XmlNode field in fieldNodes)
        {
            var parent = field.ParentNode;
            var grandparent = parent?.ParentNode;
            var isParentGroup = parent is { Name: "group" };
            var sectionName = parent is { Name: "section" }
                ? parent.Attributes?["name"]?.Value ?? string.Empty
                : (grandparent is { Name: "section" } ? grandparent.Attributes?["name"]?.Value ?? string.Empty : string.Empty);

            results.Add(new HisoRequest
            {
                SectionName = sectionName,
                GroupName = isParentGroup ? parent!.Attributes?["name"]?.Value : string.Empty,
                GroupConceptID = isParentGroup ? parent!.Attributes?["conceptID"]?.Value : string.Empty,
                GroupConceptName = isParentGroup ? parent!.Attributes?["conceptName"]?.Value : string.Empty,
                GroupLaboratoryReport = isParentGroup ? parent!.Attributes?["LaboratoryReport"]?.Value : string.Empty,
                GroupMaximumRows = isParentGroup && int.TryParse(parent!.Attributes?["numRows"]?.Value, out var numRows) ? numRows : 50000,
                Grouporder = isParentGroup ? parent!.Attributes?["order"]?.Value : string.Empty,
                GroupreferenceID = isParentGroup ? parent!.Attributes?["referenceID"]?.Value : string.Empty,
                Grouprefresh = isParentGroup ? parent!.Attributes?["refresh"]?.Value : string.Empty,
                GroupStartRowIndex = isParentGroup && int.TryParse(parent!.Attributes?["startPosition"]?.Value, out var startPos) ? startPos : 0,
                GroupminDateTime = isParentGroup ? parent!.Attributes?["minDateTime"]?.Value : string.Empty,
                GroupmaxDateTime = isParentGroup ? parent!.Attributes?["maxDateTime"]?.Value : string.Empty,
                GroupsearchString = isParentGroup ? parent!.Attributes?["searchString"]?.Value : string.Empty,
                GroupminVal = isParentGroup ? parent!.Attributes?["minVal"]?.Value : string.Empty,
                GroupmaxVal = isParentGroup ? parent!.Attributes?["maxVal"]?.Value : string.Empty,

                FieldName = field.Attributes?["name"]?.Value,
                FieldConceptID = field.Attributes?["conceptID"]?.Value,
                FieldConceptName = field.Attributes?["conceptName"]?.Value,
                FieldQualifierName = field.Attributes?["qualifierName"]?.Value,
                FieldQualifierID = field.Attributes?["qualifierID"]?.Value,
                FieldCodingSystem = field.Attributes?["codingSystem"]?.Value,
                Fieldrefresh = field.Attributes?["refresh"]?.Value,
                Fieldorder = field.Attributes?["order"]?.Value,
                FieldstartPosition = field.Attributes?["startPosition"]?.Value,
                FieldnumRows = field.Attributes?["numRows"]?.Value,
                FieldminDateTime = field.Attributes?["minDateTime"]?.Value,
                FieldmaxDateTime = field.Attributes?["maxDateTime"]?.Value,
                FieldsearchString = field.Attributes?["searchString"]?.Value,
                FieldminVal = field.Attributes?["minVal"]?.Value,
                FieldmaxVal = field.Attributes?["maxVal"]?.Value,
                FieldreferenceID = field.Attributes?["referenceID"]?.Value
            });
        }

        return results;
    }

    public (List<HisoRequest> Requests, List<string> ProcedureNames) PrepareConcepts(
        XmlDocument requestDocument,
        IReadOnlyList<HisoConceptDetail> concepts,
        List<HisoRequest> requests,
        string formStatus)
    {
        foreach (var req in requests)
        {
            var concept = concepts.FirstOrDefault(c => !string.IsNullOrEmpty(c.QualifierID) && c.QualifierID == req.FieldQualifierID);
            if (concept is not null && concept.HisoConceptDetailName != req.FieldConceptName)
            {
                if (string.Equals(concept.ConceptName, "measurements", StringComparison.OrdinalIgnoreCase) && req.SectionName == "dynamicConcept")
                {
                    concept = null;
                }
            }

            concept ??= concepts.FirstOrDefault(c =>
                (!string.IsNullOrEmpty(c.HisoID) && !string.IsNullOrEmpty(req.FieldConceptID) && c.HisoID == req.FieldConceptID) ||
                (!string.IsNullOrEmpty(c.HisoConceptDetailName) && !string.IsNullOrEmpty(req.FieldConceptName) && c.HisoConceptDetailName == req.FieldConceptName));

            req.ProcedureName = concept?.ProcedureName ?? string.Empty;

            // "Empty Group Query" expansion - legacy re-parses the whole document after cloning
            // template group children per matching concept, replacing the request list entirely.
            if (concept is null && req.SectionName == "dynamicConcept" && string.IsNullOrEmpty(req.FieldConceptName))
            {
                var groupConcept = concepts.FirstOrDefault(c =>
                    (!string.IsNullOrEmpty(c.HisoGroupID) && !string.IsNullOrEmpty(req.GroupConceptID) && c.HisoGroupID == req.GroupConceptID) ||
                    (!string.IsNullOrEmpty(c.ConceptName) && !string.IsNullOrEmpty(req.GroupConceptName) && c.ConceptName == req.GroupConceptName));

                if (groupConcept is not null)
                {
                    var matchingConcepts = concepts.Where(c =>
                        (!string.IsNullOrEmpty(c.HisoGroupID) && !string.IsNullOrEmpty(req.GroupConceptID) && c.HisoGroupID == req.GroupConceptID) ||
                        (!string.IsNullOrEmpty(c.ConceptName) && !string.IsNullOrEmpty(req.GroupConceptName) && c.ConceptName == req.GroupConceptName)).ToList();

                    var groupNodes = requestDocument.DocumentElement?.GetElementsByTagName("group");
                    if (groupNodes is not null)
                    {
                        var processedNodes = new List<XmlNode>();
                        for (var i = 0; i < groupNodes.Count; i++)
                        {
                            foreach (var item in matchingConcepts)
                            {
                                var newNode = groupNodes[i]!.ChildNodes[0]!.CloneNode(true)!;
                                newNode.Attributes!["conceptID"]!.Value = item.HisoID;
                                newNode.Attributes["conceptName"]!.Value = item.HisoConceptDetailName;
                                processedNodes.Add(newNode);
                            }
                        }

                        for (var i = 0; i < groupNodes.Count; i++)
                        {
                            foreach (var node in processedNodes)
                            {
                                groupNodes[i]!.AppendChild(node);
                            }
                        }

                        requests = ParseRequest(requestDocument);
                        requests.ForEach(r => r.ProcedureName = groupConcept.ProcedureName);
                    }
                }
            }
        }

        if (formStatus != "N")
        {
            requests.RemoveAll(r => !r.ProcedureName.Contains("currentuser", StringComparison.OrdinalIgnoreCase));
        }

        var procedureNames = requests.Select(r => r.ProcedureName).Distinct().Where(name => !string.IsNullOrEmpty(name)).ToList();
        return (requests, procedureNames);
    }

    /// <summary>Legacy: `getNodeType`. Qualifier-ID wins first (with the MeasurementGroup/measurementgroup exceptions), then conceptName, then conceptID.</summary>
    private static string GetNodeType(XmlNode node, IReadOnlyList<HisoConceptDetail> concepts, out string procedureName, out string conceptDataType)
    {
        var nodeName = string.Empty;
        procedureName = string.Empty;
        conceptDataType = string.Empty;

        var qualifierId = node.Attributes?["qualifierID"]?.Value;
        if (!string.IsNullOrEmpty(qualifierId))
        {
            nodeName = qualifierId;
            var concept = concepts.FirstOrDefault(c => c.QualifierID == nodeName);
            if (concept is not null && concept.HisoConceptID is not ("" or "0"))
            {
                var conceptName = node.Attributes?["conceptName"]?.Value?.ToLowerInvariant();
                if (conceptName is not ("patient_measurementgroup_datetaken" or "patient_measurementgroup_value"))
                {
                    nodeName = HisoConceptDetail.MeasurementGroup.Contains(concept.HisoConceptDetailName) ? nodeName : concept.QualifierName;
                    procedureName = concept.ProcedureName;
                    conceptDataType = concept.HisoDataType;
                    return nodeName;
                }

                return nodeName;
            }
        }

        var conceptNameAttr = node.Attributes?["conceptName"]?.Value;
        if (!string.IsNullOrEmpty(conceptNameAttr) && nodeName == string.Empty)
        {
            nodeName = conceptNameAttr;
            var concept = concepts.FirstOrDefault(c => c.HisoConceptDetailName == nodeName);
            if (concept is not null && concept.ConceptName != string.Empty)
            {
                procedureName = concept.ProcedureName;
                conceptDataType = concept.HisoDataType;
            }

            return nodeName;
        }

        var conceptIdAttr = node.Attributes?["conceptID"]?.Value;
        if (!string.IsNullOrEmpty(conceptIdAttr) && nodeName == string.Empty)
        {
            nodeName = conceptIdAttr;
            var concept = concepts.FirstOrDefault(c => c.HisoID == nodeName);
            if (concept is not null && concept.HisoConceptID != string.Empty)
            {
                nodeName = concept.HisoConceptDetailName;
                procedureName = concept.ProcedureName;
                conceptDataType = concept.HisoDataType;
            }
        }

        return nodeName;
    }

    /// <summary>Legacy: `HisoConceptDetail.FormatValue`.</summary>
    private static string FormatValue(string value, string type)
    {
        if (string.IsNullOrEmpty(type.Trim()) || string.IsNullOrEmpty(value))
        {
            return value;
        }

        try
        {
            return type.ToUpperInvariant() switch
            {
                "DATE" => Convert.ToDateTime(value, CultureInfo.InvariantCulture).ToString("yyyy-MM-dd"),
                "DATETIME" => Convert.ToDateTime(value, CultureInfo.InvariantCulture).ToString("yyyy-MM-ddThh:mm:ss"),
                "INTEGER" => Convert.ToInt16(Convert.ToDecimal(value, CultureInfo.InvariantCulture)).ToString(CultureInfo.InvariantCulture),
                "DECIMAL" => Convert.ToDecimal(value, CultureInfo.InvariantCulture).ToString("F1", CultureInfo.InvariantCulture),
                _ => value
            };
        }
        catch (Exception)
        {
            return value;
        }
    }

    /// <summary>Legacy: `NodeValue`. Handles both the type-string-relabel path and the real base64binary byte-conversion path.</summary>
    private async Task<string> NodeValueAsync(IReadOnlyList<ProcedureResult> procedures, string procedureName, string nodeName, string conceptDataType, int index, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(procedureName))
        {
            return string.Empty;
        }

        var result = procedures.SingleOrDefault(p => p.ProcedureName == procedureName);
        if (result?.DsResult?.Tables.Count is not > 0 || result.DsResult.Tables[0].Rows.Count == 0)
        {
            return string.Empty;
        }

        var table = result.DsResult.Tables[0];
        if (index >= table.Rows.Count || !table.Columns.Contains(nodeName))
        {
            return string.Empty;
        }

        var rawValue = table.Rows[index][nodeName];
        if (rawValue is DBNull || string.IsNullOrEmpty(rawValue?.ToString()))
        {
            return string.Empty;
        }

        if (!string.Equals(conceptDataType, "base64binary", StringComparison.OrdinalIgnoreCase))
        {
            var mimeType = rawValue.ToString()!.ToLowerInvariant().Trim();
            return mimeType switch
            {
                _ when mimeType.EndsWith("html") || mimeType.EndsWith("png") || mimeType == "bmp" => FormatValue("application/pdf", conceptDataType),
                _ when mimeType.EndsWith("xml") => FormatValue("text/plain", conceptDataType),
                _ when mimeType.EndsWith("doc") || mimeType.EndsWith("msword") => FormatValue("application/rtf", conceptDataType),
                _ => FormatValue(rawValue.ToString()!, conceptDataType)
            };
        }

        // base64binary: real byte conversion. Legacy indexes column [2] positionally for the file-type
        // hint (brittle, but reproduced exactly rather than "improved" to a by-name lookup).
        var fileType = table.Rows[index][2]?.ToString()?.ToLowerInvariant() ?? string.Empty;
        var bytes = (byte[])rawValue;

        if (fileType.EndsWith("png") || fileType.EndsWith("bmp"))
        {
            var pdfBytes = await _mimeConverter.ConvertImageToPdfAsync(bytes, ct);
            return Convert.ToBase64String(pdfBytes);
        }

        if (fileType.EndsWith("html"))
        {
            var pdfBytes = await _mimeConverter.ConvertHtmlToPdfAsync(bytes, ct);
            return Convert.ToBase64String(pdfBytes);
        }

        // xml and every other file type: raw bytes passed through unconverted, matching legacy exactly.
        return Convert.ToBase64String(bytes);
    }

    private async Task<(string Value, bool IsMapped)> NodeValueByNodeAsync(IReadOnlyList<ProcedureResult> procedures, XmlNode node, IReadOnlyList<HisoConceptDetail> concepts, int index, CancellationToken ct)
    {
        var nodeName = GetNodeType(node, concepts, out var procedureName, out var conceptDataType);
        if (string.IsNullOrEmpty(procedureName))
        {
            return (string.Empty, false);
        }

        var value = await NodeValueAsync(procedures, procedureName, nodeName, conceptDataType, index, ct);
        return (value, true);
    }

    public async Task FillXmlDetailsAsync(
        IReadOnlyList<ProcedureResult> procedures,
        XmlDocument requestDocument,
        IReadOnlyList<HisoConceptDetail> concepts,
        List<HisoRequest> requests,
        CancellationToken ct = default)
    {
        var sectionNodes = requestDocument.DocumentElement?.GetElementsByTagName("section");
        if (sectionNodes is null)
        {
            return;
        }

        var processedNodes = new List<XmlNode>();

        for (var i = 0; i < sectionNodes.Count; i++)
        {
            var section = sectionNodes[i]!;
            var groupNodes = section.ChildNodes;
            var nodeProcess = section.CloneNode(true)!;
            nodeProcess.InnerXml = string.Empty;

            foreach (XmlNode child in groupNodes)
            {
                if (string.Equals(child.Name, "group", StringComparison.OrdinalIgnoreCase))
                {
                    await FillGroupAsync(procedures, requests, concepts, child, nodeProcess, ct);
                }
                else
                {
                    var (value, isMapped) = await NodeValueByNodeAsync(procedures, child, concepts, 0, ct);
                    child.InnerText = isMapped ? value : child.InnerText;

                    if (child.ParentNode?.Attributes?["name"]?.Value == "measurements")
                    {
                        await AppendDateTakenAttributeAsync(procedures, concepts, child, requestDocument, ct);
                    }

                    nodeProcess.AppendChild(child.CloneNode(true));
                }
            }

            processedNodes.Add(nodeProcess);
        }

        for (var i = 0; i < sectionNodes.Count; i++)
        {
            sectionNodes[i]!.InnerXml = i < processedNodes.Count ? processedNodes[i].InnerXml : string.Empty;
        }
    }

    private async Task FillGroupAsync(IReadOnlyList<ProcedureResult> procedures, List<HisoRequest> requests, IReadOnlyList<HisoConceptDetail> concepts, XmlNode group, XmlNode nodeProcess, CancellationToken ct)
    {
        try
        {
            var conceptName = group.Attributes?["conceptName"]?.Value;
            var groupRequest = requests.FirstOrDefault(r => r.GroupConceptName == conceptName && !string.IsNullOrEmpty(r.ProcedureName));

            if (groupRequest is null || string.IsNullOrEmpty(groupRequest.ProcedureName))
            {
                nodeProcess.AppendChild(group.CloneNode(true));
                return;
            }

            var result = procedures.FirstOrDefault(p => p.ProcedureName == groupRequest.ProcedureName);
            var table = result?.DsResult?.Tables.Count > 0 ? result.DsResult.Tables[0] : null;

            if (table is null || table.Rows.Count == 0)
            {
                nodeProcess.AppendChild(group.CloneNode(true));
                _logger.LogInformation("[FillXMLDetails] Not found any Store Procedure against: {ConceptName}", conceptName);
                return;
            }

            for (var rowIndex = 0; rowIndex < table.Rows.Count; rowIndex++)
            {
                var clone = group.CloneNode(true);
                foreach (XmlNode field in clone.ChildNodes)
                {
                    var (value, isMapped) = await NodeValueByNodeAsync(procedures, field, concepts, rowIndex, ct);
                    field.InnerText = isMapped ? value : field.InnerText;
                }

                if (clone.Attributes?["referenceID"] is { } referenceIdAttr)
                {
                    referenceIdAttr.Value = string.Empty;
                    foreach (var idColumn in new[] { "Patient_Attachment_ID", "Patient_OutgoingLetter_ID", "Patient_IncomingLetter_ID", "Patient_LaboratoryReport_ID", "Patient_RadiologyReport_ID" })
                    {
                        if (table.Columns.Contains(idColumn))
                        {
                            referenceIdAttr.Value = table.Rows[rowIndex][idColumn]?.ToString() ?? string.Empty;
                            break;
                        }
                    }
                }

                nodeProcess.AppendChild(clone);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[FillXMLDetails] Failed processing group: {GroupXml}", group.InnerXml);
        }
    }

    private async Task AppendDateTakenAttributeAsync(IReadOnlyList<ProcedureResult> procedures, IReadOnlyList<HisoConceptDetail> concepts, XmlNode field, XmlDocument doc, CancellationToken ct)
    {
        var nodeName = GetNodeType(field, concepts, out var procedureName, out _);
        if (!_options.QualifierList.Contains(nodeName))
        {
            return;
        }

        var dateTakenColumn = nodeName + "_dateTaken";
        var attr = doc.CreateAttribute("dateTaken");
        attr.Value = string.Empty;

        if (!string.IsNullOrEmpty(procedureName))
        {
            var result = procedures.SingleOrDefault(p => p.ProcedureName == procedureName);
            if (result?.DsResult?.Tables.Count > 0 && result.DsResult.Tables[0].Rows.Count > 0)
            {
                var table = result.DsResult.Tables[0];
                if (table.Columns.Contains(dateTakenColumn) && table.Rows[0][dateTakenColumn] is { } value && !string.IsNullOrEmpty(value.ToString()))
                {
                    attr.Value = FormatValue(value.ToString()!, "DATETIME");
                }
            }
        }

        field.Attributes!.Append(attr);
        await Task.CompletedTask;
    }
}
