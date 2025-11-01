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

public class SaveAsTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _helloPath;
    private readonly string _helloAPath;

    public SaveAsTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "textedit-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _helloPath = Path.Combine(_tempDir, "Hello6.txt");
        _helloAPath = Path.Combine(_tempDir, "Hello6a.txt");
    }

    [Fact]
    public async Task SaveAs_ToExistingFile_ReplacesContent_And_UpdatesFilename()
    {
        // Arrange: Hello6 with content A, Hello6a already exists with OLD
        await File.WriteAllTextAsync(_helloPath, "A");
        await File.WriteAllTextAsync(_helloAPath, "OLD");

        var fs = new FileSystemService();
        var undo = new UndoRedoService();
        var docs = new DocumentService(fs, undo);
        var tabs = new TabService();
        var ipc = new TestIpcBridge
        {
            OpenPath = _helloPath,
            SavePath = _helloAPath
        };
        var persistence = new PersistenceService();
        var autosave = new AutosaveService(1000000); // very long interval; avoid firing in test
        var perfLogger = new PerformanceLogger();
        var dialog = new DialogService();
        var prefsRepo = new PreferencesRepository();
        var themeDetection = new ThemeDetectionService();
        var themeManager = new ThemeManager();

        var app = new AppState(docs, tabs, ipc, persistence, autosave, perfLogger, prefsRepo, themeDetection, themeManager, null, dialog);

        // Open original
        var opened = await app.OpenAsync();
    Assert.NotNull(opened);
    Assert.Equal(_helloPath, app.ActiveDocument!.FilePath);

        // Act: Save As to existing file (OS Replace is trusted)
        var result = await app.SaveAsActiveAsync();

        // Assert: content replaced and filename updated
    Assert.True(result);
    Assert.Equal("A", await File.ReadAllTextAsync(_helloAPath));
    Assert.Equal(_helloAPath, app.ActiveDocument!.FilePath);
    Assert.Equal(Path.GetFileName(_helloAPath), app.ActiveDocument!.Name);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { /* ignore */ }
    }

    private class TestIpcBridge : IpcBridge
    {
        public string? OpenPath { get; set; }
        public string? SavePath { get; set; }

        public TestIpcBridge() : base(new TextEdit.Infrastructure.Persistence.PreferencesRepository()) { }

        public override Task<string?> ShowOpenFileDialogAsync() => Task.FromResult(OpenPath);
        public override Task<string?> ShowSaveFileDialogAsync() => Task.FromResult(SavePath);
    }
}
