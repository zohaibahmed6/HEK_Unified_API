namespace HekCoreApi.Application.Common.Interfaces;

/// <summary>
/// KARO/HSS's real DMS connection routing (confirmed from `HSSDA.DocumentSave`, `HSSDA.cs:86`):
/// `ConfigurationManager.ConnectionStrings["ConnDMSDB" + practiceid]` - same suffix-concatenation
/// model as <see cref="IKaroPracticeConnectionResolver"/> but a separate connection-string family
/// (real Web.config confirms distinct `ConnDMSDB`/`ConnDMSDB_485`/`ConnDMSDB_901_FZZ999-B`/etc. entries).
/// </summary>
public interface IKaroDmsConnectionResolver
{
    Task<string> ResolveAsync(string practiceSuffix, CancellationToken ct = default);
}
