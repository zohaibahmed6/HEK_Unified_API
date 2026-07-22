using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Models;
using HekCoreApi.Application.Common.Options;
using HekCoreApi.Application.Features.Auth.Hiso;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Application.UnitTests.Auth;

public sealed class ResolveHisoSessionQueryHandlerTests
{
    private static ResolveHisoSessionQueryHandler CreateHandler(
        IHisoSessionRegistryRepository sessionRegistry,
        IHisoSessionRepository repository,
        ISecretProvider? secretProvider = null,
        int expiryHours = 12)
    {
        secretProvider ??= Substitute.For<ISecretProvider>();
        secretProvider.GetRequiredSecretAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns("User ID=pms_nz;Password=fake");

        return new ResolveHisoSessionQueryHandler(
            sessionRegistry,
            secretProvider,
            repository,
            Options.Create(new HisoSessionOptions { ExpiryHours = expiryHours }),
            NullLogger<ResolveHisoSessionQueryHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WhenSessionNotFoundInCentralRegistry_ReturnsNotFound()
    {
        var sessionRegistry = Substitute.For<IHisoSessionRegistryRepository>();
        sessionRegistry.FindAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((HisoSessionRoute?)null);
        var repository = Substitute.For<IHisoSessionRepository>();

        var result = await CreateHandler(sessionRegistry, repository).Handle(new ResolveHisoSessionQuery(Guid.NewGuid(), "server-a"), CancellationToken.None);

        result.Status.Should().Be(HisoSessionLookupStatus.NotFound);
    }

    [Fact]
    public async Task Handle_WhenCentralRegistryRouteOlderThanExpiryWindow_ReturnsExpired()
    {
        var sessionRegistry = Substitute.For<IHisoSessionRegistryRepository>();
        var staleRoute = new HisoSessionRoute("901", "local", "dbserver-local", "PMS_NZ_V2", DateTimeOffset.Now.AddHours(-13));
        sessionRegistry.FindAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(staleRoute);
        var repository = Substitute.For<IHisoSessionRepository>();

        var result = await CreateHandler(sessionRegistry, repository, expiryHours: 12).Handle(new ResolveHisoSessionQuery(Guid.NewGuid(), "server-a"), CancellationToken.None);

        result.Status.Should().Be(HisoSessionLookupStatus.Expired);
        result.Context.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenRouteWithinExpiryWindowAndSessionFound_ReturnsSuccessWithContext()
    {
        var sessionRegistry = Substitute.For<IHisoSessionRegistryRepository>();
        var freshRoute = new HisoSessionRoute("901", "local", "dbserver-local", "PMS_NZ_V2", DateTimeOffset.Now.AddHours(-1));
        sessionRegistry.FindAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(freshRoute);

        var repository = Substitute.For<IHisoSessionRepository>();
        var session = new HisoSessionContext("provider-1", "patient-1", "appt-1", "901", DateTimeOffset.Now.AddHours(-1));
        repository.FindBySessionGuidAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(session);

        var result = await CreateHandler(sessionRegistry, repository).Handle(new ResolveHisoSessionQuery(Guid.NewGuid(), "server-a"), CancellationToken.None);

        result.Status.Should().Be(HisoSessionLookupStatus.Success);
        result.Context.Should().Be(session);
    }
}
