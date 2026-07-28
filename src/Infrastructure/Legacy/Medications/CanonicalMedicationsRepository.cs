using System.Data;
using System.Xml;
using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Models;
using HekCoreApi.Contracts.Medications;
using HekCoreApi.Contracts.Security;

namespace HekCoreApi.Infrastructure.Legacy.Medications;

/// <summary>
/// Sixth canonical resource (2026-07-23) - see <see cref="MedicationCanonical"/> remarks.
/// - KARO: single real call `IKaroDataRepository.GetMedicationsAsync` (`[HSS].[uspGetMedications]`,
///   no `@pIsLongTerm` filter) - already-typed `KaroMedication`, reused as-is.
/// - ERMS: two real calls to `[HSS].[uspGetMedications]` (`@pIsLongTerm=false` then `=true`), same
///   plain-column shape confirmed live against patient 2459731 (`sctid`/`medicineName`/`dosage`/
///   `startDate`/`isLongterm`/`directions`) alongside a parallel pipe-delimited composite-reference
///   set - only the plain columns are read here (no ReferenceId needed for this resource).
/// - HISO: two real concept groups, `Patient_PrescribedMedication` and `Patient_RegularMedication`
///   (both confirmed real in a prior session), queried together in one XML request.
/// </summary>
public sealed class CanonicalMedicationsRepository : ICanonicalMedicationsRepository
{
    private readonly IKaroDataRepository _karoRepository;
    private readonly IErmsDataRepository _ermsRepository;
    private readonly IHisoConceptDictionary _hisoConceptDictionary;
    private readonly IHisoRequestEngine _hisoRequestEngine;
    private readonly IHisoConceptExecutor _hisoExecutor;

    public CanonicalMedicationsRepository(
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

    public async Task<IReadOnlyList<MedicationCanonical>> GetKaroAsync(RoutingContext routing, string? patientId, CancellationToken ct = default)
    {
        var medications = await _karoRepository.GetMedicationsAsync(routing.PracticeId, routing, patientId, ct);
        return medications.Select(m => new MedicationCanonical(
            m.Sctid,
            m.MedicineName ?? string.Empty,
            m.Dosage,
            m.Directions,
            m.StartDate,
            ParseBool(m.IsLongterm),
            OriginScope.Karo)).ToList();
    }

    public async Task<IReadOnlyList<MedicationCanonical>> GetErmsAsync(RoutingContext routing, string? patientId, CancellationToken ct = default)
    {
        var results = new List<MedicationCanonical>();

        foreach (var isLongTerm in new[] { false, true })
        {
            var table = await _ermsRepository.GetMedicationsAsync(routing.PracticeId, routing, patientId, sortOrder: null, DateTime.MinValue, DateTime.MinValue, isLongTerm, ct);
            foreach (DataRow row in table.Rows)
            {
                results.Add(new MedicationCanonical(
                    Column(row, "sctid"),
                    Column(row, "medicineName") ?? string.Empty,
                    Column(row, "dosage"),
                    Column(row, "directions"),
                    Column(row, "startDate"),
                    isLongTerm,
                    OriginScope.Erms));
            }
        }

        return results;
    }

    public async Task<IReadOnlyList<MedicationCanonical>> GetHisoAsync(HealthLinkSession session, CancellationToken ct = default)
    {
        var xDoc = new XmlDocument();
        xDoc.LoadXml("""
            <dataContainer>
              <section name="medications">
                <group name="prescribed" conceptName="Patient_PrescribedMedication">
                  <field name="code" conceptName="Patient_PrescribedMedication_Code" />
                  <field name="name" conceptName="Patient_PrescribedMedication_FullName" />
                  <field name="dosage" conceptName="Patient_PrescribedMedication_DosageQuantity" />
                  <field name="directions" conceptName="Patient_PrescribedMedication_Administrationinstructions" />
                  <field name="startDate" conceptName="Patient_PrescribedMedication_StartedDate" />
                </group>
                <group name="regular" conceptName="Patient_RegularMedication">
                  <field name="code" conceptName="Patient_RegularMedication_Code" />
                  <field name="name" conceptName="Patient_RegularMedication_Name" />
                  <field name="dosage" conceptName="Patient_RegularMedication_DosageQuantity" />
                  <field name="directions" conceptName="Patient_RegularMedication_Administrationinstructions" />
                  <field name="startDate" conceptName="Patient_RegularMedication_StartedDate" />
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

        var results = new List<MedicationCanonical>();
        ExtractGroup(xDoc, "prescribed", isLongTerm: false, results);
        ExtractGroup(xDoc, "regular", isLongTerm: true, results);

        return results;
    }

    private static void ExtractGroup(XmlDocument xDoc, string groupName, bool isLongTerm, List<MedicationCanonical> results)
    {
        var groupNodes = xDoc.SelectNodes($"//group[@name='{groupName}']");
        if (groupNodes is null)
        {
            return;
        }

        foreach (XmlNode group in groupNodes)
        {
            var name = group.SelectSingleNode("field[@name='name']")?.InnerText;
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            results.Add(new MedicationCanonical(
                group.SelectSingleNode("field[@name='code']")?.InnerText,
                name,
                group.SelectSingleNode("field[@name='dosage']")?.InnerText,
                group.SelectSingleNode("field[@name='directions']")?.InnerText,
                group.SelectSingleNode("field[@name='startDate']")?.InnerText,
                isLongTerm,
                OriginScope.Hiso));
        }
    }

    private static string? Column(DataRow row, string columnName) =>
        row.Table.Columns.Contains(columnName) && row[columnName] is not DBNull ? row[columnName]?.ToString() : null;

    private static bool ParseBool(string? value) =>
        value is not null && (string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) || value == "1");
}
