namespace Blazing.Json.Queryable.Exceptions;

/// <summary>
/// Exception thrown when JSON deserialization fails.
/// </summary>
public class JsonDeserializationException : JsonQueryableException
{
    /// <summary>
    /// Gets the JSON content that failed to deserialize (truncated if too long).
    /// </summary>
    public string? JsonContent { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonDeserializationException"/> class.
    /// </summary>
    public JsonDeserializationException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonDeserializationException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public JsonDeserializationException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonDeserializationException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public JsonDeserializationException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonDeserializationException"/> class with a specified error message,
    /// inner exception, and the JSON content that failed to deserialize.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    /// <param name="jsonContent">The JSON content that failed to deserialize (will be truncated if too long).</param>
    public JsonDeserializationException(string message, Exception innerException, string? jsonContent) 
        : base(message, innerException)
    {
        JsonContent = TruncateJson(jsonContent);
    }

    private static string? TruncateJson(string? json)
    {
        if (json == null) return null;
        const int maxLength = 200;
        return json.Length > maxLength ? json[..maxLength] + "..." : json;
    }
}
