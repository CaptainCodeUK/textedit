```

BenchmarkDotNet v0.15.4, Linux CachyOS
AMD Ryzen 5 5600X 3.87GHz, 1 CPU, 12 logical and 6 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 8.0.20 (8.0.20, 8.0.2025.41914), X64 RyuJIT x86-64-v3
  Job-QKDGBD : .NET 8.0.20 (8.0.20, 8.0.2025.41914), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 8.0.20 (8.0.20, 8.0.2025.41914), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 8.0.20 (8.0.20, 8.0.2025.41914), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                                      | Job        | InvocationCount | IterationCount | LaunchCount | UnrollFactor | Mean         | Error          | StdDev        | Median       | Gen0    | Gen1    | Allocated |
|-------------------------------------------- |----------- |---------------- |--------------- |------------ |------------- |-------------:|---------------:|--------------:|-------------:|--------:|--------:|----------:|
| Render_Small_ColdCache                      | Job-QKDGBD | 1               | 10             | Default     | 1            | 1,238.348 μs |    201.5860 μs |   133.3367 μs | 1,286.938 μs |       - |       - | 176.76 KB |
| Render_Medium_ColdCache                     | Job-QKDGBD | 1               | 10             | Default     | 1            | 4,246.471 μs |    572.8635 μs |   340.9019 μs | 4,068.666 μs |       - |       - | 516.13 KB |
| Render_Large_ColdCache                      | Job-QKDGBD | 1               | 10             | Default     | 1            | 2,834.187 μs |    496.3859 μs |   259.6194 μs | 2,717.278 μs |       - |       - | 357.59 KB |
| Render_Small_WarmCache                      | Job-YFEFPZ | Default         | 10             | Default     | 16           |     7.452 μs |      0.0497 μs |     0.0296 μs |     7.454 μs |  0.6332 |  0.0076 |  10.45 KB |
| Render_Medium_WarmCache                     | Job-YFEFPZ | Default         | 10             | Default     | 16           |    22.576 μs |      0.8808 μs |     0.5242 μs |    22.659 μs |  2.0752 |       - |  34.17 KB |
| Render_Large_WarmCache                      | Job-YFEFPZ | Default         | 10             | Default     | 16           |     8.600 μs |      0.0712 μs |     0.0471 μs |     8.578 μs |  0.7477 |  0.0153 |  12.39 KB |
| Render_MultipleDocuments_CacheEffectiveness | Job-YFEFPZ | Default         | 10             | Default     | 16           |   409.815 μs |      8.0160 μs |     5.3021 μs |   409.804 μs | 58.1055 | 27.3438 | 951.91 KB |
| Render_Small_WarmCache                      | ShortRun   | Default         | 3              | 1           | 16           |     7.552 μs |      1.7869 μs |     0.0979 μs |     7.546 μs |  0.6256 |       - |  10.45 KB |
| Render_Medium_WarmCache                     | ShortRun   | Default         | 3              | 1           | 16           |    21.521 μs |      1.5461 μs |     0.0847 μs |    21.490 μs |  2.0752 |       - |  34.17 KB |
| Render_Large_WarmCache                      | ShortRun   | Default         | 3              | 1           | 16           |     8.575 μs |      1.9297 μs |     0.1058 μs |     8.556 μs |  0.7477 |  0.0153 |  12.39 KB |
| Render_MultipleDocuments_CacheEffectiveness | ShortRun   | Default         | 3              | 1           | 16           |   413.447 μs |     99.8488 μs |     5.4730 μs |   413.607 μs | 58.1055 | 27.3438 | 951.91 KB |
| Render_Small_ColdCache                      | ShortRun   | 1               | 3              | 1           | 1            | 1,122.574 μs |  1,016.3983 μs |    55.7122 μs | 1,109.561 μs |       - |       - | 176.76 KB |
| Render_Medium_ColdCache                     | ShortRun   | 1               | 3              | 1           | 1            | 4,257.125 μs |  1,711.1831 μs |    93.7957 μs | 4,258.212 μs |       - |       - | 516.13 KB |
| Render_Large_ColdCache                      | ShortRun   | 1               | 3              | 1           | 1            | 4,943.756 μs | 56,488.2387 μs | 3,096.3110 μs | 3,546.726 μs |       - |       - | 357.59 KB |
