using Xunit;
using TextEdit.Infrastructure.Ipc;
using TextEdit.Core.Preferences;
using TextEdit.Infrastructure.Persistence;

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
    var tmp = Path.Combine(Path.GetTempPath(), "textedit-ipc-prefs-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(tmp);
    var prefsRepo = new PreferencesRepository(tmp);
        _bridge = new IpcBridge(prefsRepo);
    }

    [Fact]
    public async Task ShowOpenFileDialogAsync_ReturnsNullWhenElectronNotActive()
    {
        // Act: When Electron is not active (unit test environment)
        var result = await _bridge.ShowOpenFileDialogAsync();

        // Assert
    Assert.Null(result);
    }

    [Fact]
    public async Task ShowSaveFileDialogAsync_ReturnsNullWhenElectronNotActive()
    {
        // Act
        var result = await _bridge.ShowSaveFileDialogAsync();

        // Assert
    Assert.Null(result);
    }

    [Fact]
    public async Task ConfirmCloseDirtyAsync_ReturnsCancelWhenElectronNotActive()
    {
        // Act
        var result = await _bridge.ConfirmCloseDirtyAsync("test.txt");

        // Assert
    Assert.Equal(IpcBridge.CloseDecision.Cancel, result);
    }

    [Fact]
    public async Task ConfirmCloseDirtyAsync_WithNullName_DoesNotCrash()
    {
        // Act & Assert
    var result = await _bridge.ConfirmCloseDirtyAsync(null);
    Assert.Equal(IpcBridge.CloseDecision.Cancel, result);
    }

    [Fact]
    public async Task ConfirmCloseDirtyAsync_WithEmptyName_DoesNotCrash()
    {
        // Act & Assert
    var result = await _bridge.ConfirmCloseDirtyAsync("");
    Assert.Equal(IpcBridge.CloseDecision.Cancel, result);
    }

    [Fact]
    public async Task ConfirmReloadExternalAsync_ReturnsCancelWhenElectronNotActive()
    {
        // Act
        var result = await _bridge.ConfirmReloadExternalAsync("test.txt");

        // Assert
    Assert.Equal(IpcBridge.ExternalChangeDecision.Cancel, result);
    }

    [Fact]
    public async Task ConfirmReloadExternalAsync_WithNullName_DoesNotCrash()
    {
        // Act & Assert
    var result = await _bridge.ConfirmReloadExternalAsync(null);
    Assert.Equal(IpcBridge.ExternalChangeDecision.Cancel, result);
    }

    [Fact]
    public async Task ConfirmReloadExternalAsync_WithEmptyName_DoesNotCrash()
    {
        // Act & Assert
    var result = await _bridge.ConfirmReloadExternalAsync("");
    Assert.Equal(IpcBridge.ExternalChangeDecision.Cancel, result);
    }

    [Fact]
    public void CloseDecision_HasExpectedValues()
    {
        // Assert: Verify enum values exist
        var save = IpcBridge.CloseDecision.Save;
        var dontSave = IpcBridge.CloseDecision.DontSave;
        var cancel = IpcBridge.CloseDecision.Cancel;

    Assert.NotEqual(dontSave, save);
    Assert.NotEqual(cancel, save);
    Assert.NotEqual(cancel, dontSave);
    }

    [Fact]
    public void ExternalChangeDecision_HasExpectedValues()
    {
        // Assert: Verify enum values exist
        var reload = IpcBridge.ExternalChangeDecision.Reload;
        var keep = IpcBridge.ExternalChangeDecision.Keep;
        var cancel = IpcBridge.ExternalChangeDecision.Cancel;

    Assert.NotEqual(keep, reload);
    Assert.NotEqual(cancel, reload);
    Assert.NotEqual(cancel, keep);
    }
}
