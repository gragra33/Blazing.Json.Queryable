namespace Blazing.Json.Queryable.Exceptions;

/// <summary>
/// Exception thrown when UTF-8 encoding or validation fails.
/// </summary>
public class Utf8EncodingException : JsonQueryableException
{
    /// <summary>
    /// Gets the position in the byte sequence where the error occurred.
    /// </summary>
    public long? Position { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Utf8EncodingException"/> class.
    /// </summary>
    public Utf8EncodingException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Utf8EncodingException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public Utf8EncodingException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Utf8EncodingException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public Utf8EncodingException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Utf8EncodingException"/> class with a specified error message
    /// and position where the error occurred.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="position">The position in the byte sequence where the error occurred.</param>
    public Utf8EncodingException(string message, long? position) : base(message)
    {
        Position = position;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Utf8EncodingException"/> class with a specified error message,
    /// inner exception, and position where the error occurred.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    /// <param name="position">The position in the byte sequence where the error occurred.</param>
    public Utf8EncodingException(string message, Exception innerException, long? position) 
        : base(message, innerException)
    {
        Position = position;
    }
}
