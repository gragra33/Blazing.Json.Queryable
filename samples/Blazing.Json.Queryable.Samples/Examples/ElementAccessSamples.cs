using Blazing.Json.Queryable.Providers;
using Blazing.Json.Queryable.Samples.Models;

namespace Blazing.Json.Queryable.Samples.Examples;

/// <summary>
/// Demonstrates element access operations using Blazing.Json.Queryable.
/// Shows fundamental operations: ElementAt, ElementAtOrDefault, Last, LastOrDefault, Single, SingleOrDefault.
/// Includes C# 14 Index type support for from-end indexing (^n syntax).
/// </summary>
public static class ElementAccessSamples
{
    public static void RunAll()
    {
        Console.WriteLine("=================================================================");
        Console.WriteLine(" Element Access Operations Examples");
        Console.WriteLine("=================================================================");
        Console.WriteLine();

        ElementAtWithIntIndex();
        ElementAtWithIndexType();
        ElementAtOrDefaultSafeAccess();
        LastElementOperations();
        SingleElementValidation();
        RealWorldScenarios();
        ErrorHandlingPatterns();
    }

    private static void ElementAtWithIntIndex()
    {
        Console.WriteLine("1. ElementAt with Integer Index");
        Console.WriteLine("   Access elements by zero-based position");
        Console.WriteLine("-----------------------------------------------------------------");

        var json = """
        [
            {"Id":1,"Name":"Alice","Age":30,"City":"London","Email":"alice@example.com","IsAdmin":false,"IsActive":true},
            {"Id":2,"Name":"Bob","Age":25,"City":"Paris","Email":"bob@example.com","IsAdmin":false,"IsActive":true},
            {"Id":3,"Name":"Charlie","Age":35,"City":"London","Email":"charlie@example.com","IsAdmin":false,"IsActive":true},
            {"Id":4,"Name":"Diana","Age":28,"City":"Berlin","Email":"diana@example.com","IsAdmin":false,"IsActive":true}
        ]
        """;

        var first = JsonQueryable<PersonModel>.FromString(json)
            .ElementAt(0);
        Console.WriteLine($"   ElementAt(0): {first.Name} (first element)");

        var third = JsonQueryable<PersonModel>.FromString(json)
            .ElementAt(2);
        Console.WriteLine($"   ElementAt(2): {third.Name} (third element)");

        Console.WriteLine();
    }

    private static void ElementAtWithIndexType()
    {
        Console.WriteLine("2. ElementAt with C# Index Type (From-End Indexing)");
        Console.WriteLine("   Use ^n syntax to count from the end");
        Console.WriteLine("-----------------------------------------------------------------");

        var json = """
        [
            {"Id":1,"Name":"Alice","Age":30,"Email":"alice@example.com","IsAdmin":false,"IsActive":true},
            {"Id":2,"Name":"Bob","Age":25,"Email":"bob@example.com","IsAdmin":false,"IsActive":true},
            {"Id":3,"Name":"Charlie","Age":35,"Email":"charlie@example.com","IsAdmin":false,"IsActive":true},
            {"Id":4,"Name":"Diana","Age":28,"Email":"diana@example.com","IsAdmin":false,"IsActive":true},
            {"Id":5,"Name":"Eve","Age":32,"Email":"eve@example.com","IsAdmin":false,"IsActive":true}
        ]
        """;

        var last = JsonQueryable<PersonModel>.FromString(json)
            .ElementAt(^1);
        Console.WriteLine($"   ElementAt(^1): {last.Name} (last element)");

        var secondLast = JsonQueryable<PersonModel>.FromString(json)
            .ElementAt(^2);
        Console.WriteLine($"   ElementAt(^2): {secondLast.Name} (second from end)");

        Console.WriteLine();
    }

    private static void ElementAtOrDefaultSafeAccess()
    {
        Console.WriteLine("3. ElementAtOrDefault - Safe Element Access");
        Console.WriteLine("   Returns null instead of throwing exception for out-of-range");
        Console.WriteLine("-----------------------------------------------------------------");

        var json = """
        [
            {"Id":1,"Name":"Alice","Age":30,"Email":"alice@example.com","IsAdmin":false,"IsActive":true},
            {"Id":2,"Name":"Bob","Age":25,"Email":"bob@example.com","IsAdmin":false,"IsActive":true},
            {"Id":3,"Name":"Charlie","Age":35,"Email":"charlie@example.com","IsAdmin":false,"IsActive":true}
        ]
        """;

        var valid = JsonQueryable<PersonModel>.FromString(json)
            .ElementAtOrDefault(1);
        Console.WriteLine($"   ElementAtOrDefault(1): {(valid != null ? valid.Name : "null")} (valid index)");

        var outOfRange = JsonQueryable<PersonModel>.FromString(json)
            .ElementAtOrDefault(10);
        Console.WriteLine($"   ElementAtOrDefault(10): {(outOfRange != null ? outOfRange.Name : "null")} (out of range)");

        Console.WriteLine();
    }

    private static void LastElementOperations()
    {
        Console.WriteLine("4. Last and LastOrDefault Operations");
        Console.WriteLine("   Get the last element in the sequence");
        Console.WriteLine("-----------------------------------------------------------------");

        var json = """
        [
            {"Id":1,"Name":"Alice","Age":30,"City":"London","Email":"alice@example.com","IsAdmin":false,"IsActive":true},
            {"Id":2,"Name":"Bob","Age":25,"City":"Paris","Email":"bob@example.com","IsAdmin":false,"IsActive":true},
            {"Id":3,"Name":"Charlie","Age":35,"City":"London","Email":"charlie@example.com","IsAdmin":false,"IsActive":true},
            {"Id":4,"Name":"Diana","Age":28,"City":"Berlin","Email":"diana@example.com","IsAdmin":false,"IsActive":true}
        ]
        """;

        var last = JsonQueryable<PersonModel>.FromString(json)
            .Last();
        Console.WriteLine($"   Last(): {last.Name} (last in sequence)");

        var lastLondon = JsonQueryable<PersonModel>.FromString(json)
            .Last(p => p.City == "London");
        Console.WriteLine($"   Last(City == London): {lastLondon.Name} (last match)");

        Console.WriteLine();
    }

    private static void SingleElementValidation()
    {
        Console.WriteLine("5. Single and SingleOrDefault - Unique Element Validation");
        Console.WriteLine("   Ensure exactly one element matches the condition");
        Console.WriteLine("-----------------------------------------------------------------");

        var json = """
        [
            {"Id":1,"Name":"Alice","Age":30,"City":"London","Email":"alice@example.com","IsAdmin":false,"IsActive":true},
            {"Id":2,"Name":"Bob","Age":25,"City":"Paris","Email":"bob@example.com","IsAdmin":true,"IsActive":true},
            {"Id":3,"Name":"Charlie","Age":35,"City":"London","Email":"charlie@example.com","IsAdmin":false,"IsActive":true}
        ]
        """;

        var admin = JsonQueryable<PersonModel>.FromString(json)
            .Single(p => p.IsAdmin);
        Console.WriteLine($"   Single(IsAdmin): {admin.Name} (exactly one admin)");

        var uniqueParis = JsonQueryable<PersonModel>.FromString(json)
            .SingleOrDefault(p => p.City == "Paris");
        Console.WriteLine($"   SingleOrDefault(City == Paris): {(uniqueParis != null ? uniqueParis.Name : "null")}");

        Console.WriteLine();
    }

    private static void RealWorldScenarios()
    {
        Console.WriteLine("6. Real-World Scenarios");
        Console.WriteLine("   Practical applications of element access operations");
        Console.WriteLine("-----------------------------------------------------------------");

        var json = """
        [
            {"Id":1,"Name":"Alice","Age":35,"City":"London","Email":"alice@example.com","IsAdmin":false,"IsActive":true},
            {"Id":2,"Name":"Bob","Age":30,"City":"Paris","Email":"bob@example.com","IsAdmin":false,"IsActive":true},
            {"Id":3,"Name":"Charlie","Age":28,"City":"London","Email":"charlie@example.com","IsAdmin":false,"IsActive":true}
        ]
        """;

        Console.WriteLine("   Scenario: Top 3 Performers Leaderboard");
        var top3 = JsonQueryable<PersonModel>.FromString(json)
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.Age)
            .Take(3)
            .ToList();

        if (top3.Count >= 3)
        {
            Console.WriteLine($"      Gold: {top3.ElementAt(0).Name} (Score: {top3.ElementAt(0).Age})");
            Console.WriteLine($"      Silver: {top3.ElementAt(1).Name} (Score: {top3.ElementAt(1).Age})");
            Console.WriteLine($"      Bronze: {top3.ElementAt(2).Name} (Score: {top3.ElementAt(2).Age})");
        }

        Console.WriteLine();
    }

    private static void ErrorHandlingPatterns()
    {
        Console.WriteLine("7. Error Handling Patterns");
        Console.WriteLine("   Understanding exceptions vs default values");
        Console.WriteLine("-----------------------------------------------------------------");

        var json = """
        [
            {"Id":1,"Name":"Alice","Age":30,"City":"London","Email":"alice@example.com","IsAdmin":false,"IsActive":true},
            {"Id":2,"Name":"Bob","Age":25,"City":"Paris","Email":"bob@example.com","IsAdmin":false,"IsActive":true}
        ]
        """;

        Console.WriteLine("   Pattern 1: ElementAt throws ArgumentOutOfRangeException");
        try
        {
            var outOfRange = JsonQueryable<PersonModel>.FromString(json)
                .ElementAt(10);
            Console.WriteLine($"      Result: {outOfRange.Name}");
        }
        catch (ArgumentOutOfRangeException)
        {
            Console.WriteLine("      Exception caught: ArgumentOutOfRangeException (expected)");
        }

        Console.WriteLine("\n   Pattern 2: ElementAtOrDefault returns null");
        var safeAccess = JsonQueryable<PersonModel>.FromString(json)
            .ElementAtOrDefault(10);
        Console.WriteLine($"      Result: {(safeAccess != null ? safeAccess.Name : "null")} (no exception)");

        Console.WriteLine("\n   Key Takeaways:");
        Console.WriteLine("      - Use ElementAt when index MUST be valid (throws on error)");
        Console.WriteLine("      - Use ElementAtOrDefault for optional access (returns null)");

        Console.WriteLine();
    }
}
