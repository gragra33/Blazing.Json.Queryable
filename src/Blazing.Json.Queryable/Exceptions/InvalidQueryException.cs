namespace Blazing.Json.Queryable.Exceptions;

/// <summary>
/// Exception thrown when a query operation is invalid or unsupported.
/// </summary>
public class InvalidQueryException : JsonQueryableException
{
    /// <summary>
    /// Gets the operation that caused the error.
    /// </summary>
    public string? Operation { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidQueryException"/> class.
    /// </summary>
    public InvalidQueryException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidQueryException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public InvalidQueryException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidQueryException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public InvalidQueryException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidQueryException"/> class with a specified error message
    /// and the operation that caused the error.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="operation">The operation that caused the error.</param>
    public InvalidQueryException(string message, string? operation) : base(message)
    {
        Operation = operation;
    }
}
