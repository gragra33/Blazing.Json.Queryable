using Xunit;

namespace Blazing.Json.Queryable.Tests;

/// <summary>
/// Defines the <c>SpanPropertyAccessorCache</c> test collection.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="DisableParallelization"/> is set to <see langword="true"/> so that when any
/// test in this collection executes, no other test collection runs concurrently.
/// </para>
/// <para>
/// This is required because <see cref="Blazing.Json.Queryable.Implementations.SpanPropertyAccessor"/>
/// uses a process-wide static <see cref="System.Collections.Concurrent.ConcurrentDictionary{TKey, TValue}"/>
/// cache. Tests in this collection call <c>ClearCache()</c> and assert on <c>CacheCount</c> —
/// assertions that are only valid when no other test is concurrently populating the cache.
/// </para>
/// </remarks>
[CollectionDefinition("SpanPropertyAccessorCache", DisableParallelization = true)]
public class SpanPropertyAccessorCacheCollection { }
