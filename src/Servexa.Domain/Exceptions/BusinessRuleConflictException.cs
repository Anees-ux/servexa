namespace Servexa.Domain.Exceptions;

/// <summary>
/// Thrown when an action violates a domain invariant or business rule conflict.
/// </summary>
public class BusinessRuleConflictException : DomainException
{
    public BusinessRuleConflictException(string message)
        : base(message)
    {
    }

    public BusinessRuleConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
