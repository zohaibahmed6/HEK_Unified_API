namespace HekCoreApi.Application.Common.Options;

/// <summary>
/// PROJECT_STATUS.md open item 32: the real legacy KARO/ERMS Authenticate backends returned a
/// server-resolved `practiceId` in their response, keyed off something internal to their own
/// database (probably the `pho` code or `username` - never confirmed, since that resolution logic
/// was never supplied). Rather than invent that lookup's shape, this ships as an ADR-008-style
/// config toggle, OFF by default (Zohaib's explicit decision, 2026-07-20: "leave it configurable
/// and turn off, we will check this later"). While disabled, behavior is unchanged from before this
/// option existed - compat-issued tokens carry an empty practiceId, same as today. When enabled,
/// <see cref="Application.Features.Auth.Commands.AuthenticateCommandHandler"/> looks up
/// `Auth:LegacyPracticeMappings:{username}` via <see cref="Interfaces.ISecretProvider"/> and uses
/// it as the practiceId if the legacy request didn't already supply one - keyed by username for now
/// since that's the one field confirmed present on every legacy Authenticate payload (KARO and ERMS
/// both), not because username has been confirmed as the real lookup key the old system used.
/// </summary>
public sealed class LegacyPracticeResolutionOptions
{
    public const string SectionName = "Auth:LegacyPracticeResolution";

    public bool Enabled { get; set; }
}
