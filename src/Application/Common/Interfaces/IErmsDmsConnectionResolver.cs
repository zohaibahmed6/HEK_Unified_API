namespace HekCoreApi.Application.Common.Interfaces;

/// <summary>ERMS DMS connection routing: legacy `"ConnDMSDB" + practiceid` (`HSSDA.DocumentSave`, `HSSDA.cs:111`), sourced via <see cref="ISecretProvider"/> (`Erms:DbCredentials:ConnDMSDB{suffix}`). ERMS-owned twin of <see cref="IKaroDmsConnectionResolver"/> - kept separate for module isolation.</summary>
public interface IErmsDmsConnectionResolver
{
    Task<string> ResolveAsync(string practiceSuffix, CancellationToken ct = default);
}
