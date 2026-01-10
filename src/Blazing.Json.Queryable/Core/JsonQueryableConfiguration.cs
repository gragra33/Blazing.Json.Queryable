using System.Text.Json;
using Blazing.Json.Queryable.Exceptions;

namespace Blazing.Json.Queryable.Core;

/// <summary>
/// Configuration for JsonQueryable provider, managing evaluator, deserializer, and accessor implementations.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Performance Note:</strong> Async execution does not require separate configuration.
/// The same evaluator, deserializer, and accessor instances work for both sync and async queries.
/// Only the executor method (Execute vs ExecuteAsync) differs.
/// </para>
/// </remarks>
public class JsonQueryableConfiguration
{
    /// <summary>
    /// Gets the expression evaluator for building predicates and selectors.
    /// </summary>
    public IExpressionEvaluator ExpressionEvaluator { get; init; } = null!;

    /// <summary>
    /// Gets the JSON deserializer for parsing UTF-8 JSON data.
    /// </summary>
    public IJsonDeserializer JsonDeserializer { get; init; } = null!;

    /// <summary>
    /// Gets the property accessor for retrieving object properties.
    /// </summary>
    public IPropertyAccessor PropertyAccessor { get; init; } = null!;

    /// <summary>
    /// Gets the JSON serializer options for deserialization behavior.
    /// Supports custom converters, naming policies, and other JSON settings.
    /// </summary>
    public JsonSerializerOptions? SerializerOptions { get; init; }

    /// <summary>
    /// Creates the default configuration (Compile/Reflection).
    /// </summary>
    /// <param name="options">Optional JSON serializer options for custom deserialization behavior</param>
    /// <returns>A configuration instance</returns>
    /// <remarks>
    /// <para>
    /// <strong>Characteristics:</strong>
    /// <list type="bullet">
    /// <item>Uses Expression.Compile() for fast predicate evaluation</item>
    /// <item>Uses reflection with PropertyInfo caching for property access</item>
    /// <item>Uses System.Text.Json with span-based APIs for deserialization</item>
    /// <item>Production-ready for non-AOT scenarios</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Performance:</strong> Provides excellent performance through:
    /// <list type="bullet">
    /// <item>Compiled expressions (near-native speed)</item>
    /// <item>PropertyInfo caching (reflection cost paid once)</item>
    /// <item>UTF-8 span-based processing (zero encoding conversions)</item>
    /// <item>stackalloc buffers for sync streaming (zero heap allocation)</item>
    /// <item>ArrayPool buffers for async streaming (pooled, reused)</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Default configuration
    /// var config = JsonQueryableConfiguration.Default();
    /// 
    /// // With custom options
    /// var options = new JsonSerializerOptions
    /// {
    ///     PropertyNameCaseInsensitive = true,
    ///     Converters = { new CustomDateTimeConverter() }
    /// };
    /// var config = JsonQueryableConfiguration.Default(options);
    /// </code>
    /// </example>
    public static JsonQueryableConfiguration Default(JsonSerializerOptions? options = null)
    {
        return new JsonQueryableConfiguration
        {
            ExpressionEvaluator = new Implementations.CompiledExpressionEvaluator(),
            JsonDeserializer = new Implementations.SpanJsonDeserializer(options),
            PropertyAccessor = new Implementations.SpanPropertyAccessor(),
            SerializerOptions = options
        };
    }

    /// <summary>
    /// Validates the configuration to ensure all required components are present.
    /// </summary>
    /// <exception cref="ConfigurationException">Thrown if any required component is null</exception>
    /// <remarks>
    /// <para>
    /// Validation checks:
    /// <list type="bullet">
    /// <item>ExpressionEvaluator is not null</item>
    /// <item>JsonDeserializer is not null</item>
    /// <item>PropertyAccessor is not null</item>
    /// </list>
    /// SerializerOptions is optional and may be null (defaults to JsonSerializerOptions.Default).
    /// </para>
    /// </remarks>
    public void Validate()
    {
        if (ExpressionEvaluator == null)
        {
            throw new ConfigurationException(
                "ExpressionEvaluator cannot be null. Use JsonQueryableConfiguration.Default() or provide a custom implementation.",
                nameof(ExpressionEvaluator));
        }

        if (JsonDeserializer == null)
        {
            throw new ConfigurationException(
                "JsonDeserializer cannot be null. Use JsonQueryableConfiguration.Default() or provide a custom implementation.",
                nameof(JsonDeserializer));
        }

        if (PropertyAccessor == null)
        {
            throw new ConfigurationException(
                "PropertyAccessor cannot be null. Use JsonQueryableConfiguration.Default() or provide a custom implementation.",
                nameof(PropertyAccessor));
        }

        // SerializerOptions is optional - null means use System.Text.Json defaults
    }
}
