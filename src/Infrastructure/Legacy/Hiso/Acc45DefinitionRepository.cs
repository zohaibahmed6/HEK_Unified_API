using System.Data;
using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Models;
using Microsoft.Data.SqlClient;

namespace HekCoreApi.Infrastructure.Legacy.Hiso;

/// <summary>
/// Ported from legacy-reference/Hiso/Acc45DefinitionBuilder.cs. `UDT_tblACC45Definition`'s column
/// list is real, confirmed config (`Utitlity.GetColumnNameByTableName` reads
/// `AppSettings["UDT_tblACC45Definition"]`, a comma-separated column list) - sourced here via
/// `ISecretProvider` under the same key name, never hardcoded.
/// </summary>
public sealed class Acc45DefinitionRepository : IAcc45DefinitionRepository
{
    private readonly IHisoPracticeConnectionResolver _connectionResolver;
    private readonly ISecretProvider _secretProvider;

    public Acc45DefinitionRepository(IHisoPracticeConnectionResolver connectionResolver, ISecretProvider secretProvider)
    {
        _connectionResolver = connectionResolver;
        _secretProvider = secretProvider;
    }

    public async Task<bool> SaveDefinitionAsync(Acc45DefinitionInput input, HealthLinkSession session, CancellationToken ct = default)
    {
        var connectionString = await _connectionResolver.ResolveAsync(session.PracticeId, ct);
        var columnsRaw = await _secretProvider.GetSecretAsync("UDT_tblACC45Definition", ct);
        var columns = (columnsRaw ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var table = new DataTable();
        foreach (var column in columns)
        {
            table.Columns.Add(column);
        }

        var row = table.NewRow();
        foreach (DataColumn column in table.Columns)
        {
            row[column] = DBNull.Value;
        }

        SetIfPresent(row, table, "ACC45ID", 0);
        SetIfPresent(row, table, "AppointmentID", session.AppointmentId);
        SetIfPresent(row, table, "PatientId", session.PatientId);
        SetIfPresent(row, table, "ProviderID", session.ProviderId);
        SetIfPresent(row, table, "FormInstanceID", input.FormInstanceId);
        SetIfPresent(row, table, "FormInstanceVersion", input.FormInstanceVersion);
        SetIfPresent(row, table, "FormEngineID", input.FormEngineId);
        SetIfPresent(row, table, "FormInstanceOperationMode", input.FormInstanceOperationMode);
        SetIfPresent(row, table, "FormDefinitionId", input.FormDefinitionId);
        SetIfPresent(row, table, "FormDefinitionVersion", input.FormDefinitionVersion);
        SetIfPresent(row, table, "FormDefinitionTitle", input.FormDefinitionTitle);
        SetIfPresent(row, table, "ViewType", input.ViewType);
        SetIfPresent(row, table, "ViewSignature", input.ViewSignature);
        SetIfPresent(row, table, "ResumePath", input.ResumePath);
        SetIfPresent(row, table, "DMSDocumentID", input.DmsDocumentId);
        SetIfPresent(row, table, "IsActive", true);
        SetIfPresent(row, table, "IsDeleted", false);
        SetIfPresent(row, table, "InsertedBy", session.ProviderId);
        SetIfPresent(row, table, "UpdatedBy", session.ProviderId);
        SetIfPresent(row, table, "InsertedAt", DateTime.UtcNow.ToString("s"));
        SetIfPresent(row, table, "UpdatedAt", DateTime.UtcNow.ToString("s"));
        table.Rows.Add(row);

        var parameters = new List<SqlParameter> { new("@tblAcc45Def", table) };
        if (!string.IsNullOrEmpty(input.FormXml))
        {
            parameters.Add(new SqlParameter("@FormXml", input.FormXml));
        }

        if (!string.IsNullOrEmpty(input.FormComments))
        {
            parameters.Add(new SqlParameter("@FormComments", input.FormComments));
        }

        // Legacy: `FormSessionService.svc.cs:472` passes `objSessionKey.GUID.ToString()` here - the
        // real session GUID, not the PracticeId. Same class of bug as `GetDefinitionAsync`'s
        // `@pSessionKey` fix above - confirmed live 2026-07-31 when a genuinely new record (session
        // `D2C9E798-...`) inserted with `SessionKey='901'` (the practice ID) instead of the real GUID.
        parameters.Add(new SqlParameter("@pSessionKey", session.SessionKey is { } sessionGuid ? sessionGuid.ToString() : string.Empty));

        await LegacyDbExecutor.ExecuteNonQueryAsync(connectionString, CommandType.StoredProcedure, "[Appointment].[usptblACC45DefinitionInsert]", parameters, ct);
        return true;
    }

    public async Task<Acc45DefinitionRow?> GetDefinitionAsync(HealthLinkSession session, CancellationToken ct = default)
    {
        var connectionString = await _connectionResolver.ResolveAsync(session.PracticeId, ct);
        // Legacy: Acc45DefinitionBuilder.cs:142-145 - both parameters always sent, unconditionally.
        // `@pSessionKey` is the raw session GUID (`objSession.GUID.ToString()`), never the PracticeId -
        // the previous port here sent PracticeId as a same-named-but-wrong fallback whenever
        // ReferenceId was null, which never matches [Appointment].[tblACC45Definition]'s SessionKey
        // column (a real session GUID) - confirmed live 2026-07-30 with a real ACC45 row whose
        // SessionKey exactly matched the caller's session GUID but was never found until this fix.
        var parameters = new List<SqlParameter>
        {
            new("@ACC45ID", (object?)session.ReferenceId ?? DBNull.Value),
            new("@pSessionKey", session.SessionKey is { } sessionGuid ? sessionGuid.ToString() : DBNull.Value)
        };

        var table = await LegacyDbExecutor.ExecuteDataTableAsync(connectionString, CommandType.StoredProcedure, "[Appointment].[usptblACC45Definition_Get]", parameters, ct);
        if (table.Rows.Count == 0)
        {
            return null;
        }

        var row = table.Rows[0];
        var dmsGuid = row["DMSDocumentID"] is DBNull ? (Guid?)null : Guid.Parse(row["DMSDocumentID"].ToString()!);
        return new Acc45DefinitionRow(
            row["ResumePath"]?.ToString(),
            row["ViewType"]?.ToString(),
            dmsGuid,
            row["ACC45XML"]?.ToString());
    }

    private static void SetIfPresent(DataRow row, DataTable table, string column, object? value)
    {
        if (table.Columns.Contains(column) && value is not null)
        {
            row[column] = value;
        }
    }
}
