namespace HekCoreApi.Domain.Exceptions;

/// <summary>Maps to HTTP 403 - valid credential, insufficient scope (Contract Design doc Section 10).</summary>
public sealed class ForbiddenException : DomainException
{
    public ForbiddenException(string message) : base(message)
    {
    }
}
