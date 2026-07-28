using System.Data;
using System.Xml;
using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Models;
using HekCoreApi.Contracts.Conditions;
using HekCoreApi.Contracts.Security;

namespace HekCoreApi.Infrastructure.Legacy.Conditions;

/// <summary>
/// Second canonical resource (2026-07-22). Reuses the exact real, already-verified per-source
/// repositories - no new stored-procedure guessing, same discipline as the demographics fix.
/// - KARO/ERMS: identical real procedure `[HSS].[uspGetConditions]`, confirmed in both
///   `IKaroDataRepository.GetConditionsAsync`/`IErmsDataRepository.GetConditionsAsync` (same shared
///   DAL KARO/ERMS already use elsewhere) - so ERMS's raw `DataTable` has the same real column names
///   as KARO's typed `KaroDiagnosis` model. Mapped directly here rather than guessed twice.
/// - HISO: the real, DB-confirmed `Patient_Problem` concept group (`Hiso.uspGetPatient_Problem`,
///   confirmed live against patient 2459731 - "Chills with fever (situation)") - reuses the same real
///   concept-driven engine `DemographicsRepository.GetHisoAsync` uses, this time via the group/list
///   path (`FillGroupAsync`) since conditions are a list, not flat fields.
/// </summary>
public sealed class CanonicalConditionsRepository : ICanonicalConditionsRepository
{
    private readonly IKaroDataRepository _karoRepository;
    private readonly IErmsDataRepository _ermsRepository;
    private readonly IColDataRepository _colRepository;
    private readonly IHisoConceptDictionary _hisoConceptDictionary;
    private readonly IHisoRequestEngine _hisoRequestEngine;
    private readonly IHisoConceptExecutor _hisoExecutor;

    public CanonicalConditionsRepository(
        IKaroDataRepository karoRepository,
        IErmsDataRepository ermsRepository,
        IColDataRepository colRepository,
        IHisoConceptDictionary hisoConceptDictionary,
        IHisoRequestEngine hisoRequestEngine,
        IHisoConceptExecutor hisoExecutor)
    {
        _karoRepository = karoRepository;
        _ermsRepository = ermsRepository;
        _colRepository = colRepository;
        _hisoConceptDictionary = hisoConceptDictionary;
        _hisoRequestEngine = hisoRequestEngine;
        _hisoExecutor = hisoExecutor;
    }

    /// <summary>
    /// COL: `[OnlineClaim].[uspGetConditions]` - a different real procedure than KARO/ERMS's
    /// `[HSS].[uspGetConditions]`, confirmed live (2026-07-23, patient 2459731) to return plain
    /// columns (`diagnosisnote`/`diagnosiscode`/`diagnosiswhenclassdatetime`), no pipe-delimiting.
    /// </summary>
    public async Task<IReadOnlyList<ConditionCanonical>> GetColAsync(RoutingContext routing, string? patientId, CancellationToken ct = default)
    {
        var table = await _colRepository.GetDiagnosisDataAsync(routing.PracticeId, routing, patientId, sortOrder: null, DateTime.MinValue, DateTime.MinValue, ct);
        var results = new List<ConditionCanonical>();

        foreach (DataRow row in table.Rows)
        {
            results.Add(new ConditionCanonical(
                Column(row, "diagnosiscode"),
                Column(row, "diagnosisnote") ?? string.Empty,
                null,
                Column(row, "diagnosiswhenclassdatetime"),
                false,
                OriginScope.Col));
        }

        return results;
    }

    public async Task<IReadOnlyList<ConditionCanonical>> GetKaroAsync(RoutingContext routing, string? patientId, CancellationToken ct = default)
    {
        var diagnoses = await _karoRepository.GetConditionsAsync(routing.PracticeId, routing, patientId, ct);
        return diagnoses.Select(d => new ConditionCanonical(
            d.ConceptId,
            d.Name ?? string.Empty,
            d.Summary,
            d.DiagnosisDate,
            ParseBool(d.IsLongTerm),
            OriginScope.Karo)).ToList();
    }

    public async Task<IReadOnlyList<ConditionCanonical>> GetErmsAsync(RoutingContext routing, string? patientId, CancellationToken ct = default)
    {
        var table = await _ermsRepository.GetConditionsAsync(routing.PracticeId, routing, patientId, sortOrder: null, DateTime.MinValue, DateTime.MinValue, ct);
        var results = new List<ConditionCanonical>();

        foreach (DataRow row in table.Rows)
        {
            results.Add(new ConditionCanonical(
                Column(row, "ConceptId"),
                Column(row, "Name") ?? string.Empty,
                Column(row, "Summary"),
                Column(row, "DiagnosisDate"),
                ParseBool(Column(row, "IsLongTerm")),
                OriginScope.Erms));
        }

        return results;
    }

    public async Task<IReadOnlyList<ConditionCanonical>> GetHisoAsync(HealthLinkSession session, CancellationToken ct = default)
    {
        var xDoc = new XmlDocument();
        xDoc.LoadXml("""
            <dataContainer>
              <section name="conditions">
                <group name="problem" conceptName="Patient_Problem">
                  <field name="name" conceptName="Patient_Problem_Description" />
                  <field name="code" conceptName="Patient_Problem_Code" />
                  <field name="dateRecorded" conceptName="Patient_Problem_DateRecorded" />
                  <field name="isLongTerm" conceptName="Patient_Problem_IsChronic" />
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

        var results = new List<ConditionCanonical>();
        var groupNodes = xDoc.SelectNodes("//group[@name='problem']");
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

            results.Add(new ConditionCanonical(
                group.SelectSingleNode("field[@name='code']")?.InnerText,
                name,
                null,
                group.SelectSingleNode("field[@name='dateRecorded']")?.InnerText,
                ParseBool(group.SelectSingleNode("field[@name='isLongTerm']")?.InnerText),
                OriginScope.Hiso));
        }

        return results;
    }

    private static string? Column(DataRow row, string columnName) =>
        row.Table.Columns.Contains(columnName) && row[columnName] is not DBNull ? row[columnName]?.ToString() : null;

    private static bool ParseBool(string? value) =>
        value is not null && (string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) || value == "1");
}
