namespace Blazing.Json.Queryable.Exceptions;

/// <summary>
/// Exception thrown when property access fails.
/// </summary>
public class PropertyAccessException : JsonQueryableException
{
    /// <summary>
    /// Gets the property name that failed to access.
    /// </summary>
    public string? PropertyName { get; }

    /// <summary>
    /// Gets the type being accessed.
    /// </summary>
    public Type? TargetType { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PropertyAccessException"/> class.
    /// </summary>
    public PropertyAccessException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PropertyAccessException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public PropertyAccessException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PropertyAccessException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public PropertyAccessException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PropertyAccessException"/> class with a specified error message,
    /// property name, and target type.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="propertyName">The property name that failed to access.</param>
    /// <param name="targetType">The type being accessed.</param>
    public PropertyAccessException(string message, string? propertyName, Type? targetType) : base(message)
    {
        PropertyName = propertyName;
        TargetType = targetType;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PropertyAccessException"/> class with a specified error message,
    /// inner exception, property name, and target type.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    /// <param name="propertyName">The property name that failed to access.</param>
    /// <param name="targetType">The type being accessed.</param>
    public PropertyAccessException(string message, Exception innerException, string? propertyName, Type? targetType) 
        : base(message, innerException)
    {
        PropertyName = propertyName;
        TargetType = targetType;
    }
}
