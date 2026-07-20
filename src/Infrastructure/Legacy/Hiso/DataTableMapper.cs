using System.Data;
using System.Reflection;

namespace HekCoreApi.Infrastructure.Legacy.Hiso;

/// <summary>
/// Minimal re-implementation of the legacy `Utitlity.ToGenList&lt;T&gt;` helper referenced (but not
/// supplied) by `DBMessages.cs` - binds each DataRow to a new T by matching column names to public
/// settable property names, case-insensitive. Fresh code, not a port of unseen source.
/// </summary>
public static class DataTableMapper
{
    public static List<T> ToList<T>(DataTable table) where T : new()
    {
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite)
            .ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

        var result = new List<T>();
        foreach (DataRow row in table.Rows)
        {
            var item = new T();
            foreach (DataColumn column in table.Columns)
            {
                if (properties.TryGetValue(column.ColumnName, out var property) && row[column] != DBNull.Value)
                {
                    property.SetValue(item, Convert.ChangeType(row[column], property.PropertyType));
                }
            }

            result.Add(item);
        }

        return result;
    }
}
