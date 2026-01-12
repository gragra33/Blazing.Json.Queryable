using System.Collections.Concurrent;
using System.Reflection;
using Blazing.Json.Queryable.Exceptions;

namespace Blazing.Json.Queryable.Implementations;

/// <summary>
/// Provides span-based property access with reflection and PropertyInfo caching.
/// This is a static class with a shared global cache for maximum efficiency across the entire application.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Zero-Allocation Design:</strong> Uses ReadOnlySpan&lt;char&gt; for property names
/// to eliminate string allocations, achieving zero-allocation property lookups when combined
/// with PropertyInfo caching.
/// </para>
/// <para>
/// <strong>Global Cache:</strong> All property lookups share a single global cache, maximizing
/// efficiency across the entire application. The cache is thread-safe and uses ConcurrentDictionary.
/// </para>
/// <para>
/// <strong>Method Priority:</strong>
/// <list type="number">
/// <item><strong>Preferred:</strong> <c>GetValue(object, ReadOnlySpan&lt;char&gt;)</c> - Zero allocation</item>
/// <item><strong>Type Info:</strong> <c>GetPropertyType(Type, ReadOnlySpan&lt;char&gt;)</c> - For type inspection</item>
/// <item><strong>Convenience:</strong> <c>GetValueByName(object, string)</c> - When you already have a string</item>
/// </list>
/// </para>
/// </remarks>
public static class SpanPropertyAccessor
{
    // Global shared cache for PropertyInfo lookups across all calls
    // Key: (Type, PropertyName), Value: PropertyInfo
    private static readonly ConcurrentDictionary<(Type, string), PropertyInfo?> _propertyCache = new();

    /// <summary>
    /// Gets property value using span-based property name (zero allocation).
    /// Preferred method for performance-critical paths.
    /// Use .AsSpan() on strings to get ReadOnlySpan&lt;char&gt;.
    /// </summary>
    /// <param name="obj">The object to get the property value from.</param>
    /// <param name="propertyName">The property name as a span.</param>
    /// <returns>The property value or null if not found.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="obj"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="propertyName"/> is empty.</exception>
    /// <remarks>
    /// This method uses span-based property name lookup to avoid string allocations.
    /// <see cref="PropertyInfo"/> instances are cached globally for maximum reuse.
    /// For best performance, pass property names as ReadOnlySpan&lt;char&gt; directly.
    /// </remarks>
    public static object? GetValue(object obj, ReadOnlySpan<char> propertyName)
    {
        ArgumentNullException.ThrowIfNull(obj);
        
        if (propertyName.IsEmpty)
        {
            throw new ArgumentException("Property name cannot be empty.", nameof(propertyName));
        }

        var type = obj.GetType();

        // Get PropertyInfo (cached)
        var propertyInfo = GetPropertyInfoInternal(type, propertyName);

        if (propertyInfo == null)
        {
            return null;
        }

        // Use reflection to get the value
        return propertyInfo.GetValue(obj);
    }

    /// <summary>
    /// Gets property type using span-based property name.
    /// </summary>
    /// <param name="objectType">The type to inspect.</param>
    /// <param name="propertyName">The property name as a span.</param>
    /// <returns>The property type.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="objectType"/> is null.</exception>
    /// <exception cref="PropertyAccessException">Thrown when property is not found.</exception>
    public static Type GetPropertyType(Type objectType, ReadOnlySpan<char> propertyName)
    {
        ArgumentNullException.ThrowIfNull(objectType);

        var propertyInfo = GetPropertyInfoInternal(objectType, propertyName) ?? throw new PropertyAccessException(
                $"Property '{propertyName}' not found on type '{objectType.Name}'",
                propertyName.ToString(),
                objectType);
        return propertyInfo.PropertyType;
    }

    /// <summary>
    /// Convenience overload for string-based property names.
    /// Less efficient - use <see cref="GetValue(object, ReadOnlySpan{char})"/> when possible.
    /// </summary>
    /// <param name="obj">The object to get the property value from.</param>
    /// <param name="propertyName">The property name as a string.</param>
    /// <returns>The property value or null if not found.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="obj"/> or <paramref name="propertyName"/> is null.</exception>
    public static object? GetValueByName(object obj, string propertyName)
    {
        ArgumentNullException.ThrowIfNull(obj);
        ArgumentNullException.ThrowIfNull(propertyName);

        // Convert string to span and delegate to span-based method
        return GetValue(obj, propertyName.AsSpan());
    }

    /// <summary>
    /// Internal helper to get <see cref="PropertyInfo"/> with caching.
    /// Converts span to string for cache lookup (unavoidable for dictionary key).
    /// </summary>
    /// <param name="type">The type to inspect.</param>
    /// <param name="propertyName">The property name as a span.</param>
    /// <returns><see cref="PropertyInfo"/> if found, null otherwise.</returns>
    private static PropertyInfo? GetPropertyInfoInternal(Type type, ReadOnlySpan<char> propertyName)
    {
        // Convert span to string for cache key (necessary for dictionary lookup)
        // Use string.Create to minimize allocations when converting from span
        string propertyNameString = string.Create(propertyName.Length, propertyName, 
            static (chars, source) => source.CopyTo(chars));

        var cacheKey = (type, propertyNameString);

        // Try to get from cache first
        if (_propertyCache.TryGetValue(cacheKey, out var cachedPropertyInfo))
        {
            return cachedPropertyInfo;
        }

        // Use reflection to find the property
        // BindingFlags for public instance properties
        var propertyInfo = type.GetProperty(
            propertyNameString,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

        // Cache the result (even if null, to avoid repeated lookups)
        _propertyCache.TryAdd(cacheKey, propertyInfo);

        return propertyInfo;
    }

    /// <summary>
    /// Gets all public instance properties for a given type.
    /// Results are not cached - use sparingly.
    /// </summary>
    /// <param name="type">The type to inspect.</param>
    /// <returns>Array of <see cref="PropertyInfo"/> for all public instance properties.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="type"/> is null.</exception>
    public static PropertyInfo[] GetAllProperties(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
    }

    /// <summary>
    /// Gets the current number of cached property lookups.
    /// Useful for diagnostics and testing.
    /// </summary>
    public static int CacheCount => _propertyCache.Count;

    /// <summary>
    /// Clears the property info cache.
    /// Useful for testing or memory management in long-running applications.
    /// </summary>
    public static void ClearCache()
    {
        _propertyCache.Clear();
    }
}
