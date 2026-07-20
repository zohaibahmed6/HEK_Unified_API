using System.Data;
using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Models;
using HekCoreApi.Contracts.Conditions;
using HekCoreApi.Contracts.Security;
using Microsoft.Data.SqlClient;

namespace HekCoreApi.Infrastructure.Legacy.Conditions;

/// <summary>FLAGGED INFERENCES: procedure/column names follow the same conventions documented on ClinicalNotesRepository/DemographicsRepository - none confirmed against a live schema.</summary>
public sealed class ConditionsRepository : IConditionsRepository
{
    private readonly IHisoConceptExecutor _hisoExecutor;
    private readonly ILegacyPracticeConnectionResolver _connectionResolver;

    public ConditionsRepository(IHisoConceptExecutor hisoExecutor, ILegacyPracticeConnectionResolver connectionResolver)
    {
        _hisoExecutor = hisoExecutor;
        _connectionResolver = connectionResolver;
    }

    public async Task<IReadOnlyList<Condition>> GetAsync(OriginScope origin, int patientId, int encounterId, HealthLinkSession hisoSession, CancellationToken ct = default)
    {
        if (origin == OriginScope.Hiso)
        {
            const string procedureName = "Hiso.uspGetPatient_Diagnosis";
            var dataSet = await _hisoExecutor.ExecuteAsync(procedureName, hisoSession, [new HisoRequest { ProcedureName = procedureName }], ct);
            return MapRows(dataSet?.Tables.Count > 0 ? dataSet.Tables[0] : null);
        }

        var procedure = origin switch
        {
            OriginScope.Karo => "[HSS].[uspGetConditions]",
            OriginScope.Erms => "[HSS].[uspGetClassifications]",
            OriginScope.Col => "[HSS].[uspGetDiagnosisData]",
            _ => throw new Domain.Exceptions.ForbiddenException("Conditions are not available for this origin scope.")
        };

        var connectionString = await _connectionResolver.ResolveAsync(hisoSession.PracticeId, ct);
        var parameters = new List<SqlParameter> { new("@pPatientID", patientId), new("@pEncounterID", encounterId) };
        var table = await LegacyDbExecutor.ExecuteDataTableAsync(connectionString, CommandType.StoredProcedure, procedure, parameters, ct);
        return MapRows(table);
    }

    public async Task<Condition?> FindByNaturalKeyAsync(int encounterId, string practiceId, string diagnosisCode, CancellationToken ct = default)
    {
        var connectionString = await _connectionResolver.ResolveAsync(practiceId, ct);
        var parameters = new List<SqlParameter>
        {
            new("@pEncounterID", encounterId),
            new("@pDiagnosisCode", diagnosisCode)
        };

        var table = await LegacyDbExecutor.ExecuteDataTableAsync(connectionString, CommandType.StoredProcedure, "[HSS].[uspFindConditionByNaturalKey]", parameters, ct);
        return table.Rows.Count > 0 ? MapRow(table.Rows[0]) : null;
    }

    public async Task<Condition> SaveAsync(int patientId, int encounterId, string practiceId, ConditionInput input, CancellationToken ct = default)
    {
        var connectionString = await _connectionResolver.ResolveAsync(practiceId, ct);
        var parameters = new List<SqlParameter>
        {
            new("@pPatientID", patientId),
            new("@pEncounterID", encounterId),
            new("@pDiagnosisCode", input.DiagnosisCode),
            new("@pDescription", (object?)input.Description ?? DBNull.Value),
            new("@pIsLongTerm", input.IsLongTerm),
            new("@pSideCode", (object?)input.SideCode ?? DBNull.Value),
            new("@pSideDescription", (object?)input.SideDescription ?? DBNull.Value)
        };

        var output = new SqlParameter("@pConditionIDOut", SqlDbType.NVarChar, 64) { Direction = ParameterDirection.Output };
        parameters.Add(output);

        await LegacyDbExecutor.ExecuteNonQueryAsync(connectionString, CommandType.StoredProcedure, "[HSS].[uspSaveCondition]", parameters, ct);

        return new Condition(output.Value?.ToString() ?? Guid.NewGuid().ToString(), "created", input.DiagnosisCode, input.Description, input.IsLongTerm, input.SideCode, input.SideDescription);
    }

    private static List<Condition> MapRows(DataTable? table)
    {
        if (table is null)
        {
            return [];
        }

        var conditions = new List<Condition>();
        foreach (DataRow row in table.Rows)
        {
            conditions.Add(MapRow(row));
        }

        return conditions;
    }

    private static Condition MapRow(DataRow row) => new(
        row["ConditionId"]?.ToString() ?? Guid.NewGuid().ToString(),
        "existing",
        row["DiagnosisCode"]?.ToString() ?? string.Empty,
        row["Description"] is DBNull or null ? null : row["Description"].ToString(),
        row["IsLongTerm"] is DBNull or null ? false : Convert.ToBoolean(row["IsLongTerm"]),
        row["SideCode"] is DBNull or null ? null : row["SideCode"].ToString(),
        row["SideDescription"] is DBNull or null ? null : row["SideDescription"].ToString());
}
