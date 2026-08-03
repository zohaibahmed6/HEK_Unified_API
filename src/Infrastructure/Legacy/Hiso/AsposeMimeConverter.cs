using System.Text;
using Aspose.Words;
using HekCoreApi.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;

namespace HekCoreApi.Infrastructure.Legacy.Hiso;

/// <summary>
/// Real implementation, ported from `legacy-reference/Hiso/ConceptMapper/TypeConverter.cs`
/// (`CreatePDFromImage`/`ConvertHTMLToByte`). Both conversions originally ran through the vendored,
/// licensed net40 `Aspose.Words.dll` (PROJECT_STATUS.md item 30, resolved 2026-07-26) - that DLL's
/// license failed to load under the .NET 10 migration (root cause unconfirmed, inside Aspose's own
/// closed-source code - see MIGRATION_STATUS.md). Resolution (2026-08-03):
/// - Image-&gt;PDF moved to PdfSharp + SixLabors.ImageSharp permanently (cross-platform, no
///   licensing concern at all, no watermark) - reuses the pattern already proven in this codebase's
///   dormant `DmsDocumentService.ConvertTiffOrBitmapToPdfAsync`.
/// - HTML-&gt;PDF moved to the official `Aspose.Words` NuGet package (not the old vendored net40
///   DLL) - confirmed via a scratch probe to build/run cleanly under net10, unlike the old DLL.
///   The old `.lic` file (expiry 2017-04-20) is rejected by this newer Aspose.Words build
///   ("allows free upgrades until 20 Apr 2017, but this version was released 01 Nov 2024" -
///   confirmed live), so this currently runs unlicensed/evaluation per Zohaib's call (2026-08-03) -
///   every generated PDF carries Aspose's evaluation watermark until a current license is dropped
///   in at `Legacy/Hiso/vendor/Aspose.Words.lic` (no code change needed once that happens).
/// </summary>
public sealed class AsposeMimeConverter : IHisoMimeConverter
{
    // Lazy<Exception?> rather than Lazy<bool>: a factory that throws would otherwise cache and
    // rethrow that exception on every access, which is what crashed the whole class (not just the
    // Aspose-dependent HTML path) before this was refactored - see MIGRATION_STATUS.md. Capturing
    // the failure as data instead means a license problem only ever degrades
    // ConvertHtmlToPdfAsync's own output (unlicensed/watermarked), it can't take down
    // ConvertImageToPdfAsync, which doesn't use Aspose at all.
    private static readonly Lazy<Exception?> LicenseLoadError = new(() =>
    {
        try
        {
            var licensePath = Path.Combine(AppContext.BaseDirectory, "Aspose.Words.lic");
            new License().SetLicense(licensePath);
            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    });

    private readonly ILogger<AsposeMimeConverter> _logger;

    public AsposeMimeConverter(ILogger<AsposeMimeConverter> logger)
    {
        _logger = logger;
        if (LicenseLoadError.Value is { } ex)
        {
            _logger.LogWarning(ex, "Aspose.Words license failed to load - HTML-to-PDF output will carry Aspose's evaluation watermark until a current license is provided.");
        }
    }

    /// <summary>
    /// Legacy `TypeConverter.CreatePDFromImage`, reimplemented on PdfSharp + SixLabors.ImageSharp -
    /// decodes the image with ImageSharp, re-encodes each frame as PNG, and draws it onto a
    /// PdfSharp page the same way the existing (dormant) `DmsDocumentService.ConvertTiffOrBitmapToPdfAsync`
    /// already does for TIFF/BMP - reusing a pattern already proven in this codebase rather than a
    /// new one. Falls back to returning the original image bytes unconverted on failure, matching
    /// legacy's graceful-degradation behavior - an unconverted image is still real, viewable
    /// attachment content, far better than an empty field or a crashed request.
    /// </summary>
    public async Task<byte[]> ConvertImageToPdfAsync(byte[] imageBytes, CancellationToken ct = default)
    {
        try
        {
            using var image = Image.Load(imageBytes);
            using var document = new PdfDocument();
            var page = document.AddPage();
            using var graphics = XGraphics.FromPdfPage(page);

            using var pngStream = new MemoryStream();
            await image.SaveAsync(pngStream, new PngEncoder(), ct);
            pngStream.Position = 0;
            using var xImage = XImage.FromStream(pngStream);

            var margin = XUnit.FromPoint(25);
            graphics.DrawImage(xImage, margin.Point, margin.Point, (page.Width - margin).Point, (page.Height - margin).Point);

            using var pdfStream = new MemoryStream();
            document.Save(pdfStream);
            _logger.LogInformation("HISO image-to-PDF conversion succeeded via PdfSharp/ImageSharp.");
            return pdfStream.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "{System} image-to-PDF conversion failed - returning original image bytes unconverted, matching legacy's graceful-degradation behavior.", "hiso");
            return imageBytes;
        }
    }

    /// <summary>Legacy `TypeConverter.ConvertHTMLToByte`: load HTML into Aspose.Words, apply Letter page size + fixed margins, save as PDF. Legacy swallows conversion exceptions and returns the original bytes - reproduced as-is. Currently unlicensed (see class remarks) - output carries Aspose's evaluation watermark.</summary>
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
            _logger.LogInformation("HISO HTML-to-PDF conversion succeeded via Aspose.Words{LicenseState}.", LicenseLoadError.Value is null ? string.Empty : " (unlicensed/evaluation)");
            return Task.FromResult(ms.ToArray());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "{System} HTML-to-PDF conversion failed - returning original bytes unconverted, matching legacy's graceful-degradation behavior.", "hiso");
            return Task.FromResult(htmlBytes);
        }
    }
}
