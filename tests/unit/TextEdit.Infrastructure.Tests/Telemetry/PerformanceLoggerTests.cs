using FluentAssertions;
using TextEdit.Infrastructure.Telemetry;
using Xunit;

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
        scope.Should().NotBeNull();
        scope.Should().BeAssignableTo<IDisposable>();
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
        stats.Should().NotBeNull();
        stats!.Count.Should().Be(1);
        stats.SuccessCount.Should().Be(1);
        stats.AverageDurationMs.Should().BeGreaterOrEqualTo(10);
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
        stats.Should().NotBeNull();
        stats!.Count.Should().Be(1);
        stats.SuccessCount.Should().Be(1);
        stats.AverageDurationMs.Should().Be(42);
        stats.MinDurationMs.Should().Be(42);
        stats.MaxDurationMs.Should().Be(42);
        stats.SuccessRate.Should().Be(1.0);
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
        stats.Should().NotBeNull();
        stats!.Count.Should().Be(1);
        stats.SuccessCount.Should().Be(0);
        stats.SuccessRate.Should().Be(0.0);
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
        stats.Should().NotBeNull();
        stats!.Count.Should().Be(4);
        stats.SuccessCount.Should().Be(3);
        stats.SuccessRate.Should().BeApproximately(0.75, 0.001);
        stats.MinDurationMs.Should().Be(10);
        stats.MaxDurationMs.Should().Be(40);
        stats.AverageDurationMs.Should().Be(25); // (10+20+30+40)/4
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
        statsA!.Count.Should().Be(2);
        statsA.AverageDurationMs.Should().Be(125);

        var statsB = logger.GetStats("OpB");
        statsB!.Count.Should().Be(1);
        statsB.AverageDurationMs.Should().Be(200);
    }

    [Fact]
    public void GetStats_ReturnsNullForUnknownOperation()
    {
        // Arrange
        var logger = new PerformanceLogger();

        // Act
        var stats = logger.GetStats("NonExistent");

        // Assert
        stats.Should().BeNull();
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
        stats!.Count.Should().Be(1);
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
        stats.Should().NotBeNull();
        stats!.Count.Should().Be(threadCount * operationsPerThread);
        stats.SuccessCount.Should().Be(threadCount * operationsPerThread / 2); // Half were success
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
        stats.Should().NotBeNull();
        stats!.Count.Should().Be(threadCount * operationsPerThread);
        stats.SuccessCount.Should().Be(threadCount * operationsPerThread); // All should succeed
    }

    [Fact]
    public void OperationStats_ReturnsZeroesWhenNoOperations()
    {
        // Arrange
        var stats = new OperationStats("EmptyOp");

        // Assert
        stats.Count.Should().Be(0);
        stats.SuccessCount.Should().Be(0);
        stats.SuccessRate.Should().Be(0);
        stats.MinDurationMs.Should().Be(0);
        stats.MaxDurationMs.Should().Be(0);
        stats.AverageDurationMs.Should().Be(0);
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
        stats.Count.Should().Be(4);
        stats.SuccessCount.Should().Be(3);
        stats.SuccessRate.Should().BeApproximately(0.75, 0.001);
        stats.MinDurationMs.Should().Be(50);
        stats.MaxDurationMs.Should().Be(200);
        stats.AverageDurationMs.Should().Be(125); // (100+50+150+200)/4
        stats.OperationName.Should().Be("TestOp");
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
        // Arrange
        var logger = new PerformanceLogger();

        // Act
        using (logger.BeginOperation("TimedOp"))
        {
            Thread.Sleep(50); // Sleep for a known duration
        }

        // Assert
        var stats = logger.GetStats("TimedOp");
        stats.Should().NotBeNull();
        stats!.AverageDurationMs.Should().BeGreaterOrEqualTo(50);
        stats.AverageDurationMs.Should().BeLessThan(100); // Reasonable upper bound
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
        stats.Should().NotBeNull();
        stats!.Count.Should().Be(1);
        stats.MinDurationMs.Should().Be(0);
        stats.MaxDurationMs.Should().Be(0);
        stats.AverageDurationMs.Should().Be(0);
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
        stats.Should().NotBeNull();
        stats!.MaxDurationMs.Should().Be(long.MaxValue / 2);
    }
}
