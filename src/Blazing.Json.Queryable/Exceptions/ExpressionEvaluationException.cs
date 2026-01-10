namespace Blazing.Json.Queryable.Exceptions;

/// <summary>
/// Exception thrown when an expression cannot be evaluated or compiled.
/// </summary>
public class ExpressionEvaluationException : JsonQueryableException
{
    /// <summary>
    /// Gets the expression that failed to evaluate.
    /// </summary>
    public string? ExpressionText { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExpressionEvaluationException"/> class.
    /// </summary>
    public ExpressionEvaluationException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExpressionEvaluationException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public ExpressionEvaluationException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExpressionEvaluationException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ExpressionEvaluationException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExpressionEvaluationException"/> class with a specified error message,
    /// inner exception, and the expression text that failed to evaluate.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    /// <param name="expressionText">The expression that failed to evaluate.</param>
    public ExpressionEvaluationException(string message, Exception innerException, string? expressionText) 
        : base(message, innerException)
    {
        ExpressionText = expressionText;
    }
}
