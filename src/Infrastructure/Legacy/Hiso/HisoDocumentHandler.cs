using System.Data;
using System.Text;
using HekCoreApi.Application.Common.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace HekCoreApi.Infrastructure.Legacy.Hiso;

/// <summary>Ported from DocumentHandler.cs/Mapper.cs's `SaveDocumentToDMS`. Direct-DB path only (`AddDirectDMS=1`) - real, confirmed exact stored procedure `[dbo].[uspDocumentSave]` against a global DMS connection string.</summary>
public sealed class HisoDocumentHandler : IHisoDocumentHandler
{
    private readonly ISecretProvider _secretProvider;
    private readonly ILogger<HisoDocumentHandler> _logger;

    public HisoDocumentHandler(ISecretProvider secretProvider, ILogger<HisoDocumentHandler> logger)
    {
        _secretProvider = secretProvider;
        _logger = logger;
    }

    public async Task<Guid> AddDocumentAsync(string? view, string viewType, string? formEngineId, string practiceId, CancellationToken ct = default)
    {
        var guid = Guid.NewGuid();
        if (string.IsNullOrEmpty(view))
        {
            return guid;
        }

        try
        {
            var connectionString = await _secretProvider.GetSecretAsync("Hiso:DmsConnectionString", ct);
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                _logger.LogWarning("HISO DMS connection string not configured (Hiso:DmsConnectionString) - document not saved, returning generated GUID anyway (matches legacy's own swallow-and-continue behavior).");
                return guid;
            }

            var extension = GetFileExtension(viewType);
            var documentTypeSecretKey = extension == ".pdf" ? "Hiso:DmsPdfTypeId" : "Hiso:DmsHtmlTypeId";
            var documentTypeId = await _secretProvider.GetSecretAsync(documentTypeSecretKey, ct);
            var contentBytes = extension == ".pdf" ? Convert.FromBase64String(view) : Encoding.UTF8.GetBytes(view);

            var parameters = new List<SqlParameter>
            {
                new("@pDocumentID", 0),
                new("@pClientID", 3),
                new("@pCategoryID", 3),
                new("@pDocumentName", $"{formEngineId}{extension}"),
                new("@pDocumentTypeID", (object?)documentTypeId ?? DBNull.Value),
                new("@pDescription", string.Empty),
                new("@pDocumentKey", guid.ToString()),
                new("@pDocumentSize", contentBytes.Length),
                new("@pProfileID", 0),
                new("@pPracticeID", practiceId),
                new("@pIsSaveOnCloud", false),
                new("@pDocumentData", contentBytes)
            };

            var output = new SqlParameter("@pDocumentIDOut", SqlDbType.Int) { Direction = ParameterDirection.Output, Value = -1 };
            parameters.Add(output);

            await LegacyDbExecutor.ExecuteNonQueryAsync(connectionString, CommandType.StoredProcedure, "[dbo].[uspDocumentSave]", parameters, ct);
        }
        catch (Exception ex)
        {
            // Legacy: exceptions here are logged only, never propagated - always returns the pre-generated GUID.
            _logger.LogError(ex, "HISO AddDocument failed - swallowed, matching legacy behavior.");
        }

        return guid;
    }

    private static string GetFileExtension(string viewType) => viewType switch
    {
        "text/html" => ".html",
        "application/pdf" => ".pdf",
        "normalizedString" => ".txt",
        _ => ".html"
    };
}
