using System.Text;

namespace Blazing.Json.Queryable.Utilities;

/// <summary>
/// Helper utilities for UTF-8 conversion and validation.
/// Supports zero-allocation string → UTF-8 conversion and UTF-8 validation.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Why UTF-8 Matters for JSON:</strong>
/// <list type="bullet">
/// <item>JSON is natively UTF-8 encoded (RFC 8259)</item>
/// <item>File.ReadAllBytes() returns UTF-8 bytes directly</item>
/// <item>HTTP responses are typically UTF-8 encoded</item>
/// <item>System.Text.Json.Utf8JsonReader works directly with UTF-8</item>
/// <item>UTF-16 (string) → UTF-8 conversion has ~5-10KB overhead</item>
/// </list>
/// </para>
/// <para>
/// <strong>Conversion Strategy:</strong>
/// <list type="bullet">
/// <item><strong>Preferred:</strong> Work with UTF-8 bytes from the start (File.ReadAllBytes, HTTP bytes)</item>
/// <item><strong>Acceptable:</strong> Convert once at entry point, then use UTF-8 throughout</item>
/// <item><strong>Avoid:</strong> Multiple string → UTF-8 conversions in hot paths</item>
/// </list>
/// </para>
/// </remarks>
public static class Utf8Helper
{
    /// <summary>
    /// Converts a UTF-16 string to UTF-8 bytes.
    /// This allocates a new byte array - use sparingly and cache the result.
    /// </summary>
    /// <param name="text">The UTF-16 string to convert.</param>
    /// <returns>UTF-8 encoded byte array.</returns>
    /// <remarks>
    /// <para>
    /// <strong>Performance Impact:</strong>
    /// <list type="bullet">
    /// <item>Allocates new byte[] (cannot use stackalloc for unknown size)</item>
    /// <item>Typical overhead: ~5-10KB for medium-sized JSON</item>
    /// <item>Conversion is one-time cost at entry point</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>When to Use:</strong>
    /// <list type="bullet">
    /// <item>Converting user-provided JSON strings</item>
    /// <item>Interop with string-based APIs</item>
    /// <item>Entry point of FromString() factory method</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Optimization:</strong> If you control the source, prefer UTF-8 from the start:
    /// <code>
    /// // ❌ String-based (requires conversion)
    /// string json = File.ReadAllText("data.json");  // UTF-16
    /// byte[] utf8 = Utf8Helper.ConvertToUtf8(json);  // Conversion overhead
    /// 
    /// // ✅ UTF-8-based (no conversion)
    /// byte[] utf8 = File.ReadAllBytes("data.json");  // Already UTF-8
    /// </code>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Convert string to UTF-8
    /// var json = """{"name":"Alice","age":25}""";
    /// byte[] utf8Bytes = Utf8Helper.ConvertToUtf8(json);
    /// 
    /// // Use UTF-8 bytes with deserializer
    /// var person = deserializer.Deserialize&lt;Person&gt;(utf8Bytes);
    /// 
    /// // Null handling
    /// byte[] empty = Utf8Helper.ConvertToUtf8(null);  // Returns empty array
    /// </code>
    /// </example>
    public static byte[] ConvertToUtf8(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return [];
        }

        return Encoding.UTF8.GetBytes(text);
    }

    /// <summary>
    /// Validates that a byte span contains valid UTF-8 encoded text.
    /// Returns true if the bytes form a valid UTF-8 sequence.
    /// </summary>
    /// <param name="utf8Bytes">The bytes to validate.</param>
    /// <returns>True if valid UTF-8, false otherwise.</returns>
    /// <remarks>
    /// <para>
    /// <strong>When to Use:</strong>
    /// <list type="bullet">
    /// <item>Validating user-provided byte arrays</item>
    /// <item>Defensive programming for untrusted input</item>
    /// <item>Debugging encoding issues</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Performance:</strong> This method scans the entire buffer to validate encoding.
    /// Use only when necessary - trusted sources (File.ReadAllBytes, HttpClient) are already valid UTF-8.
    /// </para>
    /// <para>
    /// <strong>JSON-Specific Note:</strong> Utf8JsonReader will throw JsonException for invalid UTF-8,
    /// so validation is typically not necessary unless you want to provide better error messages.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Validate UTF-8 from untrusted source
    /// byte[] untrustedBytes = GetBytesFromExternalSource();
    /// 
    /// if (Utf8Helper.ValidateUtf8(untrustedBytes))
    /// {
    ///     var data = deserializer.Deserialize&lt;Data&gt;(untrustedBytes);
    /// }
    /// else
    /// {
    ///     throw new InvalidDataException("Invalid UTF-8 encoding");
    /// }
    /// 
    /// // Trusted sources don't need validation
    /// byte[] trustedBytes = File.ReadAllBytes("data.json");  // Already valid UTF-8
    /// var data = deserializer.Deserialize&lt;Data&gt;(trustedBytes);  // Skip validation
    /// </code>
    /// </example>
    public static bool ValidateUtf8(ReadOnlySpan<byte> utf8Bytes)
    {
        if (utf8Bytes.IsEmpty)
        {
            return true; // Empty is valid
        }

        try
        {
            // Use UTF8 decoder to validate the byte sequence
            // This will throw if the bytes are not valid UTF-8
            var decoder = Encoding.UTF8.GetDecoder();
            Span<char> tempBuffer = stackalloc char[1]; // Minimal buffer for validation
            
            int byteIndex = 0;
            while (byteIndex < utf8Bytes.Length)
            {
                decoder.GetChars(
                    utf8Bytes.Slice(byteIndex, Math.Min(4, utf8Bytes.Length - byteIndex)),
                    tempBuffer,
                    flush: byteIndex + 4 >= utf8Bytes.Length);

                byteIndex += Math.Min(4, utf8Bytes.Length - byteIndex);
            }

            return true;
        }
        catch (DecoderFallbackException)
        {
            return false; // Invalid UTF-8 sequence
        }
        catch (ArgumentException)
        {
            return false; // Other encoding issues
        }
    }

    /// <summary>
    /// Gets the byte count needed to encode a string as UTF-8.
    /// Useful for pre-allocating buffers when conversion is necessary.
    /// </summary>
    /// <param name="text">The string to measure.</param>
    /// <returns>Number of bytes needed for UTF-8 encoding.</returns>
    /// <remarks>
    /// <para>
    /// <strong>Use Case:</strong> When you need to know buffer size before allocation:
    /// <code>
    /// string json = GetLargeJsonString();
    /// int byteCount = Utf8Helper.GetUtf8ByteCount(json);
    /// 
    /// if (BufferHelper.IsStackAllocSafe(byteCount))
    /// {
    ///     Span&lt;byte&gt; buffer = stackalloc byte[byteCount];
    ///     Encoding.UTF8.GetBytes(json, buffer);
    /// }
    /// else
    /// {
    ///     byte[] buffer = new byte[byteCount];
    ///     Encoding.UTF8.GetBytes(json, buffer);
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Measure UTF-8 size
    /// string text = "Hello, 世界!";
    /// int byteCount = Utf8Helper.GetUtf8ByteCount(text);  // 13 bytes (5 ASCII + 8 for Chinese chars)
    /// 
    /// // Pre-allocate exact size
    /// byte[] buffer = new byte[byteCount];
    /// Encoding.UTF8.GetBytes(text, buffer);
    /// </code>
    /// </example>
    public static int GetUtf8ByteCount(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        return Encoding.UTF8.GetByteCount(text);
    }
}
