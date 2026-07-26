using System.Data;
using System.Xml;
using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Models;
using HekCoreApi.Contracts.Documents;
using HekCoreApi.Contracts.Security;

namespace HekCoreApi.Infrastructure.Legacy.Documents;

/// <summary>
/// Third canonical resource (2026-07-22) - see <see cref="DocumentCanonical"/> remarks for the
/// deliberate first-pass scope (KARO `documents` only, ERMS non-discharge `GetOtherDocs` only, HISO
/// `Patient_Attachment` only). Reuses the exact real, already-verified per-source repositories/engine -
/// same discipline as Demographics/Conditions, no new procedure/concept guessing.
/// - KARO: `IKaroDataRepository.GetDocumentsAsync` → real `[HSS].[uspGetDocuments]`.
/// - ERMS: `IErmsDataRepository.GetOtherDocsAsync` → real `[HSS].[uspGetOtherDocs]`; columns confirmed
///   live against real `PMS_NZ_V2` (`ReferenceId`, `SendingFacility`, `Subject`, `Name`, `DateReceived`,
///   `DataType`, `Comments`), not guessed.
/// - HISO: real, DB-confirmed `Patient_Attachment` concept group (`Hiso.uspGetPatient_Attachment`),
///   via the same group/list XML path `CanonicalConditionsRepository.GetHisoAsync` uses for `Patient_Problem`.
/// </summary>
public sealed class CanonicalDocumentsRepository : ICanonicalDocumentsRepository
{
    private readonly IKaroDataRepository _karoRepository;
    private readonly IErmsDataRepository _ermsRepository;
    private readonly IHisoConceptDictionary _hisoConceptDictionary;
    private readonly IHisoRequestEngine _hisoRequestEngine;
    private readonly IHisoConceptExecutor _hisoExecutor;

    public CanonicalDocumentsRepository(
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

    public async Task<IReadOnlyList<DocumentCanonical>> GetKaroAsync(RoutingContext routing, string? patientId, CancellationToken ct = default)
    {
        var documents = await _karoRepository.GetDocumentsAsync(routing.PracticeId, routing.PracticeId, patientId, identifier: null, ct);
        return documents.Select(d => new DocumentCanonical(
            d.Identifier,
            d.MessageTitle ?? d.MessageSubject ?? string.Empty,
            d.MessageSubject,
            d.ContentType,
            d.CreatedDateTime,
            OriginScope.Karo)).ToList();
    }

    public async Task<IReadOnlyList<DocumentCanonical>> GetErmsAsync(RoutingContext routing, string? patientId, CancellationToken ct = default)
    {
        var table = await _ermsRepository.GetOtherDocsAsync(routing.PracticeId, routing.PracticeId, patientId, sortOrder: null, DateTime.MinValue, DateTime.MinValue, isReferral: false, ct);
        var results = new List<DocumentCanonical>();

        foreach (DataRow row in table.Rows)
        {
            results.Add(new DocumentCanonical(
                Column(row, "ReferenceId"),
                Column(row, "Name") ?? Column(row, "Subject") ?? string.Empty,
                Column(row, "Subject"),
                Column(row, "DataType"),
                Column(row, "DateReceived"),
                OriginScope.Erms));
        }

        return results;
    }

    public async Task<IReadOnlyList<DocumentCanonical>> GetHisoAsync(HealthLinkSession session, CancellationToken ct = default)
    {
        var xDoc = new XmlDocument();
        xDoc.LoadXml("""
            <dataContainer>
              <section name="documents">
                <group name="attachment" conceptName="Patient_Attachment">
                  <field name="name" conceptName="Patient_Attachment_Name" />
                  <field name="subject" conceptName="Patient_Attachment_Subject" />
                  <field name="type" conceptName="Patient_Attachment_Type" />
                  <field name="dateCreated" conceptName="Patient_Attachment_DateCreated" />
                  <field name="id" conceptName="Patient_Attachment_ID" />
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

        var results = new List<DocumentCanonical>();
        var groupNodes = xDoc.SelectNodes("//group[@name='attachment']");
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

            results.Add(new DocumentCanonical(
                group.SelectSingleNode("field[@name='id']")?.InnerText,
                name,
                group.SelectSingleNode("field[@name='subject']")?.InnerText,
                group.SelectSingleNode("field[@name='type']")?.InnerText,
                group.SelectSingleNode("field[@name='dateCreated']")?.InnerText,
                OriginScope.Hiso));
        }

        return results;
    }

    /// <summary>
    /// `[HSS].[uspGetOtherDocs]` returns real pipe-delimited `"{conceptId}|&amp;|{text}"` cells
    /// (confirmed live, 2026-07-22 - unlike `[HSS].[uspGetConditions]`, which returns plain columns).
    /// Different real procedures, different real wire shapes - not assumed from the earlier fix.
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
