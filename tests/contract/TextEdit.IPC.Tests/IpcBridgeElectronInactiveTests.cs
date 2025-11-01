using Xunit;
using TextEdit.Infrastructure.Ipc;
using TextEdit.Infrastructure.Persistence;

namespace TextEdit.IPC.Tests;

public class IpcBridgeTests
{
    private readonly IpcBridge _bridge;

    public IpcBridgeTests()
    {
        var prefsRepo = new PreferencesRepository();
        _bridge = new IpcBridge(prefsRepo);
    }

    [Fact]
    public async Task ShowOpenFileDialogAsync_WhenElectronNotActive_ReturnsNull()
    {
        // Act
        var result = await _bridge.ShowOpenFileDialogAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ShowSaveFileDialogAsync_WhenElectronNotActive_ReturnsNull()
    {
        // Act
        var result = await _bridge.ShowSaveFileDialogAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ConfirmCloseDirtyAsync_WhenElectronNotActive_ReturnsCancel()
    {
        // Act
        var result = await _bridge.ConfirmCloseDirtyAsync("test.txt");

        // Assert
        Assert.Equal(IpcBridge.CloseDecision.Cancel, result);
    }

    [Fact]
    public async Task ConfirmCloseDirtyAsync_WithNullName_WhenElectronNotActive_ReturnsCancel()
    {
        // Act
        var result = await _bridge.ConfirmCloseDirtyAsync(null);

        // Assert
        Assert.Equal(IpcBridge.CloseDecision.Cancel, result);
    }

    [Fact]
    public async Task ConfirmCloseDirtyAsync_WithEmptyName_WhenElectronNotActive_ReturnsCancel()
    {
        // Act
        var result = await _bridge.ConfirmCloseDirtyAsync("");

        // Assert
        Assert.Equal(IpcBridge.CloseDecision.Cancel, result);
    }
}

public class CloseDecisionTests
{
    [Fact]
    public void CloseDecision_HasExpectedValues()
    {
        // Assert
        Assert.Equal(IpcBridge.CloseDecision.Save, IpcBridge.CloseDecision.Save);
        Assert.Equal(IpcBridge.CloseDecision.DontSave, IpcBridge.CloseDecision.DontSave);
        Assert.Equal(IpcBridge.CloseDecision.Cancel, IpcBridge.CloseDecision.Cancel);
    }

    [Theory]
    [InlineData(IpcBridge.CloseDecision.Save, 0)]
    [InlineData(IpcBridge.CloseDecision.DontSave, 1)]
    [InlineData(IpcBridge.CloseDecision.Cancel, 2)]
    public void CloseDecision_EnumValues_AreCorrect(IpcBridge.CloseDecision decision, int expectedValue)
    {
        // Assert
        Assert.Equal(expectedValue, (int)decision);
    }
}
