using FluentAssertions;
using TextEdit.Infrastructure.Ipc;

namespace TextEdit.Core.Tests;

/// <summary>
/// Tests for IpcBridge dialog methods
/// Note: These tests verify method signatures and null handling.
/// Full dialog functionality requires Electron runtime and is tested via integration tests.
/// </summary>
public class IpcBridgeTests
{
    private readonly IpcBridge _bridge;

    public IpcBridgeTests()
    {
        _bridge = new IpcBridge();
    }

    [Fact]
    public async Task ShowOpenFileDialogAsync_ReturnsNullWhenElectronNotActive()
    {
        // Act: When Electron is not active (unit test environment)
        var result = await _bridge.ShowOpenFileDialogAsync();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ShowSaveFileDialogAsync_ReturnsNullWhenElectronNotActive()
    {
        // Act
        var result = await _bridge.ShowSaveFileDialogAsync();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ConfirmCloseDirtyAsync_ReturnsCancelWhenElectronNotActive()
    {
        // Act
        var result = await _bridge.ConfirmCloseDirtyAsync("test.txt");

        // Assert
        result.Should().Be(IpcBridge.CloseDecision.Cancel);
    }

    [Fact]
    public async Task ConfirmCloseDirtyAsync_WithNullName_DoesNotCrash()
    {
        // Act & Assert
        var result = await _bridge.ConfirmCloseDirtyAsync(null);
        result.Should().Be(IpcBridge.CloseDecision.Cancel);
    }

    [Fact]
    public async Task ConfirmCloseDirtyAsync_WithEmptyName_DoesNotCrash()
    {
        // Act & Assert
        var result = await _bridge.ConfirmCloseDirtyAsync("");
        result.Should().Be(IpcBridge.CloseDecision.Cancel);
    }

    [Fact]
    public async Task ConfirmReloadExternalAsync_ReturnsCancelWhenElectronNotActive()
    {
        // Act
        var result = await _bridge.ConfirmReloadExternalAsync("test.txt");

        // Assert
        result.Should().Be(IpcBridge.ExternalChangeDecision.Cancel);
    }

    [Fact]
    public async Task ConfirmReloadExternalAsync_WithNullName_DoesNotCrash()
    {
        // Act & Assert
        var result = await _bridge.ConfirmReloadExternalAsync(null);
        result.Should().Be(IpcBridge.ExternalChangeDecision.Cancel);
    }

    [Fact]
    public async Task ConfirmReloadExternalAsync_WithEmptyName_DoesNotCrash()
    {
        // Act & Assert
        var result = await _bridge.ConfirmReloadExternalAsync("");
        result.Should().Be(IpcBridge.ExternalChangeDecision.Cancel);
    }

    [Fact]
    public void CloseDecision_HasExpectedValues()
    {
        // Assert: Verify enum values exist
        var save = IpcBridge.CloseDecision.Save;
        var dontSave = IpcBridge.CloseDecision.DontSave;
        var cancel = IpcBridge.CloseDecision.Cancel;

        save.Should().NotBe(dontSave);
        save.Should().NotBe(cancel);
        dontSave.Should().NotBe(cancel);
    }

    [Fact]
    public void ExternalChangeDecision_HasExpectedValues()
    {
        // Assert: Verify enum values exist
        var reload = IpcBridge.ExternalChangeDecision.Reload;
        var keep = IpcBridge.ExternalChangeDecision.Keep;
        var cancel = IpcBridge.ExternalChangeDecision.Cancel;

        reload.Should().NotBe(keep);
        reload.Should().NotBe(cancel);
        keep.Should().NotBe(cancel);
    }
}
