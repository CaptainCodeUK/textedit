using FluentAssertions;
using TextEdit.Infrastructure.Ipc;

namespace TextEdit.IPC.Tests;

public class IpcBridgeTests
{
    [Fact]
    public async Task ShowOpenFileDialogAsync_WhenElectronNotActive_ReturnsNull()
    {
        // Arrange
        var bridge = new IpcBridge();

        // Act
        var result = await bridge.ShowOpenFileDialogAsync();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ShowSaveFileDialogAsync_WhenElectronNotActive_ReturnsNull()
    {
        // Arrange
        var bridge = new IpcBridge();

        // Act
        var result = await bridge.ShowSaveFileDialogAsync();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ConfirmCloseDirtyAsync_WhenElectronNotActive_ReturnsCancel()
    {
        // Arrange
        var bridge = new IpcBridge();

        // Act
        var result = await bridge.ConfirmCloseDirtyAsync("test.txt");

        // Assert
        result.Should().Be(IpcBridge.CloseDecision.Cancel);
    }

    [Fact]
    public async Task ConfirmCloseDirtyAsync_WithNullName_WhenElectronNotActive_ReturnsCancel()
    {
        // Arrange
        var bridge = new IpcBridge();

        // Act
        var result = await bridge.ConfirmCloseDirtyAsync(null);

        // Assert
        result.Should().Be(IpcBridge.CloseDecision.Cancel);
    }

    [Fact]
    public async Task ConfirmCloseDirtyAsync_WithEmptyName_WhenElectronNotActive_ReturnsCancel()
    {
        // Arrange
        var bridge = new IpcBridge();

        // Act
        var result = await bridge.ConfirmCloseDirtyAsync("");

        // Assert
        result.Should().Be(IpcBridge.CloseDecision.Cancel);
    }
}

public class CloseDecisionTests
{
    [Fact]
    public void CloseDecision_HasExpectedValues()
    {
        // Assert
        IpcBridge.CloseDecision.Save.Should().Be(IpcBridge.CloseDecision.Save);
        IpcBridge.CloseDecision.DontSave.Should().Be(IpcBridge.CloseDecision.DontSave);
        IpcBridge.CloseDecision.Cancel.Should().Be(IpcBridge.CloseDecision.Cancel);
    }

    [Theory]
    [InlineData(IpcBridge.CloseDecision.Save, 0)]
    [InlineData(IpcBridge.CloseDecision.DontSave, 1)]
    [InlineData(IpcBridge.CloseDecision.Cancel, 2)]
    public void CloseDecision_EnumValues_AreCorrect(IpcBridge.CloseDecision decision, int expectedValue)
    {
        // Assert
        ((int)decision).Should().Be(expectedValue);
    }
}