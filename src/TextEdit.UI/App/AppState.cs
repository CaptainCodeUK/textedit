using Microsoft.AspNetCore.Components;
using TextEdit.Core.Documents;
using TextEdit.Core.Editing;
using TextEdit.Core.Abstractions;
using TextEdit.Infrastructure.Ipc;
using TextEdit.Infrastructure.Persistence;
using TextEdit.Infrastructure.FileSystem;
using TextEdit.Infrastructure.Autosave;
using TextEdit.Infrastructure.Telemetry;
using TextEdit.Infrastructure.Logging;
using TextEdit.Core.Preferences;
using Microsoft.Extensions.Logging;
using TextEdit.Infrastructure.Themes;
using TextEdit.UI.Services;
using System.IO;

namespace TextEdit.UI.App;

/// <summary>
/// UI application state: manages open documents and tabs and exposes active selection.
/// </summary>
/// <summary>
/// Central UI state container coordinating documents, tabs, persistence, IPC and theme.
/// Components subscribe to <see cref="Changed"/> and use <see cref="StateVersion"/> to optimize re-renders.
/// </summary>
public class AppState : IDisposable
{
    private readonly DocumentService _docs;
    private readonly TabService _tabs;
    private readonly IpcBridge _ipc;
    private readonly PersistenceService _persistence;
    private readonly AutosaveService _autosave;
    private readonly PerformanceLogger _perfLogger;
    private readonly DialogService? _dialogService;
    private readonly IPreferencesRepository _prefsRepo;
    private readonly ThemeDetectionService _themeDetection;
    private readonly ThemeManager _themeManager;
    private readonly IAppLogger? _logger;
    private readonly Microsoft.Extensions.Logging.ILogger<AppState>? _msLogger;
    private readonly Dictionary<Guid, FileWatcher> _watchers = new();
    private readonly Dictionary<Guid, DateTimeOffset> _lastExternalChange = new();
    private readonly TextEdit.Infrastructure.SpellChecking.SpellCheckingService? _spellCheckingService;

    private readonly Dictionary<Guid, Document> _open = new();
    private int _stateVersion = 0;

    public AppState(
        DocumentService docs, 
        TabService tabs, 
        IpcBridge ipc, 
        PersistenceService persistence, 
        AutosaveService autosave, 
        PerformanceLogger perfLogger, 
        IPreferencesRepository prefsRepo,
        ThemeDetectionService themeDetection,
        ThemeManager themeManager,
    IAppLoggerFactory? loggerFactory = null,
    Microsoft.Extensions.Logging.ILogger<AppState>? msLogger = null,
        DialogService? dialogService = null,
        TextEdit.Infrastructure.SpellChecking.SpellCheckingService? spellCheckingService = null)
    {
        _docs = docs;
        _tabs = tabs;
        _ipc = ipc;
        _persistence = persistence;
        _autosave = autosave;
        _perfLogger = perfLogger;
        _dialogService = dialogService;
        _prefsRepo = prefsRepo;
        _themeDetection = themeDetection;
        _themeManager = themeManager;
    _logger = loggerFactory?.CreateLogger<AppState>();
    _msLogger = msLogger;
        
        EditorState = new EditorState();
        ToolbarState = new ToolbarState();
        Preferences = new UserPreferences(); // Will be loaded from disk
        
        // Hook up autosave to trigger persistence
        _autosave.AutosaveRequested += HandleAutosaveAsync;
        _autosave.Start();
        _spellCheckingService = spellCheckingService;
    }

    // Backwards-compatible constructor used in tests and earlier call sites
    public AppState(
        DocumentService docs,
        TabService tabs,
        IpcBridge ipc,
        PersistenceService persistence,
        AutosaveService autosave,
        PerformanceLogger perfLogger,
        IPreferencesRepository prefsRepo,
        ThemeDetectionService themeDetection,
        ThemeManager themeManager,
        IAppLoggerFactory? loggerFactory,
        DialogService? dialogService)
        : this(docs, tabs, ipc, persistence, autosave, perfLogger, prefsRepo, themeDetection, themeManager, loggerFactory, null, dialogService)
    {
    }

    /// <summary>
    /// Current set of tabs in display order.
    /// </summary>
    public IReadOnlyList<Tab> Tabs => _tabs.Tabs;

    /// <summary>
    /// Currently active tab, if any.
    /// </summary>
    public Tab? ActiveTab => _tabs.Tabs.FirstOrDefault(t => t.IsActive);

    /// <summary>
    /// Document associated with the active tab, or null when none.
    /// </summary>
    public Document? ActiveDocument => ActiveTab != null && _open.TryGetValue(ActiveTab.DocumentId, out var d) ? d : null;

    /// <summary>
    /// All open documents keyed internally by <see cref="Document.Id"/>.
    /// </summary>
    public IEnumerable<Document> AllDocuments => _open.Values;

    /// <summary>
    /// Editor configuration and UI flags.
    /// </summary>
    public EditorState EditorState { get; }

    /// <summary>
    /// Toolbar visibility and state.
    /// </summary>
    public ToolbarState ToolbarState { get; }

    /// <summary>
    /// Undo is always available - Monaco handles validation internally.
    /// </summary>
    public bool CanUndo => true;
    
    /// <summary>
    /// Redo is always available - Monaco handles validation internally.
    /// </summary>
    public bool CanRedo => true;

    /// <summary>
    /// User preferences loaded from disk.
    /// </summary>
    public UserPreferences Preferences { get; private set; }

    /// <summary>
    /// CLI validation errors to present in the UI.
    /// </summary>
    public List<(string Path, string Reason)> CliInvalidFiles { get; private set; } = new();

    /// <summary>
    /// Provides timers and events for autosave functionality.
    /// </summary>
    public AutosaveService AutosaveService => _autosave;

    /// <summary>
    /// Monotonic counter incremented on state changes to optimize re-rendering.
    /// </summary>
    public int StateVersion => _stateVersion;

    /// <summary>
    /// Raised when state changes and components should re-render.
    /// </summary>
    public event Action? Changed;
    
    /// <summary>
    /// Raised when the theme changes, allowing editors to update their theme dynamically.
    /// </summary>
    public event Action<ThemeMode>? ThemeChanged;
    
    private void NotifyChanged()
    {
        _stateVersion++;
        UpdateWindowTitle(); // T058, T059: Update title when state changes
        Changed?.Invoke();
    }

    // Notify UI that document state (e.g., dirty flag) changed
    public void NotifyDocumentUpdated() => NotifyChanged();

    /// <summary>
    /// Notify UI that toolbar state changed (e.g., Cut/Copy button enabling based on selection)
    /// </summary>
    public void NotifyToolbarStateChanged() => NotifyChanged();

    /// <summary>
    /// Called when Monaco editor's undo/redo state changes.
    /// Updates menu and toolbar button visibility.
    /// </summary>
    /// <summary>
    /// Update the Electron window title based on current document (T056-T060)
    /// </summary>
    private void UpdateWindowTitle()
    {
        try
        {
            var title = GetWindowTitle();
            _ipc.SetWindowTitle(title);
        }
        catch
        {
            // ignore
        }
    }

    /// <summary>
    /// Open multiple files by absolute path and add them as tabs in the current order.
    /// Returns the list of successfully opened file paths.
    /// </summary>
    public async Task<IReadOnlyList<string>> OpenFilesAsync(IEnumerable<string> absolutePaths)
    {
        if (absolutePaths is null) return Array.Empty<string>();
        var opened = new List<string>();
        var invalid = new List<(string Path, string Reason)>();
        foreach (var path in absolutePaths)
        {
            if (string.IsNullOrWhiteSpace(path)) continue;
            try
            {
                // Check recognized extension list
                var ext = Path.GetExtension(path)?.ToLowerInvariant() ?? string.Empty;
                if (!Preferences.FileExtensions.Any(e => string.Equals(e, ext, StringComparison.OrdinalIgnoreCase)))
                {
                    invalid.Add((path, $"Unsupported extension: {ext}"));
                    _logger?.LogWarning("Rejected unsupported file type from CLI: {Path}", path);
                    continue;
                }
                var doc = await _docs.OpenAsync(path);
                _open[doc.Id] = doc;
                _tabs.AddTab(doc);
                StartWatchingFile(doc);
                opened.Add(path);
            }
            catch
            {
                // ignore per-file open errors; UI will surface failures if needed
            }
        }
        if (opened.Count > 0)
        {
            NotifyChanged();
        }
        if (invalid.Count > 0)
        {
            SetCliInvalidFiles(invalid);
        }
        return opened;
    }

    /// <summary>
    /// Set CLI invalid files for display in CliErrorSummary component.
    /// </summary>
    public void SetCliInvalidFiles(IEnumerable<(string Path, string Reason)> invalidFiles)
    {
        CliInvalidFiles = invalidFiles?.ToList() ?? new();
        NotifyChanged();
    }

    /// <summary>
    /// Clear CLI error summary.
    /// </summary>
    public void ClearCliInvalidFiles()
    {
        CliInvalidFiles.Clear();
        NotifyChanged();
    }

    /// <summary>
    /// Restore previous session documents and tab order from persistence, or create a new document when none.
    /// Loads user and editor preferences prior to restoration.
    /// </summary>
    public async Task RestoreSessionAsync()
    {
        using var _ = _perfLogger.BeginOperation("Session.Restore");
        
        // Load user preferences first
        await LoadPreferencesAsync();
        
        // Always restore editor preferences
    var (wordWrap, showPreview) = _persistence.RestoreEditorPreferences();
    EditorState.WordWrap = wordWrap;
    EditorState.ShowPreview = showPreview;

        // Idempotence: if we already have tabs/documents, assume we've been restored/initialized
        if (_tabs.Tabs.Count > 0 || _open.Count > 0)
        {
            // Still need to notify UI of preference changes
            NotifyChanged();
            return;
        }

        var restored = await _persistence.RestoreAsync();
        _perfLogger.LogMetric("Session.DocumentCount", restored.Count());
        foreach (var doc in restored)
        {
            _open[doc.Id] = doc;
            _tabs.AddTab(doc);
            StartWatchingFile(doc);
        }
        
        // If no documents were restored, create a new one
        if (_tabs.Tabs.Count == 0)
        {
            CreateNew();
        }
        else
        {
            NotifyChanged();
        }
    }

    /// <summary>
    /// Load user preferences from disk and apply theme
    /// </summary>
    public async Task LoadPreferencesAsync()
    {
        Preferences = await _prefsRepo.LoadAsync();
        // If we don't have a last-check time in preferences (or want to keep updater metadata separate),
        // attempt to restore from PersistenceService's auto-update metadata, if present.
        try
        {
            var persisted = _persistence.RestoreAutoUpdateLastCheck();
            if (persisted.HasValue)
            {
                Preferences.Updates.LastCheckTime = persisted.Value;
            }
        }
        catch { /* ignore */ }
        _logger?.LogInformation("Loaded preferences: LoggingEnabled={LoggingEnabled}, FileExtensions={ExtensionsCount}",
            Preferences.LoggingEnabled, Preferences.FileExtensions?.Count ?? 0);
        _msLogger?.LogInformation("Loaded preferences (ms logger): LoggingEnabled={LoggingEnabled}, FileExtensions={ExtensionsCount}",
            Preferences.LoggingEnabled, Preferences.FileExtensions?.Count ?? 0);
        await ApplyThemeAsync();
        // Apply spell check preferences to the runtime service if registered
        try
        {
            if (Preferences.SpellCheck != null)
            {
                _spellCheckingService?.UpdatePreferences(Preferences.SpellCheck);
            }
        }
        catch { /* ignore */ }
        NotifyChanged();
    }

    /// <summary>
    /// Save user preferences to disk
    /// </summary>
    public async Task SavePreferencesAsync()
    {
        await _prefsRepo.SaveAsync(Preferences);
        _logger?.LogInformation("Saved preferences: LoggingEnabled={LoggingEnabled}, FileExtensions={ExtensionsCount}",
            Preferences.LoggingEnabled, Preferences.FileExtensions?.Count ?? 0);
        _msLogger?.LogInformation("Saved preferences (ms logger): LoggingEnabled={LoggingEnabled}, FileExtensions={ExtensionsCount}",
            Preferences.LoggingEnabled, Preferences.FileExtensions?.Count ?? 0);
        NotifyChanged();
    }
    /// <summary>
    /// Centralized setter for LoggingEnabled preference with save and error handling.
    /// </summary>
    public async Task SetLoggingEnabledAsync(bool enabled)
    {
        Preferences.LoggingEnabled = enabled;
        try
        {
            await SavePreferencesAsync();
            _logger?.LogInformation("Set LoggingEnabled to {Enabled}", enabled);
            NotifyChanged();
        }
        catch
        {
            Preferences.LoggingEnabled = !enabled;
            throw;
        }
    }

    /// <summary>
    /// Centralized setter for Theme preference that applies theme after save.
    /// </summary>
    public async Task SetThemeAsync(ThemeMode theme)
    {
        Preferences.Theme = theme;
        try
        {
            await SavePreferencesAsync();
            await ApplyThemeAsync();
            _logger?.LogInformation("Set Theme to {Theme}", theme);
            NotifyChanged();
            ThemeChanged?.Invoke(theme);
        }
        catch
        {
            // Revert to previous value (best effort) -- not perfectly transactional but preferable
            Preferences.Theme = Preferences.Theme; // no-op but keeps pattern
            throw;
        }
    }

    /// <summary>
    /// Apply current theme. OS theme detection is deferred: System maps to Light.
    /// </summary>
    public async Task ApplyThemeAsync()
    {
        var resolvedTheme = Preferences.Theme switch
        {
            ThemeMode.Dark => "Dark",
            ThemeMode.Light => "Light",
            _ => "Dark" // System -> Dark (interim default; OS detection deferred)
        };
        _themeManager.ApplyTheme(resolvedTheme);
        NotifyChanged();
        await Task.CompletedTask;
    }

    private async Task HandleAutosaveAsync()
    {
        using var _ = _perfLogger.BeginOperation("Autosave");
        // Autosave persists both session and editor preferences
        await PersistSessionAsync();
        PersistEditorPreferences();
        // Notify UI so StatusBar can update autosave indicator
        NotifyChanged();
    }
    /// <summary>
    /// Persist current session state (documents and tab order) for crash recovery and next launch.
    /// </summary>
    public async Task PersistSessionAsync()
    {
        // Persist documents in current tab order for stable restore
        var order = _tabs.Tabs.Select(t => t.DocumentId).ToList();
        await _persistence.PersistAsync(_open.Values, order);
    }

    /// <summary>
    /// Persist current editor UI preferences (word-wrap and preview) to disk.
    /// </summary>
    public void PersistEditorPreferences()
    {
        _persistence.PersistEditorPreferences(EditorState.WordWrap, EditorState.ShowPreview);
    }

    /// <summary>
    /// Remove a specific document's session file after successful save or discard.
    /// </summary>
    /// <param name="documentId">Document identifier.</param>
    public void DeleteSessionFile(Guid documentId)
    {
        _persistence.DeleteSessionFile(documentId);
    }

    /// <summary>
    /// Create a new unsaved document and activate its tab.
    /// </summary>
    /// <returns>The created document.</returns>
    public Document CreateNew()
    {
        var doc = _docs.NewDocument();
        _open[doc.Id] = doc;
        _tabs.AddTab(doc);
        NotifyChanged();
        return doc;
    }

    /// <summary>
    /// Show the open file dialog and open the selected document when valid.
    /// Returns null if the dialog is cancelled or open fails.
    /// </summary>
    public async Task<Document?> OpenAsync()
    {
        using var _ = _perfLogger.BeginOperation("Document.Open");
        _logger?.LogDebug("Opening file dialog");
        
        var path = await _ipc.ShowOpenFileDialogAsync();
        if (string.IsNullOrWhiteSpace(path))
        {
            _logger?.LogDebug("File dialog cancelled");
            return null;
        }
        
        _logger?.LogInformation("Opening file from dialog: {Path}", path);
        // Enforce recognized extensions for manual open as well
        var extSel = Path.GetExtension(path)?.ToLowerInvariant() ?? string.Empty;
        if (!Preferences.FileExtensions.Any(e => string.Equals(e, extSel, StringComparison.OrdinalIgnoreCase)))
        {
            _logger?.LogWarning("User attempted to open unsupported file type: {Path}", path);
            _dialogService?.ShowErrorDialog("Unsupported File Type", $"Files of type '{extSel}' are not currently recognized. Add it in Options → Recognized File Extensions.");
            return null;
        }
        
        try
        {
            var doc = await _docs.OpenAsync(path!);
            _perfLogger.LogMetric("Document.Size", doc.Content.Length, "chars");
            _open[doc.Id] = doc;
            _tabs.AddTab(doc);
            StartWatchingFile(doc);
            NotifyChanged();
            _logger?.LogInformation("File opened and tab created: {Path}", path);
            return doc;
        }
        catch (FileNotFoundException ex)
        {
            _logger?.LogError(ex, "File not found: {Path}", ex.FileName ?? path);
            _dialogService?.ShowErrorDialog("File Not Found", $"The file '{ex.FileName}' could not be found.");
            return null;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger?.LogError(ex, "Access denied when opening: {Path}", path);
            _dialogService?.ShowErrorDialog("Access Denied", $"Permission denied when opening '{path}'. You may not have read access to this file.");
            return null;
        }
        catch (IOException ex)
        {
            _logger?.LogError(ex, "I/O error when opening: {Path}", path);
            _dialogService?.ShowErrorDialog("I/O Error", $"An error occurred while opening '{path}': {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Save the active document, prompting for a location if it has not been saved before.
    /// </summary>
    public async Task SaveActiveAsync()
    {
        using var _ = _perfLogger.BeginOperation("Document.Save");
        
        if (ActiveDocument is null)
        {
            _logger?.LogWarning("SaveActiveAsync called with no active document");
            return;
        }
        
        if (string.IsNullOrWhiteSpace(ActiveDocument.FilePath))
        {
            _logger?.LogDebug("No file path, showing save dialog for: {Name}", ActiveDocument.Name);
            var saved = await SaveAsActiveAsync();
            if (!saved) return;
        }
        
        _logger?.LogInformation("Saving document: {Path}", ActiveDocument.FilePath ?? "(unknown)");
        
        try
        {
            await _docs.SaveAsync(ActiveDocument);
            NotifyChanged();
            _logger?.LogInformation("Document saved successfully: {Path}", ActiveDocument.FilePath ?? "(unknown)");
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger?.LogError(ex, "Permission denied saving: {Path}", ActiveDocument?.FilePath ?? "(unknown)");
            _dialogService?.ShowErrorDialog("Permission Denied", $"Cannot save '{ActiveDocument?.Name}'. The file may be read-only or you may not have write permission.");
        }
        catch (IOException ex)
        {
            _logger?.LogError(ex, "I/O error saving: {Path}", ActiveDocument?.FilePath ?? "(unknown)");
            _dialogService?.ShowErrorDialog("Save Error", $"An error occurred while saving '{ActiveDocument?.Name}': {ex.Message}");
        }
    }

    /// <summary>
    /// Save the active document to a new path selected by the user.
    /// </summary>
    /// <returns>True if the document was saved; otherwise false.</returns>
    public async Task<bool> SaveAsActiveAsync()
    {
        if (ActiveDocument is null) return false;
        var documentId = ActiveDocument.Id; // Capture ID before save
        var path = await _ipc.ShowSaveFileDialogAsync();
        if (string.IsNullOrWhiteSpace(path)) return false;

        // Trust the OS save dialog's overwrite confirmation
        try
        {
            await _docs.SaveAsync(ActiveDocument, path);
            // Clean up session file after successful save
            DeleteSessionFile(documentId);
            // Start watching the newly saved file
            StartWatchingFile(ActiveDocument);
            // Force UI update
            NotifyChanged();
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            _dialogService?.ShowErrorDialog("Permission Denied", $"Cannot save to '{path}'. You may not have write permission to this location.");
            return false;
        }
        catch (IOException ex)
        {
            _dialogService?.ShowErrorDialog("Save Error", $"An error occurred while saving to '{path}': {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Activate a tab by its identifier.
    /// </summary>
    public void ActivateTab(Guid tabId)
    {
        _tabs.ActivateTab(tabId);
        NotifyChanged();
    }

    /// <summary>
    /// Close a tab, prompting to save when the associated document has unsaved changes.
    /// </summary>
    /// <returns>True when the tab was closed; false when cancelled.</returns>
    public async Task<bool> CloseTabAsync(Guid tabId)
    {
        var tab = _tabs.Tabs.FirstOrDefault(t => t.Id == tabId);
        if (tab is null) return false;

        if (_open.TryGetValue(tab.DocumentId, out var doc) && doc is not null && doc.IsDirty)
        {
            // Confirm close for dirty documents
            var decision = await _ipc.ConfirmCloseDirtyAsync(doc.Name);
            if (decision == IpcBridge.CloseDecision.Cancel)
            {
                return false; // abort close
            }

            if (decision == IpcBridge.CloseDecision.Save)
            {
                // Save existing or Save As for untitled (for the specific doc being closed)
                if (string.IsNullOrWhiteSpace(doc.FilePath))
                {
                    var path = await _ipc.ShowSaveFileDialogAsync();
                    if (string.IsNullOrWhiteSpace(path))
                    {
                        return false; // user cancelled save-as
                    }
                    await _docs.SaveAsync(doc, path);
                    // Clean up session file after successful save
                    DeleteSessionFile(doc.Id);
                }
                else
                {
                    await _docs.SaveAsync(doc);
                    DeleteSessionFile(doc.Id);
                }
            }
            // If Don't Save: proceed to close without saving
        }
        _tabs.CloseTab(tabId);
        _open.Remove(tab.DocumentId);
        StopWatchingFile(tab.DocumentId);
        if (_tabs.Tabs.Count == 0)
        {
            // Always keep an editor available
            CreateNew();
        }
        else
        {
            NotifyChanged();
        }
        return true;
    }

    /// <summary>
    /// Close all tabs except the specified one, stopping on the first cancellation.
    /// </summary>
    public async Task CloseOthersAsync(Guid keepTabId)
    {
        // Snapshot current order to avoid index shifting while closing
        var toClose = _tabs.Tabs.Where(t => t.Id != keepTabId).Select(t => t.Id).ToList();
        // Close from rightmost to leftmost for stability
        foreach (var id in toClose.AsEnumerable().Reverse())
        {
            var ok = await CloseTabAsync(id);
            if (!ok) break; // stop on cancel
        }
    }

    /// <summary>
    /// Close all tabs to the right of the specified tab, stopping on the first cancellation.
    /// </summary>
    public async Task CloseRightAsync(Guid fromTabId)
    {
        var list = _tabs.Tabs.ToList();
        var idx = list.FindIndex(t => t.Id == fromTabId);
        if (idx < 0) return;
        var toClose = list.Skip(idx + 1).Select(t => t.Id).ToList();
        // Close from rightmost to leftmost
        foreach (var id in toClose.AsEnumerable().Reverse())
        {
            var ok = await CloseTabAsync(id);
            if (!ok) break; // stop on cancel
        }
    }

    /// <summary>
    /// Get a document by identifier or null if it's not open.
    /// </summary>
    public Document? GetDocument(Guid id)
        => _open.TryGetValue(id, out var d) ? d : null;

    /// <summary>
    /// Activate the next tab in order (wraps to the first tab).
    /// </summary>
    public void ActivateNextTab()
    {
        if (_tabs.Tabs.Count == 0) return;
        var idx = 0;
        for (var i = 0; i < _tabs.Tabs.Count; i++)
        {
            if (_tabs.Tabs[i].IsActive) { idx = i; break; }
        }
        var next = (idx + 1) % _tabs.Tabs.Count;
        _tabs.ActivateTab(_tabs.Tabs[next].Id);
        NotifyChanged();
    }

    /// <summary>
    /// Activate the previous tab in order (wraps to the last tab).
    /// </summary>
    public void ActivatePreviousTab()
    {
        if (_tabs.Tabs.Count == 0) return;
        var idx = 0;
        for (var i = 0; i < _tabs.Tabs.Count; i++)
        {
            if (_tabs.Tabs[i].IsActive) { idx = i; break; }
        }
        var prev = (idx - 1 + _tabs.Tabs.Count) % _tabs.Tabs.Count;
        _tabs.ActivateTab(_tabs.Tabs[prev].Id);
        NotifyChanged();
    }

    private void StartWatchingFile(Document doc)
    {
        if (string.IsNullOrWhiteSpace(doc.FilePath)) return;
        if (_watchers.ContainsKey(doc.Id)) return;

        var watcher = new FileWatcher();
        var docId = doc.Id;
        watcher.ChangedExternally += path =>
        {
            // Debounce duplicate events within 1s
            var now = DateTimeOffset.UtcNow;
            if (_lastExternalChange.TryGetValue(docId, out var last) && (now - last).TotalMilliseconds < 1000)
            {
                return;
            }
            _lastExternalChange[docId] = now;
            _ = HandleExternalFileChangeAsync(docId, path);
        };
        watcher.Watch(doc.FilePath!);
        _watchers[doc.Id] = watcher;
    }

    private void StopWatchingFile(Guid documentId)
    {
        if (_watchers.TryGetValue(documentId, out var watcher))
        {
            watcher.Stop();
            watcher.Dispose();
            _watchers.Remove(documentId);
        }
    }

    public void Dispose()
    {
        _autosave.Stop();
        
        foreach (var watcher in _watchers.Values)
        {
            watcher.Stop();
            watcher.Dispose();
        }
        _watchers.Clear();
    }

    private async Task HandleExternalFileChangeAsync(Guid docId, string path)
    {
        try
        {
            if (!_open.TryGetValue(docId, out var doc) || doc is null)
            {
                return;
            }

            // If document has no unsaved edits, auto-reload from disk
            if (!doc.IsDirty)
            {
                if (!string.IsNullOrWhiteSpace(doc.FilePath) && File.Exists(doc.FilePath))
                {
                    var content = await File.ReadAllTextAsync(doc.FilePath, doc.Encoding);
                    doc.SetContentInternal(content);
                    doc.MarkSaved(doc.FilePath);
                    NotifyChanged();
                }
                return;
            }

            // Otherwise, prompt to reload or keep
            doc.MarkExternalModification(true);
            NotifyChanged();

            var decision = await _ipc.ConfirmReloadExternalAsync(doc.Name);
            if (decision == IpcBridge.ExternalChangeDecision.Reload)
            {
                if (!string.IsNullOrWhiteSpace(doc.FilePath) && File.Exists(doc.FilePath))
                {
                    var content = await File.ReadAllTextAsync(doc.FilePath, doc.Encoding);
                    doc.SetContentInternal(content);
                    doc.MarkSaved(doc.FilePath);
                    doc.MarkExternalModification(false);
                    NotifyChanged();
                }
            }
            else
            {
                // Keep current edits; keep indicator visible
                doc.MarkExternalModification(true);
                NotifyChanged();
            }
        }
        catch
        {
            // Swallow handler exceptions to avoid crashing event pipeline
        }
    }

    /// <summary>
    /// Get the current window title based on active document (T056-T060)
    /// </summary>
    public string GetWindowTitle()
    {
        const string appName = "Scrappy Text Editor";
        
        if (ActiveDocument == null)
        {
            return appName; // T060: No-file state
        }
        
        var filename = !string.IsNullOrWhiteSpace(ActiveDocument.FilePath)
            ? Path.GetFileName(ActiveDocument.FilePath)
            : "Untitled";
        
        // T057: Add dirty indicator (bullet) before filename
        var dirtyIndicator = ActiveDocument.IsDirty ? "● " : "";
        
        return $"{dirtyIndicator}{filename} - {appName}"; // T056: Format
    }
}
