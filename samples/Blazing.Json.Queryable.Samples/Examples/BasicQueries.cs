using Blazing.Json.Queryable.Providers;

namespace Blazing.Json.Queryable.Samples.Examples;

/// <summary>
/// Demonstrates basic LINQ query operations using Blazing.Json.Queryable.
/// Shows fundamental operations: Where, Select, OrderBy, Take, Skip, First, Single, Count, Any.
/// </summary>
public static class BasicQueries
{
    public static void RunAll()
    {
        Console.WriteLine("=== Basic Queries Examples ===\n");
        
        SimpleWhereQuery();
        SelectProjection();
        OrderByAndTake();
        SkipAndTake();
        FirstAndSingle();
        CountAndAny();
        CombinedOperations();
        AnonymousTypeProjection();
        
        Console.WriteLine("\n=== Basic Queries Complete ===\n");
    }
    
    /// <summary>
    /// Simple Where filter query.
    /// </summary>
    private static void SimpleWhereQuery()
    {
        Console.WriteLine("1. Simple Where Query (Age > 30)");
        
        var jsonPath = Path.Combine(AppContext.BaseDirectory, "Data", "people.json");
        var json = File.ReadAllText(jsonPath);
        
        var results = JsonQueryable<Person>.FromString(json)
            .Where(p => p.Age > 30)
            .ToList();
        
        Console.WriteLine($"   Found {results.Count} people over 30:");
        foreach (var person in results)
        {
            Console.WriteLine($"   - {person.Name}, Age: {person.Age}");
        }
        Console.WriteLine();
    }
    
    /// <summary>
    /// Select projection to extract specific properties.
    /// </summary>
    private static void SelectProjection()
    {
        Console.WriteLine("2. Select Projection (Names Only)");
        
        var jsonPath = Path.Combine(AppContext.BaseDirectory, "Data", "people.json");
        var json = File.ReadAllText(jsonPath);
        
        var names = JsonQueryable<Person>.FromString(json)
            .Where(p => p.IsActive)
            .Select(p => p.Name)
            .ToList();
        
        Console.WriteLine($"   Active users ({names.Count}):");
        foreach (var name in names)
        {
            Console.WriteLine($"   - {name}");
        }
        Console.WriteLine();
    }
    
    /// <summary>
    /// OrderBy with Take to get top N results.
    /// </summary>
    private static void OrderByAndTake()
    {
        Console.WriteLine("3. OrderBy and Take (Top 5 Youngest)");
        
        var jsonPath = Path.Combine(AppContext.BaseDirectory, "Data", "people.json");
        var json = File.ReadAllText(jsonPath);
        
        var youngest = JsonQueryable<Person>.FromString(json)
            .OrderBy(p => p.Age)
            .Take(5)
            .ToList();
        
        Console.WriteLine("   Top 5 youngest people:");
        foreach (var person in youngest)
        {
            Console.WriteLine($"   - {person.Name}, Age: {person.Age}");
        }
        Console.WriteLine();
    }
    
    /// <summary>
    /// Skip and Take for pagination.
    /// </summary>
    private static void SkipAndTake()
    {
        Console.WriteLine("4. Skip and Take (Pagination - Page 2, Size 3)");
        
        var jsonPath = Path.Combine(AppContext.BaseDirectory, "Data", "people.json");
        var json = File.ReadAllText(jsonPath);
        
        var page = JsonQueryable<Person>.FromString(json)
            .OrderBy(p => p.Name)
            .Skip(3)
            .Take(3)
            .ToList();
        
        Console.WriteLine("   Page 2 (items 4-6):");
        foreach (var person in page)
        {
            Console.WriteLine($"   - {person.Name}");
        }
        Console.WriteLine();
    }
    
    /// <summary>
    /// First and Single operations.
    /// </summary>
    private static void FirstAndSingle()
    {
        Console.WriteLine("5. First and FirstOrDefault");
        
        var jsonPath = Path.Combine(AppContext.BaseDirectory, "Data", "people.json");
        var json = File.ReadAllText(jsonPath);
        
        var queryable = JsonQueryable<Person>.FromString(json);
        
        var firstLondon = queryable.Where(p => p.City == "London").First();
        Console.WriteLine($"   First person in London: {firstLondon.Name}");
        
        var firstParis = queryable.Where(p => p.City == "Paris").FirstOrDefault();
        Console.WriteLine($"   First person in Paris: {firstParis?.Name ?? "None"}");
        
        var firstMars = queryable.Where(p => p.City == "Mars").FirstOrDefault();
        Console.WriteLine($"   First person on Mars: {firstMars?.Name ?? "None"}");
        Console.WriteLine();
    }
    
    /// <summary>
    /// Count and Any operations.
    /// </summary>
    private static void CountAndAny()
    {
        Console.WriteLine("6. Count and Any");
        
        var jsonPath = Path.Combine(AppContext.BaseDirectory, "Data", "people.json");
        var json = File.ReadAllText(jsonPath);
        
        var queryable = JsonQueryable<Person>.FromString(json);
        
        var totalCount = queryable.Count();
        Console.WriteLine($"   Total people: {totalCount}");
        
        var activeCount = queryable.Count(p => p.IsActive);
        Console.WriteLine($"   Active people: {activeCount}");
        
        var hasLondon = queryable.Any(p => p.City == "London");
        Console.WriteLine($"   Has people in London: {hasLondon}");
        
        var hasMars = queryable.Any(p => p.City == "Mars");
        Console.WriteLine($"   Has people on Mars: {hasMars}");
        Console.WriteLine();
    }
    
    /// <summary>
    /// Combined operations in a single query.
    /// </summary>
    private static void CombinedOperations()
    {
        Console.WriteLine("7. Combined Operations (Filter + Sort + Project + Limit)");
        
        var jsonPath = Path.Combine(AppContext.BaseDirectory, "Data", "people.json");
        var json = File.ReadAllText(jsonPath);
        
        var results = JsonQueryable<Person>.FromString(json)
            .Where(p => p.Age >= 25 && p.Age <= 35)
            .OrderByDescending(p => p.Age)
            .Select(p => p.Name)
            .Take(5)
            .ToList();
        
        Console.WriteLine("   Top 5 people aged 25-35 (oldest first):");
        foreach (var name in results)
        {
            Console.WriteLine($"   - {name}");
        }
        Console.WriteLine();
    }
    
    /// <summary>
    /// Anonymous type projection with computed fields.
    /// </summary>
    private static void AnonymousTypeProjection()
    {
        Console.WriteLine("8. Anonymous Type Projection");
        
        var jsonPath = Path.Combine(AppContext.BaseDirectory, "Data", "people.json");
        var json = File.ReadAllText(jsonPath);
        
        var results = JsonQueryable<Person>.FromString(json)
            .Where(p => p.IsActive)
            .Select(p => new
            {
                p.Name,
                p.City,
                IsAdult = p.Age >= 18,
                AgeGroup = p.Age < 25 ? "Young" : p.Age < 40 ? "Adult" : "Senior"
            })
            .OrderBy(p => p.City)
            .ToList();
        
        Console.WriteLine($"   Active people with age groups ({results.Count}):");
        foreach (var item in results)
        {
            Console.WriteLine($"   - {item.Name} ({item.City}) - {item.AgeGroup}");
        }
        Console.WriteLine();
    }
}

/// <summary>
/// Person model for sample queries.
/// </summary>
public record Person(int Id, string Name, int Age, string City, string Email, bool IsActive);
