using Hiso.ConceptMapper;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web;
using System.Xml;

namespace Hiso
{
    public static class Utitlity
    {
        public static bool IsNumeric(string input)
        {
            int result;
            return int.TryParse(input, out result);
        }
        public static string[] GetColumnNameByTableName(string tableName)
        {
            string colNames = ConfigurationManager.AppSettings[tableName];
            string coilNames = ConfigurationManager.AppSettings[tableName];
            string[] strArray = colNames.Split(',');
            return strArray;
        }

        public static DataTable ToDataTable<T>(List<T> items)
        {
            DataTable dataTable = new DataTable(typeof(T).Name);

            //Get all the properties
            PropertyInfo[] Props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (PropertyInfo prop in Props)
            {
                //Setting column names as Property names
                dataTable.Columns.Add(prop.Name, prop.PropertyType);
            }
            foreach (T item in items)
            {
                var values = new object[Props.Length];
                for (int i = 0; i < Props.Length; i++)
                {
                    //inserting property values to datatable rows
                    values[i] = Props[i].GetValue(item, null);
                }
                dataTable.Rows.Add(values);
            }
            return dataTable;
        }

        public static List<TSource> ToGenList<TSource>(DataTable dataTable) where TSource : new()
        {
            var dataList = new List<TSource>();

            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic;
            var objFieldNames = (from PropertyInfo aProp in typeof(TSource).GetProperties(flags)
                                 select new
                                 {
                                     Name = aProp.Name//,
                                     //Type = Nullable.GetUnderlyingType(aProp.PropertyType) ??aProp.PropertyType
                                 }).ToList();
            var dataTblFieldNames = (from DataColumn aHeader in dataTable.Columns
                                     select new
                                     {
                                         Name = aHeader.ColumnName//,
                                         //Type = aHeader.DataType
                                     }).ToList();

            var commonFields = objFieldNames.Intersect(dataTblFieldNames).ToList();

            foreach (DataRow dataRow in dataTable.AsEnumerable().ToList())
            {
                var aTSource = new TSource();
                foreach (var aField in commonFields)
                {
                    PropertyInfo propertyInfos = aTSource.GetType().GetProperty(aField.Name);

                    var value = (dataRow[aField.Name] == DBNull.Value) ? null : dataRow[aField.Name]; //if database field is nullable
                    if (value != null)
                        value = Convert.ChangeType(value, propertyInfos.PropertyType);

                    propertyInfos.SetValue(aTSource, value, null);
                }
                dataList.Add(aTSource);
            }
            return dataList;
        }

        public static string ConvertStringToBase64(this string Text)
        {
            return System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(Text));
        }

        public static string ConvertByteToBase64(this byte[] bt)
        {
            return System.Convert.ToBase64String(bt);
        }
    }
}