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
    private static ResolveHisoSessionQueryHandler CreateHandler(IHisoSessionRepository repository, int expiryHours = 12) =>
        new(repository, Options.Create(new HisoSessionOptions { ExpiryHours = expiryHours }), NullLogger<ResolveHisoSessionQueryHandler>.Instance);

    [Fact]
    public async Task Handle_WhenSessionNotFound_ReturnsNotFound()
    {
        var repository = Substitute.For<IHisoSessionRepository>();
        repository.FindBySessionGuidAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((HisoSessionContext?)null);

        var result = await CreateHandler(repository).Handle(new ResolveHisoSessionQuery(Guid.NewGuid(), "server-a"), CancellationToken.None);

        result.Status.Should().Be(HisoSessionLookupStatus.NotFound);
    }

    [Fact]
    public async Task Handle_WhenSessionOlderThanExpiryWindow_ReturnsExpired()
    {
        var repository = Substitute.For<IHisoSessionRepository>();
        var staleSession = new HisoSessionContext("provider-1", "patient-1", "appt-1", "practice-1", DateTimeOffset.UtcNow.AddHours(-13));
        repository.FindBySessionGuidAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(staleSession);

        var result = await CreateHandler(repository, expiryHours: 12).Handle(new ResolveHisoSessionQuery(Guid.NewGuid(), "server-a"), CancellationToken.None);

        result.Status.Should().Be(HisoSessionLookupStatus.Expired);
        result.Context.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenSessionWithinExpiryWindow_ReturnsSuccessWithContext()
    {
        var repository = Substitute.For<IHisoSessionRepository>();
        var freshSession = new HisoSessionContext("provider-1", "patient-1", "appt-1", "practice-1", DateTimeOffset.UtcNow.AddHours(-1));
        repository.FindBySessionGuidAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(freshSession);

        var result = await CreateHandler(repository, expiryHours: 12).Handle(new ResolveHisoSessionQuery(Guid.NewGuid(), "server-a"), CancellationToken.None);

        result.Status.Should().Be(HisoSessionLookupStatus.Success);
        result.Context.Should().Be(freshSession);
    }
}
