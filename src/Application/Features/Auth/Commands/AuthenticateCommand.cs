using HekCoreApi.Contracts.Auth;
using HekCoreApi.Contracts.Security;
using MediatR;

namespace HekCoreApi.Application.Features.Auth.Commands;

/// <summary>
/// One real auth code path for every entry point (canonical + all legacy compat translators).
/// <see cref="OriginScope"/> is always supplied by the caller (the controller/translator), never
/// derived from anything inside <see cref="Request"/> - origin is structurally determined by which
/// credential/entry-point authenticated the request (ADR-003), never a caller-supplied field.
/// </summary>
public sealed record AuthenticateCommand(TokenRequest Request, OriginScope OriginScope) : IRequest<AuthenticateCommandResult>;

public sealed record AuthenticateCommandResult(bool Succeeded, TokenResponse? Token);
