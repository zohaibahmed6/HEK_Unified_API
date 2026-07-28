using System.Data;
using System.Xml;
using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Models;
using HekCoreApi.Contracts.Security;
using HekCoreApi.Contracts.Smoking;

namespace HekCoreApi.Infrastructure.Legacy.Smoking;

/// <summary>
/// Canonical resource (2026-07-23) - see <see cref="SmokingStatusCanonical"/> remarks. No KARO
/// method - KARO has no real smoking-status operation.
/// - ERMS: real `[HSS].[uspGetSmokingStatus]`, pipe-delimited columns confirmed live against
///   patient 2459731.
/// - HISO: real, DB-confirmed `Patient_Smoking` concept group.
/// </summary>
public sealed class CanonicalSmokingStatusRepository : ICanonicalSmokingStatusRepository
{
    private readonly IErmsDataRepository _ermsRepository;
    private readonly IHisoConceptDictionary _hisoConceptDictionary;
    private readonly IHisoRequestEngine _hisoRequestEngine;
    private readonly IHisoConceptExecutor _hisoExecutor;

    public CanonicalSmokingStatusRepository(
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

    public async Task<IReadOnlyList<SmokingStatusCanonical>> GetErmsAsync(RoutingContext routing, string? patientId, CancellationToken ct = default)
    {
        var table = await _ermsRepository.GetSmokingStatusAsync(routing.PracticeId, routing, patientId, ct);
        var results = new List<SmokingStatusCanonical>();

        foreach (DataRow row in table.Rows)
        {
            results.Add(new SmokingStatusCanonical(
                Column(row, "ReferenceId"),
                Column(row, "ConsumptionDescription"),
                Column(row, "Date"),
                OriginScope.Erms));
        }

        return results;
    }

    public async Task<IReadOnlyList<SmokingStatusCanonical>> GetHisoAsync(HealthLinkSession session, CancellationToken ct = default)
    {
        var xDoc = new XmlDocument();
        xDoc.LoadXml("""
            <dataContainer>
              <section name="smoking">
                <group name="entry" conceptName="Patient_Smoking">
                  <field name="description" conceptName="Patient_Smoking_ConsumptionDescription" />
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

        var results = new List<SmokingStatusCanonical>();
        var groupNodes = xDoc.SelectNodes("//group[@name='entry']");
        if (groupNodes is null)
        {
            return results;
        }

        foreach (XmlNode group in groupNodes)
        {
            var description = group.SelectSingleNode("field[@name='description']")?.InnerText;
            if (string.IsNullOrWhiteSpace(description))
            {
                continue;
            }

            results.Add(new SmokingStatusCanonical(null, description, null, OriginScope.Hiso));
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
