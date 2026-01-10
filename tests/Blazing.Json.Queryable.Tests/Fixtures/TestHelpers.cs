using Shouldly;

namespace Blazing.Json.Queryable.Tests.Fixtures;

/// <summary>
/// Helper methods for test assertions and common testing patterns.
/// </summary>
public static class TestHelpers
{
    /// <summary>
    /// Asserts that two collections are equal (ignoring order).
    /// </summary>
    public static void ShouldContainSameItems<T>(this IEnumerable<T> actual, IEnumerable<T> expected)
    {
        var actualList = actual.ToList();
        var expectedList = expected.ToList();

        actualList.Count.ShouldBe(expectedList.Count);
        
        foreach (var item in expectedList)
        {
            actualList.ShouldContain(item);
        }
    }

    /// <summary>
    /// Asserts that a collection is ordered by a specific property.
    /// </summary>
    public static void ShouldBeOrderedBy<T, TKey>(this IEnumerable<T> collection, Func<T, TKey> keySelector, bool descending = false) 
        where TKey : IComparable<TKey>
    {
        var list = collection.ToList();
        var sorted = descending 
            ? list.OrderByDescending(keySelector).ToList() 
            : list.OrderBy(keySelector).ToList();

        for (int i = 0; i < list.Count; i++)
        {
            list[i].ShouldBe(sorted[i]);
        }
    }

    /// <summary>
    /// Creates a temporary file with JSON content and returns the file path.
    /// File will be automatically deleted when the returned IDisposable is disposed.
    /// </summary>
    public static TempFile CreateTempJsonFile(string jsonContent)
    {
        var tempPath = Path.GetTempFileName();
        File.WriteAllText(tempPath, jsonContent);
        return new TempFile(tempPath);
    }

    /// <summary>
    /// Creates a temporary file with UTF-8 JSON bytes and returns the file path.
    /// File will be automatically deleted when the returned IDisposable is disposed.
    /// </summary>
    public static TempFile CreateTempJsonFile(byte[] utf8JsonContent)
    {
        var tempPath = Path.GetTempFileName();
        File.WriteAllBytes(tempPath, utf8JsonContent);
        return new TempFile(tempPath);
    }

    /// <summary>
    /// Measures memory allocations during an action execution.
    /// Returns approximate bytes allocated (best effort).
    /// </summary>
    public static long MeasureAllocations(Action action)
    {
        // Force GC to get baseline
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        long before = GC.GetTotalMemory(forceFullCollection: true);

        // Execute action
        action();

        long after = GC.GetTotalMemory(forceFullCollection: false);

        return after - before;
    }

    /// <summary>
    /// Asserts that an action allocates less than a specified amount of memory.
    /// </summary>
    public static void ShouldAllocateLessThan(this Action action, long maxBytes)
    {
        var allocated = MeasureAllocations(action);
        allocated.ShouldBeLessThan(maxBytes, $"Expected less than {maxBytes} bytes allocated, but allocated {allocated} bytes");
    }

    /// <summary>
    /// Represents a temporary file that will be deleted on disposal.
    /// </summary>
    public class TempFile : IDisposable
    {
        public string Path { get; }

        internal TempFile(string path)
        {
            Path = path;
        }

        public void Dispose()
        {
            try
            {
                if (File.Exists(Path))
                {
                    File.Delete(Path);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    /// <summary>
    /// Creates a memory stream from a string.
    /// </summary>
    public static MemoryStream CreateMemoryStream(string content)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(content);
        return new MemoryStream(bytes);
    }

    /// <summary>
    /// Creates a memory stream from UTF-8 bytes.
    /// </summary>
    public static MemoryStream CreateMemoryStream(byte[] utf8Bytes)
    {
        return new MemoryStream(utf8Bytes);
    }

    /// <summary>
    /// Asserts that an async operation completes within a timeout.
    /// </summary>
    public static async Task ShouldCompleteWithin(this Task task, TimeSpan timeout)
    {
        var completed = await Task.WhenAny(task, Task.Delay(timeout));
        completed.ShouldBe(task, $"Task did not complete within {timeout}");
        
        // Propagate any exceptions
        await task;
    }

    /// <summary>
    /// Asserts that an async operation completes within a timeout.
    /// </summary>
    public static async Task<T> ShouldCompleteWithin<T>(this Task<T> task, TimeSpan timeout)
    {
        var completed = await Task.WhenAny(task, Task.Delay(timeout));
        completed.ShouldBe(task, $"Task did not complete within {timeout}");
        
        // Propagate any exceptions and return result
        return await task;
    }
}
