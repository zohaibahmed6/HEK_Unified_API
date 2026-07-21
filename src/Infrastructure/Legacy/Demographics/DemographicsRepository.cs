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
///   handled explicitly (`ParseOptionalDate`). This is the only demographics source fully verified
///   against live data so far; see PROJECT_STATUS.md open item 28.
/// - HISO (`GetHisoAsync`): still an unconfirmed inference. `Hiso.uspGetPatient_Demographics`
///   was tested against real `PMS_NZ_V2` data and does **not** exist under that name/schema there
///   (SQL error 2812, "could not find stored procedure") - confirmed wrong, not yet replaced with a
///   known-correct name.
/// - ERMS/COL (`GetErmsAsync`/`GetColAsync`): still unconfirmed inferences. `[HSS].[uspGetPatientData]`
///   was tested against real `PMS_NZ_V2` data and does not exist under that name either (same error).
/// </summary>
public sealed class DemographicsRepository : IDemographicsRepository
{
    private readonly IHisoConceptExecutor _hisoExecutor;
    private readonly ILegacyPracticeConnectionResolver _connectionResolver;

    public DemographicsRepository(IHisoConceptExecutor hisoExecutor, ILegacyPracticeConnectionResolver connectionResolver)
    {
        _hisoExecutor = hisoExecutor;
        _connectionResolver = connectionResolver;
    }

    public async Task<DemographicsHiso?> GetHisoAsync(int patientId, HealthLinkSession session, CancellationToken ct = default)
    {
        const string procedureName = "Hiso.uspGetPatient_Demographics";
        var requests = new List<HisoRequest> { new() { ProcedureName = procedureName } };

        var dataSet = await _hisoExecutor.ExecuteAsync(procedureName, session, requests, ct);
        var row = dataSet?.Tables.Count > 0 && dataSet.Tables[0].Rows.Count > 0 ? dataSet.Tables[0].Rows[0] : null;
        if (row is null)
        {
            return null;
        }

        return new DemographicsHiso(
            patientId,
            session.PracticeId,
            row["FirstName"].ToString() ?? string.Empty,
            row["LastName"].ToString() ?? string.Empty,
            DateOnly.FromDateTime(Convert.ToDateTime(row["DateOfBirth"])));
    }

    public async Task<DemographicsKaro?> GetKaroAsync(int patientId, string practiceId, CancellationToken ct = default)
    {
        var connectionString = await _connectionResolver.ResolveAsync(practiceId, ct);
        var parameters = new List<SqlParameter> { new("@pPatientID", patientId) };
        var table = await LegacyDbExecutor.ExecuteDataTableAsync(connectionString, System.Data.CommandType.StoredProcedure, "[HSS].[uspGetDemographics]", parameters, ct);

        if (table.Rows.Count == 0)
        {
            return null;
        }

        var row = table.Rows[0];
        return new DemographicsKaro(
            patientId,
            practiceId,
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

    public async Task<DemographicsErms?> GetErmsAsync(int patientId, string practiceId, CancellationToken ct = default)
    {
        var connectionString = await _connectionResolver.ResolveAsync(practiceId, ct);
        var parameters = new List<SqlParameter> { new("@pPatientID", patientId) };
        var table = await LegacyDbExecutor.ExecuteDataTableAsync(connectionString, System.Data.CommandType.StoredProcedure, "[HSS].[uspGetPatientData]", parameters, ct);

        if (table.Rows.Count == 0)
        {
            return null;
        }

        var row = table.Rows[0];
        return new DemographicsErms(
            patientId,
            row["EncounterId"] is DBNull ? 0 : Convert.ToInt32(row["EncounterId"]),
            row["FirstName"].ToString() ?? string.Empty,
            row["LastName"].ToString() ?? string.Empty,
            DateOnly.FromDateTime(Convert.ToDateTime(row["Dob"])),
            row["Nhi"] is DBNull ? null : row["Nhi"].ToString());
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
