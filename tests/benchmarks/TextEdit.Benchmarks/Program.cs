using BenchmarkDotNet.Running;

namespace TextEdit.Benchmarks;

/// <summary>
/// Entry point for running BenchmarkDotNet benchmarks.
/// Run with: dotnet run -c Release --project tests/benchmarks/TextEdit.Benchmarks
/// </summary>
public class Program
{
    public static void Main(string[] args)
    {
        // Run all benchmarks in the assembly
        var summary = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        
        // Optionally run specific benchmarks:
        // BenchmarkRunner.Run<DocumentServiceBenchmarks>(args);
    }
}
