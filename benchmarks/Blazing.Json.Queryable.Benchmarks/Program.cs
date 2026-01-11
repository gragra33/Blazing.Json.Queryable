using BenchmarkDotNet.Running;
using Blazing.Json.Queryable.Benchmarks;

// Entry point for running benchmarks
// Usage examples:
//   dotnet run -c Release                                    # Interactive mode
//   dotnet run -c Release -- --filter *SyncInMemory*         # Run Suite 1 only
//   dotnet run -c Release -- --filter *SyncFileIO*           # Run Suite 2 only
//   dotnet run -c Release -- --filter *AsyncStream*          # Run Suite 3 only
//   dotnet run -c Release -- --filter *MemoryAllocation*     # Run Suite 4 only
//   dotnet run -c Release -- --filter *LargeFileStreaming*   # Run Suite 5 only
//   dotnet run -c Release -- --filter *Comprehensive*        # Run Suite 6 only
//   dotnet run -c Release -- --filter *Traditional*          # Run all Traditional benchmarks
//   dotnet run -c Release -- --filter *Custom*               # Run all Custom provider benchmarks
//   dotnet run -c Release -- --list flat                     # List all benchmarks without running

var switcher = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly);

if (args.Length == 0)
{
    Console.WriteLine("══════════════════════════════════════════════════════════════════");
    Console.WriteLine("│  Blazing.Json.Queryable - Performance Benchmark Suites        │");
    Console.WriteLine("══════════════════════════════════════════════════════════════════");
    Console.WriteLine();
    Console.WriteLine("Available benchmark suites:");
    Console.WriteLine("  1. SyncInMemoryBenchmarks       - In-memory performance (100, 1K, 10K records)");
    Console.WriteLine("  2. SyncFileIOBenchmarks         - File I/O performance (1K, 10K records)");
    Console.WriteLine("  3. AsyncStreamBenchmarks        - Async streaming (10K, 100K records)");
    Console.WriteLine("  4. MemoryAllocationBenchmarks   - Zero-allocation validation");
    Console.WriteLine("  5. LargeFileStreamingBenchmarks - Constant memory proof (10MB, 100MB)");
    Console.WriteLine("  6. ComprehensiveComparisonBenchmarks - Side-by-side comparison");
    Console.WriteLine();
    Console.WriteLine("Quick selection:");
    Console.WriteLine("  [A] Run ALL benchmarks (takes 30-60 minutes)");
    Console.WriteLine("  [1-6] Run specific suite");
    Console.WriteLine("  [Q] Quit");
    Console.WriteLine();
    Console.Write("Your choice: ");
    
    var choice = Console.ReadLine()?.Trim().ToUpper();
    
    switch (choice)
    {
        case "A":
            Console.WriteLine("\nRunning ALL benchmark suites...");
            switcher.RunAll();
            break;
        case "1":
            Console.WriteLine("\nRunning Suite 1: SyncInMemoryBenchmarks...");
            BenchmarkRunner.Run<SyncInMemoryBenchmarks>();
            break;
        case "2":
            Console.WriteLine("\nRunning Suite 2: SyncFileIOBenchmarks...");
            BenchmarkRunner.Run<SyncFileIOBenchmarks>();
            break;
        case "3":
            Console.WriteLine("\nRunning Suite 3: AsyncStreamBenchmarks...");
            BenchmarkRunner.Run<AsyncStreamBenchmarks>();
            break;
        case "4":
            Console.WriteLine("\nRunning Suite 4: MemoryAllocationBenchmarks...");
            BenchmarkRunner.Run<MemoryAllocationBenchmarks>();
            break;
        case "5":
            Console.WriteLine("\nRunning Suite 5: LargeFileStreamingBenchmarks...");
            BenchmarkRunner.Run<LargeFileStreamingBenchmarks>();
            break;
        case "6":
            Console.WriteLine("\nRunning Suite 6: ComprehensiveComparisonBenchmarks...");
            BenchmarkRunner.Run<ComprehensiveComparisonBenchmarks>();
            break;
        case "Q":
            Console.WriteLine("Exiting...");
            return;
        default:
            Console.WriteLine();
            Console.WriteLine("Command-line usage:");
            Console.WriteLine("  dotnet run -c Release -- --filter *SyncInMemory*");
            Console.WriteLine("  dotnet run -c Release -- --filter *Custom*");
            Console.WriteLine("  dotnet run -c Release -- --list flat");
            Console.WriteLine();
            Console.WriteLine("See full options: dotnet run -c Release -- --help");
            break;
    }
}
else
{
    // Pass through to BenchmarkDotNet for advanced filtering
    switcher.Run(args);
}
