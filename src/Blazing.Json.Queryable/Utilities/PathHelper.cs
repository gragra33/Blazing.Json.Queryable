using Blazing.Json.JSONPath.Utilities;

namespace Blazing.Json.Queryable.Utilities;

/// <summary>
/// Helper methods for JSONPath classification and parsing.
/// Shared across all execution strategies.
/// </summary>
internal static class PathHelper
{
    /// <summary>
    /// Classifies a JSONPath expression for optimal strategy selection.
    /// This method is called ONCE at executor construction and the result is cached.
    /// </summary>
    /// <param name="jsonPath">The JSONPath expression to classify.</param>
    /// <returns>The path classification.</returns>
    public static PathClassification ClassifyPath(string jsonPath)
    {
        var span = jsonPath.AsSpan();
        
        // Check advanced features first (most restrictive)
        if (JsonPathHelper.HasFeatures(span))
            return PathClassification.AdvancedRFC9535;
        
        // Check simple wildcard paths
        if (IsSimpleWildcardOnlyPath(span))
            return PathClassification.SimpleWildcard;
        
        // Fallback to basic navigation
        return PathClassification.BasicPath;
    }
    
    /// <summary>
    /// Determines if a JSONPath expression is a simple wildcard-only path.
    /// Simple wildcard paths use ONLY path navigation and [*] wildcards.
    /// </summary>
    public static bool IsSimpleWildcardOnlyPath(ReadOnlySpan<char> jsonPath)
    {
        if (jsonPath.IsEmpty || jsonPath[0] != '$')
            return false;

        int i = 1;
        
        while (i < jsonPath.Length)
        {
            char c = jsonPath[i];
            
            if (c == '.')
            {
                i++;
                continue;
            }
            
            if (c == '[')
            {
                // Check for [*] wildcard
                if (i + 2 < jsonPath.Length && jsonPath[i + 1] == '*' && jsonPath[i + 2] == ']')
                {
                    i += 3;
                    continue;
                }
                
                // Check for ['property'] or ["property"]
                if (i + 1 < jsonPath.Length && (jsonPath[i + 1] == '\'' || jsonPath[i + 1] == '"'))
                {
                    char quote = jsonPath[i + 1];
                    i += 2;
                    
                    while (i < jsonPath.Length && jsonPath[i] != quote)
                        i++;
                    
                    if (i < jsonPath.Length)
                    {
                        i++;
                        if (i < jsonPath.Length && jsonPath[i] == ']')
                        {
                            i++;
                            continue;
                        }
                    }
                }
                
                // Any other bracket notation is advanced
                return false;
            }
            
            // Check for recursive descent (..)
            if (c == '.' && i + 1 < jsonPath.Length && jsonPath[i + 1] == '.')
                return false;
            
            // Check for function calls
            if (c == '(' || 
                (i + 6 < jsonPath.Length && jsonPath.Slice(i, 7) is "length(") ||
                (i + 5 < jsonPath.Length && jsonPath.Slice(i, 6) is "count(") ||
                (i + 5 < jsonPath.Length && jsonPath.Slice(i, 6) is "match(") ||
                (i + 6 < jsonPath.Length && jsonPath.Slice(i, 7) is "search(") ||
                (i + 5 < jsonPath.Length && jsonPath.Slice(i, 6) is "value("))
            {
                return false;
            }
            
            i++;
        }
        
        return true;
    }
    
    /// <summary>
    /// Parses JSONPath expression into path segments for navigation.
    /// </summary>
    public static string[] ParseJsonPath(string jsonPath)
    {
        if (!jsonPath.StartsWith('$'))
            throw new ArgumentException("JSONPath must start with '$'", nameof(jsonPath));

        var segments = jsonPath.TrimStart('$', '.')
            .Split('.')
            .Select(s => s.Replace("[*]", "").Trim())
            .Where(s => !string.IsNullOrEmpty(s))
            .ToArray();
        
        return segments;
    }
}
