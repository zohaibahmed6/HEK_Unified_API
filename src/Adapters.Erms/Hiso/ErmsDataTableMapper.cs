using System.Data;

namespace HekCoreApi.Adapters.Erms.Hiso;

/// <summary>
/// Exact port of ERMS's `ERMSDataTableToListHiso&lt;T&gt;` (`APIController.cs:1759-1856`), quirks included:
/// per-cell `"|&amp;|"` split into (conceptId, text), `"|?|"` inner split into
/// (text, name, qualifierId, qualifierName, dateTaken), `ReferenceId`/`ConceptID` property special
/// cases, silent per-property `continue` on any error (so a wrapper missing e.g. a `Name` property
/// simply keeps its default instance), and returning <b>null</b> - not an empty list - on any
/// table-level exception (callers then NRE on `.Count` exactly as legacy does).
/// </summary>
public static class ErmsDataTableMapper
{
    public static List<T>? ToListHiso<T>(DataTable table) where T : class, new()
    {
        try
        {
            var list = new List<T>();
            var typeMain = typeof(T);

            foreach (var row in table.AsEnumerable())
            {
                var mainInstance = Activator.CreateInstance(typeMain)!;
                foreach (var prop in mainInstance.GetType().GetProperties())
                {
                    try
                    {
                        var conceptId = string.Empty;
                        var text = string.Empty;
                        var name = string.Empty;
                        var qualifierId = string.Empty;
                        var qualifierName = string.Empty;
                        var dateTaken = string.Empty;
                        if (!table.Columns.Contains(prop.Name))
                        {
                            continue;
                        }

                        if (string.IsNullOrEmpty(row.Field<string>(prop.Name)))
                        {
                            continue;
                        }

                        var retVal = row.Field<string>(prop.Name)!.Split(new[] { "|&|" }, StringSplitOptions.RemoveEmptyEntries);

                        if (retVal.Length > 0)
                        {
                            conceptId = retVal[0];
                            text = retVal.Length > 1 ? retVal[1] : text;

                            if (text.Contains("|?|"))
                            {
                                var innerVal = text.Split(new[] { "|?|" }, StringSplitOptions.RemoveEmptyEntries);
                                text = innerVal[0] ?? string.Empty;

                                try
                                {
                                    name = innerVal[1] ?? string.Empty;
                                    qualifierId = innerVal[2] ?? string.Empty;
                                    qualifierName = innerVal[3] ?? string.Empty;
                                    dateTaken = innerVal[4] ?? string.Empty;
                                }
                                catch (IndexOutOfRangeException)
                                {
                                }
                            }
                        }

                        if (prop.Name.Equals("ReferenceId", StringComparison.OrdinalIgnoreCase))
                        {
                            typeMain.GetProperty("ReferenceId")!.SetValue(mainInstance, text);
                            typeMain.GetProperty("ConceptID")!.SetValue(mainInstance, conceptId);
                        }
                        else if (prop.Name.Equals("ConceptID", StringComparison.OrdinalIgnoreCase))
                        {
                            typeMain.GetProperty("ConceptID")!.SetValue(mainInstance, conceptId);
                        }
                        else
                        {
                            var nested = prop.PropertyType;
                            var nestedProperty = Activator.CreateInstance(nested)!;
                            nested.GetProperty("ConceptID")!.SetValue(nestedProperty, conceptId);

                            if (!string.IsNullOrWhiteSpace(text))
                            {
                                nested.GetProperty("Text")!.SetValue(nestedProperty, text);
                            }

                            if (!string.IsNullOrWhiteSpace(name))
                            {
                                nested.GetProperty("Name")!.SetValue(nestedProperty, name);
                            }

                            if (!string.IsNullOrWhiteSpace(qualifierId))
                            {
                                nested.GetProperty("QualifierID")!.SetValue(nestedProperty, qualifierId);
                            }

                            if (!string.IsNullOrWhiteSpace(qualifierName))
                            {
                                nested.GetProperty("QualifierName")!.SetValue(nestedProperty, qualifierName);
                            }

                            if (!string.IsNullOrWhiteSpace(dateTaken))
                            {
                                nested.GetProperty("DateTaken")!.SetValue(nestedProperty, dateTaken);
                            }

                            typeMain.GetProperty(prop.Name)!.SetValue(mainInstance, nestedProperty);
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }

                list.Add((T)mainInstance);
            }

            return list;
        }
        catch
        {
            return null;
        }
    }
}
