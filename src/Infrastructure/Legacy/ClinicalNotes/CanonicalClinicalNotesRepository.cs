using System.Data;
using System.Xml;
using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Models;
using HekCoreApi.Contracts.ClinicalNotes;
using HekCoreApi.Contracts.Security;

namespace HekCoreApi.Infrastructure.Legacy.ClinicalNotes;

/// <summary>
/// Fourth canonical resource (2026-07-23) - see <see cref="ClinicalNoteCanonical"/> remarks.
/// - KARO/ERMS: identical real procedure `[HSS].[uspGetConsultNotes]`. KARO uses the already-typed
///   `IKaroDataRepository.GetConsultNotesAsync` (plain columns, no ReferenceId in that model). ERMS
///   reads the raw `DataTable` directly by the same plain column names, confirmed live against real
///   patient 2459731 to be the real values (not the parallel pipe-delimited composite-reference set).
/// - HISO: real, DB-confirmed `Patient_Consult` concept group (`Hiso.uspGetPatient_Consult`), via the
///   same group/list XML path used for Conditions/Documents.
/// </summary>
public sealed class CanonicalClinicalNotesRepository : ICanonicalClinicalNotesRepository
{
    private readonly IKaroDataRepository _karoRepository;
    private readonly IErmsDataRepository _ermsRepository;
    private readonly IHisoConceptDictionary _hisoConceptDictionary;
    private readonly IHisoRequestEngine _hisoRequestEngine;
    private readonly IHisoConceptExecutor _hisoExecutor;

    public CanonicalClinicalNotesRepository(
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

    public async Task<IReadOnlyList<ClinicalNoteCanonical>> GetKaroAsync(RoutingContext routing, string? patientId, CancellationToken ct = default)
    {
        var notes = await _karoRepository.GetConsultNotesAsync(routing.PracticeId, patientId, ct);
        return notes.Select(n => new ClinicalNoteCanonical(
            null,
            n.SubjectiveNotes,
            n.ObjectiveNotes,
            n.Assessment,
            n.Plans,
            n.AppointmentAdvice,
            n.Date,
            OriginScope.Karo)).ToList();
    }

    public async Task<IReadOnlyList<ClinicalNoteCanonical>> GetErmsAsync(RoutingContext routing, string? patientId, CancellationToken ct = default)
    {
        var table = await _ermsRepository.GetConsultNotesAsync(routing.PracticeId, patientId, sortOrder: null, DateTime.MinValue, DateTime.MinValue, ct);
        var results = new List<ClinicalNoteCanonical>();

        foreach (DataRow row in table.Rows)
        {
            results.Add(new ClinicalNoteCanonical(
                Column(row, "ReferenceId"),
                Column(row, "subjectiveNotes"),
                Column(row, "objectiveNotes"),
                Column(row, "assessment"),
                Column(row, "plans"),
                Column(row, "appointmentAdvice"),
                Column(row, "date"),
                OriginScope.Erms));
        }

        return results;
    }

    public async Task<IReadOnlyList<ClinicalNoteCanonical>> GetHisoAsync(HealthLinkSession session, CancellationToken ct = default)
    {
        var xDoc = new XmlDocument();
        xDoc.LoadXml("""
            <dataContainer>
              <section name="clinicalnotes">
                <group name="consult" conceptName="Patient_Consult">
                  <field name="subjective" conceptName="Patient_Consult_History" />
                  <field name="objective" conceptName="Patient_Consult_Diagnosis" />
                  <field name="assessment" conceptName="Patient_Consult_Exam" />
                  <field name="plans" conceptName="Patient_Consult_Actions" />
                  <field name="date" conceptName="Patient_Consult_Date" />
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

        var results = new List<ClinicalNoteCanonical>();
        var groupNodes = xDoc.SelectNodes("//group[@name='consult']");
        if (groupNodes is null)
        {
            return results;
        }

        foreach (XmlNode group in groupNodes)
        {
            var date = group.SelectSingleNode("field[@name='date']")?.InnerText;
            var subjective = group.SelectSingleNode("field[@name='subjective']")?.InnerText;
            if (string.IsNullOrWhiteSpace(date) && string.IsNullOrWhiteSpace(subjective))
            {
                continue;
            }

            results.Add(new ClinicalNoteCanonical(
                null,
                subjective,
                group.SelectSingleNode("field[@name='objective']")?.InnerText,
                group.SelectSingleNode("field[@name='assessment']")?.InnerText,
                group.SelectSingleNode("field[@name='plans']")?.InnerText,
                null,
                date,
                OriginScope.Hiso));
        }

        return results;
    }

    /// <summary>
    /// `[HSS].[uspGetConsultNotes]` carries a real pipe-delimited `"{conceptId}|&amp;|{text}"` value
    /// specifically in `ReferenceId` (confirmed live, 2026-07-23); the plain note columns have no pipe
    /// so splitting is a no-op for them - same shared helper as Documents' fix, applied uniformly.
    /// </summary>
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
