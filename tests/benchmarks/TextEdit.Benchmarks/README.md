# TextEdit Performance Benchmarks

BenchmarkDotNet-based performance benchmarks for TextEdit document operations.

## Overview

This project provides comprehensive performance benchmarks for:
- **DocumentService.OpenAsync**: Loading files of various sizes (small, large, very large >10MB streaming)
- **DocumentService.SaveAsync**: Saving modified documents
- **DocumentService.UpdateContent**: Simulating typing and edit operations with undo/redo

## Running Benchmarks

### Quick Start

From the repository root:

```bash
dotnet run -c Release --project tests/benchmarks/TextEdit.Benchmarks
```

### Run Specific Benchmarks

```bash
# Run only DocumentService benchmarks
dotnet run -c Release --project tests/benchmarks/TextEdit.Benchmarks --filter '*DocumentServiceBenchmarks*'

# Run only OpenAsync benchmarks
dotnet run -c Release --project tests/benchmarks/TextEdit.Benchmarks --filter '*OpenAsync*'
```

### Output

Results are written to `BenchmarkDotNet.Artifacts/results/` and include:
- **Console output**: Summary table with mean times, allocations, and memory usage
- **HTML report**: Detailed report with charts
- **CSV/JSON**: Machine-readable results for tracking over time

## Benchmark Scenarios

### File Size Categories

- **Small**: 10KB (baseline for fast operations)
- **Large (below threshold)**: 5MB (standard read/write, no streaming)
- **Very Large (above 10MB threshold)**: 15MB (triggers streaming with chunking)

### Measured Operations

1. **OpenAsync_SmallFile**: Fast path for typical files
2. **OpenAsync_LargeFile_BelowThreshold**: Large files using standard I/O
3. **OpenAsync_VeryLargeFile_AboveThreshold**: Streaming path for files >10MB
4. **SaveAsync_SmallFile**: Save with minimal overhead
5. **SaveAsync_LargeFile_BelowThreshold**: Save large content (standard)
6. **UpdateContent_SmallDocument**: Typing simulation with 100 edits
7. **UpdateContent_LargeDocument**: Typing on large documents (50 edits)

### Markdown Rendering Benchmarks

1. **Render_PlainText_1KB** through **Render_VeryLargeDocument_500KB**
2. **Render_{Size}_ColdCache**: First render with hash computation overhead
3. **Render_{Size}_WarmCache**: Cached retrieval performance
4. **Render_MultipleDocuments_CacheEffectiveness**: Realistic editing session

**Cache Effectiveness Results:**
- Small (2KB): **165x faster** with cache (1.24ms → 7.5μs)
- Medium (10KB): **188x faster** with cache (4.25ms → 22.6μs)
- Large (50KB): **330x faster** with cache (2.83ms → 8.6μs)
- Multi-document (10 docs × 5 renders): **137x overall speedup**
- Memory reduction: **94%** fewer allocations on cache hits

## Interpreting Results

Key metrics:
- **Mean**: Average execution time (lower is better)
- **Allocated**: Memory allocated per operation (lower is better)
- **Gen0/Gen1/Gen2**: Garbage collection counts (lower is better)

### Expected Performance Characteristics

- Very large file opens should show **lower memory** due to streaming (chunked I/O)
- Standard file opens may be faster but use more memory
- UpdateContent should scale linearly with document size

## Adding New Benchmarks

1. Create a new class in this project
2. Add `[MemoryDiagnoser]` and benchmark method attributes
3. Use `[GlobalSetup]` and `[GlobalCleanup]` for initialization/cleanup
4. Follow naming convention: `{Operation}_{Scenario}`

Example:

```csharp
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class MyBenchmarks
{
    [Benchmark]
    public void MyOperation_Scenario()
    {
        // Benchmark code
    }
}
```

## CI Integration

To track performance regressions, consider:
- Running benchmarks on each PR
- Comparing results against baseline
- Failing builds if performance degrades >10%

Use BenchmarkDotNet's `--exporters json` and compare with tools like [BenchmarkDotNet.ResultsComparer](https://github.com/dotnet/BenchmarkDotNet.ResultsComparer).

## References

- [BenchmarkDotNet Documentation](https://benchmarkdotnet.org/)
- [Best Practices](https://benchmarkdotnet.org/articles/guides/good-practices.html)
- [Interpreting Results](https://benchmarkdotnet.org/articles/guides/console-args.html)
