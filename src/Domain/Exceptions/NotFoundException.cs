namespace HekCoreApi.Domain.Exceptions;

/// <summary>Maps to HTTP 404 (Contract Design doc Section 10).</summary>
public sealed class NotFoundException : DomainException
{
    public NotFoundException(string message) : base(message)
    {
    }
}
