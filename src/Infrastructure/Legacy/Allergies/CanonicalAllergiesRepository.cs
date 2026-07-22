using System.Data;
using System.Xml;
using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Models;
using HekCoreApi.Contracts.Allergies;
using HekCoreApi.Contracts.Security;

namespace HekCoreApi.Infrastructure.Legacy.Allergies;

/// <summary>
/// Seventh canonical resource (2026-07-23) - see <see cref="AllergyCanonical"/> remarks. No KARO
/// method - KARO has no real allergies operation.
/// - ERMS: real `[HSS].[uspGetAllergies]`, pipe-delimited columns (`Comments`/`Date`/`Description`/
///   `ReferenceId`), confirmed live against patient 2459731 - same `Column()` split helper as the
///   other pipe-delimited procedures.
/// - HISO: real, DB-confirmed `Patient_Allergy` concept group.
/// </summary>
public sealed class CanonicalAllergiesRepository : ICanonicalAllergiesRepository
{
    private readonly IErmsDataRepository _ermsRepository;
    private readonly IHisoConceptDictionary _hisoConceptDictionary;
    private readonly IHisoRequestEngine _hisoRequestEngine;
    private readonly IHisoConceptExecutor _hisoExecutor;

    public CanonicalAllergiesRepository(
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

    public async Task<IReadOnlyList<AllergyCanonical>> GetErmsAsync(RoutingContext routing, string? patientId, CancellationToken ct = default)
    {
        var table = await _ermsRepository.GetMedicalAllergiesAsync(routing.PracticeId, patientId, sortOrder: null, DateTime.MinValue, DateTime.MinValue, ct);
        var results = new List<AllergyCanonical>();

        foreach (DataRow row in table.Rows)
        {
            results.Add(new AllergyCanonical(
                Column(row, "ReferenceId"),
                Column(row, "Description"),
                Column(row, "Comments"),
                Column(row, "Date"),
                OriginScope.Erms));
        }

        return results;
    }

    public async Task<IReadOnlyList<AllergyCanonical>> GetHisoAsync(HealthLinkSession session, CancellationToken ct = default)
    {
        var xDoc = new XmlDocument();
        xDoc.LoadXml("""
            <dataContainer>
              <section name="allergies">
                <group name="allergy" conceptName="Patient_Allergy">
                  <field name="description" conceptName="Patient_Allergy_AllergenDescription" />
                  <field name="comments" conceptName="Patient_Allergy_Comments" />
                  <field name="date" conceptName="Patient_Allergy_Date" />
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

        var results = new List<AllergyCanonical>();
        var groupNodes = xDoc.SelectNodes("//group[@name='allergy']");
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

            results.Add(new AllergyCanonical(
                null,
                description,
                group.SelectSingleNode("field[@name='comments']")?.InnerText,
                group.SelectSingleNode("field[@name='date']")?.InnerText,
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
