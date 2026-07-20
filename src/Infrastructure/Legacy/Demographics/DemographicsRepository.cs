using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Models;
using HekCoreApi.Contracts.Demographics;
using Microsoft.Data.SqlClient;

namespace HekCoreApi.Infrastructure.Legacy.Demographics;

/// <summary>
/// FLAGGED INFERENCES throughout this class, since no live DB/stored-procedure schema access is
/// available (SRS: "the actual T-SQL definitions... were not available"):
/// - HISO: procedure name `Hiso.uspGetPatient_Demographics` follows the `Hiso.uspGetPatient_X`
///   naming pattern confirmed for other procedures (DBMessages.cs's AWS-enabled-procedure list) -
///   not itself confirmed.
/// - KARO: `[HSS].[uspGetDemographics]` IS a confirmed real stored procedure name
///   (PROJECT_STATUS.md Section 2 - "identical stored-procedure names ([HSS] schema,
///   uspGetDemographics etc.)").
/// - ERMS/COL: `[HSS].[uspGetPatientData]` / `[HSS].[uspGetCurrentPatientData]` follow the same
///   confirmed `[HSS]` schema convention (ERMS shares KARO's DAL/schema per ComparisonReport
///   Section 2.1) but the exact names are not themselves confirmed.
/// - Every result column name assumed below matches the OpenAPI response schema's field names
///   exactly, since that is the only field-level detail available anywhere in this project's docs.
/// All of the above needs verification against the live schema before this is trusted as correct -
/// tracked in PROJECT_STATUS.md.
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
            row["FirstName"].ToString() ?? string.Empty,
            row["LastName"].ToString() ?? string.Empty,
            DateOnly.FromDateTime(Convert.ToDateTime(row["DateOfBirth"])),
            row["DateOfEnrolment"] is DBNull ? null : DateOnly.FromDateTime(Convert.ToDateTime(row["DateOfEnrolment"])),
            row["EndEnrolmentDate"] is DBNull ? null : DateOnly.FromDateTime(Convert.ToDateTime(row["EndEnrolmentDate"])));
    }

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
