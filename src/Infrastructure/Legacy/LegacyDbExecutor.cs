using System.Data;
using Microsoft.Data.SqlClient;
using Serilog;

namespace HekCoreApi.Infrastructure.Legacy;

/// <summary>
/// Minimal, parameterized ADO.NET helper used by every ported/legacy-adjacent Infrastructure
/// component (<see cref="Dormant.Dmsda.DmsDocumentService"/>, <see cref="Hiso.HisoConceptExecutor"/>,
/// and Block 2's domain repositories). The legacy `DALHelper` class these modules originally called
/// is not part of the supplied source (`legacy-reference/DAL/`) - only `DMSDA.cs`/`DBMessages.cs`
/// themselves were provided. This is a fresh, minimal re-implementation of the same call shape
/// (`ExecuteNonQuery`/`ExecuteDataTable`/`ExecuteDataSet`/`ExecuteScalar` against a stored
/// procedure or parameterized text command), not a port of unseen code - flagged as such rather
/// than presented as a faithful `DALHelper` port. Every method here is parameterized only; no
/// method accepts a raw SQL string built by concatenation.
///
/// Every call is logged here (2026-07-31) - not opt-in per caller - so "which stored procedure ran,
/// with what parameters, returning what" is always captured centrally, for every one of the ~30
/// repositories that call through this class, with no risk of a caller forgetting to log it. Uses
/// Serilog's static <see cref="Log"/> logger directly (this is a static helper with no DI container
/// access) - it inherits the ambient "System"/"CorrelationId" LogContext properties pushed by
/// RequestResponseLoggingMiddleware/CorrelationIdMiddleware for whichever request is currently
/// executing, so these lines route to the correct per-system technical-*.log automatically, without
/// this class needing to know which system called it.
/// </summary>
public static class LegacyDbExecutor
{
    public static async Task<int> ExecuteNonQueryAsync(
        string connectionString,
        CommandType commandType,
        string commandText,
        IReadOnlyList<SqlParameter>? parameters = null,
        CancellationToken ct = default)
    {
        await using var connection = new SqlConnection(connectionString);
        await using var command = new SqlCommand(commandText, connection) { CommandType = commandType };
        AddParameters(command, parameters);

        try
        {
            await connection.OpenAsync(ct);
            var rowsAffected = await command.ExecuteNonQueryAsync(ct);
            LogCall(commandText, command.Parameters, $"rowsAffected={rowsAffected}");
            return rowsAffected;
        }
        catch (Exception ex)
        {
            LogFailure(commandText, command.Parameters, ex);
            throw;
        }
    }

    public static async Task<DataTable> ExecuteDataTableAsync(
        string connectionString,
        CommandType commandType,
        string commandText,
        IReadOnlyList<SqlParameter>? parameters = null,
        CancellationToken ct = default)
    {
        await using var connection = new SqlConnection(connectionString);
        await using var command = new SqlCommand(commandText, connection) { CommandType = commandType };
        AddParameters(command, parameters);

        try
        {
            await connection.OpenAsync(ct);
            await using var reader = await command.ExecuteReaderAsync(ct);
            var table = new DataTable();
            table.Load(reader);
            LogCall(commandText, command.Parameters, $"rows={table.Rows.Count}, columns=[{string.Join(",", table.Columns.Cast<DataColumn>().Select(c => c.ColumnName))}]");
            return table;
        }
        catch (Exception ex)
        {
            LogFailure(commandText, command.Parameters, ex);
            throw;
        }
    }

    public static async Task<DataSet> ExecuteDataSetAsync(
        string connectionString,
        CommandType commandType,
        string commandText,
        IReadOnlyList<SqlParameter>? parameters = null,
        CancellationToken ct = default)
    {
        await using var connection = new SqlConnection(connectionString);
        await using var command = new SqlCommand(commandText, connection) { CommandType = commandType };
        AddParameters(command, parameters);

        try
        {
            await connection.OpenAsync(ct);
            await using var reader = await command.ExecuteReaderAsync(ct);
            var dataSet = new DataSet();
            do
            {
                var table = new DataTable();
                table.Load(reader);
                dataSet.Tables.Add(table);
            } while (!reader.IsClosed && reader.NextResult());

            LogCall(commandText, command.Parameters, $"tables={dataSet.Tables.Count}, rowsPerTable=[{string.Join(",", dataSet.Tables.Cast<DataTable>().Select(t => t.Rows.Count))}]");
            return dataSet;
        }
        catch (Exception ex)
        {
            LogFailure(commandText, command.Parameters, ex);
            throw;
        }
    }

    public static async Task<object?> ExecuteScalarAsync(
        string connectionString,
        CommandType commandType,
        string commandText,
        IReadOnlyList<SqlParameter>? parameters = null,
        CancellationToken ct = default)
    {
        await using var connection = new SqlConnection(connectionString);
        await using var command = new SqlCommand(commandText, connection) { CommandType = commandType };
        AddParameters(command, parameters);

        try
        {
            await connection.OpenAsync(ct);
            var result = await command.ExecuteScalarAsync(ct);
            LogCall(commandText, command.Parameters, $"scalar={FormatValue(result)}");
            return result;
        }
        catch (Exception ex)
        {
            LogFailure(commandText, command.Parameters, ex);
            throw;
        }
    }

    private static void AddParameters(SqlCommand command, IReadOnlyList<SqlParameter>? parameters)
    {
        if (parameters is null)
        {
            return;
        }

        foreach (var parameter in parameters)
        {
            command.Parameters.Add(parameter);
        }
    }

    private static void LogCall(string commandText, SqlParameterCollection parameters, string outcome) =>
        Log.Information(
            "SQL call: {Procedure} params={@Params} -> {Outcome}",
            commandText,
            DescribeParameters(parameters),
            outcome);

    private static void LogFailure(string commandText, SqlParameterCollection parameters, Exception ex) =>
        Log.Error(
            ex,
            "SQL call failed: {Procedure} params={@Params}",
            commandText,
            DescribeParameters(parameters));

    // Every parameter's final value (including SqlDbType.Output ones, populated only after
    // execution completes) is captured by name - nothing about a call's real input/output data is
    // hidden, EXCEPT binary blobs (SqlDbType.Image/VarBinary/Binary, e.g. @pDocumentData), which are
    // recorded as a size only ("byte[12345]"). Dumping raw document bytes as JSON would be both
    // meaningless to read and would bloat the log files by orders of magnitude for no diagnostic
    // value - the size alone is enough to confirm "yes, real content was sent, this many bytes".
    private static Dictionary<string, string> DescribeParameters(SqlParameterCollection parameters)
    {
        var result = new Dictionary<string, string>();
        foreach (SqlParameter parameter in parameters)
        {
            result[parameter.ParameterName] = FormatValue(parameter.Value);
        }
        return result;
    }

    private static string FormatValue(object? value) => value switch
    {
        null or DBNull => "null",
        byte[] bytes => $"byte[{bytes.Length}]",
        _ => value.ToString() ?? "null",
    };
}
