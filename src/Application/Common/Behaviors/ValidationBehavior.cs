using FluentValidation;
using MediatR;

namespace HekCoreApi.Application.Common.Behaviors;

/// <summary>
/// Runs every registered FluentValidation validator for a request before its handler executes.
/// Throws FluentValidation.ValidationException on failure - the Api-layer exception middleware maps
/// this to HTTP 400 using the Contracts.Error shape (Contract Design doc Section 10), pulling
/// messages only from the generic error catalog, never FluentValidation's raw failure messages
/// verbatim (those can echo the attempted value - a PHI-safety risk).
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);
        var failures = (await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken))))
            .SelectMany(result => result.Errors)
            .Where(failure => failure is not null)
            .ToList();

        if (failures.Count != 0)
        {
            throw new ValidationException(failures);
        }

        return await next();
    }
}
