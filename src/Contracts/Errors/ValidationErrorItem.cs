namespace HekCoreApi.Contracts.Errors;

/// <summary>
/// One field-level validation failure (OpenAPI: Error.errors[]). Message is always drawn from the
/// fixed, generic error catalog - never the attempted value, an exception message, or PHI.
/// </summary>
public sealed record ValidationErrorItem(string Field, string Message);
