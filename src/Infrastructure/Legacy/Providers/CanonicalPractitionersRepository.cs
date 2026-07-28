using System.Data;
using System.Xml;
using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Models;
using HekCoreApi.Contracts.Providers;
using HekCoreApi.Contracts.Security;

namespace HekCoreApi.Infrastructure.Legacy.Providers;

/// <summary>
/// Ninth canonical resource (2026-07-23) - see <see cref="PractitionerCanonical"/> remarks. No KARO
/// method - KARO has no real registered-practitioners operation.
/// - ERMS: real `[HSS].[uspGetRegisteredPractitioners]`, pipe-delimited columns confirmed live
///   against patient 2459731 - same `Column()` split helper as the other pipe-delimited procedures.
/// - HISO: real, DB-confirmed `RegisteredPractitioner` concept group.
/// </summary>
public sealed class CanonicalPractitionersRepository : ICanonicalPractitionersRepository
{
    private readonly IErmsDataRepository _ermsRepository;
    private readonly IHisoConceptDictionary _hisoConceptDictionary;
    private readonly IHisoRequestEngine _hisoRequestEngine;
    private readonly IHisoConceptExecutor _hisoExecutor;

    public CanonicalPractitionersRepository(
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

    public async Task<IReadOnlyList<PractitionerCanonical>> GetErmsAsync(RoutingContext routing, string? patientId, CancellationToken ct = default)
    {
        var table = await _ermsRepository.GetRegisteredPractitionersAsync(routing.PracticeId, routing, patientId, locationId: null, ct);
        var results = new List<PractitionerCanonical>();

        foreach (DataRow row in table.Rows)
        {
            results.Add(new PractitionerCanonical(
                Column(row, "ReferenceId"),
                Column(row, "FullName"),
                Column(row, "RegisteringBody"),
                Column(row, "RegistrationNumber"),
                Column(row, "Email"),
                OriginScope.Erms));
        }

        return results;
    }

    public async Task<IReadOnlyList<PractitionerCanonical>> GetHisoAsync(HealthLinkSession session, CancellationToken ct = default)
    {
        var xDoc = new XmlDocument();
        xDoc.LoadXml("""
            <dataContainer>
              <section name="practitioners">
                <group name="practitioner" conceptName="RegisteredPractitioner">
                  <field name="fullName" conceptName="RegisteredPractitioner_FullName" />
                  <field name="registeringBody" conceptName="RegisteredPractitioner_RegisteringBody" />
                  <field name="registrationNumber" conceptName="RegisteredPractitioner_RegistrationNumber" />
                  <field name="email" conceptName="RegisteredPractitioner_Email" />
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

        var results = new List<PractitionerCanonical>();
        var groupNodes = xDoc.SelectNodes("//group[@name='practitioner']");
        if (groupNodes is null)
        {
            return results;
        }

        foreach (XmlNode group in groupNodes)
        {
            var fullName = group.SelectSingleNode("field[@name='fullName']")?.InnerText;
            if (string.IsNullOrWhiteSpace(fullName))
            {
                continue;
            }

            results.Add(new PractitionerCanonical(
                null,
                fullName,
                group.SelectSingleNode("field[@name='registeringBody']")?.InnerText,
                group.SelectSingleNode("field[@name='registrationNumber']")?.InnerText,
                group.SelectSingleNode("field[@name='email']")?.InnerText,
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
