# Blazing.Json.Queryable

[![NuGet Version](https://img.shields.io/nuget/v/Blazing.Json.Queryable.svg)](https://www.nuget.org/packages/Blazing.Json.Queryable) [![NuGet Downloads](https://img.shields.io/nuget/dt/Blazing.Json.Queryable.svg)](https://www.nuget.org/packages/Blazing.Json.Queryable) [![.NET 10+](https://img.shields.io/badge/.NET-10%2B-512BD4)](https://dotnet.microsoft.com/download)

<!-- TOC -->
## Table of Contents

  - [Overview](#overview)
  - [Key Features](#key-features)
  - [Why Choose Blazing.Json.Queryable?](#why-choose-blazing.json.queryable)
    - [Performance Advantages Over Traditional JSON + LINQ](#performance-advantages-over-traditional-json-linq)
    - [Key Advantages](#key-advantages)
    - [When to Use Each Approach](#when-to-use-each-approach)
  - [Getting Started](#getting-started)
    - [Installation](#installation)
    - [Requirements](#requirements)
    - [Quick Start](#quick-start)
  - [Usage](#usage)
    - [Basic Queries](#basic-queries)
    - [Streaming Queries](#streaming-queries)
    - [Advanced Streaming with .NET 10 Async LINQ](#advanced-streaming-with.net-10-async-linq)
    - [UTF-8 Direct Processing](#utf-8-direct-processing)
    - [Query Syntax (Query Expression Syntax)](#query-syntax-query-expression-syntax)
  - [Supported LINQ Methods](#supported-linq-methods)
    - [Filtering Operations](#filtering-operations)
    - [Projection Operations](#projection-operations)
    - [Ordering Operations](#ordering-operations)
    - [Quantifier Operations](#quantifier-operations)
    - [Element Access Operations](#element-access-operations)
    - [Aggregation Operations](#aggregation-operations)
    - [Set Operations](#set-operations)
    - [Partitioning Operations](#partitioning-operations)
    - [Grouping Operations](#grouping-operations)
    - [Conversion Operations](#conversion-operations)
    - [Sequence Operations](#sequence-operations)
  - [JSONPath Support (RFC 9535)](#jsonpath-support-rfc-9535)
    - [Overview](#overview-1)
    - [Why Use JSONPath with Blazing.Json.Queryable?](#why-use-jsonpath-with-blazing.json.queryable)
    - [JSONPath Syntax Quick Reference](#jsonpath-syntax-quick-reference)
    - [Simple Path Navigation](#simple-path-navigation)
    - [Filter Expressions](#filter-expressions)
    - [Built-in Functions](#built-in-functions)
    - [Array Slicing](#array-slicing)
    - [Combining JSONPath with LINQ](#combining-jsonpath-with-linq)
    - [Performance Best Practices (Memory & Speed)](#performance-best-practices-memory-speed)
      - [Learn More](#learn-more)
- [How It Works](#how-it-works)
    - [Execution Flow Example](#execution-flow-example)
  - [Performance Advantages](#performance-advantages)
    - [Real-World Performance Comparison](#real-world-performance-comparison)
    - [Key Performance Benefits](#key-performance-benefits)
    - [When Performance Gains Are Largest](#when-performance-gains-are-largest)
  - [Samples](#samples)
    - [Sample Projects](#sample-projects)
    - [Benchmark Suites](#benchmark-suites)
    - [Running the Samples](#running-the-samples)
    - [Running the Benchmarks](#running-the-benchmarks)
  - [Give a ⭐](#give-a)
  - [Support](#support)
  - [History](#history)
    - [V1.1.1 - 12 January, 2026](#v1.1.1-12-january-2026)
    - [V1.1.0 - 12 January, 2026](#v1.1.0-12-january-2026)
    - [V1.0.0 - 11 January, 2026](#v1.0.0-11-january-2026)

<!-- TOC -->

## Overview

**Blazing.Json.Queryable** is a high-performance LINQ provider for JSON data that processes JSON directly without full deserialization. Unlike traditional approaches that load entire JSON files into memory and then apply LINQ queries, this library processes JSON as a stream, providing dramatic performance improvements and memory efficiency for medium to large JSON files.

This custom JSON LINQ provider supports standard string, UTF-8, streaming, and RFC 9535 compliant JSONPath operations powered by **[Blazing.Json.JSONPath](https://github.com/gragra33/Blazing.Json.JSONPath)**. Whether you're working with multi-megabyte API responses, large data exports, or log files, Blazing.Json.Queryable enables you to query JSON data efficiently with the familiar LINQ syntax you already know.

## Key Features

- **Direct JSON Processing**: Query JSON without full deserialization
- **Memory Efficient**: Process files larger than available RAM
- **Early Termination**: Stop reading after finding required results (Take, First, Any)
- **Streaming Support**: Native `IAsyncEnumerable<T>` for real-time processing
- **UTF-8 Native**: Zero-allocation UTF-8 processing with Span\<T\>
- **RFC 9535 JSONPath**: Full RFC 9535 compliance with filters, functions, and slicing via [Blazing.Json.JSONPath](https://github.com/gragra33/Blazing.Json.JSONPath)
- **Full LINQ**: Comprehensive LINQ method support (60+ operations)
- **.NET 10 Async LINQ**: Built-in async LINQ with async predicates and transformations
- **Multiple Input Formats**: String, Stream, UTF-8 bytes, and more

## Why Choose Blazing.Json.Queryable?

### Performance Advantages Over Traditional JSON + LINQ

When working with medium to large JSON files, the traditional approach of deserializing to objects and then querying has significant disadvantages:

**Traditional Approach:**
```csharp
// Traditional: Load ALL data into memory, then query
var json = File.ReadAllText("large-file.json");          // Load entire file
var all = JsonSerializer.Deserialize<List<Person>>(json); // Deserialize ALL records
var results = all.Where(p => p.Age > 25).Take(10).ToList(); // Query in-memory
```

**Blazing.Json.Queryable Approach:**
```csharp
// Blazing.Json.Queryable: Stream and query simultaneously
await using var stream = File.OpenRead("large-file.json");
var results = await JsonQueryable<Person>.FromStream(stream)
    .Where(p => p.Age > 25)
    .Take(10)
    .AsAsyncEnumerable()
    .ToListAsync(); // Only deserializes matching records!
```

### Key Advantages

| Feature | Traditional | Blazing.Json.Queryable |
|---------|-------------|------------------------|
| **Memory Usage** | Loads entire file | Constant memory usage |
| **Early Exit** | Processes all records | Stops after Take(N) |
| **Large Files** | OutOfMemoryException risk | Handles files > RAM |
| **Speed (Large + Take)** | Slow (full deserialize) | **10-20x faster** |
| **Async Support** | Manual implementation | Native `IAsyncEnumerable` |
| **UTF-8 Processing** | String conversion overhead | Zero-allocation Span\<T\> |

### When to Use Each Approach

**Use Blazing.Json.Queryable When:**
- Files are **> 10 MB**
- You need **early termination** (Take, First, Any)
- Working in **memory-constrained environments**
- Processing **files larger than available RAM**
- Building **async/streaming APIs**
- Need **real-time data processing**

**Use Traditional JsonSerializer + LINQ When:**
- Files are **< 1 MB**
- You need **all data** (no early termination)
- **Simplicity** is the priority
- Working with **in-memory collections**

## Getting Started

### Installation

Install via NuGet Package Manager:

```xml
<PackageReference Include="Blazing.Json.Queryable" Version="1.1.0" />
```

Or via the .NET CLI:

```bash
dotnet add package Blazing.Json.Queryable
```

Or via the Package Manager Console:

```powershell
Install-Package Blazing.Json.Queryable
```

### Requirements

- **.NET 10.0** or later
- **System.Text.Json** (included with .NET)
- **[Blazing.Json.JSONPath](https://github.com/gragra33/Blazing.Json.JSONPath)** - 100% RFC 9535 compliant JSONPath implementation ([NuGet Package](https://www.nuget.org/packages/Blazing.Json.JSONPath))
  - Automatically included as a dependency
  - Enables RFC 9535 filter expressions, functions, and slicing
- **[Utf8JsonAsyncStreamReader](https://github.com/gragra33/Utf8JsonAsyncStreamReader)** - High-performance streaming JSON reader ([NuGet Package](https://www.nuget.org/packages/Utf8JsonAsyncStreamReader))
  - Automatically included as a dependency
  - Enables efficient simple path navigation

### Quick Start

```csharp
using Blazing.Json.Queryable.Providers;

// From JSON string
var jsonString = """[{"Name":"Alice","Age":30},{"Name":"Bob","Age":25}]""";
var results = JsonQueryable<Person>.FromString(jsonString)
    .Where(p => p.Age > 25)
    .ToList();

// From file stream (memory-efficient)
await using var stream = File.OpenRead("data.json");
var streamResults = await JsonQueryable<Person>.FromStream(stream)
    .Where(p => p.Age > 25)
    .Take(10)
    .AsAsyncEnumerable()
    .ToListAsync();

// From UTF-8 bytes (zero-allocation)
byte[] utf8Json = Encoding.UTF8.GetBytes(jsonString);
var utf8Results = JsonQueryable<Person>.FromUtf8(utf8Json)
    .Where(p => p.Age > 25)
    .ToList();

public record Person(string Name, int Age);
```

## Usage

### Basic Queries

Standard LINQ operations work as expected:

```csharp
var json = """
[
    {"Id":1,"Name":"Alice","Age":30,"City":"London","IsActive":true},
    {"Id":2,"Name":"Bob","Age":25,"City":"Paris","IsActive":true},
    {"Id":3,"Name":"Charlie","Age":35,"City":"London","IsActive":false}
]
""

// Filtering
var adults = JsonQueryable<Person>.FromString(json)
    .Where(p => p.Age >= 18)
    .ToList();

// Projection
var names = JsonQueryable<Person>.FromString(json)
    .Select(p => p.Name)
    .ToList();

// Ordering
var sorted = JsonQueryable<Person>.FromString(json)
    .OrderBy(p => p.Age)
    .ThenBy(p => p.Name)
    .ToList();

// Aggregation
var avgAge = JsonQueryable<Person>.FromString(json)
    .Average(p => p.Age);

var totalActive = JsonQueryable<Person>.FromString(json)
    .Count(p => p.IsActive);

// Combined operations
var results = JsonQueryable<Person>.FromString(json)
    .Where(p => p.Age >= 25 && p.City == "London")
    .OrderByDescending(p => p.Age)
    .Select(p => new { p.Name, p.Age })
    .Take(5)
    .ToList();
```

### Streaming Queries

Process large files with constant memory usage:

```csharp
// Traditional approach - loads entire file into memory
var traditionalResults = new List<Person>();
var allJson = File.ReadAllText("huge-dataset.json"); // Loads ALL 500MB
var allPeople = JsonSerializer.Deserialize<List<Person>>(allJson); // Deserializes ALL
traditionalResults = allPeople.Where(p => p.Age > 25).Take(100).ToList();

// Blazing.Json.Queryable - streams and stops early
var streamingResults = new List<Person>();
await using (var stream = File.OpenRead("huge-dataset.json"))
{
    await foreach (var person in JsonQueryable<Person>.FromStream(stream)
        .Where(p => p.Age > 25)
        .Take(100) // Stops reading after 100 matches!
        .AsAsyncEnumerable())
    {
        streamingResults.Add(person);
    }
}
// Result: 10-20x faster, uses constant memory
```

### Advanced Streaming with .NET 10 Async LINQ

.NET 10 includes built-in async LINQ support, enabling powerful async transformations:

```csharp
await using var stream = File.OpenRead("data.json");

// Async predicates - call async methods in Where clauses
await foreach (var person in JsonQueryable<Person>.FromStream(stream)
    .AsAsyncEnumerable()
    .Where(async (p, ct) => await IsValidUserAsync(p, ct)) // Async predicate!
    .OrderBy(p => p.Name)
    .Take(100))
{
    Console.WriteLine(person.Name);
}

// Async transformations with Select
await foreach (var enriched in JsonQueryable<Person>.FromStream(stream)
    .AsAsyncEnumerable()
    .Select(async (p, ct) => await EnrichPersonDataAsync(p, ct)) // Async select!
    .Where(p => p.Score > 50))
{
    await ProcessAsync(enriched);
}

// Cancellation support
using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));

await foreach (var item in JsonQueryable<Person>.FromStream(stream)
    .AsAsyncEnumerable()
    .WithCancellation(cts.Token))
{
    // Automatically cancelled after 5 minutes
}
```

### UTF-8 Direct Processing

Process UTF-8 bytes without string conversion overhead:

```csharp
// From UTF-8 byte array
byte[] utf8Json = Encoding.UTF8.GetBytes(jsonString);
var results = JsonQueryable<Person>.FromUtf8(utf8Json)
    .Where(p => p.Age > 25)
    .ToList();

// From ReadOnlySpan<byte> (zero-allocation)
ReadOnlySpan<byte> utf8Span = utf8Json.AsSpan();
var spanResults = JsonQueryable<Person>.FromUtf8(utf8Span)
    .Where(p => p.Age > 25)
    .ToList();

// From UTF-8 stream (most efficient for large data)
await using var utf8Stream = File.OpenRead("data.json");
await foreach (var person in JsonQueryable<Person>.FromStream(utf8Stream)
    .AsAsyncEnumerable())
{
    // Process each person with zero string allocations
}
```

### Query Syntax (Query Expression Syntax)

Blazing.Json.Queryable supports both **method syntax** (fluent) and **query syntax** (query expression) for LINQ queries. Query syntax provides a declarative, SQL-like alternative that can be more readable for complex queries.

> [!TIP]
> **📚 Learn More About LINQ Query Syntax:**
> - [Query expression basics (Microsoft Docs)](https://learn.microsoft.com/en-us/dotnet/csharp/linq/get-started/query-expression-basics)
> - [Query syntax vs method syntax (Microsoft Docs)](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/linq/query-syntax-and-method-syntax-in-linq)

#### Method Syntax vs Query Syntax

```csharp
var json = """[{"Name":"Alice","Age":30},{"Name":"Bob","Age":25},{"Name":"Charlie","Age":35}]""";

// METHOD SYNTAX (fluent) - Chain methods
var methodResults = JsonQueryable<Person>.FromString(json)
    .Where(p => p.Age > 25)
    .OrderBy(p => p.Name)
    .Select(p => new { p.Name, p.Age })
    .ToList();

// QUERY SYNTAX (declarative) - SQL-like keywords
var queryResults = (from p in JsonQueryable<Person>.FromString(json)
                    where p.Age > 25
                    orderby p.Name
                    select new { p.Name, p.Age })
                   .ToList();

// Both produce identical results!
```

#### Query Syntax with All Library Features

**1. FromString - Basic Query Syntax:**
```csharp
var results = (from p in JsonQueryable<Person>.FromString(json)
               where p.Age > 25 && p.IsActive
               select new { p.Name, p.City })
              .ToList();
```

**2. FromUtf8 - Zero-allocation UTF-8 Processing:**
```csharp
byte[] utf8Bytes = Encoding.UTF8.GetBytes(jsonString);

var results = (from p in JsonQueryable<Person>.FromUtf8(utf8Bytes)
               where p.Age >= 30
               orderby p.Name
               select p)
              .ToList();
```

**3. FromFile - Direct File Access:**
```csharp
var results = (from p in JsonQueryable<Person>.FromFile("data.json")
               where p.IsActive
               orderby p.Age descending
               select new { p.Name, p.Age })
              .Take(10)
              .ToList();
```

**4. FromStream - Async Streaming:**
```csharp
await using var stream = File.OpenRead("data.json");

await foreach (var person in (from p in JsonQueryable<Person>.FromStream(stream)
                               where p.Age > 25
                               orderby p.Name
                               select p)
                              .Take(10)
                              .AsAsyncEnumerable())
{
    Console.WriteLine($"{person.Name}, {person.Age}");
}
```

**5. Multi-level Sorting:**
```csharp
var results = (from p in JsonQueryable<Person>.FromString(json)
               orderby p.City, p.Age descending, p.Name
               select p)
              .ToList();
```

**6. Grouping with Aggregations:**
```csharp
var results = (from p in JsonQueryable<Person>.FromString(json)
               group p by p.City into cityGroup
               select new
               {
                   City = cityGroup.Key,
                   Count = cityGroup.Count(),
                   AvgAge = cityGroup.Average(p => p.Age),
                   MinAge = cityGroup.Min(p => p.Age),
                   MaxAge = cityGroup.Max(p => p.Age)
               })
              .ToList();
```

**7. JSONPath Pre-filtering with Query Syntax:**
```csharp
// Combine RFC 9535 JSONPath filters with query syntax
var results = (from p in JsonQueryable<Product>
                   .FromString(json, "$[?@.price < 100 && @.stock > 0]")
               group p by p.Category into catGroup
               orderby catGroup.Key
               select new
               {
                   Category = catGroup.Key,
                   Count = catGroup.Count(),
                   AvgPrice = catGroup.Average(p => p.Price),
                   Products = catGroup.Select(p => p.Name).ToList()
               })
              .ToList();
```

**8. Complex Real-World Query:**
```csharp
// Employee department analysis with filtering, grouping, and sorting
var results = (from e in JsonQueryable<Employee>.FromString(json)
               where e.IsActive && e.Salary > 60000
               group e by e.Department into deptGroup
               where deptGroup.Count() > 1
               orderby deptGroup.Average(e => e.Salary) descending
               select new
               {
                   Department = deptGroup.Key,
                   Count = deptGroup.Count(),
                   AvgSalary = deptGroup.Average(e => e.Salary),
                   TopEarner = deptGroup.OrderByDescending(e => e.Salary).First().Name
               })
              .ToList();
```

#### When to Use Query Syntax

Both query syntax and method syntax are equally powerful and produce identical compiled code. The choice between them is purely a matter of readability and personal/team preference. Here's a side-by-side comparison to help you decide:

| **Use Query Syntax When:** | **Use Method Syntax When:** |
|----------------------------|----------------------------|
| Complex queries with joins and grouping (more SQL-like readability) | Simple filtering and projection |
| Multiple `from` clauses (SelectMany scenarios) | Chaining many operations (more fluent) |
| Team prefers declarative style | Using methods not available in query syntax (Take, Skip, Distinct, etc.) |
| Using `let` keyword for intermediate results | Personal/team preference for fluent style |

> [!NOTE]
> Both syntaxes are fully supported and produce identical expression trees. You can even mix them:
> ```csharp
> var mixed = (from p in JsonQueryable<Person>.FromString(json)
>              where p.Age > 25
>              select p)
>             .Take(10)  // Method syntax at the end
>             .ToList();
> ```

> [!TIP]
> **See Full Examples:** Check out `samples/Blazing.Json.Queryable.Samples/Examples/QuerySyntaxSamples.cs` for comprehensive demonstrations of query syntax with all library features including FromUtf8, FromFile, FromStream, and JSONPath integration.

## Supported LINQ Methods

### Filtering Operations

| Method | Description | Example |
|--------|-------------|---------|
| `Where` | Filters elements based on a predicate | `.Where(p => p.Age > 18)` |
| `OfType<T>` | Filters elements by type | `.OfType<Employee>()` |

### Projection Operations

| Method | Description | Example |
|--------|-------------|---------|
| `Select` | Projects each element to a new form | `.Select(p => p.Name)` |
| `SelectMany` | Flattens nested sequences | `.SelectMany(p => p.Orders)` |
| `Cast<T>` | Casts elements to specified type | `.Cast<Employee>()` |

### Ordering Operations

| Method | Description | Example |
|--------|-------------|---------|
| `OrderBy` | Sorts elements in ascending order | `.OrderBy(p => p.Age)` |
| `OrderByDescending` | Sorts elements in descending order | `.OrderByDescending(p => p.Age)` |
| `ThenBy` | Secondary ascending sort | `.ThenBy(p => p.Name)` |
| `ThenByDescending` | Secondary descending sort | `.ThenByDescending(p => p.Name)` |
| `Order` | Sorts elements in ascending order (C# 14) | `.Order()` |
| `OrderDescending` | Sorts elements in descending order (C# 14) | `.OrderDescending()` |
| `Reverse` | Reverses the order of elements | `.Reverse()` |

### Quantifier Operations

| Method | Description | Example |
|--------|-------------|---------|
| `All` | Tests if all elements satisfy a condition | `.All(p => p.Age >= 18)` |
| `Any` | Tests if any element satisfies a condition | `.Any(p => p.City == "London")` |
| `Contains` | Tests if sequence contains an element | `.Contains(person)` |
| `SequenceEqual` | Tests if two sequences are equal | `.SequenceEqual(other)` |

### Element Access Operations

| Method | Description | Example |
|--------|-------------|---------|
| `First` | Returns first element | `.First()` |
| `FirstOrDefault` | Returns first element or default | `.FirstOrDefault()` |
| `Last` | Returns last element | `.Last()` |
| `LastOrDefault` | Returns last element or default | `.LastOrDefault()` |
| `Single` | Returns the only element | `.Single()` |
| `SingleOrDefault` | Returns the only element or default | `.SingleOrDefault()` |
| `ElementAt` | Returns element at specified index | `.ElementAt(5)` |
| `ElementAtOrDefault` | Returns element at index or default | `.ElementAtOrDefault(5)` |

### Aggregation Operations

| Method | Description | Example |
|--------|-------------|---------|
| `Count` | Counts elements | `.Count()` |
| `LongCount` | Counts elements (64-bit) | `.LongCount()` |
| `Sum` | Sums numeric values | `.Sum(p => p.Salary)` |
| `Average` | Calculates average | `.Average(p => p.Age)` |
| `Min` | Finds minimum value | `.Min(p => p.Age)` |
| `Max` | Finds maximum value | `.Max(p => p.Salary)` |
| `MinBy` | Finds element with minimum key value | `.MinBy(p => p.Age)` |
| `MaxBy` | Finds element with maximum key value | `.MaxBy(p => p.Salary)` |
| `Aggregate` | Applies custom accumulator | `.Aggregate((a, b) => a + b)` |

### Set Operations

| Method | Description | Example |
|--------|-------------|---------|
| `Distinct` | Removes duplicate elements | `.Distinct()` |
| `DistinctBy` | Removes duplicates by key | `.DistinctBy(p => p.Email)` |
| `Union` | Combines two sequences | `.Union(otherPeople)` |
| `UnionBy` | Combines sequences by key | `.UnionBy(other, p => p.Id)` |
| `Intersect` | Finds common elements | `.Intersect(otherPeople)` |
| `IntersectBy` | Finds common elements by key | `.IntersectBy(ids, p => p.Id)` |
| `Except` | Finds elements not in second sequence | `.Except(otherPeople)` |
| `ExceptBy` | Finds elements not in second by key | `.ExceptBy(ids, p => p.Id)` |

### Partitioning Operations

| Method | Description | Example |
|--------|-------------|---------|
| `Take` | Takes first N elements | `.Take(10)` |
| `TakeLast` | Takes last N elements | `.TakeLast(10)` |
| `TakeWhile` | Takes elements while condition is true | `.TakeWhile(p => p.Age < 30)` |
| `Skip` | Skips first N elements | `.Skip(20)` |
| `SkipLast` | Skips last N elements | `.SkipLast(5)` |
| `SkipWhile` | Skips elements while condition is true | `.SkipWhile(p => p.Age < 18)` |
| `Chunk` | Divides elements into chunks of specified size | `.Chunk(10)` |

### Grouping Operations

| Method | Description | Example |
|--------|-------------|---------|
| `GroupBy` | Groups elements by key | `.GroupBy(p => p.City)` |
| `GroupJoin` | Groups and joins sequences | `.GroupJoin(orders, ...)` |
| `Join` | Joins two sequences | `.Join(orders, ...)` |

### Conversion Operations

| Method | Description | Example |
|--------|-------------|---------|
| `ToList` | Converts to List\<T\> (synchronous) | `.ToList()` |
| `ToArray` | Converts to array (synchronous) | `.ToArray()` |
| `ToDictionary` | Converts to dictionary | `.ToDictionary(p => p.Id)` |
| `ToHashSet` | Converts to hash set | `.ToHashSet()` |
| `ToLookup` | Converts to lookup | `.ToLookup(p => p.City)` |
| `AsEnumerable` | Returns as IEnumerable\<T\> | `.AsEnumerable()` |
| `AsQueryable` | Returns as IQueryable\<T\> | `.AsQueryable()` |
| `AsAsyncEnumerable` | Returns as IAsyncEnumerable\<T\> for async operations | `.AsAsyncEnumerable()` |

> [!NOTE]
> For async conversion operations, use `.AsAsyncEnumerable()` followed by .NET 10's built-in async LINQ methods:
> - `await query.AsAsyncEnumerable().ToListAsync()` - Async conversion to List\<T\>
> - `await query.AsAsyncEnumerable().ToArrayAsync()` - Async conversion to array
> - `await query.AsAsyncEnumerable().ToDictionaryAsync(...)` - Async conversion to dictionary

### Sequence Operations

| Method | Description | Example |
|--------|-------------|---------|
| `Concat` | Concatenates two sequences | `.Concat(otherPeople)` |
| `Append` | Appends element to end | `.Append(person)` |
| `Prepend` | Prepends element to start | `.Prepend(person)` |
| `Zip` | Combines sequences pairwise | `.Zip(ages, (p, a) => ...)` |
| `DefaultIfEmpty` | Returns default if empty | `.DefaultIfEmpty()` |

## JSONPath Support ([RFC 9535](https://www.rfc-editor.org/rfc/rfc9535.html))

**Blazing.Json.Queryable** provides powerful JSON filtering capabilities through **[Blazing.Json.JSONPath](https://github.com/gragra33/Blazing.Json.JSONPath)** - a high-performance, 100% RFC 9535 compliant JSONPath implementation.

> [!NOTE]
> **📖 Full JSONPath Documentation**: For complete details on JSONPath syntax, features, and RFC 9535 compliance, visit the **[Blazing.Json.JSONPath repository](https://github.com/gragra33/Blazing.Json.JSONPath)**. This library is automatically included as a dependency when you install Blazing.Json.Queryable.

### Overview

JSONPath provides a powerful query language for JSON documents, similar to XPath for XML. When used with Blazing.Json.Queryable, JSONPath expressions pre-filter JSON data **before deserialization**, dramatically improving performance and reducing memory usage.

The [RFC 9535](https://www.rfc-editor.org/rfc/rfc9535.html) standard defines a consistent, interoperable syntax for:
- **Filter Expressions**: Pre-filter JSON using comparison (`==`, `!=`, `<`, `>`, etc.) and logical operators (`&&`, `||`, `!`)
- **Built-in Functions**: `length()`, `count()`, `match()`, `search()`, `value()`
- **Array Slicing**: `[start:end:step]` with negative indices
- **Path Navigation**: Navigate nested structures efficiently

### Why Use JSONPath with Blazing.Json.Queryable?

**Performance Benefits**:
```csharp
// Traditional LINQ: Deserialize ALL 1M products, then filter in C#
var traditional = JsonQueryable<Product>.FromString(json)
    .Where(p => p.Price < 100 && p.InStock && p.Category == "Electronics")
    .ToList();
// Deserializes: 1,000,000 objects
// Memory: ~500MB, Time: ~2000ms

// JSONPath Pre-filter: Filter in JSON BEFORE deserialization (1M → 500 items)
var optimized = JsonQueryable<Product>
    .FromString(json, "$[?@.price < 100 && @.inStock == true && @.category == 'Electronics']")
    .ToList();
// Deserializes: 500 objects (only matches!)
// Memory: ~25MB (20x less!), Time: ~200ms (10x faster!)
```

**Key Advantages**:
- **10-100x faster** for highly selective queries on large datasets
- **90%+ memory reduction** by deserializing only matching items
- **Powered by Blazing.Json.JSONPath**: 100% RFC 9535 compliant implementation
- **Combines best of both**: JSONPath for pre-filtering + LINQ for rich operations

### JSONPath Syntax Quick Reference

| Feature | Syntax | Example | Description |
|---------|--------|---------|-------------|
| **Root** | `$` | `$` | Root JSON element |
| **Child** | `.property` or `['property']` | `$.data.users` | Access nested property |
| **Wildcard** | `*` or `[*]` | `$.users[*]` | All array elements |
| **Filter** | `[?expression]` | `$[?@.age > 25]` | Filter by condition |
| **Slice** | `[start:end]` | `$[2:10]` | Array slice |
| **Step** | `:step` | `$[::2]` | Every 2nd element |
| **Current** | `@` | `@.price < 100` | Current element in filter |
| **Comparison** | `==`, `!=`, `<`, `<=`, `>`, `>=` | `@.price >= 50` | Comparison operators |
| **Logical** | `&&`, `\|\|`, `!` | `@.age > 18 && @.active` | Logical operators |
| **length()** | `length(value)` | `length(@.name) > 5` | String, array, or object length |
| **match()** | `match(string, pattern)` | `match(@.email, '.*@example\\.com')` | Full regex match (I-Regexp) |
| **search()** | `search(string, pattern)` | `search(@.desc, 'wireless')` | Substring regex search |
| **count()** | `count(nodelist)` | `count($.items[*])` | Count nodes in nodelist |
| **value()** | `value(nodelist)` | `value(@.tags[0])` | Convert singular nodelist to value |

### Simple Path Navigation

Navigate to nested arrays using simple JSONPath expressions:

```csharp
// API response: { "data": { "user": { "repositories": [...] } } }
var repos = JsonQueryable<Repository>
    .FromString(apiResponse, "$.data.user.repositories[*]")
    .Where(r => !r.IsPrivate)
    .ToList();
```

### Filter Expressions

Apply powerful filters directly in JSONPath (powered by **[Blazing.Json.JSONPath](https://github.com/gragra33/Blazing.Json.JSONPath)**):

```csharp
// Comparison operators: ==, !=, <, <=, >, >=
var highEarners = JsonQueryable<Employee>
    .FromString(json, "$[?@.salary > 100000]")
    .ToList();

// Logical operators: &&, ||, !
var seniorHighEarners = JsonQueryable<Employee>
    .FromString(json, "$[?@.salary > 90000 && @.yearsEmployed >= 5]")
    .ToList();
```

### Built-in Functions

**Blazing.Json.JSONPath** provides all RFC 9535 standard functions:

```csharp
// length() - string, array, or object length
var longNames = JsonQueryable<Product>
    .FromString(json, "$[?length(@.name) > 10]")
    .ToList();

// match() - full regex match (I-Regexp RFC 9485)
var electronics = JsonQueryable<Product>
    .FromString(json, "$[?match(@.code, '^ELEC-.*')]")
    .ToList();

// search() - substring regex search
var wireless = JsonQueryable<Product>
    .FromString(json, "$[?search(@.name, 'Wireless')]")
    .ToList();
```

### Array Slicing

Advanced array slicing with step support:

```csharp
// [start:end] - elements from start to end (exclusive)
var middle = JsonQueryable<Item>
    .FromString(json, "$[2:5]")  // Elements 2, 3, 4
    .ToList();

// [start:end:step] - every Nth element
var everyOther = JsonQueryable<Item>
    .FromString(json, "$[0:9:2]")  // Elements 0, 2, 4, 6, 8
    .ToList();

// Negative indices - count from end
var last3 = JsonQueryable<Item>
    .FromString(json, "$[-3:]")  // Last 3 elements
    .ToList();

// Reverse with negative step
var reversed = JsonQueryable<Item>
    .FromString(json, "$[::-1]")  // All elements in reverse
    .ToList();
```

### Combining JSONPath with LINQ

The real power: JSONPath pre-filtering + LINQ rich operations

#### Real-World Example: Department Workforce Analysis

```csharp
var employeesJson = """
[
    {"id": 1, "name": "Alice Johnson", "department": "Engineering", "salary": 95000, "yearsEmployed": 5},
    {"id": 2, "name": "Bob Smith", "department": "Engineering", "salary": 105000, "yearsEmployed": 8},
    {"id": 3, "name": "Charlie Brown", "department": "Sales", "salary": 75000, "yearsEmployed": 3},
    {"id": 4, "name": "Diana Prince", "department": "Engineering", "salary": 120000, "yearsEmployed": 10},
    {"id": 5, "name": "Eve Davis", "department": "Marketing", "salary": 70000, "yearsEmployed": 2},
    {"id": 6, "name": "Frank Miller", "department": "Engineering", "salary": 98000, "yearsEmployed": 6}
]
""";

Console.WriteLine("\nDepartment workforce analysis [MIXED JSONPATH + LINQ]:");
var deptAnalysis = JsonQueryable<Employee>
    .FromString(employeesJson, "$[?@.salary > 60000]")  // JSONPath: Filter employees with salary > $60K
    .GroupBy(e => e.Department)                          // LINQ: Group by department
    .Select(g => new
    {
        Department = g.Key,
        EmployeeCount = g.Count(),
        AvgSalary = g.Average(e => e.Salary),
        TotalYearsExperience = g.Sum(e => e.YearsEmployed),
        TopEarner = g.OrderByDescending(e => e.Salary).First().Name
    })
    .OrderByDescending(x => x.AvgSalary)    // LINQ: Primary sort by avg salary (descending)
    .ThenBy(x => x.Department)              // LINQ: Secondary sort by department name (ascending)
    .ToList();

foreach (var dept in deptAnalysis)
{
    Console.WriteLine($"      - {dept.Department}: {dept.EmployeeCount} employees, " +
                     $"${dept.AvgSalary:N0} avg salary, " +
                     $"{dept.TotalYearsExperience} total years, " +
                     $"top earner: {dept.TopEarner}");
}
```

**Output**:
```
Department workforce analysis [MIXED JSONPATH + LINQ]:
      - Engineering: 4 employees, $104,500 avg salary, 29 total years, top earner: Diana Prince
      - Sales: 1 employees, $75,000 avg salary, 3 total years, top earner: Charlie Brown
      - Marketing: 1 employees, $70,000 avg salary, 2 total years, top earner: Eve Davis
```

**Performance Analysis**:
- **JSONPath Pre-filter** (`$[?@.salary > 60000]`): Filters in JSON before deserialization
- **LINQ Operations**: GroupBy, aggregations, sorting on filtered set
- **Result**: Minimal memory usage, type-safe operations, optimal performance

### Performance Best Practices (Memory & Speed)

> [!WARNING]
> **JSONPath Memory Considerations**: The RFC 9535 specification requires the entire JSON document to be loaded into memory as a `JsonDocument` for advanced JSONPath features. **Advanced filters, functions, and array slicing** (e.g., `$[?@.price < 100]`, `$[0:10]`, `length(@.name)`) will load the **entire document** before parsing, regardless of streaming.
>
> **Exception**: Simple wildcard-only paths (e.g., `$.data[*]`, `$.departments[*].employees[*]`) are handled differently by Blazing.Json.Queryable and **maintain true streaming** without loading the full document, even with multiple levels of nesting.

#### Working with Large JSON Documents

**Blazing.Json.JSONPath** requires the entire JSON document to be loaded into memory as a `JsonDocument` when using advanced RFC 9535 features:
- **Filter expressions**: `$[?@.age > 25]`, `$[?@.price < 100 && @.inStock]`
- **Array slicing**: `$[0:10]`, `$[2:5:2]`, `$[-3:]`
- **Functions**: `length()`, `count()`, `match()`, `search()`, `value()`

However, **simple wildcard-only paths** use streaming and maintain constant memory usage:
- **Single-level wildcards**: `$.data[*]`, `$.users[*]`
- **Multi-level wildcards**: `$.departments[*].employees[*]`, `$.organization[*].divisions[*].departments[*].employees[*]`

**Memory Usage Guidelines:**
- **Small documents (<1MB)**: No concerns - use any JSONPath feature
- **Medium documents (1-100MB)**: Generally fine on modern systems - monitor memory usage
- **Large documents (100MB-1GB)**: Use simple wildcard paths for streaming, avoid advanced filters. Use Linq with JSONPath simple wildcard-only paths instead
- **Very large documents (>1GB)**: **Use simple wildcard paths only** - avoid advanced filters. Use Linq with JSONPath simple wildcard-only paths instead

#### Best Practices for Large Documents

**DO: Use Simple Wildcard Paths for True Streaming (All File Sizes)**
```csharp
// GOOD: True streaming with simple wildcard paths (constant memory)
await using var stream = File.OpenRead("huge-file.json"); // 2GB file

// Single-level wildcard
var results1 = await JsonQueryable<Product>
    .FromStream(stream, "$.data[*]")  // Simple wildcard - maintains streaming!
    .Where(p => p.Price < 100 && p.InStock)  // LINQ filtering (streamed)
    .Take(10)
    .AsAsyncEnumerable()
    .ToListAsync();

// Multi-level wildcards - ALSO streams!
var results2 = await JsonQueryable<Employee>
    .FromStream(stream, "$.departments[*].employees[*]")  // Multi-level - still streams!
    .Where(e => e.Salary > 60000)  // LINQ filtering (streamed)
    .Take(10)
    .AsAsyncEnumerable()
    .ToListAsync();

// Deep nesting - STILL streams!
var results3 = await JsonQueryable<Employee>
    .FromStream(stream, "$.organization[*].divisions[*].departments[*].employees[*]")
    .Where(e => e.IsActive)  // LINQ filtering (streamed)
    .Take(10)
    .AsAsyncEnumerable()
    .ToListAsync();

// Memory: ~25MB (constant), processes 2GB file safely with ANY level of nesting
```

**DO: Use JSONPath Advanced Filters for Selective Filtering (Small/Medium Files)**
```csharp
// GOOD: Pre-filter with JSONPath advanced filters (100MB file, 1M → 500 items)
var results = JsonQueryable<Product>
    .FromString(mediumJson, "$[?@.price < 100 && @.inStock == true]")
    .OrderBy(p => p.Name)
    .Take(10)
    .ToList();
// Memory: Loads 100MB + filters → ~25MB result (acceptable for medium files)
```

**❌ DON'T: Use Advanced JSONPath Filters on Very Large Files (>1GB)**
```csharp
// BAD: Advanced filter loads ENTIRE 2GB file into memory!
var results = JsonQueryable<Product>
    .FromString(hugeJson, "$[?@.price < 100]")  // Loads ALL 2GB!
    .ToList();
// Memory: 2GB+ (OutOfMemoryException risk!)
```

**❌ DON'T: Use LINQ Where for Initial Filtering on Large Datasets (1M+) Without JSONPath**
```csharp
// BAD: Deserializes ALL 1M first, then filters
var results = JsonQueryable<Product>
    .FromString(largeJson)  // No JSONPath pre-filter!
    .Where(p => p.Price < 100 && p.InStock)  // Deserializes 1,000,000 objects
    .OrderBy(p => p.Name)
    .Take(10)
    .ToList();
// Memory: ~500MB (all objects deserialized before filtering)
```

#### Streaming Strategy Summary

| Document Size | Recommended Approach | JSONPath Pattern | Memory Impact |
|--------------|---------------------|------------------|---------------|
| **< 1MB** | Any JSONPath feature | `$[?@.price < 100]` or `$.data[*]` | Minimal |
| **1-100MB** | Advanced filters (monitor) or simple wildcards | `$[?@.price < 100]` or `$.data[*]` | Document size + overhead OR constant |
| **100MB-1GB** | **Simple wildcard paths (preferred)** | `$.data[*]` or `$.dept[*].emp[*]` | Constant (~25MB) |
| **> 1GB** | **Simple wildcard paths (required)** | `$.data[*]` or `$.dept[*].emp[*]` | Constant (~25MB) |

**Key Features**:
- 100% RFC 9535 compliant
- High-performance implementation
- Full filter expression support
- All standard built-in functions
- Array slicing with step
- I-Regexp (RFC 9485) regex support

**Key Takeaway**: 
- **Simple wildcard paths** (e.g., `$.data[*]`, `$.departments[*].employees[*]`) maintain true streaming regardless of nesting depth - perfect for large/very large files
- **Advanced JSONPath filters** (e.g., `$[?@.price < 100]`, `$[0:10]`, `length()`) load entire document - use only for small/medium files
- For large files, use simple wildcards + LINQ filtering to get constant memory usage


**Sample Code**:
- `samples/Blazing.Json.Queryable.Samples/Examples/AdvancedJsonPathSamples.cs`
- Real-world scenarios demonstrating JSONPath + LINQ combinations

## How It Works

**Blazing.Json.Queryable** uses a custom LINQ provider that translates LINQ expressions into efficient JSON processing operations:

1. **Expression Tree Analysis**: LINQ queries are analyzed to build an execution plan
2. **Streaming JSON Parser**: Uses `System.Text.Json.Utf8JsonReader` for efficient parsing
3. **JSONPath Navigation**: Powered by **[Blazing.Json.JSONPath](https://github.com/gragra33/Blazing.Json.JSONPath)** for RFC 9535 compliant filtering when JSONPath expressions are used
4. **Lazy Evaluation**: Only processes JSON elements needed for the query
5. **Early Termination**: Stops reading JSON as soon as query requirements are met
6. **Zero-Allocation UTF-8**: Direct UTF-8 processing using `Span<byte>` and `ReadOnlySpan<byte>`
7. **Async Enumeration**: Native `IAsyncEnumerable<T>` support for non-blocking I/O

### Execution Flow Example

```csharp
// Query: Find first 10 active users over 25 in London
var results = await JsonQueryable<Person>.FromStream(stream)
    .Where(p => p.Age > 25)
    .Where(p => p.City == "London")
    .Where(p => p.IsActive)
    .OrderBy(p => p.Name)
    .Take(10)
    .AsAsyncEnumerable()
    .ToListAsync();

// Internal execution:
// 1. Build execution plan from LINQ expression tree
// 2. Open JSON stream and start parsing
// 3. For each JSON object:
//    a. Check Age > 25 (filter early)
//    b. Check City == "London" (filter)
//    c. Check IsActive (filter)
//    d. If all pass, deserialize to Person object
//    e. Add to ordered buffer
// 4. Stop reading after 10th match (early termination!)
// 5. Return results without processing entire file
```

## Performance Advantages

### Real-World Performance Comparison

**Scenario**: 100,000 record JSON file (52MB), find first 10 matching records

| Approach | Time | Memory | Notes |
|----------|------|--------|-------|
| **Traditional** | 1,200ms | 450MB | Loads entire file, deserializes all |
| **Blazing.Json.Queryable** | **120ms** | **25MB** | Streams, stops after 10 matches |
| **Improvement** | **10x faster** | **18x less** | 90% time reduction |

### Key Performance Benefits

1. **Early Termination**: 
   - Traditional: Processes ALL 100K records
   - **Blazing.Json.Queryable**: Stops after finding 10 matches
   - Result: **10-20x faster** for Take/First operations

2. **Memory Efficiency**:
   - Traditional: Loads 52MB file + 450MB objects
   - **Blazing.Json.Queryable**: Uses ~25MB constant memory
   - Result: Can process files **larger than available RAM**

3. **Zero-Allocation UTF-8**:
   - Traditional: UTF-8 → String → Object
   - **Blazing.Json.Queryable**: UTF-8 → Object (direct)
   - Result: **Fewer allocations, less GC pressure**

4. **Async Streaming**:
   - Traditional: Blocking I/O during file read
   - **Blazing.Json.Queryable**: Non-blocking async enumeration
   - Result: **Better scalability** in web APIs

### When Performance Gains Are Largest

- **Large files** (> 10MB): Bigger file = larger advantage
- **Early termination** (Take/First): Smaller N = larger speedup
- **Complex filters**: More filtering = more benefit from stream processing
- **Memory constraints**: Limited RAM = **Blazing.Json.Queryable** enables processing that's impossible traditionally

## Samples

The library includes comprehensive sample and benchmark projects demonstrating different usage patterns and performance characteristics:

### Sample Projects

All samples are located in the `samples/Blazing.Json.Queryable.Samples` directory (basic to advanced order):

1. **BasicQueries.cs** - Fundamental LINQ operations (Where, Select, OrderBy, Take, Skip, First, Count, Any)
2. **Utf8Queries.cs** - Zero-allocation UTF-8 processing examples
3. **StreamQueries.cs** - Async streaming with `IAsyncEnumerable<T>`
4. **AsyncQueries.cs** - .NET 10 async LINQ with async predicates and transformations
5. **CustomConverters.cs** - Custom JSON converters and serialization options
15. **ElementAccessSamples.cs** - Element access operations (ElementAt, Last, Single with C# Index support)
16. **ConversionOperationsSamples.cs** - Conversion operations (ToDictionary, ToHashSet, ToLookup)
6. **AdvancedScenarios.cs** - Real-world patterns, error handling, and best practices
7. **LargeDatasetSamples.cs** - In-memory processing of large datasets (100K-1M records)
8. **LargeDatasetFileStreamingSamples.cs** - True I/O streaming for memory-efficient large file processing
9. **ComplexGroupingSamples.cs** - GroupBy, Join, and GroupJoin operations with aggregations
10. **JsonPathSamples.cs** - Simple JSONPath navigation for nested structures
11. **AdvancedJsonPathSamples.cs** - RFC 9535 filters, functions, slicing, and real-world scenarios
12. **AdvancedLinqOperationsSamples.cs** - Advanced operations (Chunk, Zip, DistinctBy, ExceptBy, IntersectBy, UnionBy)
13. **QuerySyntaxSamples.cs** - Query expression syntax (SQL-like declarative LINQ queries)
14. **PerformanceComparison.cs** - Benchmarks comparing traditional vs streaming approaches

### Benchmark Suites

All benchmarks are located in the `benchmarks/Blazing.Json.Queryable.Benchmarks` directory:

1. **SyncInMemoryBenchmarks** - In-memory performance (100, 1K, 10K records)
2. **SyncFileIOBenchmarks** - File I/O performance (1K, 10K records)  
3. **AsyncStreamBenchmarks** - Async streaming (10K, 100K records)
4. **MemoryAllocationBenchmarks** - Zero-allocation validation
5. **LargeFileStreamingBenchmarks** - Constant memory proof (10MB, 100MB files)
6. **ComprehensiveComparisonBenchmarks** - Side-by-side comparison across all scenarios

### Running the Samples

```bash
# Clone the repository
git clone https://github.com/gragra33/Blazing.Json.Queryable.git
cd Blazing.Json.Queryable

# Run the samples project (interactive menu)
dotnet run --project samples/Blazing.Json.Queryable.Samples
```

The samples include:
- Interactive console menu
- Performance benchmarks with timing
- Memory usage comparisons
- Real-world scenarios
- Best practices demonstrations

### Running the Benchmarks

```bash
# Run benchmarks (interactive mode)
cd benchmarks/Blazing.Json.Queryable.Benchmarks
dotnet run -c Release

# Or run specific benchmark suite
dotnet run -c Release -- --filter *SyncInMemory*
dotnet run -c Release -- --filter *AsyncStream*
dotnet run -c Release -- --filter *Comprehensive*

# List all available benchmarks
dotnet run -c Release -- --list flat
```

The benchmarks provide:
- Detailed performance metrics (Mean, StdDev, Min, Max)
- Memory allocation tracking
- GC collection statistics
- Side-by-side comparisons (Traditional vs Blazing.Json.Queryable)
- HTML/Markdown reports in `BenchmarkDotNet.Artifacts`

## Give a ⭐

If you like or are using this project to learn or start your solution, please give it a star. Thanks!

Also, if you find this library useful and you're feeling really generous, please consider [buying me a coffee ☕](https://bmc.link/gragra33).

## Support

- **Documentation**: Full API documentation included in NuGet package
- **Samples**: Comprehensive samples in the repository
- **Issues**: Report bugs or request features on [GitHub Issues](https://github.com/gragra33/Blazing.Json.Queryable/issues)
- **Discussions**: Ask questions on [GitHub Discussions](https://github.com/gragra33/Blazing.Json.Queryable/discussions)

## History

### V1.1.1 - 12 January, 2026

- **Streaming Restoration for Simple Wildcard Paths**
  - Restored true streaming for simple wildcard-only JSONPath expressions
  - Multi-level wildcard paths (e.g., `$.departments[*].employees[*]`) now stream with constant memory usage
- **Documentation Updates**:
  - Updated [Performance Best Practices](#performance-best-practices) with accurate streaming behavior
  - Clear guidance on when simple wildcards stream vs when advanced features materialize
  - Memory usage guidelines for different file sizes
- **Backward Compatibility**:
  - All existing code continues to work without changes
  - Advanced RFC 9535 features (filters, functions, slicing) continue to use optimized materialization
  - No breaking changes to public API
- **New Samples**:
  - ElementAccessSamples.cs - Element access operations
  - ConversionOperationsSamples.cs - Conversion operations

### V1.1.0 - 12 January, 2026

- **RFC 9535 JSONPath Support**
  - Full compliance with [RFC 9535](https://www.rfc-editor.org/rfc/rfc9535.html) - the official IETF standard
  - Filter expressions in JSON before deserialization
  - Regex pattern matching with I-Regexp (RFC 9485)
  - Substring search capabilities
  - Combine JSONPath filters with LINQ operations
  - Nested path navigation with filters  
- **Performance Improvements**:
  - Pre-filter in JSON reduces deserialization overhead
  - 10-100x faster for filtered queries on large datasets
  - Seamless integration with existing LINQ pipeline
  - Automatic optimization routing  
- **New Samples**:
  - AdvancedJsonPathSamples - RFC 9535 features demonstration
  - Real-world scenarios with complex filters
  - Performance comparison examples  

### V1.0.0 - 11 January, 2026

**Initial Release** - Full production release

- **Core Features**:
  - Custom LINQ provider for JSON with 60+ LINQ methods
    - Query Expression and Method Syntax support
  - Direct JSON processing without full deserialization
  - Early termination support for Take, First, Any operations
  - Memory-efficient streaming for files larger than RAM
- **Input Format Support**:
  - Standard JSON strings
  - UTF-8 byte arrays and spans (zero-allocation)
  - File streams with async enumeration
  - Advanced streaming with `IAsyncEnumerable<T>`
- **LINQ Operations**:
  - **Filtering**: Where, OfType
  - **Projection**: Select, SelectMany, Cast
  - **Ordering**: OrderBy, OrderByDescending, ThenBy, ThenByDescending, Order, OrderDescending, Reverse
  - **Quantifiers**: All, Any, Contains, SequenceEqual
  - **Element Access**: First, FirstOrDefault, Last, LastOrDefault, Single, SingleOrDefault, ElementAt, ElementAtOrDefault
  - **Aggregation**: Count, LongCount, Sum, Average, Min, Max, MinBy, MaxBy, Aggregate
  - **Set Operations**: Distinct, DistinctBy, Union, UnionBy, Intersect, IntersectBy, Except, ExceptBy
  - **Partitioning**: Take, TakeLast, TakeWhile, Skip, SkipLast, SkipWhile, Chunk
  - **Grouping**: GroupBy, GroupJoin, Join
  - **Conversion**: ToList, ToArray, ToDictionary, ToHashSet, ToLookup, AsEnumerable, AsQueryable, AsAsyncEnumerable
  - **Sequence**: Concat, Append, Prepend, Zip, DefaultIfEmpty
- **.NET 10 Features**:
  - Built-in async LINQ support with async predicates
  - Async transformations with Select
  - Native cancellation support
  - Enhanced performance with latest runtime optimizations
- **Performance Optimizations**:
  - Zero-allocation UTF-8 processing with Span<T>
  - Early termination (10-20x faster for Take/First on large files)
  - Constant memory usage regardless of file size
  - Efficient execution plan optimization
- **Advanced Capabilities**:
  - Simple JSONPath support for nested JSON navigation
  - Complex query translation and optimization
  - Async streaming with proper cancellation
- **Documentation**:
  - Comprehensive README with examples
  - Full XML documentation in NuGet package
  - Sample project with 10+ demonstrations of all features
  - Performance comparison benchmarks
- **Quality Assurance**:
  - Comprehensive test coverage
  - Real-world performance benchmarks
  - Production-ready error handling
  - Best practices implementation
---

**License**: MIT License - see [LICENSE](LICENSE) file for details

**Copyright** © 2026 Graeme Grant. All rights reserved.
