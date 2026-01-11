using Blazing.Json.Queryable.Providers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Blazing.Json.Queryable.Samples.Examples;

/// <summary>
/// Demonstrates custom JsonConverter usage and JsonSerializerOptions configuration.
/// Shows how to customize JSON deserialization behavior.
/// </summary>
public static class CustomConverters
{
    public static void RunAll()
    {
        Console.WriteLine("=== Custom Converters Examples ===\n");
        
        CaseInsensitiveProperties();
        CustomDateFormatting();
        EnumHandling();
        CustomTypeConverter();
        
        Console.WriteLine("\n=== Custom Converters Complete ===\n");
    }
    
    /// <summary>
    /// Case-insensitive property matching.
    /// </summary>
    private static void CaseInsensitiveProperties()
    {
        Console.WriteLine("1. Case-Insensitive Property Matching");
        
        var json = """
            [
                {"ID":1,"NAME":"Alice","AGE":28},
                {"id":2,"name":"Bob","age":35},
                {"Id":3,"Name":"Charlie","Age":22}
            ]
            """;
        
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        
        var results = JsonQueryable<Person>.FromString(json, options)
            .Where(p => p.Age > 25)
            .ToList();
        
        Console.WriteLine($"   Found {results.Count} people over 25:");
        foreach (var person in results)
        {
            Console.WriteLine($"   - {person.Name}, Age: {person.Age}");
        }
        
        Console.WriteLine("\n   * Case-insensitive matching enabled");
        Console.WriteLine();
    }
    
    /// <summary>
    /// Custom date formatting with converter.
    /// </summary>
    private static void CustomDateFormatting()
    {
        Console.WriteLine("2. Custom Date Formatting");
        
        var json = """
            [
                {"id":1,"eventName":"Conference","eventDate":"2024-01-15"},
                {"id":2,"eventName":"Workshop","eventDate":"2024-02-20"},
                {"id":3,"eventName":"Meetup","eventDate":"2024-03-10"}
            ]
            """;
        
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new DateOnlyJsonConverter() }
        };
        
        var results = JsonQueryable<Event>.FromString(json, options)
            .Where(e => e.EventDate.Year == 2024 && e.EventDate.Month >= 2)
            .OrderBy(e => e.EventDate)
            .ToList();
        
        Console.WriteLine($"   Events in Feb-Mar 2024 ({results.Count}):");
        foreach (var evt in results)
        {
            Console.WriteLine($"   - {evt.EventName}: {evt.EventDate:yyyy-MM-dd}");
        }
        
        Console.WriteLine("\n   * Custom date converter applied");
        Console.WriteLine();
    }
    
    /// <summary>
    /// Enum handling with JsonStringEnumConverter.
    /// </summary>
    private static void EnumHandling()
    {
        Console.WriteLine("3. Enum Handling (String to Enum)");
        
        var json = """
            [
                {"taskId":1,"title":"Fix bug","priority":"High","status":"InProgress"},
                {"taskId":2,"title":"Write docs","priority":"Medium","status":"NotStarted"},
                {"taskId":3,"title":"Code review","priority":"Low","status":"Completed"}
            ]
            """;
        
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };
        
        var results = JsonQueryable<WorkTask>.FromString(json, options)
            .Where(t => t.Priority == Priority.High || t.Status == TaskStatus.InProgress)
            .ToList();
        
        Console.WriteLine($"   High priority or in-progress tasks ({results.Count}):");
        foreach (var task in results)
        {
            Console.WriteLine($"   - {task.Title}: {task.Priority} ({task.Status})");
        }
        
        Console.WriteLine("\n   * String enums properly deserialized");
        Console.WriteLine();
    }
    
    /// <summary>
    /// Custom type converter for complex transformations.
    /// </summary>
    private static void CustomTypeConverter()
    {
        Console.WriteLine("4. Custom Type Converter (Currency)");
        
        var json = """
            [
                {"productId":1,"name":"Laptop","price":"$1299.99","currencyCode":"USD"},
                {"productId":2,"name":"Mouse","price":"€29.99","currencyCode":"EUR"},
                {"productId":3,"name":"Keyboard","price":"£89.99","currencyCode":"GBP"}
            ]
            """;
        
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new CurrencyConverter() }
        };
        
        var results = JsonQueryable<ProductWithCurrency>.FromString(json, options)
            .Where(p => p.Price != null && p.Price.Amount > 50)
            .OrderByDescending(p => p.Price.Amount)
            .ToList();
        
        Console.WriteLine($"   Products over $50 ({results.Count}):");
        foreach (var product in results)
        {
            Console.WriteLine($"   - {product.Name}: {product.Price}");
        }
        
        Console.WriteLine("\n   * Custom currency converter applied");
        Console.WriteLine();
    }
}

// Custom Models for Examples

public record Event(int Id, string EventName, DateOnly EventDate);

public record WorkTask(int TaskId, string Title, Priority Priority, TaskStatus Status);

public enum Priority { Low, Medium, High }

public enum TaskStatus { NotStarted, InProgress, Completed }

public record ProductWithCurrency(
    int ProductId,
    string Name,
    [property: JsonConverter(typeof(CurrencyConverter))] Currency Price,
    string CurrencyCode);

public record Currency(decimal Amount, string Code)
{
    public override string ToString() => $"{Code} {Amount:F2}";
}

// Custom Converters

/// <summary>
/// Custom converter for DateOnly type.
/// </summary>
public class DateOnlyJsonConverter : JsonConverter<DateOnly>
{
    private const string DateFormat = "yyyy-MM-dd";
    
    public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return DateOnly.ParseExact(value!, DateFormat);
    }
    
    public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString(DateFormat));
    }
}

/// <summary>
/// Custom converter for Currency type.
/// Parses strings like "$1299.99" into Currency objects.
/// </summary>
public class CurrencyConverter : JsonConverter<Currency>
{
    public override Currency Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException("Expected string for currency");
        }
        
        var value = reader.GetString()!;
        
        // Extract currency symbol
        var currencySymbol = value[0];
        var amountStr = value[1..];
        
        var amount = decimal.Parse(amountStr);
        
        var code = currencySymbol switch
        {
            '$' => "USD",
            '€' => "EUR",
            '£' => "GBP",
            _ => "UNKNOWN"
        };
        
        return new Currency(amount, code);
    }
    
    public override void Write(Utf8JsonWriter writer, Currency value, JsonSerializerOptions options)
    {
        var symbol = value.Code switch
        {
            "USD" => "$",
            "EUR" => "€",
            "GBP" => "£",
            _ => ""
        };
        
        writer.WriteStringValue($"{symbol}{value.Amount:F2}");
    }
}
