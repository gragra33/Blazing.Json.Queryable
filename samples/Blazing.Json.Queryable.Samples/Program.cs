using Blazing.Json.Queryable.Samples.Examples;
using Blazing.Json.Queryable.Samples.Data;

Console.WriteLine("+================================================================+");
Console.WriteLine("|   Blazing.Json.Queryable - Sample Application                  |");
Console.WriteLine("|   High-Performance LINQ Provider for System.Text.Json          |");
Console.WriteLine("+================================================================+");
Console.WriteLine();

// Generate large datasets if needed
Console.WriteLine("Initializing sample data...");
DatasetGenerator.GenerateAllDatasets();
Console.WriteLine();

while (true)
{
    Console.WriteLine("=================================================================");
    Console.WriteLine("MAIN MENU");
    Console.WriteLine("=================================================================");
    Console.WriteLine();
    Console.WriteLine("  1. Basic Queries (Where, Select, OrderBy, Take, Skip, etc.)");
    Console.WriteLine("  2. UTF-8 Optimized Queries (Performance benefits)");
    Console.WriteLine("  3. Stream Processing (Memory-efficient large files)");
    Console.WriteLine("  4. Async Queries (.NET 10 async LINQ support)");
    Console.WriteLine("  5. Custom Converters (JsonSerializerOptions)");
    Console.WriteLine("  6. Advanced Scenarios (Complex queries, error handling)");
    Console.WriteLine("  7. Large Dataset Samples (100K-1M records, in-memory streaming)");
    Console.WriteLine("  8. Large Dataset File Streaming (True I/O streaming benefits)");
    Console.WriteLine("  9. Complex GroupBy Operations (Aggregations, nested grouping)");
    Console.WriteLine(" 10. JSONPath Filtering (Multi-level array wildcards)");
    Console.WriteLine(" 11. Advanced LINQ Operations (Chunk, Join, GroupJoin, GroupBy)");
    Console.WriteLine(" 12. Performance Comparisons (Benchmarks)");
    Console.WriteLine(" 13. Run All Examples");
    Console.WriteLine("  0. Exit");
    Console.WriteLine();
    Console.Write("Select an option (0-13): ");
    
    var input = Console.ReadLine();
    Console.WriteLine();
    
    try
    {
        switch (input)
        {
            case "1":
                BasicQueries.RunAll();
                break;
                
            case "2":
                Utf8Queries.RunAll();
                break;
                
            case "3":
                StreamQueries.RunAll();
                break;
                
            case "4":
                await AsyncQueries.RunAllAsync();
                break;
                
            case "5":
                CustomConverters.RunAll();
                break;
                
            case "6":
                await AdvancedScenarios.RunAllAsync();
                break;
                
            case "7":
                await LargeDatasetSamples.RunAllAsync();
                break;
                
            case "8":
                await LargeDatasetFileStreamingSamples.RunAllAsync();
                break;
                
            case "9":
                ComplexGroupingSamples.RunAll();
                break;
                
            case "10":
                JsonPathSamples.RunAll();
                break;
                
            case "11":
                AdvancedLinqOperationsSamples.RunAll();
                break;
                
            case "12":
                await PerformanceComparison.RunAllAsync();
                break;
                
            case "13":
                Console.WriteLine("=================================================================");
                Console.WriteLine("RUNNING ALL EXAMPLES");
                Console.WriteLine("=================================================================");
                Console.WriteLine();
                
                BasicQueries.RunAll();
                Utf8Queries.RunAll();
                StreamQueries.RunAll();
                await AsyncQueries.RunAllAsync();
                CustomConverters.RunAll();
                await AdvancedScenarios.RunAllAsync();
                await LargeDatasetSamples.RunAllAsync();
                await LargeDatasetFileStreamingSamples.RunAllAsync();
                ComplexGroupingSamples.RunAll();
                JsonPathSamples.RunAll();
                AdvancedLinqOperationsSamples.RunAll();
                await PerformanceComparison.RunAllAsync();
                
                Console.WriteLine("=================================================================");
                Console.WriteLine("ALL EXAMPLES COMPLETE");
                Console.WriteLine("=================================================================");
                Console.WriteLine();
                break;
                
            case "0":
                Console.WriteLine("Thank you for using Blazing.Json.Queryable!");
                Console.WriteLine("Visit https://github.com/gragra33/Blazing.Json.Queryable for more info.");
                return;
                
            default:
                Console.WriteLine("Invalid option. Please select 0-13.");
                Console.WriteLine();
                break;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"ERROR: {ex.Message}");
        Console.WriteLine();
        Console.WriteLine("Stack Trace:");
        Console.WriteLine(ex.StackTrace);
        Console.WriteLine();
    }
    
    Console.WriteLine("Press any key to continue...");
    Console.ReadLine();
    Console.Clear();
}
