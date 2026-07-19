using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Features.Auth.Commands;
using HekCoreApi.Contracts.Auth;
using HekCoreApi.Contracts.Security;
using FluentAssertions;
using NSubstitute;

namespace Application.UnitTests.Auth;

public sealed class AuthenticateCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenCredentialValid_IssuesTokenWithRequestedOriginScope()
    {
        var identityValidator = Substitute.For<IIdentityValidator>();
        identityValidator.ValidateAsync("staginghss", "secret", Arg.Any<CancellationToken>())
            .Returns(new IdentityValidationResult(true, "staginghss"));

        var tokenIssuer = Substitute.For<IJwtTokenIssuer>();
        var expectedToken = new TokenResponse("jwt", DateTimeOffset.UtcNow.AddHours(12), "demo", OriginScope.Karo);
        tokenIssuer.IssueAsync(Arg.Any<ResourceScope>(), Arg.Any<CancellationToken>()).Returns(expectedToken);

        var handler = new AuthenticateCommandHandler(identityValidator, tokenIssuer);
        var request = new TokenRequest("staginghss", "secret", 1950057, null, "demo");

        var result = await handler.Handle(new AuthenticateCommand(request, OriginScope.Karo), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Token.Should().Be(expectedToken);
        await tokenIssuer.Received(1).IssueAsync(
            Arg.Is<ResourceScope>(s => s.OriginScope == OriginScope.Karo && s.PracticeId == "demo"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCredentialInvalid_DoesNotIssueToken()
    {
        var identityValidator = Substitute.For<IIdentityValidator>();
        identityValidator.ValidateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new IdentityValidationResult(false, null));

        var tokenIssuer = Substitute.For<IJwtTokenIssuer>();
        var handler = new AuthenticateCommandHandler(identityValidator, tokenIssuer);
        var request = new TokenRequest("bad", "bad", null, null, null);

        var result = await handler.Handle(new AuthenticateCommand(request, OriginScope.Erms), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Token.Should().BeNull();
        await tokenIssuer.DidNotReceive().IssueAsync(Arg.Any<ResourceScope>(), Arg.Any<CancellationToken>());
    }
}
