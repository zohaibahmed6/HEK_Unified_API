using System.Data;
using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Models;
using HekCoreApi.Contracts.Medications;
using HekCoreApi.Contracts.Security;
using Microsoft.Data.SqlClient;

namespace HekCoreApi.Infrastructure.Legacy.Medications;

/// <summary>FLAGGED INFERENCES: see ClinicalNotesRepository/ConditionsRepository remarks - same naming-convention assumptions, none confirmed against a live schema.</summary>
public sealed class MedicationsRepository : IMedicationsRepository
{
    private readonly IHisoConceptExecutor _hisoExecutor;
    private readonly ILegacyPracticeConnectionResolver _connectionResolver;

    public MedicationsRepository(IHisoConceptExecutor hisoExecutor, ILegacyPracticeConnectionResolver connectionResolver)
    {
        _hisoExecutor = hisoExecutor;
        _connectionResolver = connectionResolver;
    }

    public async Task<IReadOnlyList<Medication>> GetAsync(OriginScope origin, int patientId, int encounterId, HealthLinkSession hisoSession, string? view, CancellationToken ct = default)
    {
        var effectiveView = string.IsNullOrEmpty(view) ? "regular" : view;

        if (origin == OriginScope.Hiso)
        {
            const string procedureName = "Hiso.uspGetPatient_Medications";
            var dataSet = await _hisoExecutor.ExecuteAsync(procedureName, hisoSession, [new HisoRequest { ProcedureName = procedureName }], ct);
            return MapRows(dataSet?.Tables.Count > 0 ? dataSet.Tables[0] : null, effectiveView);
        }

        var procedure = origin switch
        {
            OriginScope.Karo => "[HSS].[uspGetMedications]",
            OriginScope.Erms => effectiveView == "prescribed" ? "[HSS].[uspGetPrescribedMedications]" : "[HSS].[uspGetRegularMedications]",
            _ => throw new Domain.Exceptions.ForbiddenException("Medications are not available for this origin scope.")
        };

        var connectionString = await _connectionResolver.ResolveAsync(hisoSession.PracticeId, ct);
        var parameters = new List<SqlParameter> { new("@pPatientID", patientId), new("@pEncounterID", encounterId) };
        var table = await LegacyDbExecutor.ExecuteDataTableAsync(connectionString, CommandType.StoredProcedure, procedure, parameters, ct);
        return MapRows(table, effectiveView);
    }

    private static List<Medication> MapRows(DataTable? table, string view)
    {
        if (table is null)
        {
            return [];
        }

        var medications = new List<Medication>();
        foreach (DataRow row in table.Rows)
        {
            medications.Add(new Medication(
                row["MedicationId"]?.ToString() ?? Guid.NewGuid().ToString(),
                row["Name"]?.ToString() ?? string.Empty,
                view,
                row["PrescribedDate"] is DBNull or null ? null : DateOnly.FromDateTime(Convert.ToDateTime(row["PrescribedDate"]))));
        }

        return medications;
    }
}
