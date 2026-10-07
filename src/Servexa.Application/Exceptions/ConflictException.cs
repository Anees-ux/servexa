namespace Servexa.Application.Exceptions;

/// <summary>
/// Thrown when an operation conflicts with the current state of a resource.
/// </summary>
public class ConflictException : Exception
{
    public ConflictException(string message)
        : base(message)
    {
    }

    public ConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
