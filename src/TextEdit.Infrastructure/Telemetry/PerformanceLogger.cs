namespace TextEdit.Infrastructure.Telemetry;

using System;
using System.Collections.Concurrent;
using System.Diagnostics;

/// <summary>
/// Structured performance logging for telemetry and monitoring.
/// Tracks operation durations, counts, and provides aggregate statistics.
/// </summary>
public class PerformanceLogger
{
    private readonly ConcurrentDictionary<string, OperationStats> _stats = new();
    private readonly object _consoleLock = new();

    public IDisposable BeginOperation(string operationName)
    {
        return new OperationScope(this, operationName);
    }

    public void LogOperation(string operationName, long durationMs, bool success = true)
    {
        var stats = _stats.GetOrAdd(operationName, _ => new OperationStats(operationName));
        stats.RecordOperation(durationMs, success);

        // Log to console in structured format
        lock (_consoleLock)
        {
            Console.WriteLine($"[PERF] {operationName}: {durationMs}ms (success={success})");
        }
    }

    public void LogMetric(string metricName, long value, string? unit = null)
    {
        lock (_consoleLock)
        {
            Console.WriteLine($"[METRIC] {metricName}: {value}{(unit != null ? $" {unit}" : "")}");
        }
    }

    public OperationStats? GetStats(string operationName)
    {
        return _stats.TryGetValue(operationName, out var stats) ? stats : null;
    }

    public void PrintAllStats()
    {
        lock (_consoleLock)
        {
            Console.WriteLine("\n=== Performance Statistics ===");
            foreach (var kvp in _stats.OrderBy(x => x.Key))
            {
                var stats = kvp.Value;
                Console.WriteLine($"{stats.OperationName}:");
                Console.WriteLine($"  Count: {stats.Count}");
                Console.WriteLine($"  Success: {stats.SuccessCount}/{stats.Count} ({stats.SuccessRate:P1})");
                Console.WriteLine($"  Duration: avg={stats.AverageDurationMs:F1}ms, min={stats.MinDurationMs}ms, max={stats.MaxDurationMs}ms");
            }
            Console.WriteLine("==============================\n");
        }
    }

    private class OperationScope : IDisposable
    {
        private readonly PerformanceLogger _logger;
        private readonly string _operationName;
        private readonly Stopwatch _stopwatch;
        private bool _disposed;

        public OperationScope(PerformanceLogger logger, string operationName)
        {
            _logger = logger;
            _operationName = operationName;
            _stopwatch = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _stopwatch.Stop();
            _logger.LogOperation(_operationName, _stopwatch.ElapsedMilliseconds);
        }
    }
}

/// <summary>
/// Aggregate statistics for a specific operation.
/// </summary>
public class OperationStats
{
    private readonly object _lock = new();
    private long _totalDurationMs;
    private long _count;
    private long _successCount;
    private long _minDurationMs = long.MaxValue;
    private long _maxDurationMs;

    public string OperationName { get; }
    public long Count => _count;
    public long SuccessCount => _successCount;
    public double SuccessRate => _count > 0 ? (double)_successCount / _count : 0;
    public long MinDurationMs => _count > 0 ? _minDurationMs : 0;
    public long MaxDurationMs => _maxDurationMs;
    public double AverageDurationMs => _count > 0 ? (double)_totalDurationMs / _count : 0;

    public OperationStats(string operationName)
    {
        OperationName = operationName;
    }

    public void RecordOperation(long durationMs, bool success)
    {
        lock (_lock)
        {
            _count++;
            if (success) _successCount++;
            _totalDurationMs += durationMs;
            if (durationMs < _minDurationMs) _minDurationMs = durationMs;
            if (durationMs > _maxDurationMs) _maxDurationMs = durationMs;
        }
    }
}
