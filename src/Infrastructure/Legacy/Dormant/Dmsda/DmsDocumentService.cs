using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Options;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace HekCoreApi.Infrastructure.Legacy.Dormant.Dmsda;

/// <summary>
/// Ported from `legacy-reference/DAL/DMS/DMSDA.cs` (SRS-confirmed SQL-injection module, Phase A
/// hard-blocker item 4 / PROJECT_STATUS.md open item 23). Retained per the stakeholder's "don't
/// remove anything old APIs implemented" principle - registered in DI but not wired to any
/// controller/endpoint (dormant), consistent with every other retained-but-dormant DAL module.
///
/// FIX APPLIED: <see cref="UpdateInboxFolderDocumentsAsync"/> replaces the original's string-
/// concatenated `CommandText` (`"...set DMSID='" + Guid + "' where InboxFolderItemID=" + InboxFolderItemID`)
/// with a fully parameterized command - the confirmed injection this module carried in the legacy
/// codebase. That original method was itself `private` and never called by anything in the
/// supplied source, i.e. dead code within its own class - matching "dormant" at the method level too.
///
/// Everything else in the legacy file already used parameterized `SqlParameter` lists via a
/// `DALHelper` class whose source was not supplied alongside these two files - re-implemented here
/// as <see cref="LegacyDbExecutor"/>, a fresh minimal equivalent, not a port of unseen code.
///
/// KNOWN GAP, FLAGGED: <see cref="ConvertTiffOrBitmapToPdfAsync"/> uses `System.Drawing.Common`,
/// which is Windows-only since .NET 6 (throws PlatformNotSupportedException on Linux). The legacy
/// method's own catch-all fallback (return the original bytes unconverted) already degrades
/// gracefully, so this is not a functional break under Docker/Linux (ADR-005) - just silently
/// reduced capability there. Worth a cross-platform image library decision before this module is
/// ever activated for real traffic.
/// </summary>
public sealed class DmsDocumentService
{
    private readonly ISecretProvider _secretProvider;
    private readonly LegacyDmsOptions _options;

    public DmsDocumentService(ISecretProvider secretProvider, IOptions<LegacyDmsOptions> options)
    {
        _secretProvider = secretProvider;
        _options = options.Value;
    }

    /// <summary>
    /// The confirmed-vulnerable operation, fixed. Originally `private`, string-concatenated, and
    /// unreachable from any call path in the supplied source - kept public here since a fixed,
    /// dormant capability should at least be callable once wired up, per "don't remove capability."
    /// </summary>
    public async Task<int> UpdateInboxFolderDocumentsAsync(string dmsGuid, int inboxFolderItemId, CancellationToken ct = default)
    {
        var connectionString = await GetPmsConnectionStringAsync(ct);

        var parameters = new List<SqlParameter>
        {
            new("@DMSID", dmsGuid),
            new("@InboxFolderItemID", inboxFolderItemId)
        };

        return await LegacyDbExecutor.ExecuteNonQueryAsync(
            connectionString,
            CommandType.Text,
            "UPDATE Prompt.tblInboxFolderItem SET DMSID = @DMSID WHERE InboxFolderItemID = @InboxFolderItemID",
            parameters,
            ct);
    }

    public Task<int> Hl7SaveInboxAsync(Guid guidDms, string nhiNumber, string receivingFacility, string nzMc, CancellationToken ct = default) =>
        Hl7SaveInboxAsync(guidDms, nhiNumber, receivingFacility, nzMc, comments: null, providerFamilyName: null, providerGivenName: null,
            providerMiddleName: null, sendingApplication: null, sendingFacility: null, versionId: null, messageType: null,
            messageControlId: null, patientFamilyName: null, patientGivenName: null, patientMiddleName: null, dob: null,
            inboxFolderId: -1, messageSubject: null, msaMessageControlId: null, inboxItemTypeId: -1, usDescription: null,
            receivingDate: null, gender: null, ct: ct);

    public async Task<int> Hl7SaveInboxAsync(
        Guid guidDms, string nhiNumber, string receivingFacility, string nzMc, string? comments, string? providerFamilyName,
        string? providerGivenName, string? providerMiddleName, string? sendingApplication, string? sendingFacility,
        string? versionId, string? messageType, string? messageControlId, string? patientFamilyName, string? patientGivenName,
        string? patientMiddleName, DateTime? dob, int inboxFolderId, string? messageSubject, string? msaMessageControlId,
        int inboxItemTypeId, string? usDescription, DateTime? receivingDate, string? gender, CancellationToken ct = default)
    {
        var connectionString = await GetPmsConnectionStringAsync(ct);

        var parameters = new List<SqlParameter>
        {
            new("@pNHINumber", nhiNumber),
            new("@pReceivingFacility", receivingFacility),
            new("@pNZMC", nzMc)
        };

        if (guidDms != Guid.Empty) parameters.Add(new SqlParameter("@pDMSID", guidDms.ToString()));
        if (!string.IsNullOrEmpty(comments)) parameters.Add(new SqlParameter("@pComments", comments));
        if (!string.IsNullOrEmpty(providerFamilyName)) parameters.Add(new SqlParameter("@pProviderFamilyName", providerFamilyName));
        if (!string.IsNullOrEmpty(providerGivenName)) parameters.Add(new SqlParameter("@pProviderGivenName", providerGivenName));
        if (!string.IsNullOrEmpty(providerMiddleName)) parameters.Add(new SqlParameter("@pProviderMiddleName", providerMiddleName));
        if (!string.IsNullOrEmpty(sendingApplication)) parameters.Add(new SqlParameter("@pSendingApplication", sendingApplication));
        if (!string.IsNullOrEmpty(sendingFacility)) parameters.Add(new SqlParameter("@pSendingFacility", sendingFacility));
        if (!string.IsNullOrEmpty(versionId)) parameters.Add(new SqlParameter("@pVersionID", versionId));
        if (!string.IsNullOrEmpty(messageType)) parameters.Add(new SqlParameter("@pMessageType", messageType));
        if (!string.IsNullOrEmpty(messageControlId)) parameters.Add(new SqlParameter("@pMessageControlID", messageControlId));
        if (!string.IsNullOrEmpty(patientFamilyName)) parameters.Add(new SqlParameter("@pPatientFamilyName", patientFamilyName));
        if (!string.IsNullOrEmpty(patientGivenName)) parameters.Add(new SqlParameter("@pPatientGivenName", patientGivenName));
        if (!string.IsNullOrEmpty(patientMiddleName)) parameters.Add(new SqlParameter("@pPatientMiddelName", patientMiddleName));
        if (dob is not null) parameters.Add(new SqlParameter("@pDOB", dob));
        if (!string.IsNullOrEmpty(messageSubject)) parameters.Add(new SqlParameter("@pMessageSubject", messageSubject));
        if (inboxFolderId > -1) parameters.Add(new SqlParameter("@pInBoxFolderID", inboxFolderId));
        if (!string.IsNullOrEmpty(msaMessageControlId)) parameters.Add(new SqlParameter("@pMSAMessageControlID", msaMessageControlId));
        if (inboxItemTypeId > -1) parameters.Add(new SqlParameter("@pInboxItemTypeID", inboxItemTypeId));
        if (!string.IsNullOrEmpty(usDescription)) parameters.Add(new SqlParameter("@pUSDescription", usDescription));
        if (receivingDate is not null) parameters.Add(new SqlParameter("@pReceivingDate", receivingDate));
        parameters.Add(!string.IsNullOrWhiteSpace(gender)
            ? new SqlParameter("@pGender", gender)
            : new SqlParameter("@pGender", DBNull.Value));

        var output = new SqlParameter("@pOutputParam", SqlDbType.BigInt) { Direction = ParameterDirection.Output, Value = -1 };
        parameters.Add(output);

        await LegacyDbExecutor.ExecuteNonQueryAsync(connectionString, CommandType.StoredProcedure, "[dbo].[uspHL7SaveInbox]", parameters, ct);
        return int.TryParse(output.Value?.ToString(), out var result) ? result : -1;
    }

    public async Task<int> TaskPathLabInsertAsync(string nhiNumber, string nzMc, string receivingFacility, string? taskSubject, int inboxFolderItemId, CancellationToken ct = default)
    {
        var connectionString = await GetPmsConnectionStringAsync(ct);

        var parameters = new List<SqlParameter>
        {
            new("@pNhiNumber", nhiNumber),
            new("@pEDIAccount", receivingFacility),
            new("@pNZMCNo", nzMc)
        };

        if (!string.IsNullOrEmpty(taskSubject)) parameters.Add(new SqlParameter("@pTaskSubject", taskSubject));
        if (inboxFolderItemId > -1) parameters.Add(new SqlParameter("@pInboxFolderItemID", inboxFolderItemId));

        var output = new SqlParameter("@pOutputParam", SqlDbType.Int) { Direction = ParameterDirection.Output, Value = -1 };
        parameters.Add(output);

        await LegacyDbExecutor.ExecuteNonQueryAsync(connectionString, CommandType.StoredProcedure, "[Task].[uspTaskPathLabInsertUpdate]", parameters, ct);
        return Convert.ToInt32(output.Value);
    }

    public async Task<string> GetOrganizationByEdiAsync(string ediAccount, CancellationToken ct = default)
    {
        var connectionString = await GetPmsConnectionStringAsync(ct);
        var parameters = new List<SqlParameter> { new("@pEDIAccount", ediAccount) };

        var table = await LegacyDbExecutor.ExecuteDataTableAsync(connectionString, CommandType.StoredProcedure, "[Profile].[uspGetOrganizationByEDI]", parameters, ct);
        return table.Rows.Count > 0 ? Convert.ToString(table.Rows[0]["OrgName"]) ?? string.Empty : string.Empty;
    }

    public async Task<int> SaveDmsAsync(int clientId, int categoryId, string documentName, int documentTypeId, string? description, string documentKey, byte[] contentData, CancellationToken ct = default)
    {
        var connectionString = await GetDmsConnectionStringAsync(ct);

        var parameters = new List<SqlParameter>
        {
            new("@pDocumentID", 0),
            new("@pClientID", clientId),
            new("@pCategoryID", categoryId),
            new("@pDocumentName", documentName),
            new("@pDocumentTypeID", documentTypeId),
            new("@pDescription", (object?)description ?? DBNull.Value),
            new("@pDocumentKey", documentKey),
            new("@pDocumentSize", contentData.Length),
            new("@pProfileID", "1"),
            new("@pDocumentData", SqlDbType.VarBinary) { Value = contentData }
        };

        var output = new SqlParameter("@pDocumentIDOut", SqlDbType.Int) { Direction = ParameterDirection.Output, Value = -1 };
        parameters.Add(output);

        await LegacyDbExecutor.ExecuteNonQueryAsync(connectionString, CommandType.StoredProcedure, "[dbo].[uspDocumentSave]", parameters, ct);
        return output.Value is DBNull ? 0 : Convert.ToInt32(output.Value);
    }

    public async Task<Guid> AddDocumentAsync(string extension, string fileName, byte[] docBytes, string? description = null, int categoryId = 12, int clientId = 3, CancellationToken ct = default)
    {
        var guid = Guid.NewGuid();
        var isTiff = extension.EndsWith("tiff", StringComparison.OrdinalIgnoreCase);
        var documentTypeId = GetDocumentTypeId(isTiff ? "pdf" : extension);

        try
        {
            // ConvertTiffOrBitmapToPdfAsync requires System.Drawing.Common, Windows-only since
            // .NET 6 (see class remarks). Guarded here rather than relying solely on the method's
            // internal catch-all, so the platform-compatibility analyzer can verify this call site
            // and so the fallback is explicit, not incidental.
            var contentToSave = isTiff && OperatingSystem.IsWindows()
                ? await ConvertTiffOrBitmapToPdfAsync(docBytes, ct)
                : docBytes;
            var result = await SaveDmsAsync(clientId, categoryId, fileName, documentTypeId, description, guid.ToString(), contentToSave, ct);
            return result <= 0 ? Guid.Empty : guid;
        }
        catch
        {
            return Guid.Empty;
        }
    }

    public Task<Guid> AddDocumentAsync(string base64Content, string extension, string fileName, string? description = null, CancellationToken ct = default)
    {
        var bytes = extension == ".html"
            ? System.Text.Encoding.UTF8.GetBytes(base64Content)
            : Convert.FromBase64String(base64Content);

        return AddDocumentAsync(extension, fileName, bytes, description, ct: ct);
    }

    /// <summary>
    /// Reads from <see cref="LegacyDmsOptions.DocumentTypes"/> (config, IOptions pattern) instead
    /// of the legacy `ConfigurationManager.AppSettings["DMSDocTypes"]` call.
    /// </summary>
    private int GetDocumentTypeId(string extension)
    {
        var documentTypes = _options.DocumentTypes.Split('|', StringSplitOptions.RemoveEmptyEntries);
        foreach (var entry in documentTypes)
        {
            var parts = entry.Split(',');
            if (parts.Length > 1 && parts[1].Contains(extension.Replace(".", string.Empty), StringComparison.OrdinalIgnoreCase))
            {
                return Convert.ToInt16(parts[0]);
            }
        }

        return 0;
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static Task<byte[]> ConvertTiffOrBitmapToPdfAsync(byte[] receivedBytes, CancellationToken ct) =>
        Task.Run(() =>
        {
            try
            {
                using var memoryStream = new MemoryStream(receivedBytes);
                using var image = Image.FromStream(memoryStream, useEmbeddedColorManagement: true, validateImageData: true);
                var frameCount = image.GetFrameCount(FrameDimension.Page);

                // PdfSharp 6.x dropped direct GDI+ (System.Drawing.Image) interop - XImage is now
                // populated from an encoded image stream instead of XImage.FromGdiPlusImage
                // (present in the legacy PdfSharp 1.x API this module originally used). Each frame
                // is re-encoded to PNG via System.Drawing first - same net effect, adapted API.
                using var document = new PdfDocument();
                for (var pageNumber = 0; pageNumber < frameCount; pageNumber++)
                {
                    image.SelectActiveFrame(FrameDimension.Page, pageNumber);
                    var page = document.AddPage();
                    using var graphics = XGraphics.FromPdfPage(page);

                    using var frameStream = new MemoryStream();
                    image.Save(frameStream, ImageFormat.Png);
                    frameStream.Position = 0;
                    using var xImage = XImage.FromStream(frameStream);

                    var margin = XUnit.FromPoint(25);
                    graphics.DrawImage(xImage, margin.Point, margin.Point, (page.Width - margin).Point, (page.Height - margin).Point);
                }

                using var pdfStream = new MemoryStream();
                document.Save(pdfStream);
                return pdfStream.ToArray();
            }
            catch
            {
                // Faithful to the legacy fallback: any conversion failure (including
                // PlatformNotSupportedException on non-Windows, see class remarks) returns the
                // original bytes unconverted rather than failing the whole save.
                return receivedBytes;
            }
        }, ct);

    /// <summary>Ad-hoc SQL text execution - preserved from the legacy source as-is. Must only ever be
    /// called with a fixed, developer-authored query, never a value derived from external input -
    /// this method has no way to parameterize an entire query string.</summary>
    public async Task<int> GetColumnMaxValueAsync(string query, CancellationToken ct = default)
    {
        var connectionString = await GetDmsConnectionStringAsync(ct);
        var result = await LegacyDbExecutor.ExecuteScalarAsync(connectionString, CommandType.Text, query, ct: ct);
        return Convert.ToInt32(result);
    }

    public async Task<DataTable> GetDmsDataAsync(int pageNo, int pageSize, CancellationToken ct = default)
    {
        var connectionString = await GetDmsConnectionStringAsync(ct);
        var parameters = new List<SqlParameter> { new("@pPageNo", pageNo), new("@pPageSize", pageSize) };
        return await LegacyDbExecutor.ExecuteDataTableAsync(connectionString, CommandType.StoredProcedure, "[dbo].[uspGetDMSData]", parameters, ct);
    }

    public async Task<int> UpdateDataAsync(long documentId, byte[]? documentData, bool idOnly, bool isCorrupt, CancellationToken ct = default)
    {
        var connectionString = await GetDmsConnectionStringAsync(ct);
        var parameters = new List<SqlParameter>
        {
            new("@pDocumentID", documentId),
            new("@pIsUpdateID", idOnly ? 1 : 0),
            new("@pIsCorrupt", isCorrupt ? 1 : 0)
        };

        if (documentData is not null)
        {
            parameters.Add(new SqlParameter("@pDocumentData", SqlDbType.VarBinary) { Value = documentData });
        }

        return await LegacyDbExecutor.ExecuteNonQueryAsync(connectionString, CommandType.StoredProcedure, "uspUpdateDocumentDetailData", parameters, ct);
    }

    /// <summary>Bulk variant, ported from the legacy `UpdateAllData(DataTable)` overload (table-valued parameter to `uspUpdateDocumentDetailDataInBulk`).</summary>
    public async Task UpdateAllDataAsync(DataTable modifiedRows, CancellationToken ct = default)
    {
        var connectionString = await GetDmsConnectionStringAsync(ct);
        var parameters = new List<SqlParameter>
        {
            new("@ptblDocDetail", SqlDbType.Structured) { Value = modifiedRows }
        };

        await LegacyDbExecutor.ExecuteNonQueryAsync(connectionString, CommandType.StoredProcedure, "uspUpdateDocumentDetailDataInBulk", parameters, ct);
    }

    private async Task<string> GetPmsConnectionStringAsync(CancellationToken ct) =>
        await _secretProvider.GetRequiredSecretAsync("Legacy:ConnMHNPMS", ct);

    private async Task<string> GetDmsConnectionStringAsync(CancellationToken ct) =>
        await _secretProvider.GetRequiredSecretAsync("Legacy:ConnMHNDMS", ct);
}
