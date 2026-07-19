using FluentValidation;
using HekCoreApi.Api.Middleware;
using HekCoreApi.Contracts.Errors;
using HekCoreApi.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace HekCoreApi.Api.Errors;

/// <summary>
/// Centralized exception middleware (architecture-rules skill) - the single point where HISO's
/// includeExceptionDetailInFaults and KARO/ERMS's debug=true exception-leakage patterns become
/// structurally impossible to reproduce. Maps every unhandled exception to the canonical
/// Contracts.Error shape (Contract Design doc Section 10), using only ErrorCatalog messages.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var traceId = httpContext.Items.TryGetValue(CorrelationIdMiddleware.HttpContextItemKey, out var id)
            ? id?.ToString()
            : httpContext.TraceIdentifier;

        var (status, title, detail, errors) = Map(exception);

        // Full detail (exception type/message/stack trace) goes only to structured server-side logs,
        // correlated via traceId - never to the client-visible response body.
        _logger.LogError(exception, "Unhandled exception mapped to status {Status}, traceId {TraceId}.", status, traceId);

        var problem = new Error(
            Title: title,
            Status: status,
            Type: $"https://api.hek.example/errors/{title.ToLowerInvariant().Replace(' ', '-')}",
            Detail: detail,
            TraceId: traceId,
            Errors: errors);

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }

    private static (int Status, string Title, string Detail, IReadOnlyList<ValidationErrorItem>? Errors) Map(Exception exception) =>
        exception switch
        {
            ValidationException validationException => (
                StatusCodes.Status400BadRequest,
                "Validation failed",
                ErrorCatalog.ValidationFailed,
                validationException.Errors
                    .Select(e => new ValidationErrorItem(e.PropertyName, ErrorCatalog.InvalidField))
                    .ToList()),
            UnauthorizedException => (StatusCodes.Status401Unauthorized, "Unauthorized", ErrorCatalog.Unauthorized, null),
            ForbiddenException => (StatusCodes.Status403Forbidden, "Forbidden", ErrorCatalog.Forbidden, null),
            NotFoundException => (StatusCodes.Status404NotFound, "Not Found", ErrorCatalog.NotFound, null),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict", ErrorCatalog.Conflict, null),
            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error", ErrorCatalog.UnexpectedError, null)
        };
}
