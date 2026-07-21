namespace HekCoreApi.Application.Common.Interfaces;

/// <summary>
/// Legacy `DMSProxy.DMSProxy.InstanceDMSProxy.GetDocumentData` (confirmed from
/// legacy-reference/Hiso/Acc45DefinitionBuilder.cs's `GetACC45Definition`) - an external DMS proxy
/// service, not a database call, and not part of the supplied source (same class of gap as
/// `AWSDoc.IndiciDMS`). No implementation registered yet - `getFormView`'s `view` bytes stay null
/// until real access is available.
/// </summary>
public interface IDmsProxyService
{
    Task<byte[]?> GetDocumentDataAsync(Guid documentGuid, CancellationToken ct = default);
}
