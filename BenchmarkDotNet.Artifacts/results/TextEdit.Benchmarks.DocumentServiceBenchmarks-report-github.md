```

BenchmarkDotNet v0.15.4, Linux CachyOS
AMD Ryzen 5 5600X 4.62GHz, 1 CPU, 12 logical and 6 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 8.0.20 (8.0.20, 8.0.2025.41914), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 8.0.20 (8.0.20, 8.0.2025.41914), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 8.0.20 (8.0.20, 8.0.2025.41914), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method              | Job        | IterationCount | LaunchCount | Mean     | Error    | StdDev   | Gen0   | Gen1   | Allocated |
|-------------------- |----------- |--------------- |------------ |---------:|---------:|---------:|-------:|-------:|----------:|
| OpenAsync_SmallFile | Job-YFEFPZ | 10             | Default     | 59.49 μs | 2.964 μs | 1.764 μs | 5.0049 | 2.4414 |  61.55 KB |
| OpenAsync_SmallFile | ShortRun   | 3              | 1           | 53.68 μs | 3.932 μs | 0.216 μs | 5.0049 | 2.4414 |  61.55 KB |
