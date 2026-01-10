using System.Text.Json;

namespace Blazing.Json.Queryable.Benchmarks;

/// <summary>
/// Helper methods for benchmark data generation and validation.
/// </summary>
public static class BenchmarkHelpers
{
    /// <summary>
    /// Generate person dataset for benchmarks.
    /// </summary>
    public static List<Person> GeneratePersonDataset(int count)
    {
        var people = new List<Person>(count);
        var cities = new[] { "New York", "Los Angeles", "Chicago", "Houston", "Phoenix", "Philadelphia", "San Antonio", "San Diego", "Dallas", "San Jose" };
        var random = new Random(42); // Fixed seed for reproducibility

        for (int i = 1; i <= count; i++)
        {
            people.Add(new Person
            {
                Id = i,
                Name = $"Person {i}",
                Age = random.Next(18, 70),
                City = cities[random.Next(cities.Length)],
                Email = $"person{i}@example.com",
                IsActive = random.Next(2) == 1,
                CreatedDate = DateTime.Now.AddDays(-random.Next(1, 365))
            });
        }

        return people;
    }

    /// <summary>
    /// Serialize data to JSON string.
    /// </summary>
    public static string ToJsonString<T>(List<T> data)
    {
        return JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = false });
    }

    /// <summary>
    /// Serialize data to UTF-8 bytes.
    /// </summary>
    public static byte[] ToUtf8Bytes<T>(List<T> data)
    {
        return JsonSerializer.SerializeToUtf8Bytes(data, new JsonSerializerOptions { WriteIndented = false });
    }

    /// <summary>
    /// Create a temporary file with JSON data and return the path.
    /// </summary>
    public static string CreateTempJsonFile<T>(List<T> data, string prefix = "benchmark")
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"{prefix}_{Guid.NewGuid()}.json");
        var json = ToJsonString(data);
        File.WriteAllText(tempPath, json);
        return tempPath;
    }

    /// <summary>
    /// Create a temporary file with UTF-8 JSON data and return the path.
    /// </summary>
    public static string CreateTempUtf8JsonFile<T>(List<T> data, string prefix = "benchmark")
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"{prefix}_{Guid.NewGuid()}.json");
        var utf8Bytes = ToUtf8Bytes(data);
        File.WriteAllBytes(tempPath, utf8Bytes);
        return tempPath;
    }

    /// <summary>
    /// Cleanup temp file.
    /// </summary>
    public static void CleanupTempFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    /// <summary>
    /// Standard query for benchmarks: filter adults in New York, order by name, take 10.
    /// </summary>
    public static IEnumerable<Person> StandardQueryLinq(List<Person> people)
    {
        return people
            .Where(p => p.Age > 25 && p.City == "New York")
            .OrderBy(p => p.Name)
            .Take(10);
    }
}
