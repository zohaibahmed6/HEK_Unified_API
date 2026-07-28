using System.Data;
using System.Xml;
using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Models;
using HekCoreApi.Contracts.NextOfKin;
using HekCoreApi.Contracts.Security;

namespace HekCoreApi.Infrastructure.Legacy.NextOfKin;

/// <summary>
/// Canonical resource (2026-07-23) - see <see cref="NextOfKinCanonical"/> remarks. No KARO method -
/// KARO has no real next-of-kin operation.
/// - ERMS: real `[HSS].[uspGetNextOfKin]`, pipe-delimited columns confirmed live against patient 2459731.
/// - HISO: real, DB-confirmed `PatientNOK` concept group.
/// </summary>
public sealed class CanonicalNextOfKinRepository : ICanonicalNextOfKinRepository
{
    private readonly IErmsDataRepository _ermsRepository;
    private readonly IHisoConceptDictionary _hisoConceptDictionary;
    private readonly IHisoRequestEngine _hisoRequestEngine;
    private readonly IHisoConceptExecutor _hisoExecutor;

    public CanonicalNextOfKinRepository(
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

    public async Task<IReadOnlyList<NextOfKinCanonical>> GetErmsAsync(RoutingContext routing, string? patientId, CancellationToken ct = default)
    {
        var table = await _ermsRepository.GetNextOfKinAsync(routing.PracticeId, routing, patientId, ct);
        var results = new List<NextOfKinCanonical>();

        foreach (DataRow row in table.Rows)
        {
            results.Add(new NextOfKinCanonical(
                Column(row, "ReferenceId"),
                Column(row, "Firstname"),
                Column(row, "Surname"),
                Column(row, "Relationship"),
                Column(row, "Mobile"),
                OriginScope.Erms));
        }

        return results;
    }

    public async Task<IReadOnlyList<NextOfKinCanonical>> GetHisoAsync(HealthLinkSession session, CancellationToken ct = default)
    {
        var xDoc = new XmlDocument();
        xDoc.LoadXml("""
            <dataContainer>
              <section name="nextofkin">
                <group name="nok" conceptName="PatientNOK">
                  <field name="firstName" conceptName="PatientNOK_Firstname" />
                  <field name="surname" conceptName="PatientNOK_Surname" />
                  <field name="relationship" conceptName="PatientNOK_Relationship" />
                  <field name="mobile" conceptName="PatientNOK_Mobile" />
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

        var results = new List<NextOfKinCanonical>();
        var groupNodes = xDoc.SelectNodes("//group[@name='nok']");
        if (groupNodes is null)
        {
            return results;
        }

        foreach (XmlNode group in groupNodes)
        {
            var firstName = group.SelectSingleNode("field[@name='firstName']")?.InnerText;
            var surname = group.SelectSingleNode("field[@name='surname']")?.InnerText;
            if (string.IsNullOrWhiteSpace(firstName) && string.IsNullOrWhiteSpace(surname))
            {
                continue;
            }

            results.Add(new NextOfKinCanonical(
                null,
                firstName,
                surname,
                group.SelectSingleNode("field[@name='relationship']")?.InnerText,
                group.SelectSingleNode("field[@name='mobile']")?.InnerText,
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
