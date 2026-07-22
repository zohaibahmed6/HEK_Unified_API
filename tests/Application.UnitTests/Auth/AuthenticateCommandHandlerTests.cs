using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Common.Options;
using HekCoreApi.Application.Features.Auth.Commands;
using HekCoreApi.Contracts.Auth;
using HekCoreApi.Contracts.Security;
using FluentAssertions;
using Microsoft.Extensions.Options;
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

        var secretProvider = Substitute.For<ISecretProvider>();
        var handler = new AuthenticateCommandHandler(identityValidator, tokenIssuer, secretProvider, Options.Create(new LegacyPracticeResolutionOptions()), Substitute.For<IKaroRoutingResolver>(), Substitute.For<IErmsRoutingResolver>());
        var request = new TokenRequest("staginghss", "secret", OriginScope.Karo, 1950057, null, "demo");

        var result = await handler.Handle(new AuthenticateCommand(request, OriginScope.Karo), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Token.Should().Be(expectedToken);
        await tokenIssuer.Received(1).IssueAsync(
            Arg.Is<ResourceScope>(s => s.OriginScope == OriginScope.Karo && s.PracticeId == "demo"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenPracticeIdMissing_AndResolutionDisabled_LeavesPracticeIdEmpty()
    {
        var identityValidator = Substitute.For<IIdentityValidator>();
        identityValidator.ValidateAsync("staginghss", "secret", Arg.Any<CancellationToken>())
            .Returns(new IdentityValidationResult(true, "staginghss"));

        var tokenIssuer = Substitute.For<IJwtTokenIssuer>();
        var secretProvider = Substitute.For<ISecretProvider>();
        secretProvider.GetSecretAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns("901");

        var handler = new AuthenticateCommandHandler(identityValidator, tokenIssuer, secretProvider, Options.Create(new LegacyPracticeResolutionOptions { Enabled = false }), Substitute.For<IKaroRoutingResolver>(), Substitute.For<IErmsRoutingResolver>());
        var request = new TokenRequest("staginghss", "secret", OriginScope.Karo, 1950057, null, null);

        await handler.Handle(new AuthenticateCommand(request, OriginScope.Karo), CancellationToken.None);

        await secretProvider.DidNotReceive().GetSecretAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await tokenIssuer.Received(1).IssueAsync(Arg.Is<ResourceScope>(s => s.PracticeId == string.Empty), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenPracticeIdMissing_AndResolutionEnabled_ResolvesFromSecretProvider()
    {
        var identityValidator = Substitute.For<IIdentityValidator>();
        identityValidator.ValidateAsync("staginghss", "secret", Arg.Any<CancellationToken>())
            .Returns(new IdentityValidationResult(true, "staginghss"));

        var tokenIssuer = Substitute.For<IJwtTokenIssuer>();
        var secretProvider = Substitute.For<ISecretProvider>();
        secretProvider.GetSecretAsync("Auth:LegacyPracticeMappings:staginghss", Arg.Any<CancellationToken>()).Returns("901");

        var handler = new AuthenticateCommandHandler(identityValidator, tokenIssuer, secretProvider, Options.Create(new LegacyPracticeResolutionOptions { Enabled = true }), Substitute.For<IKaroRoutingResolver>(), Substitute.For<IErmsRoutingResolver>());
        var request = new TokenRequest("staginghss", "secret", OriginScope.Karo, 1950057, null, null);

        await handler.Handle(new AuthenticateCommand(request, OriginScope.Karo), CancellationToken.None);

        await tokenIssuer.Received(1).IssueAsync(Arg.Is<ResourceScope>(s => s.PracticeId == "901"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCredentialInvalid_DoesNotIssueToken()
    {
        var identityValidator = Substitute.For<IIdentityValidator>();
        identityValidator.ValidateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new IdentityValidationResult(false, null));

        var tokenIssuer = Substitute.For<IJwtTokenIssuer>();
        var secretProvider = Substitute.For<ISecretProvider>();
        var handler = new AuthenticateCommandHandler(identityValidator, tokenIssuer, secretProvider, Options.Create(new LegacyPracticeResolutionOptions()), Substitute.For<IKaroRoutingResolver>(), Substitute.For<IErmsRoutingResolver>());
        var request = new TokenRequest("bad", "bad", OriginScope.Erms, null, null, null);

        var result = await handler.Handle(new AuthenticateCommand(request, OriginScope.Erms), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Token.Should().BeNull();
        await tokenIssuer.DidNotReceive().IssueAsync(Arg.Any<ResourceScope>(), Arg.Any<CancellationToken>());
    }
}
