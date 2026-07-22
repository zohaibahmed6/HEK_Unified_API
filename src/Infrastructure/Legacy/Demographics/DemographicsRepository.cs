using System.Xml;
using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Models;
using HekCoreApi.Contracts.Demographics;
using Microsoft.Data.SqlClient;

namespace HekCoreApi.Infrastructure.Legacy.Demographics;

/// <summary>
/// - KARO (`GetKaroAsync`): **confirmed against real `PMS_NZ_V2` data (2026-07-20, patient
///   2459731)**. `[HSS].[uspGetDemographics]` is a real, executable procedure and its real result
///   columns are `Given`/`Family`/`BirthDate` for name/DOB - not `FirstName`/`LastName`/`DateOfBirth`
///   as originally inferred (those column names DO exist too, but hold an unrelated composite
///   internal-reference string like `554:1000111/1000310/1|&amp;|LnB`, not a usable value). Also
///   confirmed the procedure returns empty string, not `DBNull`, for an unset enrolment date -
///   handled explicitly (`ParseOptionalDate`). See PROJECT_STATUS.md open item 28.
/// - HISO (`GetHisoAsync`): fixed 2026-07-22 - now reuses the exact same real, working pipeline as
///   the verified `/hiso/getData` endpoint (`GetDataQueryHandler`): the real concept dictionary
///   (`[Hiso].[UspGetHisoConcepts]`) + `IHisoRequestEngine`'s concept-to-procedure resolution +
///   `IHisoConceptExecutor`. The earlier hardcoded `Hiso.uspGetPatient_Demographics` guess was
///   confirmed not to exist against real `PMS_NZ_V2` data (SQL error 2812) - this replaces it with
///   the real concept names confirmed from a live production trace
///   (`legacy-reference/hiso_snippet_sanitized.txt`): `Patient_FirstName`, `Patient_Surname` (not
///   "LastName"), `Patient_DateOfBirth`, resolved by the engine to the real `Hiso.uspGetPatient`
///   procedure - never hardcoded here.
/// - ERMS (`GetErmsAsync`): fixed 2026-07-22 - now calls `IErmsDemographicsRepository.GetDemographicsAsync`,
///   the exact real repository the verified `/erms/GetPatientData` endpoint uses (`[HSS].[uspGetDemographics]`
///   - the *same* real procedure KARO uses, confirmed identical result-set shape by direct `sqlcmd`
///   inspection against real `PMS_NZ_V2` data for patient 2459731). Initially assumed the wire-shape
///   `Patient_FirstName`/`Patient_Surname`/`Patient_DateOfBirth`/`Patient_NHI` pipe-delimited columns
///   `ErmsDataTableMapper`/`PatientData` use for `/erms/GetPatientData` - **wrong**: those XML element
///   names don't exist as DataTable columns at all. The real columns matching `PatientData`'s C#
///   property names (`FirstName`/`Surname`/`DateOfBirth`/`PatientNHI`) exist but - confirmed by direct
///   query - hold the exact same unusable composite-reference garbage already documented for KARO's
///   `FirstName`/`LastName`/`DateOfBirth` columns above. The real, clean values are in `Given`/`Family`/
///   `BirthDate`/`NHI` (verified: `LnB`/`WMKCU`/`2019-01-12`/`ZZZ0121` for patient 2459731) - identical
///   to `GetKaroAsync`'s already-fixed columns, since it's the identical procedure/row shape. Read
///   directly rather than via `ErmsDataTableMapper` (which targets the wire-shape columns, appropriate
///   for `/erms/GetPatientData`'s XML contract but not this canonical mapping).
///   FLAGGED ASSUMPTION: `IErmsDemographicsRepository` routes by ERMS's own `practiceSuffix` (parsed
///   from `encounterId` in the legacy compat flow), which the canonical JWT doesn't carry - the
///   caller's `practiceId` claim is passed through as the suffix directly (works today because
///   `TEST-PRACTICE-001` is a valid value on both sides), not a confirmed general mapping.
/// - COL (`GetColAsync`): still an unconfirmed inference (`[HSS].[uspGetCurrentPatientData]`), out of
///   scope for this fix - not exercised by the Wednesday demo's KARO/HISO/ERMS walkthrough.
/// </summary>
public sealed class DemographicsRepository : IDemographicsRepository
{
    private readonly IHisoConceptExecutor _hisoExecutor;
    private readonly IHisoConceptDictionary _hisoConceptDictionary;
    private readonly IHisoRequestEngine _hisoRequestEngine;
    private readonly IErmsDemographicsRepository _ermsDemographicsRepository;
    private readonly ILegacyPracticeConnectionResolver _connectionResolver;

    public DemographicsRepository(
        IHisoConceptExecutor hisoExecutor,
        IHisoConceptDictionary hisoConceptDictionary,
        IHisoRequestEngine hisoRequestEngine,
        IErmsDemographicsRepository ermsDemographicsRepository,
        ILegacyPracticeConnectionResolver connectionResolver)
    {
        _hisoExecutor = hisoExecutor;
        _hisoConceptDictionary = hisoConceptDictionary;
        _hisoRequestEngine = hisoRequestEngine;
        _ermsDemographicsRepository = ermsDemographicsRepository;
        _connectionResolver = connectionResolver;
    }

    public async Task<DemographicsHiso?> GetHisoAsync(int patientId, HealthLinkSession session, CancellationToken ct = default)
    {
        // Real HisoRequestEngine.FillXmlDetailsAsync only fills fields nested under <section> -
        // matching the real getData request shape, not a bare <field> list.
        var xDoc = new XmlDocument();
        xDoc.LoadXml("""
            <dataContainer>
              <section name="demographics">
                <field name="firstName" conceptName="Patient_FirstName" />
                <field name="lastName" conceptName="Patient_Surname" />
                <field name="dateOfBirth" conceptName="Patient_DateOfBirth" />
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
        var lastName = xDoc.SelectSingleNode("//field[@name='lastName']")?.InnerText;
        var dateOfBirthText = xDoc.SelectSingleNode("//field[@name='dateOfBirth']")?.InnerText;

        if (string.IsNullOrWhiteSpace(firstName) && string.IsNullOrWhiteSpace(lastName) && string.IsNullOrWhiteSpace(dateOfBirthText))
        {
            return null;
        }

        return new DemographicsHiso(
            patientId,
            session.PracticeId,
            firstName ?? string.Empty,
            lastName ?? string.Empty,
            DateOnly.TryParse(dateOfBirthText, out var dob) ? dob : default);
    }

    public async Task<DemographicsKaro?> GetKaroAsync(int patientId, RoutingContext routing, CancellationToken ct = default)
    {
        var connectionString = await _connectionResolver.ResolveAsync(routing, ct);
        var parameters = new List<SqlParameter> { new("@pPatientID", patientId) };
        var table = await LegacyDbExecutor.ExecuteDataTableAsync(connectionString, System.Data.CommandType.StoredProcedure, "[HSS].[uspGetDemographics]", parameters, ct);

        if (table.Rows.Count == 0)
        {
            return null;
        }

        var row = table.Rows[0];
        return new DemographicsKaro(
            patientId,
            routing.PracticeId,
            row["Given"].ToString() ?? string.Empty,
            row["Family"].ToString() ?? string.Empty,
            DateOnly.FromDateTime(Convert.ToDateTime(row["BirthDate"])),
            ParseOptionalDate(row["DateOfEnrolment"]),
            ParseOptionalDate(row["EndEnrolmentDate"]));
    }

    /// <summary>
    /// Confirmed against real PMS_NZ_V2 data (2026-07-20, patient 2459731): [HSS].[uspGetDemographics]
    /// returns empty string, not DBNull, for an unset date column - handled explicitly rather than
    /// assuming DBNull is the only "no value" case.
    /// </summary>
    private static DateOnly? ParseOptionalDate(object value) =>
        value is DBNull || string.IsNullOrWhiteSpace(value.ToString()) ? null : DateOnly.FromDateTime(Convert.ToDateTime(value));

    public async Task<DemographicsErms?> GetErmsAsync(int patientId, RoutingContext routing, CancellationToken ct = default)
    {
        // ERMS's own connection routing (IErmsPracticeConnectionResolver, separate from the tenant
        // registry) still keys by a flat practiceSuffix - RoutingContext.PracticeId doubles as that
        // suffix today (see FLAGGED ASSUMPTION above).
        var table = await _ermsDemographicsRepository.GetDemographicsAsync(routing.PracticeId, patientId.ToString(), ct);

        if (table.Rows.Count == 0)
        {
            return null;
        }

        var row = table.Rows[0];

        return new DemographicsErms(
            patientId,
            0,
            row["Given"].ToString() ?? string.Empty,
            row["Family"].ToString() ?? string.Empty,
            DateOnly.FromDateTime(Convert.ToDateTime(row["BirthDate"])),
            row["NHI"] is DBNull ? null : row["NHI"].ToString());
    }

    public async Task<DemographicsCol?> GetColAsync(int patientId, string practiceId, CancellationToken ct = default)
    {
        var connectionString = await _connectionResolver.ResolveAsync(practiceId, ct);
        var parameters = new List<SqlParameter> { new("@pPatientID", patientId) };
        var table = await LegacyDbExecutor.ExecuteDataTableAsync(connectionString, System.Data.CommandType.StoredProcedure, "[HSS].[uspGetCurrentPatientData]", parameters, ct);

        if (table.Rows.Count == 0)
        {
            return null;
        }

        var row = table.Rows[0];
        return new DemographicsCol(
            patientId,
            practiceId,
            row["FirstName"].ToString() ?? string.Empty,
            row["LastName"].ToString() ?? string.Empty);
    }
}
