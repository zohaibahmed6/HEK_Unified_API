namespace HekCoreApi.Application.Common.Interfaces;

/// <summary>
/// Real byte-level attachment conversion confirmed from
/// legacy-reference/Hiso/ConceptMapper/TypeConverter.cs (`CreatePDFromImage`/`ConvertHTMLToByte`),
/// both backed by Aspose.Words - the same Aspose-licensing gap already flagged for ACC45 form
/// rendering (PROJECT_STATUS.md item 30). Placeholder implementation returns the original bytes
/// unconverted until a license is available - matches legacy's own graceful-degradation behavior on
/// conversion failure (`ConvertHTMLToByte` swallows exceptions and returns the original bytes too).
/// </summary>
public interface IHisoMimeConverter
{
    /// <summary>Legacy: `TypeConverter.CreatePDFromImage` (PNG/BMP source -> PDF bytes via Aspose.Words).</summary>
    Task<byte[]> ConvertImageToPdfAsync(byte[] imageBytes, CancellationToken ct = default);

    /// <summary>Legacy: `TypeConverter.ConvertHTMLToByte` (HTML source -> PDF bytes via Aspose.Words, Letter page size, fixed margins).</summary>
    Task<byte[]> ConvertHtmlToPdfAsync(byte[] htmlBytes, CancellationToken ct = default);
}
