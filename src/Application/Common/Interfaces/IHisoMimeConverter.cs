namespace HekCoreApi.Application.Common.Interfaces;

/// <summary>
/// Real byte-level attachment conversion confirmed from
/// legacy-reference/Hiso/ConceptMapper/TypeConverter.cs (`CreatePDFromImage`/`ConvertHTMLToByte`),
/// both backed by the real, licensed `Aspose.Words.dll` (`AsposeMimeConverter`, PROJECT_STATUS.md
/// item 30 resolved 2026-07-26).
/// </summary>
public interface IHisoMimeConverter
{
    /// <summary>Legacy: `TypeConverter.CreatePDFromImage` (PNG/BMP source -> PDF bytes via Aspose.Words).</summary>
    Task<byte[]> ConvertImageToPdfAsync(byte[] imageBytes, CancellationToken ct = default);

    /// <summary>Legacy: `TypeConverter.ConvertHTMLToByte` (HTML source -> PDF bytes via Aspose.Words, Letter page size, fixed margins).</summary>
    Task<byte[]> ConvertHtmlToPdfAsync(byte[] htmlBytes, CancellationToken ct = default);
}
