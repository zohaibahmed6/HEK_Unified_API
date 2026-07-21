using System.Data;

namespace HekCoreApi.Adapters.Erms.Col;

/// <summary>
/// Exact port of legacy `Utility.DataTableToList&lt;T&gt;` (`Logger/Utility.cs:403`) used by every COL
/// read: per-property `Convert.ChangeType(row[prop.Name], prop.PropertyType)` with a silent
/// per-property catch-continue (missing column / DBNull / bad cast just leaves the default), and
/// <b>null</b> - not an empty list - on any table-level exception.
/// </summary>
public static class ColDataTableMapper
{
    public static List<T>? ToList<T>(DataTable table) where T : class, new()
    {
        try
        {
            var list = new List<T>();

            foreach (var row in table.AsEnumerable())
            {
                var obj = new T();
                foreach (var prop in obj.GetType().GetProperties())
                {
                    try
                    {
                        prop.SetValue(obj, Convert.ChangeType(row[prop.Name], prop.PropertyType), null);
                    }
                    catch
                    {
                        continue;
                    }
                }

                list.Add(obj);
            }

            return list;
        }
        catch
        {
            return null;
        }
    }
}
