using HekCoreApi.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace HekCoreApi.Infrastructure.Legacy.Hiso;

/// <summary>
/// // TODO(Aspose): replace with a real Aspose.Words-backed implementation once a valid company
/// license is available (PROJECT_STATUS.md item 30). Do not substitute a different rendering
/// library - keep aligned with what the legacy system did (legacy-reference/Hiso/ConceptMapper/TypeConverter.cs).
/// Until then, returns the original bytes unconverted, matching legacy's own graceful-degradation
/// behavior on conversion failure rather than throwing.
/// </summary>
public sealed class AsposeUnavailableMimeConverter : IHisoMimeConverter
{
    private readonly ILogger<AsposeUnavailableMimeConverter> _logger;

    public AsposeUnavailableMimeConverter(ILogger<AsposeUnavailableMimeConverter> logger)
    {
        _logger = logger;
    }

    public Task<byte[]> ConvertImageToPdfAsync(byte[] imageBytes, CancellationToken ct = default)
    {
        _logger.LogWarning("HISO image-to-PDF conversion requested but Aspose is not licensed in this environment - returning original bytes unconverted.");
        return Task.FromResult(imageBytes);
    }

    public Task<byte[]> ConvertHtmlToPdfAsync(byte[] htmlBytes, CancellationToken ct = default)
    {
        _logger.LogWarning("HISO HTML-to-PDF conversion requested but Aspose is not licensed in this environment - returning original bytes unconverted.");
        return Task.FromResult(htmlBytes);
    }
}
