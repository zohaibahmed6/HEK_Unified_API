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
        // Literal "\r\n", not Environment.NewLine: real legacy always runs on Windows (where
        // Environment.NewLine is "\r\n"), but this port runs in a Linux Docker container, where
        // Environment.NewLine resolves to "\n" - confirmed live 2026-07-31 as the exact cause of a
        // real content mismatch (legacy's decoded content: "\r\n\r\nCBC:..."; ours: "\n\nCBC:...").
        // Using the literal keeps the output correct regardless of which OS this runs on.
        backslashed.Replace("|br|", "\r\n\r\n");
        backslashed.Replace("|t|", "\t");
        backslashed.Replace("<br/>", "\r\n");

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
