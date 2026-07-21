namespace HekCoreApi.Application.Common.Interfaces;

/// <summary>
/// Ported from ERMS's `SaveDocument` DAL calls (`DAL/South/HSSDA.cs`): `UpdateExistingDocument`
/// (`[HSS].[uspUpdateExistingDoc]`, non-AWS path only), `DocumentSave` (`[dbo].[uspDocumentSave]` on the
/// real DMS connection) and `InsertDocument` (`[HSS].[uspInsertDocument]`).
/// </summary>
public interface IErmsWriteRepository
{
    /// <summary>`HSSDA.UpdateExistingDocument` (`HSSDA.cs:38`) - legacy swallows its own errors (out param only logged by the controller), so this never throws.</summary>
    Task UpdateExistingDocumentAsync(string practiceSuffix, string? referralId, string? practiceSuffixNumeric, CancellationToken ct = default);

    /// <summary>
    /// Legacy `SaveToDMS` (`APIController.cs:1720`): new Guid document key, quirky `DocumentTypeID`
    /// resolution from `Erms:DMSDocTypes`, `[dbo].[uspDocumentSave]` with `@pDescription = referralId`
    /// and category 17 (in) / 18 (out). Returns the key, or empty when the proc's output id is &lt;= 0;
    /// proc failures throw (legacy rethrows the DAL's error).
    /// </summary>
    Task<string> SaveToDmsAsync(string practiceSuffix, string? practiceSuffixNumeric, byte[] contentData, string? contentType, string? messageSubject, int categoryId, string? referralId, CancellationToken ct = default);

    /// <summary>`HSSDA.InsertDocument` 10-arg overload (`HSSDA.cs:1017`) - `@pDataSourceId` is the literal "23" for ERMS SaveDocument; throws on proc failure (legacy captures the message into the error envelope).</summary>
    Task InsertDocumentAsync(string practiceSuffix, string? patientId, string? encounterId, string? messageSubject, string dmsKey, int itemTypeId, DateTime resultDate, string? referralId, string? userId, CancellationToken ct = default);
}
