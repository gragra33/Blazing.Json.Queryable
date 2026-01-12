namespace Blazing.Json.Queryable.Utilities;

/// <summary>
/// Classification of JSONPath expressions based on their complexity and streaming capabilities.
/// Cached at executor construction time to avoid repeated parsing (20-100x performance improvement).
/// </summary>
internal enum PathClassification
{
    /// <summary>
    /// Simple paths with only wildcards and property navigation.
    /// Examples: $.data[*], $.departments[*].employees[*]
    /// Supports TRUE STREAMING with constant memory usage.
    /// </summary>
    SimpleWildcard,
    
    /// <summary>
    /// Advanced RFC 9535 features requiring full document materialization.
    /// Examples: $[?@.age > 25], $[0:10], $[?length(@.name) > 5]
    /// Includes: filters, functions, slicing, recursive descent.
    /// </summary>
    AdvancedRFC9535,
    
    /// <summary>
    /// Basic paths without wildcards or advanced features.
    /// Uses simple navigation with fallback to materialization if needed.
    /// </summary>
    BasicPath
}
