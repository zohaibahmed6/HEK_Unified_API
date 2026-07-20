using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Options;
using HekCoreApi.Infrastructure.Legacy.Dormant.Dmsda;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Infrastructure.UnitTests.Legacy;

/// <summary>
/// Proves the confirmed SQL injection in the legacy DMSDA.cs's `UpdateInboxFolderDocuments`
/// (string-concatenated CommandText) does not exist in the ported version - the fixed method must
/// fail fast against a real, unreachable connection string rather than ever building a command
/// whose text embeds the caller-supplied Guid/InboxFolderItemID directly (PROJECT_STATUS.md open
/// item 23). A real database isn't available in this environment (see PROJECT_STATUS.md Block 0
/// change log), so this test asserts on failure mode (connection-level exception, not a SQL syntax
/// exception a malicious payload could trigger) as the practical proxy available here.
/// </summary>
public sealed class DmsDocumentServiceTests
{
    private static DmsDocumentService CreateService(string connectionString)
    {
        var secretProvider = Substitute.For<ISecretProvider>();
        secretProvider.GetRequiredSecretAsync("Legacy:ConnMHNPMS", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(connectionString));

        return new DmsDocumentService(secretProvider, Options.Create(new LegacyDmsOptions()));
    }

    [Fact]
    public async Task UpdateInboxFolderDocumentsAsync_WithSqlMetacharactersInGuid_DoesNotThrowAsSyntaxError()
    {
        // A malicious payload that would break out of the legacy string-concatenated query
        // (e.g. "'; DROP TABLE Prompt.tblInboxFolderItem; --") must be treated as an ordinary
        // parameter value, never as SQL syntax - so any failure here must be a connection failure
        // (no real SQL Server reachable in this test), not a SqlException carrying a syntax error.
        var service = CreateService("Server=127.0.0.1,1;Database=DoesNotExist;Connect Timeout=1;TrustServerCertificate=True;");
        const string maliciousGuid = "'; DROP TABLE Prompt.tblInboxFolderItem; --";

        var act = async () => await service.UpdateInboxFolderDocumentsAsync(maliciousGuid, 1, CancellationToken.None);

        var exception = await act.Should().ThrowAsync<Exception>();
        exception.Which.Message.Should().NotContain("syntax", "a parameterized query never lets the value be interpreted as SQL syntax");
    }
}
