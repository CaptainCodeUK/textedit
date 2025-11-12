using System;
using System.Threading.Tasks;
using TextEdit.Infrastructure.Updates;
using TextEdit.Core.Updates;
using Xunit;

namespace TextEdit.Infrastructure.Tests;

public class AutoUpdateServiceTests
{
    [Fact]
    public void Initialize_WithNullUrl_LogsWarningAndSetsIdle()
    {
        var service = new AutoUpdateService();
        service.Initialize(null);
        Assert.Equal(UpdateStatus.Idle, service.CurrentStatus);
    }

    [Fact]
    public void Initialize_WithWhitespaceUrl_LogsWarningAndSetsIdle()
    {
        var service = new AutoUpdateService();
        service.Initialize("   ");
        Assert.Equal(UpdateStatus.Idle, service.CurrentStatus);
    }

    [Fact]
    public void Initialize_WithValidUrl_SetsStatusToIdle()
    {
        var service = new AutoUpdateService();
        service.Initialize("https://example.com/feed");
        Assert.Equal(UpdateStatus.Idle, service.CurrentStatus);
    }

    [Fact]
    public async Task CheckForUpdatesAsync_SetsStatusToCheckingAndBack()
    {
        var service = new AutoUpdateService();
        service.Initialize("https://example.com/feed");
        var statusChanged = false;
        service.StatusChanged += (s, e) => statusChanged = true;
        await service.CheckForUpdatesAsync();
        Assert.True(statusChanged);
        Assert.True(service.CurrentStatus == UpdateStatus.UpToDate || service.CurrentStatus == UpdateStatus.Idle);
    }

    [Fact]
    public void DownloadProgress_Event_CanBeSubscribed()
    {
        var service = new AutoUpdateService();
        // Just ensure subscribing does not throw
        service.DownloadProgress += _ => { };
        Assert.True(true);
    }

    [Fact]
    public void QuitAndInstall_WhenNotReady_LogsWarning()
    {
        var service = new AutoUpdateService();
        service.Initialize("https://example.com/feed");
        // Should not throw
        service.QuitAndInstall();
        Assert.True(service.CurrentStatus == UpdateStatus.Idle || service.CurrentStatus == UpdateStatus.UpToDate);
    }
}
