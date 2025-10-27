using FluentAssertions;
using TextEdit.Infrastructure.Autosave;

namespace TextEdit.Core.Tests;

public class AutosaveServiceTests : IDisposable
{
    private readonly AutosaveService _service;

    public AutosaveServiceTests()
    {
        _service = new AutosaveService(100); // 100ms for faster tests
    }

    [Fact]
    public void Constructor_InitializesWithDefaultValues()
    {
        // Act
        var service = new AutosaveService();

        // Assert
        service.LastAutosave.Should().Be(DateTime.MinValue);
    }

    [Fact]
    public void LastAutosave_InitiallyMinValue()
    {
        // Assert
        _service.LastAutosave.Should().Be(DateTime.MinValue);
    }

    [Fact]
    public async Task Start_TriggersAutosaveEvent()
    {
        // Arrange
        var triggered = false;
        var tcs = new TaskCompletionSource<bool>();
        
        _service.AutosaveRequested += async () =>
        {
            triggered = true;
            tcs.SetResult(true);
            await Task.CompletedTask;
        };

        // Act
        _service.Start();
        var completed = await Task.WhenAny(tcs.Task, Task.Delay(500));

        // Assert
        triggered.Should().BeTrue();
        completed.Should().Be(tcs.Task);
    }

    [Fact]
    public async Task AutosaveRequested_UpdatesLastAutosave()
    {
        // Arrange
        var tcs = new TaskCompletionSource<bool>();
        var initialValue = _service.LastAutosave;
        
        _service.AutosaveRequested += async () =>
        {
            tcs.SetResult(true);
            await Task.CompletedTask;
        };

        // Act
        _service.Start();
        await Task.WhenAny(tcs.Task, Task.Delay(500));

        // Assert
        _service.LastAutosave.Should().BeAfter(initialValue);
        _service.LastAutosave.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task Stop_PreventsFurtherAutosaveTriggers()
    {
        // Arrange
        var triggerCount = 0;
        _service.AutosaveRequested += async () =>
        {
            triggerCount++;
            await Task.CompletedTask;
        };

        _service.Start();
        await Task.Delay(150); // Wait for first trigger

        // Act
        _service.Stop();
        var countAfterStop = triggerCount;
        await Task.Delay(200); // Wait to ensure no more triggers

        // Assert
        triggerCount.Should().Be(countAfterStop);
    }

    [Fact]
    public async Task AutosaveRequested_WhenNull_DoesNotThrow()
    {
        // Act
        _service.Start();
        await Task.Delay(150);

        // Assert - if we get here without exception, test passes
        _service.LastAutosave.Should().Be(DateTime.MinValue);
    }

    [Fact]
    public async Task AutosaveRequested_WhenExceptionThrown_ContinuesOperation()
    {
        // Arrange
        var triggerCount = 0;
        var tcs = new TaskCompletionSource<bool>();
        
        _service.AutosaveRequested += async () =>
        {
            triggerCount++;
            if (triggerCount == 1)
            {
                throw new InvalidOperationException("Test exception");
            }
            tcs.SetResult(true);
            await Task.CompletedTask;
        };

        // Act
        _service.Start();
        await Task.WhenAny(tcs.Task, Task.Delay(500));

        // Assert
        triggerCount.Should().BeGreaterThan(1);
    }

    [Fact]
    public async Task MultipleSubscribers_AllGetNotified()
    {
        // Arrange
        var triggered1 = false;
        var triggered2 = false;
        var tcs = new TaskCompletionSource<bool>();

        _service.AutosaveRequested += async () =>
        {
            triggered1 = true;
            await Task.CompletedTask;
        };

        _service.AutosaveRequested += async () =>
        {
            triggered2 = true;
            tcs.SetResult(true);
            await Task.CompletedTask;
        };

        // Act
        _service.Start();
        await Task.WhenAny(tcs.Task, Task.Delay(500));

        // Assert
        triggered1.Should().BeTrue();
        triggered2.Should().BeTrue();
    }

    public void Dispose()
    {
        _service.Stop();
        _service.Dispose();
    }
}
