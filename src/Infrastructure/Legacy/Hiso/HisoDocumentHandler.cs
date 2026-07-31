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
                _logger.LogWarning("{System} DMS connection string not configured (Hiso:DmsConnectionString) - document not saved, returning generated GUID anyway (matches legacy's own swallow-and-continue behavior).", "hiso");
                return guid;
            }

            var extension = GetFileExtension(viewType);
            var documentTypeSecretKey = extension == ".pdf" ? "Hiso:DmsPdfTypeId" : "Hiso:DmsHtmlTypeId";
            var documentTypeId = await _secretProvider.GetSecretAsync(documentTypeSecretKey, ct);
            var contentBytes = extension == ".pdf" ? Convert.FromBase64String(view) : Encoding.UTF8.GetBytes(view);

            // Every parameter here is explicitly typed to match `uspDocumentSave`'s real declared
            // signature exactly (int/nvarchar/bigint) - confirmed 2026-07-31 this matters for real:
            // several of these (documentTypeId as a string from config, contentBytes.Length as Int32
            // for a BIGINT column, practiceId as a plain string for an int column, 0 as Int32 for an
            // nvarchar(500) column) were silently causing SQL Server's RPC parameter binding to reject
            // the call with "expects parameter 'X', which was not supplied" - even though the
            // parameter genuinely was in the collection - because implicit type coercion that works
            // fine for a literal inline `EXEC` batch does not apply the same way to ADO.NET's
            // parameterized RPC call. This silently swallowed exception (see catch block below) is why
            // `saveContainer` looked successful (200, `response=true`) while never actually writing a
            // document - confirmed by comparing against real legacy's own successful save and a raw
            // `sqlcmd` reproduction of the same call.
            var parameters = new List<SqlParameter>
            {
                new("@pDocumentID", SqlDbType.Int) { Value = 0 },
                new("@pClientID", SqlDbType.Int) { Value = 3 },
                new("@pCategoryID", SqlDbType.Int) { Value = 3 },
                new("@pDocumentName", SqlDbType.NVarChar, 200) { Value = $"{formEngineId}{extension}" },
                new("@pDocumentTypeID", SqlDbType.Int) { Value = int.TryParse(documentTypeId, out var typeId) ? typeId : (object)DBNull.Value },
                new("@pDescription", SqlDbType.NVarChar, -1) { Value = string.Empty },
                new("@pDocumentKey", SqlDbType.NVarChar, 50) { Value = guid.ToString() },
                new("@pDocumentSize", SqlDbType.BigInt) { Value = (long)contentBytes.Length },
                new("@pProfileID", SqlDbType.NVarChar, 500) { Value = "0" },
                new("@pPracticeID", SqlDbType.Int) { Value = int.Parse(practiceId) },
                new("@pIsSaveOnCloud", SqlDbType.Bit) { Value = false },
                new("@pDocumentData", SqlDbType.Image) { Value = contentBytes }
            };

            var output = new SqlParameter("@pDocumentIDOut", SqlDbType.Int) { Direction = ParameterDirection.Output, Value = -1 };
            parameters.Add(output);

            await LegacyDbExecutor.ExecuteNonQueryAsync(connectionString, CommandType.StoredProcedure, "[dbo].[uspDocumentSave]", parameters, ct);
        }
        catch (Exception ex)
        {
            // Legacy: exceptions here are logged only, never propagated - always returns the pre-generated GUID.
            _logger.LogError(ex, "{System} AddDocument failed - swallowed, matching legacy behavior.", "hiso");
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
