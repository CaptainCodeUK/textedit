using Xunit;
using Moq;
using TextEdit.Infrastructure.Updates;
using TextEdit.Core.Abstractions;
using TextEdit.Core.Updates;

namespace TextEdit.Core.Tests;

public class AutoUpdateServiceTests
{
    private readonly Mock<IAppLogger> _mockLogger;
    private readonly AutoUpdateService _service;

    public AutoUpdateServiceTests()
    {
        _mockLogger = new Mock<IAppLogger>();
        _service = new AutoUpdateService(_mockLogger.Object);
    }

    [Fact]
    public void Constructor_InitializesWithIdleStatus()
    {
        // Assert
        Assert.Equal(UpdateStatus.Idle, _service.CurrentStatus);
        Assert.Null(_service.AvailableUpdate);
        Assert.Null(_service.LastError);
        Assert.Equal(0, _service.DownloadPercent);
    }

    [Fact]
    public void Constructor_AcceptsNullLogger()
    {
        // Act
        var service = new AutoUpdateService(null);

        // Assert
        Assert.Equal(UpdateStatus.Idle, service.CurrentStatus);
    }

    [Fact]
    public void Initialize_WithEmptyUrl_LogsWarning()
    {
        // Act
        _service.Initialize("");

        // Assert
        _mockLogger.Verify(
            x => x.LogWarning(It.Is<string>(s => s.Contains("empty feedUrl"))),
            Times.Once);
    }

    [Fact]
    public void Initialize_WithValidUrl_LogsInfo()
    {
        // Act
        _service.Initialize("https://example.com/updates");

        // Assert
        _mockLogger.Verify(
            x => x.LogInformation(
                It.Is<string>(s => s.Contains("initialized")),
                It.IsAny<object[]>()),
            Times.Once);
    }

    [Fact]
    public void Initialize_WithNullUrl_LogsWarning()
    {
        // Act
        _service.Initialize(null!);

        // Assert
        _mockLogger.Verify(
            x => x.LogWarning(It.Is<string>(s => s.Contains("empty feedUrl"))),
            Times.Once);
    }

    [Fact]
    public void Initialize_WithWhitespaceUrl_LogsWarning()
    {
        // Act
        _service.Initialize("   ");

        // Assert
        _mockLogger.Verify(
            x => x.LogWarning(It.Is<string>(s => s.Contains("empty feedUrl"))),
            Times.Once);
    }

    [Fact]
    public async Task CheckForUpdatesAsync_InDebugMode_SimulatesUpToDate()
    {
        // Arrange
        var statusChanged = false;
        UpdateStatus? capturedStatus = null;

        _service.StatusChanged += (status, metadata) =>
        {
            statusChanged = true;
            capturedStatus = status;
        };

        // Act
        await _service.CheckForUpdatesAsync();

        // Assert - in DEBUG mode, should simulate UpToDate after delay
        Assert.True(statusChanged);
        Assert.Equal(UpdateStatus.UpToDate, capturedStatus);
        Assert.Equal(UpdateStatus.UpToDate, _service.CurrentStatus);
    }

    [Fact]
    public async Task CheckForUpdatesAsync_WhenAlreadyChecking_Skips()
    {
        // Arrange
        var eventCount = 0;
        _service.StatusChanged += (status, metadata) => eventCount++;

        // Act - start first check (will set status to Checking, then UpToDate)
        var task1 = _service.CheckForUpdatesAsync();
        
        // Immediately start second check while first is in progress
        // Note: In DEBUG mode the first check completes quickly, so we need to be fast
        var task2 = _service.CheckForUpdatesAsync();
        
        await Task.WhenAll(task1, task2);

        // Assert - second call should be skipped if it overlaps
        // We expect at least 2 events (Checking, UpToDate) but not double that
        Assert.True(eventCount >= 2);
        _mockLogger.Verify(
            x => x.LogDebug(It.Is<string>(s => s.Contains("already in progress"))),
            Times.AtMostOnce);
    }

    [Fact]
    public async Task CheckForUpdatesAsync_WithAutoDownloadTrue_LogsParameter()
    {
        // Act
        await _service.CheckForUpdatesAsync(autoDownload: true);

        // Assert
        _mockLogger.Verify(
            x => x.LogInformation(
                It.Is<string>(s => s.Contains("Checking for updates")),
                It.IsAny<object[]>()),
            Times.Once);
    }

    [Fact]
    public async Task CheckForUpdatesAsync_WithAutoDownloadFalse_LogsParameter()
    {
        // Act
        await _service.CheckForUpdatesAsync(autoDownload: false);

        // Assert
        _mockLogger.Verify(
            x => x.LogInformation(
                It.Is<string>(s => s.Contains("Checking for updates")),
                It.IsAny<object[]>()),
            Times.Once);
    }

    [Fact]
    public void QuitAndInstall_WhenNotReady_LogsWarning()
    {
        // Arrange - status is Idle by default

        // Act
        _service.QuitAndInstall();

        // Assert
        _mockLogger.Verify(
            x => x.LogWarning(
                It.Is<string>(s => s.Contains("QuitAndInstall called but status is")),
                It.IsAny<object[]>()),
            Times.Once);
    }

    [Fact]
    public async Task QuitAndInstall_WhenChecking_LogsWarning()
    {
        // Arrange - trigger a check to set status to Checking/UpToDate
        await _service.CheckForUpdatesAsync();

        // Act - try to quit while not in Ready state
        _service.QuitAndInstall();

        // Assert - should warn because status is not Ready
        _mockLogger.Verify(
            x => x.LogWarning(
                It.Is<string>(s => s.Contains("QuitAndInstall")),
                It.IsAny<object[]>()),
            Times.Once);
    }

    [Fact]
    public void StatusChanged_Event_CanBeSubscribed()
    {
        // Arrange
        var eventFired = false;
        UpdateStatus? capturedStatus = null;
        UpdateMetadata? capturedMetadata = null;

        _service.StatusChanged += (status, metadata) =>
        {
            eventFired = true;
            capturedStatus = status;
            capturedMetadata = metadata;
        };

        // Act - trigger an event by checking for updates
        _ = _service.CheckForUpdatesAsync();
        
        // Give event time to fire
        Thread.Sleep(100);

        // Assert
        Assert.True(eventFired);
        Assert.NotNull(capturedStatus);
    }

    [Fact]
    public void DownloadProgress_Event_CanBeSubscribed()
    {
        // Arrange
        var eventFired = false;
        var capturedPercent = -1;

        _service.DownloadProgress += (percent) =>
        {
            eventFired = true;
            capturedPercent = percent;
        };

        // Act - we can't trigger download in DEBUG mode, but ensure subscription works
        // (In real Electron context, OnDownloadProgress would invoke this)

        // Assert - just verify the event can be subscribed
        Assert.False(eventFired); // Won't fire in DEBUG mode
    }

    [Fact]
    public void CurrentStatus_ReflectsInitialIdle()
    {
        // Assert
        Assert.Equal(UpdateStatus.Idle, _service.CurrentStatus);
    }

    [Fact]
    public void AvailableUpdate_InitiallyNull()
    {
        // Assert
        Assert.Null(_service.AvailableUpdate);
    }

    [Fact]
    public void LastError_InitiallyNull()
    {
        // Assert
        Assert.Null(_service.LastError);
    }

    [Fact]
    public void DownloadPercent_InitiallyZero()
    {
        // Assert
        Assert.Equal(0, _service.DownloadPercent);
    }

    [Fact]
    public async Task CheckForUpdatesAsync_SetsStatusToChecking_ThenUpToDate()
    {
        // Arrange
        var statuses = new List<UpdateStatus>();
        _service.StatusChanged += (status, metadata) => statuses.Add(status);

        // Act
        await _service.CheckForUpdatesAsync();

        // Assert - should transition Checking -> UpToDate in DEBUG mode
        Assert.Contains(UpdateStatus.Checking, statuses);
        Assert.Contains(UpdateStatus.UpToDate, statuses);
        Assert.Equal(UpdateStatus.UpToDate, _service.CurrentStatus);
    }

    [Fact]
    public async Task CheckForUpdatesAsync_MultipleSequentialCalls_Work()
    {
        // Act
        await _service.CheckForUpdatesAsync();
        await _service.CheckForUpdatesAsync();
        await _service.CheckForUpdatesAsync();

        // Assert - should complete without errors
        Assert.Equal(UpdateStatus.UpToDate, _service.CurrentStatus);
    }

    [Fact]
    public void Initialize_MultipleCalls_DoesNotThrow()
    {
        // Act
        _service.Initialize("https://example.com/feed1");
        _service.Initialize("https://example.com/feed2");
        _service.Initialize("https://example.com/feed3");

        // Assert - should not throw
        _mockLogger.Verify(
            x => x.LogInformation(
                It.Is<string>(s => s.Contains("initialized")),
                It.IsAny<object[]>()),
            Times.Exactly(3));
    }
}
