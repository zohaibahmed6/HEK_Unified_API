using System.Data;
using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Models;
using HekCoreApi.Contracts.ClinicalNotes;
using HekCoreApi.Contracts.Security;
using Microsoft.Data.SqlClient;

namespace HekCoreApi.Infrastructure.Legacy.ClinicalNotes;

/// <summary>
/// FLAGGED INFERENCES: `Hiso.uspGetPatient_ConsultNotes` follows the `Hiso.uspGetPatient_X`
/// naming pattern; `[HSS].[uspGetClinicalNotes]`/`[HSS].[uspSaveClinicalNotes]`/
/// `[HSS].[uspGetConsultNotes]` follow the confirmed `[HSS]` schema convention
/// (PROJECT_STATUS.md). None of the three procedure names themselves, nor the result column
/// names assumed below, are confirmed against a live schema.
/// </summary>
public sealed class ClinicalNotesRepository : IClinicalNotesRepository
{
    private readonly IHisoConceptExecutor _hisoExecutor;
    private readonly ILegacyPracticeConnectionResolver _connectionResolver;

    public ClinicalNotesRepository(IHisoConceptExecutor hisoExecutor, ILegacyPracticeConnectionResolver connectionResolver)
    {
        _hisoExecutor = hisoExecutor;
        _connectionResolver = connectionResolver;
    }

    public async Task<IReadOnlyList<ClinicalNote>> GetAsync(
        OriginScope origin, int patientId, int encounterId, HealthLinkSession hisoSession,
        DateOnly? sinceDate, DateOnly? untilDate, string? sortOrder, CancellationToken ct = default)
    {
        // ERMS-BR-05/FR-CLIN-02: default 24-month lookback if no date range supplied.
        var effectiveSince = sinceDate ?? DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-24));
        var effectiveUntil = untilDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        return origin switch
        {
            OriginScope.Hiso => await GetViaHisoAsync(patientId, encounterId, hisoSession, effectiveSince, effectiveUntil, sortOrder, ct),
            OriginScope.Karo => await GetViaNamedProcedureAsync("[HSS].[uspGetClinicalNotes]", hisoSession.PracticeId, patientId, encounterId, effectiveSince, effectiveUntil, sortOrder, ct),
            OriginScope.Erms => await GetViaNamedProcedureAsync("[HSS].[uspGetConsultNotes]", hisoSession.PracticeId, patientId, encounterId, effectiveSince, effectiveUntil, sortOrder, ct),
            _ => throw new Domain.Exceptions.ForbiddenException("Clinical notes are not available for this origin scope.")
        };
    }

    public async Task<ClinicalNote> SaveAsync(int patientId, int encounterId, string practiceId, string content, CancellationToken ct = default)
    {
        var connectionString = await _connectionResolver.ResolveAsync(practiceId, ct);
        var parameters = new List<SqlParameter>
        {
            new("@pPatientID", patientId),
            new("@pEncounterID", encounterId),
            new("@pContent", content)
        };

        var output = new SqlParameter("@pNoteIDOut", SqlDbType.NVarChar, 64) { Direction = ParameterDirection.Output };
        parameters.Add(output);

        await LegacyDbExecutor.ExecuteNonQueryAsync(connectionString, CommandType.StoredProcedure, "[HSS].[uspSaveClinicalNotes]", parameters, ct);

        return new ClinicalNote(output.Value?.ToString() ?? Guid.NewGuid().ToString(), patientId, encounterId, "unknown", DateTimeOffset.UtcNow, content);
    }

    private async Task<IReadOnlyList<ClinicalNote>> GetViaHisoAsync(int patientId, int encounterId, HealthLinkSession session, DateOnly since, DateOnly until, string? sortOrder, CancellationToken ct)
    {
        const string procedureName = "Hiso.uspGetPatient_ConsultNotes";
        var request = new HisoRequest
        {
            ProcedureName = procedureName,
            FieldminDateTime = since.ToString("O"),
            FieldmaxDateTime = until.ToString("O"),
            Fieldorder = sortOrder
        };

        var dataSet = await _hisoExecutor.ExecuteAsync(procedureName, session, [request], ct);
        return MapRows(dataSet, patientId, encounterId);
    }

    private async Task<IReadOnlyList<ClinicalNote>> GetViaNamedProcedureAsync(
        string procedureName, string practiceId, int patientId, int encounterId, DateOnly since, DateOnly until, string? sortOrder, CancellationToken ct)
    {
        var connectionString = await _connectionResolver.ResolveAsync(practiceId, ct);
        var parameters = new List<SqlParameter>
        {
            new("@pPatientID", patientId),
            new("@pEncounterID", encounterId),
            new("@pSinceDate", since.ToDateTime(TimeOnly.MinValue)),
            new("@pUntilDate", until.ToDateTime(TimeOnly.MinValue)),
            new("@pSortOrder", (object?)sortOrder ?? DBNull.Value)
        };

        var table = await LegacyDbExecutor.ExecuteDataTableAsync(connectionString, CommandType.StoredProcedure, procedureName, parameters, ct);
        return MapRows(table, patientId, encounterId);
    }

    private static List<ClinicalNote> MapRows(DataSet? dataSet, int patientId, int encounterId) =>
        dataSet?.Tables.Count > 0 ? MapRows(dataSet.Tables[0], patientId, encounterId) : [];

    private static List<ClinicalNote> MapRows(DataTable table, int patientId, int encounterId)
    {
        var notes = new List<ClinicalNote>();
        foreach (DataRow row in table.Rows)
        {
            notes.Add(new ClinicalNote(
                row["NoteId"]?.ToString() ?? Guid.NewGuid().ToString(),
                patientId,
                encounterId,
                row["Author"]?.ToString() ?? string.Empty,
                row["CreatedAt"] is DBNull or null ? DateTimeOffset.UtcNow : Convert.ToDateTime(row["CreatedAt"]),
                row["Content"]?.ToString() ?? string.Empty));
        }

        return notes;
    }
}
