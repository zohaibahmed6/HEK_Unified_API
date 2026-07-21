using HekCoreApi.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace HekCoreApi.Infrastructure.Legacy.Hiso;

/// <summary>Default registration until real DMS Proxy access is available - always returns null, matching a graceful "no view content" outcome rather than throwing.</summary>
public sealed class NotConfiguredDmsProxyService : IDmsProxyService
{
    private readonly ILogger<NotConfiguredDmsProxyService> _logger;

    public NotConfiguredDmsProxyService(ILogger<NotConfiguredDmsProxyService> logger) => _logger = logger;

    public Task<byte[]?> GetDocumentDataAsync(Guid documentGuid, CancellationToken ct = default)
    {
        _logger.LogWarning("DMS Proxy document retrieval requested but no real DMS Proxy access is configured - returning null.");
        return Task.FromResult<byte[]?>(null);
    }
}
