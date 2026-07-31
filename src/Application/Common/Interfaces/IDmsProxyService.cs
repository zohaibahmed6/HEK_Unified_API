namespace HekCoreApi.Application.Common.Interfaces;

/// <summary>
/// Legacy `DMSProxy.DMSProxy.InstanceDMSProxy.GetDocumentData` (confirmed from
/// legacy-reference/Hiso/Acc45DefinitionBuilder.cs's `GetACC45Definition`) calls an external DMS
/// proxy ASMX service (`DMSServiceURL`/`PMSDmsServices`) - confirmed live 2026-07-31 that this
/// service is unreachable even from the real legacy server in this environment (a live test against
/// the real production address hung ~44s then faulted "Unable to connect to the remote server"; the
/// project on disk claiming to be "DMSProxy" is only the client wrapper + WSDL reference stub, the
/// real server-side implementation was never supplied). Implemented instead via a direct read
/// against the same `DMS_PMS` database this app already connects to for other HISO DMS work
/// (`IHisoDmsConnectionResolver`) - the real document bytes live there
/// (`dbo.tblDocument`/`dbo.tblDocumentDetail`, via `dbo.uspDocumentGetByDMSID`), so this works for any
/// practice/document that has real DMS data in that database, which legacy itself cannot reach here.
/// Real implementation: `DmsDocumentRepository` (Infrastructure layer).
/// </summary>
public interface IDmsProxyService
{
    Task<byte[]?> GetDocumentDataAsync(Guid documentGuid, string practiceId, CancellationToken ct = default);
}
