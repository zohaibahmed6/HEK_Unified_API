using System.Net;
using System.Text;

namespace HekCoreApi.Adapters.Erms.Hiso;

/// <summary>
/// Exact port of legacy `Utility.ConvertString2RTF(input, toBase64)` (`Logger/Utility.cs:524`) -
/// used by `GetLaboratoryReportDetails`/`GetRadiologyReportDetails` to RTF-escape the report content
/// and Base64-encode it (legacy always passes `toBase64: true`; the encode uses ASCII, dropping any
/// non-ASCII bytes exactly as legacy's `EncodeTo64` does - after the `\u...?` escaping there are none).
/// </summary>
public static class ErmsRtfConverter
{
    public static string ConvertString2Rtf(string input, bool toBase64)
    {
        var decoded = WebUtility.HtmlDecode(input);
        var backslashed = new StringBuilder(decoded);
        backslashed.Replace(@"\", @"\\");
        backslashed.Replace(@"{", @"\{");
        backslashed.Replace(@"}", @"\}");
        backslashed.Replace("|br|", Environment.NewLine + Environment.NewLine);
        backslashed.Replace("|t|", "\t");
        backslashed.Replace("<br/>", Environment.NewLine);

        var sb = new StringBuilder();
        foreach (var character in backslashed.ToString())
        {
            if (character <= 0x7f)
            {
                sb.Append(character);
            }
            else
            {
                sb.Append("\\u" + Convert.ToUInt32(character) + "?");
            }
        }

        return toBase64 ? Convert.ToBase64String(Encoding.ASCII.GetBytes(sb.ToString())) : sb.ToString();
    }
}
