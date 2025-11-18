using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using TextEdit.UI.App;
using TextEdit.Core.Documents;
using TextEdit.Core.Editing;
using TextEdit.Core.Preferences;
using TextEdit.Infrastructure.Ipc;
using TextEdit.Infrastructure.FileSystem;
using TextEdit.Infrastructure.Persistence;
using TextEdit.Infrastructure.Autosave;
using TextEdit.Infrastructure.Telemetry;
using TextEdit.Infrastructure.Themes;
using TextEdit.UI.Services;

namespace TextEdit.App.Tests;

public class AppStatePreferencesTests : IDisposable
{
    private readonly string _tempDir;

    public AppStatePreferencesTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "textedit-prefs-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    [Fact]
    public async Task LoadPreferencesAsync_RestoresAutoUpdateLastCheck_FromPersistence()
    {
        // Arrange: Persist a last-check timestamp in a session-bound persistence store
        var persistence = new PersistenceService(_tempDir);
        var expected = DateTimeOffset.UtcNow;
        await persistence.PersistAutoUpdateMetadataAsync(expected);

        var fs = new FileSystemService();
        var undo = new UndoRedoService();
        var docs = new DocumentService(fs, undo);
        var tabs = new TabService();

    var tmp = Path.Combine(_tempDir, "prefs");
    Directory.CreateDirectory(tmp);
    var ipc = new TestIpcBridge(tmp);
        var autosave = new AutosaveService(1000000); // avoid firing
        var perfLogger = new PerformanceLogger();
        var dialog = new DialogService();
    var prefsRepo = new PreferencesRepository(tmp);
        var themeDetection = new ThemeDetectionService();
        var themeManager = new ThemeManager();

        var app = new AppState(docs, tabs, ipc, persistence, autosave, perfLogger, prefsRepo, themeDetection, themeManager, null, dialog);

        // Act
        await app.LoadPreferencesAsync();

        // Assert
        Assert.NotNull(app.Preferences);
        Assert.NotNull(app.Preferences.Updates);
        Assert.True(app.Preferences.Updates.LastCheckTime.HasValue, "LastCheckTime should be populated from persistence");
        // Compare exact values using round-trip string to avoid minor formatting differences
        Assert.Equal(expected.ToString("O"), app.Preferences.Updates.LastCheckTime!.Value.ToString("O"));
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { /* ignore */ }
    }

    private class TestIpcBridge : IpcBridge
    {
    public TestIpcBridge(string prefsBaseDir) : base(new TextEdit.Infrastructure.Persistence.PreferencesRepository(prefsBaseDir)) { }
        public override Task<string?> ShowOpenFileDialogAsync() => Task.FromResult<string?>(null);
        public override Task<string?> ShowSaveFileDialogAsync() => Task.FromResult<string?>(null);
    }
}
