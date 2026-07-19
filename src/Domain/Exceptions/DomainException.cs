namespace HekCoreApi.Domain.Exceptions;

/// <summary>
/// Base type for exceptions that carry a specific HTTP-outcome intent (Contract Design doc Section
/// 10). Domain itself has zero HTTP knowledge - the Api-layer exception middleware maps each
/// concrete subtype to a status code; Domain only expresses "what kind of failure this is."
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message)
    {
    }
}
