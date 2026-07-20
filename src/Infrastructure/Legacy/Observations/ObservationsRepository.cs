using System.Data;
using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Models;
using HekCoreApi.Contracts.Observations;
using HekCoreApi.Contracts.Security;
using Microsoft.Data.SqlClient;

namespace HekCoreApi.Infrastructure.Legacy.Observations;

/// <summary>FLAGGED INFERENCES: see other Block 2 repositories' remarks - procedure/column names follow the same naming-convention assumptions, none confirmed against a live schema.</summary>
public sealed class ObservationsRepository : IObservationsRepository
{
    private readonly IHisoConceptExecutor _hisoExecutor;
    private readonly ILegacyPracticeConnectionResolver _connectionResolver;

    public ObservationsRepository(IHisoConceptExecutor hisoExecutor, ILegacyPracticeConnectionResolver connectionResolver)
    {
        _hisoExecutor = hisoExecutor;
        _connectionResolver = connectionResolver;
    }

    public async Task<IReadOnlyList<Observation>> GetAsync(OriginScope origin, int patientId, int encounterId, HealthLinkSession hisoSession, string? conceptId, CancellationToken ct = default)
    {
        if (origin == OriginScope.Hiso)
        {
            const string procedureName = "Hiso.uspGetPatient_Measurements";
            var dataSet = await _hisoExecutor.ExecuteAsync(procedureName, hisoSession, [new HisoRequest { ProcedureName = procedureName }], ct);
            return MapRows(dataSet?.Tables.Count > 0 ? dataSet.Tables[0] : null);
        }

        var procedure = origin switch
        {
            OriginScope.Karo => "[HSS].[uspGetObservations]",
            OriginScope.Erms => "[HSS].[uspGetPatientMeasurement]",
            _ => throw new Domain.Exceptions.ForbiddenException("Observations are not available for this origin scope.")
        };

        var connectionString = await _connectionResolver.ResolveAsync(hisoSession.PracticeId, ct);
        var parameters = new List<SqlParameter>
        {
            new("@pPatientID", patientId),
            new("@pEncounterID", encounterId),
            new("@pConceptID", (object?)conceptId ?? DBNull.Value)
        };

        var table = await LegacyDbExecutor.ExecuteDataTableAsync(connectionString, CommandType.StoredProcedure, procedure, parameters, ct);
        return MapRows(table);
    }

    public async Task<Observation> SaveAsync(int patientId, int encounterId, string practiceId, ObservationInput input, CancellationToken ct = default)
    {
        var connectionString = await _connectionResolver.ResolveAsync(practiceId, ct);
        var parameters = new List<SqlParameter>
        {
            new("@pPatientID", patientId),
            new("@pEncounterID", encounterId),
            new("@pHeight", (object?)input.Height ?? DBNull.Value),
            new("@pWeight", (object?)input.Weight ?? DBNull.Value),
            new("@pBMI", (object?)input.Bmi ?? DBNull.Value),
            new("@pBloodPressureSystolic", (object?)input.BloodPressureSystolic ?? DBNull.Value),
            new("@pBloodPressureDiastolic", (object?)input.BloodPressureDiastolic ?? DBNull.Value),
            new("@pWaistCircumference", (object?)input.WaistCircumference ?? DBNull.Value),
            new("@pSmokingStatus", (object?)input.SmokingStatus ?? DBNull.Value),
            new("@pHeartRate", (object?)input.HeartRate ?? DBNull.Value),
            new("@pTemperature", (object?)input.Temperature ?? DBNull.Value)
        };

        var output = new SqlParameter("@pObservationIDOut", SqlDbType.NVarChar, 64) { Direction = ParameterDirection.Output };
        parameters.Add(output);

        await LegacyDbExecutor.ExecuteNonQueryAsync(connectionString, CommandType.StoredProcedure, "[HSS].[uspSaveObservations]", parameters, ct);

        return new Observation(output.Value?.ToString() ?? Guid.NewGuid().ToString(), null, "saved", DateTimeOffset.UtcNow);
    }

    private static List<Observation> MapRows(DataTable? table)
    {
        if (table is null)
        {
            return [];
        }

        var observations = new List<Observation>();
        foreach (DataRow row in table.Rows)
        {
            observations.Add(new Observation(
                row["ObservationId"]?.ToString() ?? Guid.NewGuid().ToString(),
                row["ConceptId"] is DBNull or null ? null : row["ConceptId"].ToString(),
                row["Value"]?.ToString() ?? string.Empty,
                row["RecordedAt"] is DBNull or null ? DateTimeOffset.UtcNow : Convert.ToDateTime(row["RecordedAt"])));
        }

        return observations;
    }
}
