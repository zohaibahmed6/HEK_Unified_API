namespace HekCoreApi.Application.Common.Interfaces;

/// <summary>
/// Ported from legacy-reference/Hiso/DocumentHandler.cs's `AddDocument`. Legacy branches on
/// `AppSettings["AddDirectDMS"]`: direct DB write (`Mapper.SaveDocumentToDMS` -> `[dbo].[uspDocumentSave]`,
/// against a *global* `ConectionStringPMS_NZ_DMS` - not per-practice) vs. an external `DMSProxy`
/// service call. Only the direct-DB path is portable without more external access - the proxy path
/// isn't attempted, matching the same class of gap as AWS/DMS-proxy-for-reads.
/// </summary>
public interface IHisoDocumentHandler
{
    /// <summary>Returns the generated document GUID - always succeeds from the caller's perspective (legacy swallows the underlying save exception and always returns the pre-generated GUID).</summary>
    Task<Guid> AddDocumentAsync(string? view, string viewType, string? formEngineId, string practiceId, CancellationToken ct = default);
}
