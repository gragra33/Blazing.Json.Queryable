namespace Blazing.Json.Queryable.Core;

/// <summary>
/// Handles property access with minimal allocations using span-based APIs.
/// Avoids string allocations for property name lookups through ReadOnlySpan&lt;char&gt; parameters.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Zero-Allocation Design:</strong> Traditional reflection-based property access
/// requires string allocations for property names. This interface uses ReadOnlySpan&lt;char&gt;
/// to eliminate these allocations, achieving zero-allocation property lookups when combined
/// with PropertyInfo caching.
/// </para>
/// <para>
/// <strong>Method Priority:</strong>
/// <list type="number">
/// <item><strong>Preferred:</strong> <c>GetValue(object, ReadOnlySpan&lt;char&gt;)</c> - Zero allocation</item>
/// <item><strong>Type Info:</strong> <c>GetPropertyType(Type, ReadOnlySpan&lt;char&gt;)</c> - For type inspection</item>
/// <item><strong>Convenience:</strong> <c>GetValueByName(object, string)</c> - When you already have a string</item>
/// </list>
/// </para>
/// <para>
/// <strong>Implementation Note:</strong> Implementations should cache PropertyInfo instances
/// to minimize reflection overhead. The first lookup incurs reflection cost, subsequent
/// lookups are cached and fast.
/// </para>
/// </remarks>
public interface IPropertyAccessor
{
    /// <summary>
    /// Gets property value using span-based property name (zero allocation).
    /// Preferred method for performance-critical paths.
    /// </summary>
    /// <param name="obj">The object to get the property value from</param>
    /// <param name="propertyName">The property name as a span (zero allocation)</param>
    /// <returns>The property value, or null if not found or null</returns>
    /// <remarks>
    /// <para>
    /// <strong>Performance:</strong> When combined with PropertyInfo caching, this method
    /// achieves zero allocations per call. Use .AsSpan() on string literals to get
    /// ReadOnlySpan&lt;char&gt; at zero cost.
    /// </para>
    /// <para>
    /// <strong>Caching Strategy:</strong> Implementations typically use:
    /// <code>
    /// Dictionary&lt;(Type, string), PropertyInfo&gt; cache
    /// </code>
    /// The string in the cache key is interned/cached once, then span-based lookups
    /// avoid string allocations.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var person = new Person { Name = "Alice", Age = 25 };
    /// 
    /// // Zero allocation - span from string literal
    /// var name = accessor.GetValue(person, "Name".AsSpan());
    /// 
    /// // Zero allocation - span from existing string
    /// string propName = "Age";
    /// var age = accessor.GetValue(person, propName.AsSpan());
    /// 
    /// // Zero allocation - span from memory
    /// ReadOnlySpan&lt;char&gt; span = propertyNameChars.AsSpan();
    /// var value = accessor.GetValue(person, span);
    /// </code>
    /// </example>
    object? GetValue(object obj, ReadOnlySpan<char> propertyName);

    /// <summary>
    /// Gets property type using span-based property name.
    /// Useful for type inspection and validation before accessing values.
    /// </summary>
    /// <param name="objectType">The type containing the property</param>
    /// <param name="propertyName">The property name as a span</param>
    /// <returns>The Type of the property</returns>
    /// <remarks>
    /// <para>
    /// <strong>Use Cases:</strong>
    /// <list type="bullet">
    /// <item>Validating property existence before access</item>
    /// <item>Type-safe conversions in projection scenarios</item>
    /// <item>Dynamic query compilation with type checking</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var personType = typeof(Person);
    /// 
    /// // Get property type
    /// var nameType = accessor.GetPropertyType(personType, "Name".AsSpan());
    /// // nameType == typeof(string)
    /// 
    /// var ageType = accessor.GetPropertyType(personType, "Age".AsSpan());
    /// // ageType == typeof(int)
    /// </code>
    /// </example>
    Type GetPropertyType(Type objectType, ReadOnlySpan<char> propertyName);

    /// <summary>
    /// Convenience overload for string-based property names.
    /// Less efficient than GetValue(ReadOnlySpan) - use when you already have a string.
    /// </summary>
    /// <param name="obj">The object to get the property value from</param>
    /// <param name="propertyName">The property name as a string</param>
    /// <returns>The property value, or null if not found or null</returns>
    /// <remarks>
    /// <para>
    /// <strong>When to Use:</strong>
    /// <list type="bullet">
    /// <item>You already have a string variable (avoid .AsSpan() wrapper)</item>
    /// <item>Interop with APIs that provide strings</item>
    /// <item>Convenience in non-performance-critical code</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Performance Note:</strong> This method is still efficient due to PropertyInfo
    /// caching, but the span-based overload is preferred to establish zero-allocation patterns.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var person = new Person { Name = "Bob", Age = 30 };
    /// 
    /// // When you already have a string
    /// string propertyName = GetPropertyNameFromSomewhere();
    /// var value = accessor.GetValueByName(person, propertyName);
    /// 
    /// // Equivalent span-based (preferred for consistency)
    /// var value2 = accessor.GetValue(person, propertyName.AsSpan());
    /// </code>
    /// </example>
    object? GetValueByName(object obj, string propertyName);
}
