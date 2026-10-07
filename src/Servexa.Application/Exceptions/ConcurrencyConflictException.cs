namespace Servexa.Application.Exceptions;

/// <summary>
/// Thrown when an optimistic concurrency conflict occurs during state modification.
/// </summary>
public class ConcurrencyConflictException : ConflictException
{
    public ConcurrencyConflictException(string message)
        : base(message)
    {
    }

    public ConcurrencyConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
