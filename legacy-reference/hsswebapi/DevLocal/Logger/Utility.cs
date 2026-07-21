using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Data;
using System.Reflection;
using System.Text.RegularExpressions;
using System.IO.Compression;
using System.Collections;
using System.Globalization;

namespace Logger
{
    public class Utility
    {
        public static Utility instance = null;

        public static Utility Instance
        {
            get
            {
                if (instance == null)
                    instance = new Utility();

                return instance;
            }
        }

        public Utility()
        {

        }

        public string ToString(object value)
        {
            try
            {
                return Convert.ToString(value);
            }
            catch
            {
                return string.Empty;
            }
        }

        public int ToInt32(object value)
        {
            try
            {
                return Convert.ToInt32(value);
            }
            catch
            {
                return -1;
            }
        }

        public Int64 ToInt64(object value)
        {
            try
            {
                return Convert.ToInt64(value);
            }
            catch
            {
                return -1;
            }
        }

        public double ToDouble(object value)
        {
            try
            {
                return Convert.ToDouble(value);
            }
            catch
            {
                return 0;
            }
        }

        public bool ToBoolean(object value)
        {
            try
            {
                return Convert.ToBoolean(value);
            }
            catch
            {
                return false;
            }
        }

        public bool ValidateDateTime(object value)
        {
            try
            {
                Convert.ToDateTime(value);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public string ReplaceWithIgnoreCase(string original, string replaceWord, string replaceWith)
        {
            string result = string.Empty;

            try
            {
                result = Regex.Replace(original, replaceWord, replaceWith, RegexOptions.IgnoreCase);
            }
            catch { }

            return result;
        }

        public string GetBetweenString(string startCharacter, string endCharacter, string stringValue, bool randomize)
        {
            string returnValue = string.Empty;

            if (stringValue.Contains(startCharacter)
                && stringValue.Contains(endCharacter))
            {
                returnValue = stringValue.Substring((stringValue.IndexOf(startCharacter) + startCharacter.Length),
                                                    (stringValue.IndexOf(endCharacter) - stringValue.IndexOf(startCharacter) - startCharacter.Length));
            }

            if (string.IsNullOrEmpty(stringValue) && randomize)
                returnValue = RandomDigits(15);

            return returnValue;
        }

        public string HtmlDecode(string webUtility)
        {
            try
            {
                if (!string.IsNullOrEmpty(webUtility))
                {
                    webUtility = WebUtility.HtmlDecode(webUtility);
                }
            }
            catch (Exception)
            {

                throw;
            }

            return webUtility;
        }

        public DataTable ToDataTable<T>(List<T> items)
        {
            DataTable dtResult = new DataTable(typeof(T).Name);

            PropertyInfo[] props = typeof(T).GetProperties();

            foreach (PropertyInfo prop in props)
            {
                var type = (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>)
                            ? Nullable.GetUnderlyingType(prop.PropertyType) : prop.PropertyType);

                dtResult.Columns.Add(prop.Name, type);
            }

            foreach (T item in items)
            {
                DataRow row = dtResult.NewRow();

                foreach (PropertyInfo prop in props)
                    row[prop.Name] = prop.GetValue(item) ?? DBNull.Value;

                dtResult.Rows.Add(row);
            }

            return dtResult;
        }

        public string RandomDigits(int length)
        {
            var random = new Random();
            string value = string.Empty;

            for (int i = 0; i < length; i++)
                value = string.Concat(value, random.Next(1, 9).ToString());

            return value;
        }

        /// <summary>
        /// Parse the input string by placing a space between character case changes in the string
        /// </summary>
        /// <param name="strInput">The string to parse</param>
        /// <returns>The altered string</returns>
        public string ParseByCase(string strInput)
        {
            // The altered string (with spaces between the case changes)
            string strOutput = "";

            // The index of the current character in the input string
            int intCurrentCharPos = 0;

            // The index of the last character in the input string
            int intLastCharPos = strInput.Length - 1;

            // for every character in the input string
            for (intCurrentCharPos = 0; intCurrentCharPos <= intLastCharPos; intCurrentCharPos++)
            {
                // Get the current character from the input string
                char chrCurrentInputChar = strInput[intCurrentCharPos];

                // At first, set previous character to the current character in the input string
                char chrPreviousInputChar = chrCurrentInputChar;

                // If this is not the first character in the input string
                if (intCurrentCharPos > 0)
                {
                    // Get the previous character from the input string
                    chrPreviousInputChar = strInput[intCurrentCharPos - 1];

                } // end if

                // Put a space before each upper case character if the previous character is lower case
                if (char.IsUpper(chrCurrentInputChar) == true && char.IsLower(chrPreviousInputChar) == true)
                {
                    // Add a space to the output string
                    strOutput += " ";

                } // end if

                // Add the character from the input string to the output string
                strOutput += chrCurrentInputChar;

            } // next

            // Return the altered string
            return strOutput;

        }

        public bool IsNumeric(string parameter)
        {
            int n;
            bool isNumeric = int.TryParse(parameter, out n);
            return isNumeric;
        }

        public bool ExtractFirstInteger(string value, out int integerValue)
        {
            integerValue = 0;

            if (value.Any(c => char.IsDigit(c)))
                integerValue = (int)Regex.Split(value, @"\D+").Where(x => x.Length > 0).Select(int.Parse).ToArray().GetValue(0);

            return integerValue > 0;
        }

        public bool FileExistsRecursive(string rootPath, string filename)
        {
            if (File.Exists(Path.Combine(rootPath, filename)))
                return true;

            foreach (string subDir in Directory.GetDirectories(rootPath))
            {
                return FileExistsRecursive(subDir, filename);
            }

            return false;
        }

        public string RemoveReservedChar(string fileName)
        {
            string returnFileName = string.Empty;

            if (!string.IsNullOrEmpty(fileName))
            {
                returnFileName = fileName.Replace("<", string.Empty)
                                .Replace(">", string.Empty)
                                .Replace(":", string.Empty)
                                .Replace("\"", string.Empty)
                                .Replace("/", string.Empty)
                                .Replace("\\", string.Empty)
                                .Replace("|", string.Empty)
                                .Replace("?", string.Empty)
                                .Replace("*", string.Empty);
            }

            return returnFileName;
        }

        public int GetNthOccurence(string message, char charValue, int index)
        {
            return message.TakeWhile(c => (index -= (c == charValue ? 1 : 0)) > 0).Count();
        }

        public byte[] CompressGZip(string input)
        {
            Encoding encoding = Encoding.Unicode;
            byte[] bytes = encoding.GetBytes(input);
            using (MemoryStream stream = new MemoryStream())
            {
                using (GZipStream zipStream = new GZipStream(stream, CompressionMode.Compress))
                {
                    zipStream.Write(bytes, 0, bytes.Length);
                    return stream.ToArray();
                }
            }
        }

        public string DecompressGZip(byte[] bytesToDecompress)
        {
            using (GZipStream stream = new GZipStream(new MemoryStream(bytesToDecompress), CompressionMode.Decompress))
            {
                const int size = 4096;
                byte[] buffer = new byte[size];
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    int count;
                    do
                    {
                        count = stream.Read(buffer, 0, size);
                        if (count > 0)
                        {
                            memoryStream.Write(buffer, 0, count);
                        }
                    } while (count > 0);

                    return Encoding.UTF8.GetString(memoryStream.ToArray());
                }
            }
        }

        public bool IsList(object o)
        {
            try
            {
                if (o == null) return false;
                return o is IList &&
                       o.GetType().IsGenericType &&
                       o.GetType().GetGenericTypeDefinition().IsAssignableFrom(typeof(List<>));
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public string RemoveBetween(string wholeString, string beginString, string endString)
        {
            Regex regex = new Regex(string.Format("\\{0}.*?\\{1}", beginString, endString));
            return regex.Replace(wholeString, string.Empty);
        }

        public DateTime? GetDateTimeOfMessage(string dateTimeValue)
        {
            DateTime? dtMessageDateTime = null;//DateTime.Now;

            dateTimeValue = dateTimeValue.Replace(" ", string.Empty).Replace("/", string.Empty);
            if (!string.IsNullOrEmpty(dateTimeValue))
            {
                try
                {
                    if (dateTimeValue.Length.Equals(8))
                        dtMessageDateTime = DateTime.ParseExact(dateTimeValue, "ddMMyyyy", CultureInfo.InvariantCulture);
                    else if (dateTimeValue.Length.Equals(10))
                        dtMessageDateTime = DateTime.ParseExact(dateTimeValue, "ddMMyyyyHH", CultureInfo.InvariantCulture);
                    else if (dateTimeValue.Length.Equals(12))
                        dtMessageDateTime = DateTime.ParseExact(dateTimeValue, "ddMMyyyyHHmm", CultureInfo.InvariantCulture);
                    else if (dateTimeValue.Length.Equals(14))
                        dtMessageDateTime = DateTime.ParseExact(dateTimeValue, "ddMMyyyyHHmmss", CultureInfo.InvariantCulture);
                }
                catch { }
            }

            return dtMessageDateTime;
        }

        public string ConvertSymbols(string htmlTemplateContents)
        {
            return htmlTemplateContents.Replace(@"\H\", string.Empty)
                                        .Replace(@"\N\", string.Empty)
                                        .Replace(@"\F\", string.Empty)
                                        .Replace(@"\S\", "S")
                                        .Replace(@"\T\", "T")
                                        .Replace(@"\R\", "R")
                                        .Replace(@"\E\", @"E")
                                        .Replace(@"\.br\", @"<br\>")
                                        .Replace(@".br", @"<br\>");
        }

        /// <summary>
        /// Converts a DataTable to a list with generic objects
        /// </summary>
        /// <typeparam name="T">Generic object</typeparam>
        /// <param name="table">DataTable</param>
        /// <returns>List with generic objects</returns>
        public List<T> DataTableToList<T>(DataTable table) where T : class, new()
        {
            try
            {
                List<T> list = new List<T>();

                foreach (var row in table.AsEnumerable())
                {
                    T obj = new T();

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
        public List<T> DataTableToListHiso<T>(DataTable table) where T : class, new()
        {
            try
            {
                List<T> list = new List<T>();
                Type typeMain = typeof(T);

                foreach (var row in table.AsEnumerable())
                {
                    var mainInstance = Activator.CreateInstance(typeMain);
                    foreach (var prop in mainInstance.GetType().GetProperties())
                    {
                        try
                        {
                            string conceptId = string.Empty;
                            string text = string.Empty;
                            string name = string.Empty;
                            string qualifierId = string.Empty;
                            string qualifierName = string.Empty;
                            string dateTaken = string.Empty;
                            string[] retVal = row.Field<string>(prop.Name).Split(new string[] { "|&|" }, StringSplitOptions.RemoveEmptyEntries);

                            if (retVal.Length > 0)
                            {
                                conceptId = retVal[0];
                                text = retVal.Length > 1 ? retVal[1] : text;

                                if (text.Contains("|?|"))
                                {
                                    string[] innerVal = text.Split(new string[] { "|?|" }, StringSplitOptions.RemoveEmptyEntries);
                                    text = Utility.Instance.ToString(innerVal[0]);

                                    try
                                    {
                                        name = Utility.Instance.ToString(innerVal[1]);
                                        qualifierId = Utility.Instance.ToString(innerVal[2]);
                                        qualifierName = Utility.Instance.ToString(innerVal[3]);
                                        dateTaken = Utility.Instance.ToString(innerVal[4]);
                                    }
                                    catch (IndexOutOfRangeException)
                                    { }
                                }
                            }

                            if (prop.Name.Equals("ReferenceId", StringComparison.OrdinalIgnoreCase))
                            {
                                typeMain.GetProperty("ReferenceId").SetValue(mainInstance, text);
                                typeMain.GetProperty("ConceptId").SetValue(mainInstance, conceptId);
                            }
                            else
                            {
                                Type nested = prop.PropertyType;
                                var nestedProperty = Activator.CreateInstance(nested);
                                nested.GetProperty("ConceptID").SetValue(nestedProperty, conceptId);

                                if (!string.IsNullOrWhiteSpace(text))
                                    nested.GetProperty("Text").SetValue(nestedProperty, text);

                                if (!string.IsNullOrWhiteSpace(name))
                                    nested.GetProperty("Name").SetValue(nestedProperty, name);

                                if (!string.IsNullOrWhiteSpace(qualifierId))
                                    nested.GetProperty("QualifierID").SetValue(nestedProperty, qualifierId);

                                if (!string.IsNullOrWhiteSpace(qualifierName))
                                    nested.GetProperty("QualifierName").SetValue(nestedProperty, qualifierName);

                                if (!string.IsNullOrWhiteSpace(dateTaken))
                                    nested.GetProperty("DateTaken").SetValue(nestedProperty, dateTaken);

                                typeMain.GetProperty(prop.Name).SetValue(mainInstance, nestedProperty);
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
        public string ConvertString2RTF(string input, bool toBase64)
        {
            //first take care of special RTF chars
            string sResult = WebUtility.HtmlDecode(input);
            StringBuilder backslashed = new StringBuilder(sResult);
            backslashed.Replace(@"\", @"\\");
            backslashed.Replace(@"{", @"\{");
            backslashed.Replace(@"}", @"\}");
            backslashed.Replace("|br|", Environment.NewLine + Environment.NewLine);
            backslashed.Replace("|t|", "\t");
            backslashed.Replace("<br/>", Environment.NewLine);
            
            //then convert the string char by char
            StringBuilder sb = new StringBuilder();
            foreach (char character in backslashed.ToString())
            {
                if (character <= 0x7f)
                    sb.Append(character);
                else
                    sb.Append("\\u" + Convert.ToUInt32(character) + "?");
            }
            return toBase64 ? EncodeTo64(sb.ToString()) : sb.ToString();
        }
        public string EncodeTo64(string toEncode)
        {
            byte[] toEncodeAsBytes = System.Text.ASCIIEncoding.ASCII.GetBytes(toEncode);
            string returnValue = System.Convert.ToBase64String(toEncodeAsBytes);
            return returnValue;
        }
        public string DecodeFrom64(string encodedData)
        {
            byte[] encodedDataAsBytes = System.Convert.FromBase64String(encodedData);
            string returnValue = System.Text.ASCIIEncoding.ASCII.GetString(encodedDataAsBytes);
            return returnValue;
        }
    }
}
