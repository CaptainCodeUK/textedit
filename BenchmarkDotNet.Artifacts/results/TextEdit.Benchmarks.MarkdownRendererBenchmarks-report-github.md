```

BenchmarkDotNet v0.15.4, Linux CachyOS
AMD Ryzen 5 5600X 3.73GHz, 1 CPU, 12 logical and 6 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 8.0.20 (8.0.20, 8.0.2025.41914), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 8.0.20 (8.0.20, 8.0.2025.41914), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 8.0.20 (8.0.20, 8.0.2025.41914), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                         | Job        | IterationCount | LaunchCount | Mean             | Error             | StdDev           | Gen0      | Gen1      | Gen2     | Allocated  |
|------------------------------- |----------- |--------------- |------------ |-----------------:|------------------:|-----------------:|----------:|----------:|---------:|-----------:|
| Render_PlainText_1KB           | Job-YFEFPZ | 10             | Default     |      5,498.67 ns |        643.308 ns |       382.822 ns |    0.1678 |         - |        - |     2840 B |
| Render_SimpleMarkdown_2KB      | Job-YFEFPZ | 10             | Default     |     63,343.62 ns |      1,048.678 ns |       624.051 ns |    3.7842 |    0.4883 |        - |    64329 B |
| Render_MediumMarkdown_10KB     | Job-YFEFPZ | 10             | Default     |    331,249.63 ns |      5,523.924 ns |     3,287.199 ns |   19.5313 |    8.3008 |        - |   327148 B |
| Render_ComplexMarkdown_50KB    | Job-YFEFPZ | 10             | Default     |  2,270,335.02 ns |     44,738.093 ns |    29,591.478 ns |  187.5000 |  164.0625 |  54.6875 |  2427037 B |
| Render_LargeDocument_100KB     | Job-YFEFPZ | 10             | Default     |  4,950,380.19 ns |    107,110.308 ns |    70,846.837 ns |  367.1875 |  359.3750 | 101.5625 |  4977036 B |
| Render_VeryLargeDocument_500KB | Job-YFEFPZ | 10             | Default     | 43,735,979.91 ns |  1,154,195.595 ns |   686,843.285 ns | 1750.0000 | 1666.6667 | 416.6667 | 25363332 B |
| CheckIsLargeContent_Small      | Job-YFEFPZ | 10             | Default     |         41.08 ns |          0.783 ns |         0.466 ns |         - |         - |        - |          - |
| CheckIsLargeContent_Large      | Job-YFEFPZ | 10             | Default     |      1,959.60 ns |         40.793 ns |        26.982 ns |         - |         - |        - |          - |
| Render_PlainText_1KB           | ShortRun   | 3              | 1           |      5,169.28 ns |        899.422 ns |        49.300 ns |    0.1678 |         - |        - |     2840 B |
| Render_SimpleMarkdown_2KB      | ShortRun   | 3              | 1           |     64,828.66 ns |      5,455.798 ns |       299.051 ns |    3.7842 |    0.4883 |        - |    64329 B |
| Render_MediumMarkdown_10KB     | ShortRun   | 3              | 1           |    328,223.80 ns |     39,589.357 ns |     2,170.026 ns |   19.5313 |    8.3008 |        - |   327148 B |
| Render_ComplexMarkdown_50KB    | ShortRun   | 3              | 1           |  2,301,675.24 ns |    380,639.195 ns |    20,864.119 ns |  187.5000 |  164.0625 |  54.6875 |  2427029 B |
| Render_LargeDocument_100KB     | ShortRun   | 3              | 1           |  5,022,215.91 ns |  3,398,694.743 ns |   186,293.928 ns |  367.1875 |  359.3750 | 101.5625 |  4977002 B |
| Render_VeryLargeDocument_500KB | ShortRun   | 3              | 1           | 49,869,755.75 ns | 72,027,865.675 ns | 3,948,090.381 ns | 1750.0000 | 1666.6667 | 416.6667 | 25363349 B |
| CheckIsLargeContent_Small      | ShortRun   | 3              | 1           |         42.17 ns |         28.114 ns |         1.541 ns |         - |         - |        - |          - |
| CheckIsLargeContent_Large      | ShortRun   | 3              | 1           |      1,908.81 ns |         82.786 ns |         4.538 ns |         - |         - |        - |          - |
