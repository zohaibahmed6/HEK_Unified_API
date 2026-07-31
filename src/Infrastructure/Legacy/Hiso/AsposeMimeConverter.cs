using System.Text;
using Aspose.Words;
using HekCoreApi.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace HekCoreApi.Infrastructure.Legacy.Hiso;

/// <summary>
/// Real implementation, ported from `legacy-reference/Hiso/ConceptMapper/TypeConverter.cs`
/// (`CreatePDFromImage`/`ConvertHTMLToByte`) - both backed by the real, licensed `Aspose.Words.dll`
/// Zohaib supplied (`Legacy/Hiso/vendor/Aspose.Words.dll` + `.lic`, PROJECT_STATUS.md item 30
/// resolved 2026-07-26; the earlier "no license available" framing was wrong - the license and
/// assembly were already vendored for AWSDocCore's sibling gap, just never checked).
/// License is process-global static state inside the DLL (same pattern as AWSDocCore's
/// <c>DocumentManager.publicKey</c>) - set once via a static constructor rather than per-call.
/// </summary>
public sealed class AsposeMimeConverter : IHisoMimeConverter
{
    private static readonly Lazy<bool> LicenseLoaded = new(() =>
    {
        // Confirmed via a scratch probe (2026-07-26): Aspose.Words.dll (a .NET Framework 4.0
        // assembly) throws NotSupportedException constructing any Document unless the legacy
        // Windows-1252 codepage is registered first - .NET Core no longer registers it by default.
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        var licensePath = Path.Combine(AppContext.BaseDirectory, "Aspose.Words.lic");
        new License().SetLicense(licensePath);
        return true;
    });

    private readonly ILogger<AsposeMimeConverter> _logger;

    public AsposeMimeConverter(ILogger<AsposeMimeConverter> logger)
    {
        _logger = logger;
        _ = LicenseLoaded.Value;
    }

    /// <summary>
    /// Legacy `TypeConverter.CreatePDFromImage`: insert the image into a blank document, save as PDF.
    /// Falls back to returning the original image bytes unconverted on failure (2026-07-30 - matching
    /// `ConvertHtmlToPdfAsync`'s existing graceful-degradation behavior below), rather than throwing -
    /// confirmed live that Aspose's internal image handling (a vendored .NET Framework 4.0 assembly)
    /// hard-depends on `System.Drawing`, which .NET 8 unconditionally blocks on Linux regardless of
    /// `libgdiplus`/config switches. An unconverted image is still real, viewable attachment content -
    /// far better than an empty field or a crashed request.
    /// </summary>
    public Task<byte[]> ConvertImageToPdfAsync(byte[] imageBytes, CancellationToken ct = default)
    {
        try
        {
            var doc = new Document();
            var builder = new DocumentBuilder(doc);
            builder.MoveToDocumentEnd();
            builder.InsertImage(imageBytes);
            using var ms = new MemoryStream();
            doc.Save(ms, SaveFormat.Pdf);
            _logger.LogInformation("HISO image-to-PDF conversion succeeded via Aspose.Words.");
            return Task.FromResult(ms.ToArray());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "{System} image-to-PDF conversion failed - returning original image bytes unconverted, matching ConvertHtmlToPdfAsync's graceful-degradation behavior.", "hiso");
            return Task.FromResult(imageBytes);
        }
    }

    /// <summary>Legacy `TypeConverter.ConvertHTMLToByte`: load HTML into Aspose.Words, apply Letter page size + fixed margins, save as PDF. Legacy swallows conversion exceptions and returns the original bytes - reproduced as-is.</summary>
    public Task<byte[]> ConvertHtmlToPdfAsync(byte[] htmlBytes, CancellationToken ct = default)
    {
        try
        {
            using var stream = new MemoryStream(htmlBytes);
            var doc = new Document(stream);
            foreach (Section section in doc)
            {
                section.PageSetup.PaperSize = PaperSize.Letter;
                section.PageSetup.RightMargin = 20;
                section.PageSetup.LeftMargin = 10;
                section.PageSetup.TopMargin = 10;
                section.PageSetup.BottomMargin = 10;
            }

            using var ms = new MemoryStream();
            doc.Save(ms, SaveFormat.Pdf);
            _logger.LogInformation("HISO HTML-to-PDF conversion succeeded via Aspose.Words.");
            return Task.FromResult(ms.ToArray());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "{System} HTML-to-PDF conversion failed - returning original bytes unconverted, matching legacy's graceful-degradation behavior.", "hiso");
            return Task.FromResult(htmlBytes);
        }
    }
}
