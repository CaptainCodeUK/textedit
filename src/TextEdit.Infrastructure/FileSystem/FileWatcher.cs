namespace TextEdit.Infrastructure.FileSystem;

using System.IO;

/// <summary>
/// Watches a single file for external changes and raises a simple callback.
/// </summary>
public class FileWatcher : IDisposable
{
    private readonly FileSystemWatcher _watcher;

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

    public void Watch(string path)
    {
        var dir = Path.GetDirectoryName(path);
        var file = Path.GetFileName(path);
        if (string.IsNullOrEmpty(dir) || string.IsNullOrEmpty(file)) return;
        _watcher.Path = dir!;
        _watcher.Filter = file!;
        _watcher.EnableRaisingEvents = true;
    }

    public void Stop() => _watcher.EnableRaisingEvents = false;

    private void OnChanged(object sender, FileSystemEventArgs e) => ChangedExternally?.Invoke(e.FullPath);
    private void OnRenamed(object sender, RenamedEventArgs e) => ChangedExternally?.Invoke(e.FullPath);
    private void OnDeleted(object sender, FileSystemEventArgs e) => ChangedExternally?.Invoke(e.FullPath);

    public void Dispose() => _watcher.Dispose();
}
