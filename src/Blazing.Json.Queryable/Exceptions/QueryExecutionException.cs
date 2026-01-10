namespace Blazing.Json.Queryable.Exceptions;

/// <summary>
/// Exception thrown when query execution fails.
/// </summary>
public class QueryExecutionException : JsonQueryableException
{
    /// <summary>
    /// Gets the phase of execution where the error occurred.
    /// </summary>
    public string? ExecutionPhase { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryExecutionException"/> class.
    /// </summary>
    public QueryExecutionException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryExecutionException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public QueryExecutionException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryExecutionException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public QueryExecutionException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryExecutionException"/> class with a specified error message
    /// and execution phase.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="executionPhase">The phase of execution where the error occurred.</param>
    public QueryExecutionException(string message, string? executionPhase) : base(message)
    {
        ExecutionPhase = executionPhase;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryExecutionException"/> class with a specified error message,
    /// inner exception, and execution phase.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    /// <param name="executionPhase">The phase of execution where the error occurred.</param>
    public QueryExecutionException(string message, Exception innerException, string? executionPhase) 
        : base(message, innerException)
    {
        ExecutionPhase = executionPhase;
    }
}
