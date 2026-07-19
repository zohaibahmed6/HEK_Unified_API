namespace HekCoreApi.Domain.Exceptions;

/// <summary>
/// Maps to HTTP 409 - a genuine conflict outside the documented idempotency path (Contract Design
/// doc Section 10/12). Not used for duplicate-submission-as-success cases, which return 200/201.
/// </summary>
public sealed class ConflictException : DomainException
{
    public ConflictException(string message) : base(message)
    {
    }
}
