using System.Data;
using System.Xml;
using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Models;
using HekCoreApi.Contracts.Radiology;
using HekCoreApi.Contracts.Security;

namespace HekCoreApi.Infrastructure.Legacy.Radiology;

/// <summary>
/// Canonical resource (2026-07-23) - see <see cref="RadiologyReportCanonical"/> remarks. No KARO
/// method - KARO has no real radiology operation.
/// - ERMS: real `[HSS].[uspGetRads]`, same pipe-delimited report-level column shape as `uspGetOtherDocs`
///   (confirmed live - zero rows for patient 2459731, a real absence not a bug).
/// - HISO: real, DB-confirmed `Patient_RadiologyReport` concept group.
/// </summary>
public sealed class CanonicalRadiologyRepository : ICanonicalRadiologyRepository
{
    private readonly IErmsDataRepository _ermsRepository;
    private readonly IHisoConceptDictionary _hisoConceptDictionary;
    private readonly IHisoRequestEngine _hisoRequestEngine;
    private readonly IHisoConceptExecutor _hisoExecutor;

    public CanonicalRadiologyRepository(
        IErmsDataRepository ermsRepository,
        IHisoConceptDictionary hisoConceptDictionary,
        IHisoRequestEngine hisoRequestEngine,
        IHisoConceptExecutor hisoExecutor)
    {
        _ermsRepository = ermsRepository;
        _hisoConceptDictionary = hisoConceptDictionary;
        _hisoRequestEngine = hisoRequestEngine;
        _hisoExecutor = hisoExecutor;
    }

    public async Task<IReadOnlyList<RadiologyReportCanonical>> GetErmsAsync(RoutingContext routing, string? patientId, CancellationToken ct = default)
    {
        var table = await _ermsRepository.GetRadsAsync(routing.PracticeId, patientId, sortOrder: null, DateTime.MinValue, DateTime.MinValue, ct);
        var results = new List<RadiologyReportCanonical>();

        foreach (DataRow row in table.Rows)
        {
            results.Add(new RadiologyReportCanonical(
                Column(row, "ReferenceId"),
                Column(row, "Name"),
                Column(row, "Subject"),
                Column(row, "DataType"),
                Column(row, "DateReceived"),
                Column(row, "Comments"),
                OriginScope.Erms));
        }

        return results;
    }

    public async Task<IReadOnlyList<RadiologyReportCanonical>> GetHisoAsync(HealthLinkSession session, CancellationToken ct = default)
    {
        var xDoc = new XmlDocument();
        xDoc.LoadXml("""
            <dataContainer>
              <section name="radiology">
                <group name="report" conceptName="Patient_RadiologyReport">
                  <field name="name" conceptName="Patient_RadiologyReport_Name" />
                  <field name="subject" conceptName="Patient_RadiologyReport_Subject" />
                  <field name="dataType" conceptName="Patient_RadiologyReport_DataType" />
                  <field name="dateReceived" conceptName="Patient_RadiologyReport_DateReceived" />
                  <field name="comments" conceptName="Patient_RadiologyReport_Comments" />
                  <field name="id" conceptName="Patient_RadiologyReport_ID" />
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

        var results = new List<RadiologyReportCanonical>();
        var groupNodes = xDoc.SelectNodes("//group[@name='report']");
        if (groupNodes is null)
        {
            return results;
        }

        foreach (XmlNode group in groupNodes)
        {
            var name = group.SelectSingleNode("field[@name='name']")?.InnerText;
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            results.Add(new RadiologyReportCanonical(
                group.SelectSingleNode("field[@name='id']")?.InnerText,
                name,
                group.SelectSingleNode("field[@name='subject']")?.InnerText,
                group.SelectSingleNode("field[@name='dataType']")?.InnerText,
                group.SelectSingleNode("field[@name='dateReceived']")?.InnerText,
                group.SelectSingleNode("field[@name='comments']")?.InnerText,
                OriginScope.Hiso));
        }

        return results;
    }

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
