namespace HekCoreApi.Api.Errors;

/// <summary>
/// Fixed catalog of generic, static, non-PHI error strings (Contract Design doc Section 10). The
/// exception-handling middleware and FluentValidation failure mapping pull messages only from here
/// - never exception text, DB error text, or user input. Full detail goes to structured Serilog
/// output only, correlated via traceId.
/// </summary>
public static class ErrorCatalog
{
    public const string UnexpectedError = "An unexpected error occurred.";
    public const string ValidationFailed = "One or more fields failed validation.";
    public const string RequiredField = "This field is required.";
    public const string InvalidField = "This field is invalid.";
    public const string Unauthorized = "Authentication is required or the supplied credential is invalid or expired.";
    public const string Forbidden = "The supplied credential does not have sufficient scope for this operation.";
    public const string NotFound = "The requested resource was not found.";
    public const string Conflict = "The request conflicts with the current state of the resource.";
    public const string RateLimited = "Too many requests. Please retry later.";
}
