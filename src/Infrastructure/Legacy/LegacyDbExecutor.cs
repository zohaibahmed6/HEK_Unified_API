using System.Data;
using Microsoft.Data.SqlClient;

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

        await connection.OpenAsync(ct);
        return await command.ExecuteNonQueryAsync(ct);
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

        await connection.OpenAsync(ct);
        await using var reader = await command.ExecuteReaderAsync(ct);
        var table = new DataTable();
        table.Load(reader);
        return table;
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

        await connection.OpenAsync(ct);
        await using var reader = await command.ExecuteReaderAsync(ct);
        var dataSet = new DataSet();
        do
        {
            var table = new DataTable();
            table.Load(reader);
            dataSet.Tables.Add(table);
        } while (!reader.IsClosed && reader.NextResult());

        return dataSet;
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

        await connection.OpenAsync(ct);
        return await command.ExecuteScalarAsync(ct);
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
}
