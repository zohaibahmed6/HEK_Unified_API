namespace HekCoreApi.Domain.Exceptions;

/// <summary>Maps to HTTP 401 - missing/invalid/expired credential (Contract Design doc Section 10).</summary>
public sealed class UnauthorizedException : DomainException
{
    public UnauthorizedException(string message) : base(message)
    {
    }
}
