namespace HekCoreApi.Contracts.Errors;

/// <summary>
/// Canonical, data-minimized error shape (RFC 7807-inspired; OpenAPI: Error; Contract Design doc
/// Section 10). <see cref="Detail"/> and every <see cref="ValidationErrorItem.Message"/> are always
/// generic, static, non-PHI strings from a fixed catalog - never exception text, DB error text, or
/// user input. Full detail goes only to server-side structured logs, correlated via <see cref="TraceId"/>.
/// </summary>
public sealed record Error(
    string Title,
    int Status,
    string? Type = null,
    string? Detail = null,
    string? TraceId = null,
    IReadOnlyList<ValidationErrorItem>? Errors = null);
