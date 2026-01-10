using System.Text.Json;

namespace Blazing.Json.Queryable.Tests.Fixtures;

/// <summary>
/// Provides test data for unit and integration tests.
/// </summary>
public static class TestData
{
    /// <summary>
    /// Small dataset for basic testing (10 people).
    /// </summary>
    public static List<Person> GetSmallPersonDataset() => new()
    {
        new Person { Id = 1, Name = "Alice Johnson", Age = 25, City = "New York", Email = "alice@example.com", IsActive = true, CreatedDate = new DateTime(2023, 1, 15) },
        new Person { Id = 2, Name = "Bob Smith", Age = 30, City = "Los Angeles", Email = "bob@example.com", IsActive = true, CreatedDate = new DateTime(2023, 2, 20) },
        new Person { Id = 3, Name = "Charlie Brown", Age = 35, City = "Chicago", Email = "charlie@example.com", IsActive = false, CreatedDate = new DateTime(2023, 3, 10) },
        new Person { Id = 4, Name = "Diana Prince", Age = 28, City = "New York", Email = "diana@example.com", IsActive = true, CreatedDate = new DateTime(2023, 4, 5) },
        new Person { Id = 5, Name = "Eve Davis", Age = 22, City = "Boston", Email = "eve@example.com", IsActive = true, CreatedDate = new DateTime(2023, 5, 12) },
        new Person { Id = 6, Name = "Frank Miller", Age = 45, City = "Seattle", Email = "frank@example.com", IsActive = false, CreatedDate = new DateTime(2023, 6, 18) },
        new Person { Id = 7, Name = "Grace Lee", Age = 32, City = "San Francisco", Email = "grace@example.com", IsActive = true, CreatedDate = new DateTime(2023, 7, 22) },
        new Person { Id = 8, Name = "Henry Wilson", Age = 29, City = "Austin", Email = "henry@example.com", IsActive = true, CreatedDate = new DateTime(2023, 8, 8) },
        new Person { Id = 9, Name = "Ivy Chen", Age = 26, City = "Denver", Email = "ivy@example.com", IsActive = false, CreatedDate = new DateTime(2023, 9, 14) },
        new Person { Id = 10, Name = "Jack Anderson", Age = 40, City = "Portland", Email = "jack@example.com", IsActive = true, CreatedDate = new DateTime(2023, 10, 1) }
    };

    /// <summary>
    /// Medium dataset for performance testing (100 people).
    /// </summary>
    public static List<Person> GetMediumPersonDataset()
    {
        var people = new List<Person>();
        var cities = new[] { "New York", "Los Angeles", "Chicago", "Houston", "Phoenix", "Philadelphia", "San Antonio", "San Diego", "Dallas", "San Jose" };
        var random = new Random(42); // Fixed seed for reproducibility

        for (int i = 1; i <= 100; i++)
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
    /// Large dataset for stress testing (1000 people).
    /// </summary>
    public static List<Person> GetLargePersonDataset()
    {
        var people = new List<Person>();
        var cities = new[] { "New York", "Los Angeles", "Chicago", "Houston", "Phoenix", "Philadelphia", "San Antonio", "San Diego", "Dallas", "San Jose" };
        var random = new Random(42); // Fixed seed for reproducibility

        for (int i = 1; i <= 1000; i++)
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
    /// Small product dataset for basic testing.
    /// </summary>
    public static List<Product> GetSmallProductDataset() => new()
    {
        new Product { Id = 1, Name = "Laptop", Price = 999.99m, Category = "Electronics", Stock = 50 },
        new Product { Id = 2, Name = "Mouse", Price = 29.99m, Category = "Electronics", Stock = 200 },
        new Product { Id = 3, Name = "Keyboard", Price = 79.99m, Category = "Electronics", Stock = 150 },
        new Product { Id = 4, Name = "Desk Chair", Price = 199.99m, Category = "Furniture", Stock = 75 },
        new Product { Id = 5, Name = "Notebook", Price = 4.99m, Category = "Stationery", Stock = 500 }
    };

    /// <summary>
    /// Product dataset for testing.
    /// </summary>
    public static List<Product> GetProductDataset() => new()
    {
        new Product { Id = 1, Name = "Laptop", Price = 999.99m, Category = "Electronics", Stock = 50, LastRestocked = DateTime.Now.AddDays(-10) },
        new Product { Id = 2, Name = "Mouse", Price = 29.99m, Category = "Electronics", Stock = 200, LastRestocked = DateTime.Now.AddDays(-5) },
        new Product { Id = 3, Name = "Keyboard", Price = 79.99m, Category = "Electronics", Stock = 150, LastRestocked = DateTime.Now.AddDays(-7) },
        new Product { Id = 4, Name = "Monitor", Price = 299.99m, Category = "Electronics", Stock = 0, LastRestocked = DateTime.Now.AddDays(-30) },
        new Product { Id = 5, Name = "Desk Chair", Price = 199.99m, Category = "Furniture", Stock = 75, LastRestocked = DateTime.Now.AddDays(-15) },
        new Product { Id = 6, Name = "Standing Desk", Price = 499.99m, Category = "Furniture", Stock = 30, LastRestocked = DateTime.Now.AddDays(-20) },
        new Product { Id = 7, Name = "Notebook", Price = 4.99m, Category = "Stationery", Stock = 500, LastRestocked = DateTime.Now.AddDays(-3) },
        new Product { Id = 8, Name = "Pen Set", Price = 12.99m, Category = "Stationery", Stock = 300, LastRestocked = DateTime.Now.AddDays(-2) },
        new Product { Id = 9, Name = "Headphones", Price = 149.99m, Category = "Electronics", Stock = 100, LastRestocked = DateTime.Now.AddDays(-12) },
        new Product { Id = 10, Name = "Webcam", Price = 89.99m, Category = "Electronics", Stock = 0, LastRestocked = null }
    };

    /// <summary>
    /// Serialize dataset to JSON string.
    /// </summary>
    public static string SerializeToJson<T>(List<T> data, JsonSerializerOptions? options = null)
    {
        return JsonSerializer.Serialize(data, options ?? new JsonSerializerOptions { WriteIndented = false });
    }

    /// <summary>
    /// Serialize dataset to UTF-8 bytes.
    /// </summary>
    public static byte[] SerializeToUtf8<T>(List<T> data, JsonSerializerOptions? options = null)
    {
        return JsonSerializer.SerializeToUtf8Bytes(data, options ?? new JsonSerializerOptions { WriteIndented = false });
    }

    /// <summary>
    /// Get empty JSON array string.
    /// </summary>
    public static string GetEmptyJsonArray() => "[]";

    /// <summary>
    /// Get empty JSON array as UTF-8 bytes.
    /// </summary>
    public static byte[] GetEmptyJsonArrayUtf8() => "[]"u8.ToArray();

    /// <summary>
    /// Get invalid JSON string.
    /// </summary>
    public static string GetInvalidJson() => "{invalid json}";

    /// <summary>
    /// Get invalid JSON as UTF-8 bytes.
    /// </summary>
    public static byte[] GetInvalidJsonUtf8() => System.Text.Encoding.UTF8.GetBytes(GetInvalidJson());

    /// <summary>
    /// Get JSON object (not array) for error testing.
    /// </summary>
    public static string GetJsonObject() => """{"name":"Alice","age":25}""";

    /// <summary>
    /// Get JSON object (not array) as UTF-8 bytes.
    /// </summary>
    public static byte[] GetJsonObjectUtf8() => System.Text.Encoding.UTF8.GetBytes(GetJsonObject());

    /// <summary>
    /// Get nullable model dataset for testing.
    /// </summary>
    public static List<NullableModel> GetNullableDataset() => new()
    {
        new NullableModel { Id = 1, Name = "Alice", Age = 25, CreatedDate = DateTime.Now, IsActive = true },
        new NullableModel { Id = 2, Name = null, Age = null, CreatedDate = null, IsActive = null },
        new NullableModel { Id = 3, Name = "Charlie", Age = 30, CreatedDate = DateTime.Now.AddDays(-10), IsActive = false },
        new NullableModel { Id = null, Name = "Diana", Age = 28, CreatedDate = DateTime.Now.AddDays(-5), IsActive = true },
        new NullableModel { Id = 5, Name = "Eve", Age = null, CreatedDate = DateTime.Now.AddDays(-15), IsActive = null }
    };
}
