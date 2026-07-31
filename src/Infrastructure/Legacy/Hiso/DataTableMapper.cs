using System.Data;
using System.Reflection;

namespace HekCoreApi.Infrastructure.Legacy.Hiso;

/// <summary>
/// Minimal re-implementation of the legacy `Utitlity.ToGenList&lt;T&gt;` helper referenced (but not
/// supplied) by `DBMessages.cs` - binds each DataRow to a new T by matching column names to public
/// settable property names, case-insensitive. Fresh code, not a port of unseen source.
///
/// String columns holding DBNull are set to `""`, not skipped/left null (2026-07-31 fix) - confirmed
/// live against the real legacy server (`http://localhost:2345/API/GetDemographics`) that it sends
/// `"dayPhone":""`/`"endEnrolmentDate":""` for these, not `null`. Traced why: legacy's own equivalent
/// reflection mapper (`Utility.DataTableToList&lt;T&gt;`, `legacy-reference/.../Logger/Utility.cs:403`)
/// calls `Convert.ChangeType(row[prop.Name], prop.PropertyType)` inside a try/catch that silently
/// skips the property on any exception - `Convert.ChangeType(DBNull.Value, typeof(string))` does NOT
/// throw (unlike int/DateTime/bool, where it does): `DBNull` implements `IConvertible.ToString()` as
/// `string.Empty`, so `Convert.ChangeType` succeeds and legacy ends up assigning `""`. Non-string
/// (value-typed) properties keep the previous DBNull-skip behavior (matches legacy's real throw-and-
/// skip path for those types, leaving them at their type's default).
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
                if (!properties.TryGetValue(column.ColumnName, out var property))
                {
                    continue;
                }

                if (row[column] == DBNull.Value)
                {
                    if (property.PropertyType == typeof(string))
                    {
                        property.SetValue(item, string.Empty);
                    }

                    continue;
                }

                property.SetValue(item, Convert.ChangeType(row[column], property.PropertyType));
            }

            result.Add(item);
        }

        return result;
    }
}
