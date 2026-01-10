using System.Text.Json;

namespace Blazing.Json.Queryable.Samples.Data;

/// <summary>
/// Utility class to generate large JSON dataset files for performance testing.
/// Run this once to generate large-dataset.json and huge-dataset.json.
/// </summary>
public static class DatasetGenerator
{
    public static void GenerateLargeDataset(string filePath, int recordCount)
    {
        Console.WriteLine($"Generating {recordCount:N0} records to {filePath}...");
        
        using var fileStream = File.Create(filePath);
        using var writer = new Utf8JsonWriter(fileStream, new JsonWriterOptions { Indented = false });
        
        writer.WriteStartArray();
        
        var random = new Random(42); // Fixed seed for reproducibility
        var cities = new[] { "London", "New York", "Paris", "Tokyo", "Seoul", "Beijing", "Sydney", "Mumbai", "Berlin", "Toronto" };
        var firstNames = new[] { "Alice", "Bob", "Charlie", "Diana", "Eve", "Frank", "Grace", "Henry", "Iris", "Jack" };
        var lastNames = new[] { "Johnson", "Smith", "Brown", "Prince", "Wilson", "Miller", "Lee", "Davis", "Chen", "Robinson" };
        
        for (int i = 0; i < recordCount; i++)
        {
            writer.WriteStartObject();
            writer.WriteNumber("id", i + 1);
            writer.WriteString("name", $"{firstNames[random.Next(firstNames.Length)]} {lastNames[random.Next(lastNames.Length)]}");
            writer.WriteNumber("age", random.Next(18, 70));
            writer.WriteString("city", cities[random.Next(cities.Length)]);
            writer.WriteString("email", $"user{i + 1}@example.com");
            writer.WriteBoolean("isActive", random.Next(100) > 20); // 80% active
            writer.WriteEndObject();
            
            if ((i + 1) % 10000 == 0)
            {
                Console.WriteLine($"  Generated {i + 1:N0} records...");
            }
        }
        
        writer.WriteEndArray();
        writer.Flush();
        
        var fileInfo = new FileInfo(filePath);
        Console.WriteLine($"* Generated {recordCount:N0} records ({fileInfo.Length / 1024.0 / 1024.0:F2} MB)");
    }
    
    public static void GenerateAllDatasets()
    {
        var baseDir = Path.Combine(AppContext.BaseDirectory, "Data");
        Directory.CreateDirectory(baseDir);
        
        // Generate large dataset (100K records, ~10MB)
        var largeDatasetPath = Path.Combine(baseDir, "large-dataset.json");
        if (!File.Exists(largeDatasetPath))
        {
            GenerateLargeDataset(largeDatasetPath, 100_000);
        }
        
        // Generate huge dataset (1M records, ~100MB)
        var hugeDatasetPath = Path.Combine(baseDir, "huge-dataset.json");
        if (!File.Exists(hugeDatasetPath))
        {
            GenerateLargeDataset(hugeDatasetPath, 1_000_000);
        }
    }
}
