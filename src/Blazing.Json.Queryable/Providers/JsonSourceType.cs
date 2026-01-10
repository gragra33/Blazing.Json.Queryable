namespace Blazing.Json.Queryable.Providers;

/// <summary>
/// Represents the type of JSON source data.
/// </summary>
public enum JsonSourceType
{
    /// <summary>UTF-8 encoded byte array.</summary>
    Utf8Bytes,
    
    /// <summary>JSON string (converted to UTF-8 internally).</summary>
    String,
    
    /// <summary>Stream containing JSON data.</summary>
    Stream,
    
    /// <summary>File path to JSON data.</summary>
    File
}