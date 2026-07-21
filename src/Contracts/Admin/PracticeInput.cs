using HekCoreApi.Contracts.Security;

namespace HekCoreApi.Contracts.Admin;

/// <summary>
/// PROJECT_STATUS.md open items 24/31, resolved 2026-07-20: the tenant registry (ADR-001,
/// `PracticeRegistryEntry`) needs three independent values to route a request - which physical
/// server, which database on it, and which legacy system owns this practice's data
/// (`ILegacyPracticeConnectionResolver` builds `Server={DbServerHost};Database={DbName};...`). An
/// earlier draft of this contract collapsed all three into one `databaseServerId` field, which the
/// repository then had to duplicate into both `DbServerHost` and `DbName` - producing a broken
/// connection string for any practice whose database name doesn't equal its server hostname (always,
/// in practice). No source document ever defined this admin API's schema (item 24's own "confirm or
/// sign off as new design" - signing off as new design now, since ADR-001's only text is "one row
/// per practice with which physical database server it lives on", not a literal admin API shape).
/// </summary>
public sealed record PracticeInput(string Name, string? PhoCode, string DbServerHost, string DbName, OriginScope SourceSystem);
