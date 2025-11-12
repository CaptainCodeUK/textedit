using Xunit;
using TextEdit.Infrastructure.Telemetry;

namespace TextEdit.Infrastructure.Tests.Telemetry;

public class PerformanceLoggerTests
{
    [Fact]
    public void BeginOperation_CreatesDisposableScope()
    {
        // Arrange
        var logger = new PerformanceLogger();

        // Act
        var scope = logger.BeginOperation("TestOperation");

        // Assert
    Assert.NotNull(scope);
    Assert.IsAssignableFrom<IDisposable>(scope);
    }

    [Fact]
    public void BeginOperation_LogsOperationOnDispose()
    {
        // Arrange
        var logger = new PerformanceLogger();

        // Act
        using (logger.BeginOperation("TestOperation"))
        {
            Thread.Sleep(10); // Ensure some measurable time passes
        }

        // Assert
        var stats = logger.GetStats("TestOperation");
    Assert.NotNull(stats);
    Assert.Equal(1, stats!.Count);
    Assert.Equal(1, stats.SuccessCount);
    Assert.True(stats.AverageDurationMs >= 10);
    }

    [Fact]
    public void LogOperation_RecordsSuccessfulOperation()
    {
        // Arrange
        var logger = new PerformanceLogger();

        // Act
        logger.LogOperation("TestOp", 42, success: true);

        // Assert
        var stats = logger.GetStats("TestOp");
    Assert.NotNull(stats);
    Assert.Equal(1, stats!.Count);
    Assert.Equal(1, stats.SuccessCount);
    Assert.Equal(42, stats.AverageDurationMs);
    Assert.Equal(42, stats.MinDurationMs);
    Assert.Equal(42, stats.MaxDurationMs);
    Assert.Equal(1.0, stats.SuccessRate);
    }

    [Fact]
    public void LogOperation_RecordsFailedOperation()
    {
        // Arrange
        var logger = new PerformanceLogger();

        // Act
        logger.LogOperation("TestOp", 100, success: false);

        // Assert
        var stats = logger.GetStats("TestOp");
    Assert.NotNull(stats);
    Assert.Equal(1, stats!.Count);
    Assert.Equal(0, stats.SuccessCount);
    Assert.Equal(0.0, stats.SuccessRate);
    }

    [Fact]
    public void LogOperation_AggregatesMultipleOperations()
    {
        // Arrange
        var logger = new PerformanceLogger();

        // Act
        logger.LogOperation("BatchOp", 10, success: true);
        logger.LogOperation("BatchOp", 20, success: true);
        logger.LogOperation("BatchOp", 30, success: false);
        logger.LogOperation("BatchOp", 40, success: true);

        // Assert
        var stats = logger.GetStats("BatchOp");
    Assert.NotNull(stats);
    Assert.Equal(4, stats!.Count);
    Assert.Equal(3, stats.SuccessCount);
    Assert.InRange(stats.SuccessRate, 0.749, 0.751);
    Assert.Equal(10, stats.MinDurationMs);
    Assert.Equal(40, stats.MaxDurationMs);
    Assert.Equal(25, stats.AverageDurationMs); // (10+20+30+40)/4
    }

    [Fact]
    public void LogOperation_TracksDistinctOperationsSeparately()
    {
        // Arrange
        var logger = new PerformanceLogger();

        // Act
        logger.LogOperation("OpA", 100);
        logger.LogOperation("OpB", 200);
        logger.LogOperation("OpA", 150);

        // Assert
        var statsA = logger.GetStats("OpA");
    Assert.Equal(2, statsA!.Count);
    Assert.Equal(125, statsA.AverageDurationMs);

        var statsB = logger.GetStats("OpB");
    Assert.Equal(1, statsB!.Count);
    Assert.Equal(200, statsB.AverageDurationMs);
    }

    [Fact]
    public void GetStats_ReturnsNullForUnknownOperation()
    {
        // Arrange
        var logger = new PerformanceLogger();

        // Act
        var stats = logger.GetStats("NonExistent");

        // Assert
    Assert.Null(stats);
    }

    [Fact]
    public void LogMetric_AcceptsValueWithoutUnit()
    {
        // Arrange
        var logger = new PerformanceLogger();

        // Act & Assert (should not throw)
        logger.LogMetric("FileSize", 1024);
    }

    [Fact]
    public void LogMetric_AcceptsValueWithUnit()
    {
        // Arrange
        var logger = new PerformanceLogger();

        // Act & Assert (should not throw)
        logger.LogMetric("FileSize", 1024, "bytes");
    }

    [Fact]
    public void OperationScope_CanBeDisposedMultipleTimes()
    {
        // Arrange
        var logger = new PerformanceLogger();
        var scope = logger.BeginOperation("SafeOp");

        // Act & Assert (should not throw)
        scope.Dispose();
        scope.Dispose();
        scope.Dispose();

        // Verify only one operation was logged
        var stats = logger.GetStats("SafeOp");
    Assert.Equal(1, stats!.Count);
    }

    [Fact]
    public void LogOperation_IsThreadSafe()
    {
        // Arrange
        var logger = new PerformanceLogger();
        const int threadCount = 10;
        const int operationsPerThread = 100;
        var threads = new Thread[threadCount];

        // Act
        for (int i = 0; i < threadCount; i++)
        {
            threads[i] = new Thread(() =>
            {
                for (int j = 0; j < operationsPerThread; j++)
                {
                    logger.LogOperation("ConcurrentOp", j, success: j % 2 == 0);
                }
            });
            threads[i].Start();
        }

        foreach (var thread in threads)
        {
            thread.Join();
        }

        // Assert
        var stats = logger.GetStats("ConcurrentOp");
    Assert.NotNull(stats);
    Assert.Equal(threadCount * operationsPerThread, stats!.Count);
    Assert.Equal(threadCount * operationsPerThread / 2, stats.SuccessCount); // Half were success
    }

    [Fact]
    public void BeginOperation_IsThreadSafe()
    {
        // Arrange
        var logger = new PerformanceLogger();
        const int threadCount = 10;
        const int operationsPerThread = 50;
        var threads = new Thread[threadCount];

        // Act
        for (int i = 0; i < threadCount; i++)
        {
            threads[i] = new Thread(() =>
            {
                for (int j = 0; j < operationsPerThread; j++)
                {
                    using (logger.BeginOperation("ConcurrentScope"))
                    {
                        Thread.Sleep(1); // Simulate work
                    }
                }
            });
            threads[i].Start();
        }

        foreach (var thread in threads)
        {
            thread.Join();
        }

        // Assert
        var stats = logger.GetStats("ConcurrentScope");
    Assert.NotNull(stats);
    Assert.Equal(threadCount * operationsPerThread, stats!.Count);
    Assert.Equal(threadCount * operationsPerThread, stats.SuccessCount); // All should succeed
    }

    [Fact]
    public void OperationStats_ReturnsZeroesWhenNoOperations()
    {
        // Arrange
        var stats = new OperationStats("EmptyOp");

        // Assert
    Assert.Equal(0, stats.Count);
    Assert.Equal(0, stats.SuccessCount);
    Assert.Equal(0, stats.SuccessRate);
    Assert.Equal(0, stats.MinDurationMs);
    Assert.Equal(0, stats.MaxDurationMs);
    Assert.Equal(0, stats.AverageDurationMs);
    }

    [Fact]
    public void OperationStats_CalculatesStatsCorrectly()
    {
        // Arrange
        var stats = new OperationStats("TestOp");

        // Act
        stats.RecordOperation(100, success: true);
        stats.RecordOperation(50, success: true);
        stats.RecordOperation(150, success: false);
        stats.RecordOperation(200, success: true);

        // Assert
    Assert.Equal(4, stats.Count);
    Assert.Equal(3, stats.SuccessCount);
    Assert.InRange(stats.SuccessRate, 0.749, 0.751);
    Assert.Equal(50, stats.MinDurationMs);
    Assert.Equal(200, stats.MaxDurationMs);
    Assert.Equal(125, stats.AverageDurationMs); // (100+50+150+200)/4
    Assert.Equal("TestOp", stats.OperationName);
    }

    [Fact]
    public void PrintAllStats_DoesNotThrowWithNoStats()
    {
        // Arrange
        var logger = new PerformanceLogger();

        // Act & Assert (should not throw)
        logger.PrintAllStats();
    }

    [Fact]
    public void PrintAllStats_DoesNotThrowWithMultipleStats()
    {
        // Arrange
        var logger = new PerformanceLogger();
        logger.LogOperation("Op1", 100);
        logger.LogOperation("Op2", 200);
        logger.LogOperation("Op3", 300);

        // Act & Assert (should not throw)
        logger.PrintAllStats();
    }

    [Fact]
    public void BeginOperation_MeasuresElapsedTime()
    {
        // Arrange: Use a mock stopwatch for deterministic timing
        var fakeStopwatch = new FakeStopwatch(123);
        var logger = new PerformanceLogger(() => fakeStopwatch);

        // Act
        using (logger.BeginOperation("TimedOp"))
        {
            // No sleep needed; fake stopwatch controls elapsed time
        }

        // Assert
        var stats = logger.GetStats("TimedOp");
        Assert.NotNull(stats);
        Assert.Equal(123, stats!.AverageDurationMs);
    }

    private class FakeStopwatch : IStopwatch
    {
        private readonly long _elapsedMs;
        public FakeStopwatch(long elapsedMs) { _elapsedMs = elapsedMs; }
        public long ElapsedMilliseconds => _elapsedMs;
        public void Stop() { }
    }

    [Fact]
    public void LogOperation_WithZeroDuration_IsHandledCorrectly()
    {
        // Arrange
        var logger = new PerformanceLogger();

        // Act
        logger.LogOperation("InstantOp", 0);

        // Assert
        var stats = logger.GetStats("InstantOp");
    Assert.NotNull(stats);
    Assert.Equal(1, stats!.Count);
    Assert.Equal(0, stats.MinDurationMs);
    Assert.Equal(0, stats.MaxDurationMs);
    Assert.Equal(0, stats.AverageDurationMs);
    }

    [Fact]
    public void LogOperation_WithLargeDuration_IsHandledCorrectly()
    {
        // Arrange
        var logger = new PerformanceLogger();

        // Act
        logger.LogOperation("SlowOp", long.MaxValue / 2);

        // Assert
        var stats = logger.GetStats("SlowOp");
    Assert.NotNull(stats);
    Assert.Equal(long.MaxValue / 2, stats!.MaxDurationMs);
    }
}
