using System.Buffers;

namespace Blazing.Json.Queryable.Utilities;

/// <summary>
/// Helper utilities for efficient buffer management using stackalloc and ArrayPool.
/// Provides guidance on when to use stack vs heap allocation strategies.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Buffer Strategy Decision Tree:</strong>
/// <list type="bullet">
/// <item><strong>Synchronous + Small (&lt;4KB):</strong> Use stackalloc (stack allocation)</item>
/// <item><strong>Synchronous + Large (>=4KB):</strong> Use ArrayPool (pooled heap)</item>
/// <item><strong>Asynchronous (any size):</strong> Use ArrayPool (cannot use stackalloc across await)</item>
/// </list>
/// </para>
/// <para>
/// <strong>Why 4KB Limit?</strong>
/// <list type="bullet">
/// <item>Stack size is limited (typically 1MB on Windows, 8MB on Linux)</item>
/// <item>Large stack allocations can cause StackOverflowException</item>
/// <item>4KB is conservative and safe for most scenarios</item>
/// <item>Aligns with common page size and cache line considerations</item>
/// </list>
/// </para>
/// <para>
/// <strong>Async Constraint:</strong>
/// stackalloc creates a Span&lt;T&gt; which is a ref struct. Ref structs:
/// <list type="bullet">
/// <item>Cannot cross await boundaries</item>
/// <item>Cannot be stored as fields</item>
/// <item>Must live entirely on the stack</item>
/// </list>
/// Therefore, async methods MUST use ArrayPool for buffer management.
/// </para>
/// </remarks>
public static class BufferHelper
{
    /// <summary>
    /// Maximum safe size for stackalloc in bytes.
    /// Buffers larger than this should use ArrayPool.
    /// </summary>
    public const int MaxStackAllocSize = 4096; // 4KB

    /// <summary>
    /// Checks if a buffer size is safe for stackalloc.
    /// Returns true if size is less than or equal to 4KB.
    /// </summary>
    /// <param name="sizeInBytes">The buffer size in bytes</param>
    /// <returns>True if safe for stackalloc, false if ArrayPool should be used</returns>
    /// <remarks>
    /// <para>
    /// <strong>Usage Guidelines:</strong>
    /// <code>
    /// if (BufferHelper.IsStackAllocSafe(bufferSize))
    /// {
    ///     // ✅ Safe for synchronous methods
    ///     Span&lt;byte&gt; buffer = stackalloc byte[bufferSize];
    ///     // Use buffer...
    /// }
    /// else
    /// {
    ///     // ✅ Use ArrayPool for large buffers or async
    ///     byte[] buffer = ArrayPool&lt;byte&gt;.Shared.Rent(bufferSize);
    ///     try
    ///     {
    ///         // Use buffer...
    ///     }
    ///     finally
    ///     {
    ///         ArrayPool&lt;byte&gt;.Shared.Return(buffer);
    ///     }
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Small buffer - use stackalloc
    /// if (BufferHelper.IsStackAllocSafe(1024))  // true
    /// {
    ///     Span&lt;byte&gt; small = stackalloc byte[1024];
    /// }
    /// 
    /// // Large buffer - use ArrayPool
    /// if (!BufferHelper.IsStackAllocSafe(8192))  // false
    /// {
    ///     byte[] large = ArrayPool&lt;byte&gt;.Shared.Rent(8192);
    ///     // ... use large ...
    ///     ArrayPool&lt;byte&gt;.Shared.Return(large);
    /// }
    /// </code>
    /// </example>
    public static bool IsStackAllocSafe(int sizeInBytes)
    {
        return sizeInBytes is > 0 and <= MaxStackAllocSize;
    }

    /// <summary>
    /// Rents a buffer from the ArrayPool with the specified minimum size.
    /// IMPORTANT: Must call ReturnPooledBuffer when done to avoid memory leaks.
    /// </summary>
    /// <typeparam name="T">The element type of the buffer</typeparam>
    /// <param name="minimumSize">The minimum size needed</param>
    /// <returns>A rented array (may be larger than requested)</returns>
    /// <remarks>
    /// <para>
    /// <strong>CRITICAL:</strong> Always return buffers in a finally block:
    /// <code>
    /// byte[] buffer = BufferHelper.RentPooledBuffer&lt;byte&gt;(4096);
    /// try
    /// {
    ///     // Use buffer...
    /// }
    /// finally
    /// {
    ///     BufferHelper.ReturnPooledBuffer(buffer);
    /// }
    /// </code>
    /// </para>
    /// <para>
    /// <strong>When to Use:</strong>
    /// <list type="bullet">
    /// <item>Buffer size >= 4KB</item>
    /// <item>Async methods (cannot use stackalloc)</item>
    /// <item>Buffers that need to survive across method calls</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>ArrayPool Behavior:</strong>
    /// <list type="bullet">
    /// <item>Returns array from pool (fast, no allocation if available)</item>
    /// <item>Returned array may be larger than requested (power-of-2 sizing)</item>
    /// <item>Returned array contents are NOT cleared (may contain old data)</item>
    /// <item>Use only the requested size, ignore extra capacity</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Async method - MUST use ArrayPool
    /// public async IAsyncEnumerable&lt;T&gt; ProcessAsync&lt;T&gt;()
    /// {
    ///     byte[] buffer = BufferHelper.RentPooledBuffer&lt;byte&gt;(4096);
    ///     try
    ///     {
    ///         while (await stream.ReadAsync(buffer) > 0)
    ///         {
    ///             // Process buffer...
    ///             yield return item;
    ///         }
    ///     }
    ///     finally
    ///     {
    ///         BufferHelper.ReturnPooledBuffer(buffer);  // ✅ CRITICAL
    ///     }
    /// }
    /// </code>
    /// </example>
    public static T[] RentPooledBuffer<T>(int minimumSize)
    {
        if (minimumSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumSize), "Buffer size must be positive");
        }

        return ArrayPool<T>.Shared.Rent(minimumSize);
    }

    /// <summary>
    /// Returns a rented buffer to the ArrayPool.
    /// MUST be called for every RentPooledBuffer call to avoid memory leaks.
    /// </summary>
    /// <typeparam name="T">The element type of the buffer</typeparam>
    /// <param name="buffer">The buffer to return</param>
    /// <param name="clearArray">Whether to clear the array before returning (default: false)</param>
    /// <remarks>
    /// <para>
    /// <strong>Clear Array Guidance:</strong>
    /// <list type="bullet">
    /// <item><strong>false (default):</strong> Faster, suitable for primitive types or when security is not a concern</item>
    /// <item><strong>true:</strong> Clears array contents, use for sensitive data or reference types to prevent leaks</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Best Practice:</strong> Always call in finally block:
    /// <code>
    /// byte[] buffer = null!;
    /// try
    /// {
    ///     buffer = BufferHelper.RentPooledBuffer&lt;byte&gt;(size);
    ///     // Use buffer...
    /// }
    /// finally
    /// {
    ///     if (buffer != null)
    ///     {
    ///         BufferHelper.ReturnPooledBuffer(buffer);
    ///     }
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Primitive types - no clear needed
    /// byte[] buffer = BufferHelper.RentPooledBuffer&lt;byte&gt;(1024);
    /// try
    /// {
    ///     // Use buffer...
    /// }
    /// finally
    /// {
    ///     BufferHelper.ReturnPooledBuffer(buffer, clearArray: false);  // Fast
    /// }
    /// 
    /// // Reference types or sensitive data - clear recommended
    /// Person[] personBuffer = BufferHelper.RentPooledBuffer&lt;Person&gt;(100);
    /// try
    /// {
    ///     // Use buffer...
    /// }
    /// finally
    /// {
    ///     BufferHelper.ReturnPooledBuffer(personBuffer, clearArray: true);  // Prevent leaks
    /// }
    /// </code>
    /// </example>
    public static void ReturnPooledBuffer<T>(T[]? buffer, bool clearArray = false)
    {
        if (buffer == null)
        {
            return; // Gracefully handle null (defensive programming)
        }

        ArrayPool<T>.Shared.Return(buffer, clearArray);
    }
}
