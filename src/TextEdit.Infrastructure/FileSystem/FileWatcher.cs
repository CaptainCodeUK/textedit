namespace TextEdit.Infrastructure.FileSystem;

using System.IO;

/// <summary>
/// Watches a single file for external changes and raises a simple callback.
/// </summary>
public class FileWatcher : IDisposable
{
    private readonly FileSystemWatcher _watcher;

    /// <summary>
    /// Raised when the watched file changes, is renamed, or deleted externally.
    /// Provides the full path to the affected file.
    /// </summary>
    public event Action<string>? ChangedExternally;

    public FileWatcher()
    {
        _watcher = new FileSystemWatcher
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
            IncludeSubdirectories = false,
            EnableRaisingEvents = false
        };

        _watcher.Changed += OnChanged;
        _watcher.Renamed += OnRenamed;
        _watcher.Deleted += OnDeleted;
    }

    /// <summary>
    /// Begin watching the specified file for changes.
    /// </summary>
    /// <param name="path">Absolute file path to watch.</param>
    public void Watch(string path)
    {
        var dir = Path.GetDirectoryName(path);
        var file = Path.GetFileName(path);
        if (string.IsNullOrEmpty(dir) || string.IsNullOrEmpty(file)) return;
        _watcher.Path = dir!;
        _watcher.Filter = file!;
        _watcher.EnableRaisingEvents = true;
    }

    /// <summary>
    /// Stop raising change events for the current file.
    /// </summary>
    public void Stop() => _watcher.EnableRaisingEvents = false;

    private void OnChanged(object sender, FileSystemEventArgs e) => ChangedExternally?.Invoke(e.FullPath);
    private void OnRenamed(object sender, RenamedEventArgs e) => ChangedExternally?.Invoke(e.FullPath);
    private void OnDeleted(object sender, FileSystemEventArgs e) => ChangedExternally?.Invoke(e.FullPath);

    /// <summary>
    /// Dispose the underlying <see cref="FileSystemWatcher"/>.
    /// </summary>
    public void Dispose() => _watcher.Dispose();
}
