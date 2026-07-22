using System.Text.Json.Serialization;
using HekCoreApi.Contracts.Security;

namespace HekCoreApi.Contracts.Auth;

/// <summary>
/// Canonical POST /auth/token request shape (OpenAPI: TokenRequest). Edge adapters translate each
/// legacy consumer's existing Authenticate payload into this shape before dispatch.
///
/// <see cref="OriginScope"/> is a demo/testing-only field (PROJECT_STATUS.md open item 26 is still
/// unresolved for production): a direct caller of this canonical endpoint currently states its own
/// origin scope. ADR-003's structural-origin-scope rule is properly honoured everywhere else (every
/// legacy compat controller hardcodes its own scope, never reading it from the request) - this field
/// exists only so the canonical layer can be exercised end-to-end before that open item is resolved.
///
/// <c>RawEncounterId</c> is an optional real composite EncounterId (e.g.
/// "280210498__518__F2G091-H__SOUTH") - when supplied for a Karo/Erms origin, the matching routing
/// resolver extracts PracticeCode/Environment from it and stamps them onto the issued token's claims
/// (RoutingContext). Distinct from the plain numeric <c>EncounterId</c> field below.
/// </summary>
public sealed record TokenRequest(
    string Username,
    string Password,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] OriginScope OriginScope,
    int? PatientId = null,
    int? EncounterId = null,
    string? PracticeId = null,
    string? RawEncounterId = null);
