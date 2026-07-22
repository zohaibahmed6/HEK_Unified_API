using System.Xml;
using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Models;
using HekCoreApi.Contracts.Providers;
using HekCoreApi.Contracts.Security;

namespace HekCoreApi.Infrastructure.Legacy.Providers;

/// <summary>
/// Eighth canonical resource (2026-07-23) - see <see cref="CurrentProviderCanonical"/> remarks.
/// - KARO: `IKaroDataRepository.GetProviderAsync` (`[HSS].[uspGetProvider]`, first row only, already
///   typed). Returned zero rows for patient 2459731 in live testing - a real, honestly-flagged gap
///   for this patient, not a code bug (confirmed no exception, clean empty result).
/// - ERMS: same real procedure via raw `DataTable`, plain `given`/`family`/`email`/`dayPhone` columns.
/// - HISO: real, DB-confirmed flat `CurrentUser` concept (`Hiso.uspGetCurrentUser`), same flat-field
///   pattern as `DemographicsRepository.GetHisoAsync`.
/// </summary>
public sealed class CanonicalCurrentProviderRepository : ICanonicalCurrentProviderRepository
{
    private readonly IKaroDataRepository _karoRepository;
    private readonly IErmsDataRepository _ermsRepository;
    private readonly IHisoConceptDictionary _hisoConceptDictionary;
    private readonly IHisoRequestEngine _hisoRequestEngine;
    private readonly IHisoConceptExecutor _hisoExecutor;

    public CanonicalCurrentProviderRepository(
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

    public async Task<CurrentProviderCanonical?> GetKaroAsync(RoutingContext routing, string? patientId, CancellationToken ct = default)
    {
        var providers = await _karoRepository.GetProviderAsync(routing.PracticeId, patientId, userId: null, ct);
        var provider = providers.FirstOrDefault();
        return provider is null
            ? null
            : new CurrentProviderCanonical(provider.Given, provider.Family, provider.Email, provider.DayPhone, OriginScope.Karo);
    }

    public async Task<CurrentProviderCanonical?> GetErmsAsync(RoutingContext routing, string? patientId, CancellationToken ct = default)
    {
        var table = await _ermsRepository.GetProviderAsync(routing.PracticeId, patientId, userId: null, locationId: null, encounterId: null, ct);
        if (table.Rows.Count == 0)
        {
            return null;
        }

        var row = table.Rows[0];
        return new CurrentProviderCanonical(
            Column(row, "given"),
            Column(row, "family"),
            Column(row, "email"),
            Column(row, "dayPhone"),
            OriginScope.Erms);
    }

    public async Task<CurrentProviderCanonical?> GetHisoAsync(HealthLinkSession session, CancellationToken ct = default)
    {
        var xDoc = new XmlDocument();
        xDoc.LoadXml("""
            <dataContainer>
              <section name="currentprovider">
                <field name="firstName" conceptName="CurrentUser_FirstName" />
                <field name="surname" conceptName="CurrentUser_Surname" />
                <field name="email" conceptName="CurrentUser_Email" />
                <field name="workPhone" conceptName="CurrentUser_WorkPhone" />
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

        var firstName = xDoc.SelectSingleNode("//field[@name='firstName']")?.InnerText;
        var surname = xDoc.SelectSingleNode("//field[@name='surname']")?.InnerText;
        var email = xDoc.SelectSingleNode("//field[@name='email']")?.InnerText;
        var workPhone = xDoc.SelectSingleNode("//field[@name='workPhone']")?.InnerText;

        if (string.IsNullOrWhiteSpace(firstName) && string.IsNullOrWhiteSpace(surname))
        {
            return null;
        }

        return new CurrentProviderCanonical(firstName, surname, email, workPhone, OriginScope.Hiso);
    }

    private static string? Column(System.Data.DataRow row, string columnName) =>
        row.Table.Columns.Contains(columnName) && row[columnName] is not System.DBNull ? row[columnName]?.ToString() : null;
}
