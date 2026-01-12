using Blazing.Json.Queryable.Providers;
using Blazing.Json.Queryable.Samples.Models;

namespace Blazing.Json.Queryable.Samples.Examples;

/// <summary>
/// Demonstrates conversion operations using Blazing.Json.Queryable.
/// Shows terminal conversion operations: ToDictionary, ToHashSet, ToLookup.
/// These operations execute the query immediately and return materialized collections.
/// </summary>
public static class ConversionOperationsSamples
{
    public static void RunAll()
    {
        Console.WriteLine("=================================================================");
        Console.WriteLine(" Conversion Operations Examples");
        Console.WriteLine("=================================================================");
        Console.WriteLine();

        ToDictionaryBasicUsage();
        ToDictionaryWithElementSelector();
        ToDictionaryWithComparer();
        ToHashSetBasicUsage();
        ToHashSetWithComparer();
        ToLookupBasicUsage();
        ToLookupWithElementSelector();
        RealWorldScenarios();
    }

    /// <summary>
    /// ToDictionary basic usage - convert sequence to dictionary by key.
    /// </summary>
    private static void ToDictionaryBasicUsage()
    {
        Console.WriteLine("1. ToDictionary - Basic Key-Value Mapping");
        Console.WriteLine("   Convert sequence to Dictionary<TKey, TElement> for fast lookups");
        Console.WriteLine("-----------------------------------------------------------------");

        var json = """
        [
            {"Id":1,"Name":"Alice","Age":30,"Email":"alice@example.com","City":"London","Department":"Engineering","IsActive":true},
            {"Id":2,"Name":"Bob","Age":25,"Email":"bob@example.com","City":"Paris","Department":"Sales","IsActive":true},
            {"Id":3,"Name":"Charlie","Age":35,"Email":"charlie@example.com","City":"London","Department":"Engineering","IsActive":true}
        ]
        """;

        // Convert to dictionary using Id as key
        var peopleById = JsonQueryable<PersonConv>.FromString(json)
            .ToDictionary(p => p.Id);

        Console.WriteLine($"   Created Dictionary<int, Person> with {peopleById.Count} entries");
        Console.WriteLine("\n   Fast lookup by ID:");
        Console.WriteLine($"      peopleById[2] = {peopleById[2].Name}, {peopleById[2].Age}");
        Console.WriteLine($"      peopleById[1] = {peopleById[1].Name}, {peopleById[1].Age}");

        // Convert to dictionary using Email as key
        var peopleByEmail = JsonQueryable<PersonConv>.FromString(json)
            .Where(p => p.Age > 25)
            .ToDictionary(p => p.Email);

        Console.WriteLine($"\n   Filtered Dictionary (Age > 25): {peopleByEmail.Count} entries");
        foreach (var kvp in peopleByEmail)
        {
            Console.WriteLine($"      {kvp.Key} -> {kvp.Value.Name}");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// ToDictionary with element selector - project values during conversion.
    /// </summary>
    private static void ToDictionaryWithElementSelector()
    {
        Console.WriteLine("2. ToDictionary with Element Selector");
        Console.WriteLine("   Project values while creating the dictionary");
        Console.WriteLine("-----------------------------------------------------------------");

        var json = """
        [
            {"Id":1,"Name":"Alice","Age":30,"Department":"Engineering","Salary":95000},
            {"Id":2,"Name":"Bob","Age":25,"Department":"Sales","Salary":75000},
            {"Id":3,"Name":"Charlie","Age":35,"Department":"Engineering","Salary":105000}
        ]
        """;

        // Dictionary<int, string> - ID to Name mapping
        var idToName = JsonQueryable<EmployeeConv>.FromString(json)
            .ToDictionary(
                keySelector: e => e.Id,
                elementSelector: e => e.Name
            );

        Console.WriteLine("   Dictionary<int, string> - ID to Name:");
        foreach (var kvp in idToName)
        {
            Console.WriteLine($"      {kvp.Key} -> {kvp.Value}");
        }

        // Dictionary<string, decimal> - Name to Salary mapping
        var salaryByName = JsonQueryable<EmployeeConv>.FromString(json)
            .ToDictionary(
                keySelector: e => e.Name,
                elementSelector: e => e.Salary
            );

        Console.WriteLine("\n   Dictionary<string, decimal> - Name to Salary:");
        foreach (var kvp in salaryByName.OrderByDescending(x => x.Value))
        {
            Console.WriteLine($"      {kvp.Key} -> ${kvp.Value:N0}");
        }

        // Anonymous type projection
        var employeeInfo = JsonQueryable<EmployeeConv>.FromString(json)
            .Where(e => e.Department == "Engineering")
            .ToDictionary(
                keySelector: e => e.Id,
                elementSelector: e => new { e.Name, e.Salary, e.Department }
            );

        Console.WriteLine("\n   Dictionary<int, AnonymousType> - Engineering employees:");
        foreach (var kvp in employeeInfo)
        {
            Console.WriteLine($"      {kvp.Key} -> {kvp.Value.Name} (${kvp.Value.Salary:N0})");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// ToDictionary with custom equality comparer.
    /// </summary>
    private static void ToDictionaryWithComparer()
    {
        Console.WriteLine("3. ToDictionary with Custom Comparer");
        Console.WriteLine("   Use case-insensitive or custom equality for keys");
        Console.WriteLine("-----------------------------------------------------------------");

        var json = """
        [
            {"ProductId":1,"Code":"PROD-001","Name":"Widget","Category":"Tools","Price":15.00,"Stock":100,"Rating":4.5},
            {"ProductId":2,"Code":"PROD-002","Name":"Gadget","Category":"Electronics","Price":25.00,"Stock":200,"Rating":4.2},
            {"ProductId":3,"Code":"prod-003","Name":"Device","Category":"Electronics","Price":150.00,"Stock":50,"Rating":4.8}
        ]
        """;

        // Case-sensitive (default) - "PROD-003" and "prod-003" are different
        var caseSensitive = JsonQueryable<ProductConv>.FromString(json)
            .ToDictionary(p => p.Code);
        Console.WriteLine($"   Case-sensitive dictionary: {caseSensitive.Count} entries");
        Console.WriteLine($"      Contains \"PROD-003\": {caseSensitive.ContainsKey("PROD-003")}");
        Console.WriteLine($"      Contains \"prod-003\": {caseSensitive.ContainsKey("prod-003")}");

        // Case-insensitive - "PROD-003" and "prod-003" are the same
        var caseInsensitive = JsonQueryable<ProductConv>.FromString(json)
            .ToDictionary(
                keySelector: p => p.Code,
                comparer: StringComparer.OrdinalIgnoreCase
            );

        Console.WriteLine($"\n   Case-insensitive dictionary: {caseInsensitive.Count} entries");
        Console.WriteLine($"      Lookup \"prod-001\": {caseInsensitive["prod-001"].Name}");
        Console.WriteLine($"      Lookup \"PROD-002\": {caseInsensitive["PROD-002"].Name}");

        Console.WriteLine();
    }

    /// <summary>
    /// ToHashSet basic usage - create unique set.
    /// </summary>
    private static void ToHashSetBasicUsage()
    {
        Console.WriteLine("4. ToHashSet - Create Unique Sets");
        Console.WriteLine("   Remove duplicates and enable fast membership testing");
        Console.WriteLine("-----------------------------------------------------------------");

        var json = """
        [
            {"Id":1,"Name":"Alice","City":"London","Department":"Engineering","IsActive":true},
            {"Id":2,"Name":"Bob","City":"Paris","Department":"Sales","IsActive":true},
            {"Id":3,"Name":"Charlie","City":"London","Department":"Engineering","IsActive":true},
            {"Id":4,"Name":"Diana","City":"Berlin","Department":"Sales","IsActive":true},
            {"Id":5,"Name":"Eve","City":"London","Department":"Marketing","IsActive":true}
        ]
        """;

        // Get unique cities
        var uniqueCities = JsonQueryable<PersonConv>.FromString(json)
            .Select(p => p.City)
            .ToHashSet();

        Console.WriteLine($"   Unique cities ({uniqueCities.Count}):");
        foreach (var city in uniqueCities.OrderBy(c => c))
        {
            Console.WriteLine($"      - {city}");
        }

        // Get unique departments
        var uniqueDepartments = JsonQueryable<PersonConv>.FromString(json)
            .Select(p => p.Department)
            .ToHashSet();

        Console.WriteLine($"\n   Unique departments ({uniqueDepartments.Count}):");
        foreach (var dept in uniqueDepartments.OrderBy(d => d))
        {
            Console.WriteLine($"      - {dept}");
        }

        // Fast membership testing
        Console.WriteLine("\n   Fast membership testing:");
        Console.WriteLine($"      Contains \"London\": {uniqueCities.Contains("London")}");
        Console.WriteLine($"      Contains \"Tokyo\": {uniqueCities.Contains("Tokyo")}");

        Console.WriteLine();
    }

    /// <summary>
    /// ToHashSet with custom comparer.
    /// </summary>
    private static void ToHashSetWithComparer()
    {
        Console.WriteLine("5. ToHashSet with Custom Comparer");
        Console.WriteLine("   Case-insensitive uniqueness and membership testing");
        Console.WriteLine("-----------------------------------------------------------------");

        var json = """
        [
            {"Tag":"JavaScript"},
            {"Tag":"Python"},
            {"Tag":"javascript"},
            {"Tag":"PYTHON"},
            {"Tag":"TypeScript"},
            {"Tag":"typescript"}
        ]
        """;

        // Case-sensitive (default) - duplicates remain
        var caseSensitive = JsonQueryable<TaggedItem>.FromString(json)
            .Select(t => t.Tag)
            .ToHashSet();

        Console.WriteLine($"   Case-sensitive HashSet: {caseSensitive.Count} unique tags");
        foreach (var tag in caseSensitive.OrderBy(t => t))
        {
            Console.WriteLine($"      - {tag}");
        }

        // Case-insensitive - true deduplication
        var caseInsensitive = JsonQueryable<TaggedItem>.FromString(json)
            .Select(t => t.Tag)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Console.WriteLine($"\n   Case-insensitive HashSet: {caseInsensitive.Count} unique tags");
        foreach (var tag in caseInsensitive.OrderBy(t => t))
        {
            Console.WriteLine($"      - {tag}");
        }

        // Case-insensitive membership testing
        Console.WriteLine("\n   Case-insensitive membership:");
        Console.WriteLine($"      Contains \"JAVASCRIPT\": {caseInsensitive.Contains("JAVASCRIPT")}");
        Console.WriteLine($"      Contains \"python\": {caseInsensitive.Contains("python")}");

        Console.WriteLine();
    }

    /// <summary>
    /// ToLookup basic usage - one-to-many mapping.
    /// </summary>
    private static void ToLookupBasicUsage()
    {
        Console.WriteLine("6. ToLookup - One-to-Many Grouping");
        Console.WriteLine("   Group elements by key (multiple values per key)");
        Console.WriteLine("-----------------------------------------------------------------");

        var json = """
        [
            {"Id":1,"Name":"Alice","City":"London","Age":30,"IsActive":true},
            {"Id":2,"Name":"Bob","City":"Paris","Age":25,"IsActive":true},
            {"Id":3,"Name":"Charlie","City":"London","Age":35,"IsActive":true},
            {"Id":4,"Name":"Diana","City":"Berlin","Age":28,"IsActive":true},
            {"Id":5,"Name":"Eve","City":"London","Age":32,"IsActive":true}
        ]
        """;

        // Group people by city
        var peopleByCity = JsonQueryable<PersonConv>.FromString(json)
            .ToLookup(p => p.City);

        Console.WriteLine($"   ILookup<string, Person> - People grouped by city:");
        Console.WriteLine($"   Total cities: {peopleByCity.Count}\n");

        foreach (var cityGroup in peopleByCity.OrderBy(g => g.Key))
        {
            Console.WriteLine($"   {cityGroup.Key} ({cityGroup.Count()} people):");
            foreach (var person in cityGroup.OrderBy(p => p.Name))
            {
                Console.WriteLine($"      - {person.Name}, {person.Age}");
            }
        }

        // Lookup specific city
        Console.WriteLine($"\n   Direct lookup - London residents:");
        var londonPeople = peopleByCity["London"];
        foreach (var person in londonPeople)
        {
            Console.WriteLine($"      - {person.Name}");
        }

        // Lookup non-existent city (returns empty sequence, not null)
        var tokyoPeople = peopleByCity["Tokyo"];
        Console.WriteLine($"\n   Lookup non-existent city \"Tokyo\": {tokyoPeople.Count()} people (empty, not null)");

        Console.WriteLine();
    }

    /// <summary>
    /// ToLookup with element selector.
    /// </summary>
    private static void ToLookupWithElementSelector()
    {
        Console.WriteLine("7. ToLookup with Element Selector");
        Console.WriteLine("   Project values while creating the lookup");
        Console.WriteLine("-----------------------------------------------------------------");

        var json = """
        [
            {"ProductId":1,"Name":"Laptop Pro","Category":"Electronics","Price":1200,"Stock":50,"Rating":4.5},
            {"ProductId":2,"Name":"Mouse","Category":"Electronics","Price":25,"Stock":200,"Rating":4.2},
            {"ProductId":3,"Name":"Desk","Category":"Furniture","Price":300,"Stock":75,"Rating":4.6},
            {"ProductId":4,"Name":"Chair","Category":"Furniture","Price":150,"Stock":100,"Rating":4.7},
            {"ProductId":5,"Name":"Monitor","Category":"Electronics","Price":400,"Stock":150,"Rating":4.8}
        ]
        """;

        // Lookup product names by category
        var productNamesByCategory = JsonQueryable<ProductConv>.FromString(json)
            .ToLookup(
                keySelector: p => p.Category,
                elementSelector: p => p.Name
            );

        Console.WriteLine("   ILookup<string, string> - Product names by category:\n");
        foreach (var categoryGroup in productNamesByCategory.OrderBy(g => g.Key))
        {
            Console.WriteLine($"   {categoryGroup.Key}:");
            foreach (var name in categoryGroup.OrderBy(n => n))
            {
                Console.WriteLine($"      - {name}");
            }
        }

        // Lookup with anonymous type projection
        var productInfoByCategory = JsonQueryable<ProductConv>.FromString(json)
            .ToLookup(
                keySelector: p => p.Category,
                elementSelector: p => new { p.Name, p.Price }
            );

        Console.WriteLine("\n   ILookup<string, AnonymousType> - Product details by category:\n");
        foreach (var categoryGroup in productInfoByCategory.OrderBy(g => g.Key))
        {
            var avgPrice = categoryGroup.Average(p => p.Price);
            Console.WriteLine($"   {categoryGroup.Key} (Avg: ${avgPrice:N0}):");
            foreach (var product in categoryGroup.OrderByDescending(p => p.Price))
            {
                Console.WriteLine($"      - {product.Name}: ${product.Price:N0}");
            }
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Real-world scenarios using conversion operations.
    /// </summary>
    private static void RealWorldScenarios()
    {
        Console.WriteLine("8. Real-World Scenarios");
        Console.WriteLine("   Practical applications of conversion operations");
        Console.WriteLine("-----------------------------------------------------------------");

        var jsonPath = Path.Combine(AppContext.BaseDirectory, "Data", "people.json");
        if (!File.Exists(jsonPath))
        {
            Console.WriteLine("   [Skipped - people.json not found]");
            Console.WriteLine();
            return;
        }

        var json = File.ReadAllText(jsonPath);

        // Scenario 1: Build user cache (Dictionary)
        Console.WriteLine("   Scenario 1: User Cache (Fast ID Lookup)");
        var userCache = JsonQueryable<PersonConv>.FromString(json)
            .Where(p => p.IsActive)
            .ToDictionary(p => p.Id);

        Console.WriteLine($"      Cache built: {userCache.Count} active users");
        if (userCache.Count > 0)
        {
            var firstId = userCache.Keys.First();
            Console.WriteLine($"      Fast lookup [ID={firstId}]: {userCache[firstId].Name} (O(1) time)");
        }

        // Scenario 2: Tag cloud / Category filter (HashSet)
        Console.WriteLine("\n   Scenario 2: Available Cities Filter (HashSet)");
        var availableCities = JsonQueryable<PersonConv>.FromString(json)
            .Where(p => p.IsActive)
            .Select(p => p.City)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Console.WriteLine($"      Available cities: {string.Join(", ", availableCities.OrderBy(c => c).Take(5))}");
        Console.WriteLine($"      Filter check - \"london\" available: {availableCities.Contains("london")}");

        // Scenario 3: Email distribution list (Lookup)
        Console.WriteLine("\n   Scenario 3: Email Distribution Lists by City");
        var emailsByCity = JsonQueryable<PersonConv>.FromString(json)
            .Where(p => p.IsActive && !string.IsNullOrEmpty(p.Email))
            .ToLookup(
                keySelector: p => p.City,
                elementSelector: p => p.Email
            );

        Console.WriteLine($"      Distribution lists for {emailsByCity.Count} cities:");
        foreach (var cityList in emailsByCity.OrderByDescending(g => g.Count()).Take(3))
        {
            Console.WriteLine($"      {cityList.Key}: {cityList.Count()} recipients");
        }

        // Scenario 4: Configuration mapping (Dictionary with projection)
        Console.WriteLine("\n   Scenario 4: User Preferences Map");
        var userPreferences = JsonQueryable<PersonConv>.FromString(json)
            .Where(p => p.IsActive)
            .Take(5)
            .ToDictionary(
                keySelector: p => p.Email,
                elementSelector: p => new UserPreference
                {
                    Name = p.Name,
                    City = p.City,
                    IsActive = p.IsActive
                }
            );

        Console.WriteLine($"      Preferences stored for {userPreferences.Count} users");
        var firstEmail = userPreferences.Keys.FirstOrDefault();
        if (firstEmail != null)
        {
            var pref = userPreferences[firstEmail];
            Console.WriteLine($"      Example [{firstEmail}]: {pref.Name} in {pref.City}");
        }

        Console.WriteLine("\n   Summary:");
        Console.WriteLine("      - ToDictionary: Fast O(1) lookups, unique keys, throws on duplicates");
        Console.WriteLine("      - ToHashSet: Unique values, fast Contains(), supports custom equality");
        Console.WriteLine("      - ToLookup: One-to-many grouping, never throws, empty for missing keys");

        Console.WriteLine();
    }
}
