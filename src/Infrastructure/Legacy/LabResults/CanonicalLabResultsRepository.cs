using System.Data;
using System.Xml;
using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Models;
using HekCoreApi.Contracts.LabResults;
using HekCoreApi.Contracts.Security;

namespace HekCoreApi.Infrastructure.Legacy.LabResults;

/// <summary>
/// Fifth canonical resource (2026-07-23) - see <see cref="LabResultCanonical"/> remarks.
/// - KARO/ERMS: identical real procedure `[HSS].[uspGetLabResults]`. KARO uses the already-typed
///   `IKaroDataRepository.GetLabResultsAsync` (`KaroLabResult`: MessageSubject/Title/Code/EffectiveDateTime/Value
///   only - no ReferenceId/Unit/ReferenceRanges exposed in that model, reused as-is, no new guessing).
///   ERMS reads the raw `DataTable` directly by the real column names (`MessageSubject`, `Title`, `Code`,
///   `EffectiveDateTime`, `Value`, `ReferenceId` (pipe-delimited, same quirk as Documents/ClinicalNotes),
///   `Unit`, `ReferenceRanges`), confirmed live against real patient 2459731.
/// - HISO: real, DB-confirmed `Patient_LaboratoryReport` concept group (`Hiso.uspGetPatient_LaboratoryReport`) -
///   report/document level only, no Value/Unit/ReferenceRange concept fields exist for it.
/// </summary>
public sealed class CanonicalLabResultsRepository : ICanonicalLabResultsRepository
{
    private readonly IKaroDataRepository _karoRepository;
    private readonly IErmsDataRepository _ermsRepository;
    private readonly IHisoConceptDictionary _hisoConceptDictionary;
    private readonly IHisoRequestEngine _hisoRequestEngine;
    private readonly IHisoConceptExecutor _hisoExecutor;

    public CanonicalLabResultsRepository(
        IKaroDataRepository karoRepository,
        IErmsDataRepository ermsRepository,
        IHisoConceptDictionary hisoConceptDictionary,
        IHisoRequestEngine hisoRequestEngine,
        IHisoConceptExecutor hisoExecutor)
    {
        _karoRepository = karoRepository;
        _ermsRepository = ermsRepository;
        _hisoConceptDictionary = hisoConceptDictionary;
        _hisoRequestEngine = hisoRequestEngine;
        _hisoExecutor = hisoExecutor;
    }

    public async Task<IReadOnlyList<LabResultCanonical>> GetKaroAsync(RoutingContext routing, string? patientId, CancellationToken ct = default)
    {
        var results = await _karoRepository.GetLabResultsAsync(routing.PracticeId, patientId, ct);
        return results.Select(r => new LabResultCanonical(
            null,
            r.Title,
            r.MessageSubject,
            r.Value,
            null,
            null,
            null,
            r.EffectiveDateTime,
            OriginScope.Karo)).ToList();
    }

    public async Task<IReadOnlyList<LabResultCanonical>> GetErmsAsync(RoutingContext routing, string? patientId, CancellationToken ct = default)
    {
        var table = await _ermsRepository.GetLabResultsAsync(routing.PracticeId, patientId, referenceId: null, ct);
        var results = new List<LabResultCanonical>();

        foreach (DataRow row in table.Rows)
        {
            results.Add(new LabResultCanonical(
                Column(row, "ReferenceId"),
                Column(row, "Title"),
                Column(row, "MessageSubject"),
                Column(row, "Value"),
                Column(row, "Unit"),
                Column(row, "ReferenceRanges"),
                null,
                Column(row, "EffectiveDateTime"),
                OriginScope.Erms));
        }

        return results;
    }

    public async Task<IReadOnlyList<LabResultCanonical>> GetHisoAsync(HealthLinkSession session, CancellationToken ct = default)
    {
        var xDoc = new XmlDocument();
        xDoc.LoadXml("""
            <dataContainer>
              <section name="labresults">
                <group name="report" conceptName="Patient_LaboratoryReport">
                  <field name="testName" conceptName="Patient_LaboratoryReport_TestDescription" />
                  <field name="subject" conceptName="Patient_LaboratoryReport_Subject" />
                  <field name="comments" conceptName="Patient_LaboratoryReport_Comments" />
                  <field name="date" conceptName="Patient_LaboratoryReport_DateReceived" />
                  <field name="id" conceptName="Patient_LaboratoryReport_ID" />
                </group>
              </section>
            </dataContainer>
            """);

        var concepts = await _hisoConceptDictionary.GetConceptsAsync(session.PracticeId, ct);
        var parsedRequests = _hisoRequestEngine.ParseRequest(xDoc);
        var (preparedRequests, procedureNames) = _hisoRequestEngine.PrepareConcepts(xDoc, concepts, parsedRequests, "N");

        var procedureResults = new List<ProcedureResult>();
        foreach (var procedureName in procedureNames)
        {
            var dataSet = await _hisoExecutor.ExecuteAsync(procedureName, session, preparedRequests, ct);
            procedureResults.Add(new ProcedureResult { ProcedureName = procedureName, DsResult = dataSet });
        }

        await _hisoRequestEngine.FillXmlDetailsAsync(procedureResults, xDoc, concepts, preparedRequests, ct);

        var results = new List<LabResultCanonical>();
        var groupNodes = xDoc.SelectNodes("//group[@name='report']");
        if (groupNodes is null)
        {
            return results;
        }

        foreach (XmlNode group in groupNodes)
        {
            var testName = group.SelectSingleNode("field[@name='testName']")?.InnerText;
            if (string.IsNullOrWhiteSpace(testName))
            {
                continue;
            }

            results.Add(new LabResultCanonical(
                group.SelectSingleNode("field[@name='id']")?.InnerText,
                testName,
                group.SelectSingleNode("field[@name='subject']")?.InnerText,
                null,
                null,
                null,
                group.SelectSingleNode("field[@name='comments']")?.InnerText,
                group.SelectSingleNode("field[@name='date']")?.InnerText,
                OriginScope.Hiso));
        }

        return results;
    }

    /// <summary>Same real pipe-delimited `"{conceptId}|&amp;|{text}"` quirk as Documents/ClinicalNotes.</summary>
    private static string? Column(DataRow row, string columnName)
    {
        if (!row.Table.Columns.Contains(columnName) || row[columnName] is DBNull)
        {
            return null;
        }

        var raw = row[columnName]?.ToString();
        if (string.IsNullOrEmpty(raw))
        {
            return null;
        }

        var parts = raw.Split(new[] { "|&|" }, StringSplitOptions.None);
        return parts.Length > 1 ? parts[1] : raw;
    }
}
