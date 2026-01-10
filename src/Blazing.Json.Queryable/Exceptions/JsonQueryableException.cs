namespace Blazing.Json.Queryable.Exceptions;

/// <summary>
/// Base exception for all Blazing.Json.Queryable exceptions.
/// </summary>
public class JsonQueryableException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JsonQueryableException"/> class.
    /// </summary>
    public JsonQueryableException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonQueryableException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public JsonQueryableException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonQueryableException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public JsonQueryableException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
