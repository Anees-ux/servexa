namespace Servexa.Application.Exceptions;

/// <summary>
/// Thrown when an idempotency conflict occurs (e.g., conflicting duplicate request).
/// </summary>
public class IdempotencyConflictException : ConflictException
{
    public IdempotencyConflictException(string message)
        : base(message)
    {
    }

    public IdempotencyConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
