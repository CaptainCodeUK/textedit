using System;
using System.IO;

namespace TextEdit.Infrastructure.Persistence;

/// <summary>
/// Centralized application paths for user-scoped data such as preferences, session files, and logs.
/// Ensures a consistent base directory across the app.
/// </summary>
public static class AppPaths
{
    /// <summary>
    /// Base application data directory (per-user), e.g.
    /// - Windows: %AppData%/Scrappy
    /// - macOS: ~/Library/Application Support/Scrappy
    /// - Linux: ~/.config/Scrappy
    /// </summary>
    public static string BaseDir { get; }

    /// <summary>
    /// Preferences JSON file path under <see cref="BaseDir"/>.
    /// </summary>
    public static string PreferencesPath { get; }

    /// <summary>
    /// Session directory used for crash-recovery and session restore.
    /// </summary>
    public static string SessionDir { get; }

    /// <summary>
    /// Path to auto-updater metadata (last check time, etc.). Kept separate from user preferences to
    /// avoid unintentional overwrites during early startup checks.
    /// </summary>
    public static string AutoUpdateMetadataPath { get; }

    /// <summary>
    /// Optional logs directory for file-based logging (not all hosts may use this).
    /// </summary>
    public static string LogsDir { get; }

    static AppPaths()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        // New canonical base directory
        var newBaseDir = Path.Combine(appData, "Scrappy");
        var newPrefsPath = Path.Combine(newBaseDir, "preferences.json");
        var newSessionDir = Path.Combine(newBaseDir, "Session");
        var newLogsDir = Path.Combine(newBaseDir, "Logs");
    var newAutoUpdatePath = Path.Combine(newBaseDir, "auto-update.json");

        // Legacy base directory we migrate from
        var oldBaseDir = Path.Combine(appData, "TextEdit");
        var oldPrefsPath = Path.Combine(oldBaseDir, "preferences.json");
        var oldSessionDir = Path.Combine(oldBaseDir, "Session");
        var oldLogsDir = Path.Combine(oldBaseDir, "Logs");
    var oldAutoUpdatePath = Path.Combine(oldBaseDir, "auto-update.json");

        // Ensure new directories exist
        Directory.CreateDirectory(newBaseDir);
        Directory.CreateDirectory(newSessionDir);
        Directory.CreateDirectory(newLogsDir);
    // Ensure parent for auto-update metadata exists
    Directory.CreateDirectory(newBaseDir);

        // Best-effort migration from TextEdit -> Scrappy
        // Rule: If legacy folder exists, attempt migration; otherwise skip.
        try
        {
            if (Directory.Exists(oldBaseDir))
            {
                // Move preferences file if present and not already in new location
                TryMoveFile(oldPrefsPath, newPrefsPath);

                // Move Session directory contents
                TryMoveDirectory(oldSessionDir, newSessionDir);

                // Move Logs directory contents
                TryMoveDirectory(oldLogsDir, newLogsDir);
                TryMoveFile(oldAutoUpdatePath, newAutoUpdatePath);

                // Attempt to delete empty legacy base directory
                TryDeleteIfEmpty(oldBaseDir);
            }
            // else: nothing to migrate
        }
        catch
        {
            // Suppress any migration errors; continue with new paths
        }

        // Publish final properties
        BaseDir = newBaseDir;
        PreferencesPath = newPrefsPath;
        SessionDir = newSessionDir;
        LogsDir = newLogsDir;
    AutoUpdateMetadataPath = newAutoUpdatePath;
    }

    private static void TryMoveFile(string sourcePath, string destPath)
    {
        try
        {
            if (File.Exists(sourcePath))
            {
                var destDir = Path.GetDirectoryName(destPath)!;
                Directory.CreateDirectory(destDir);
                if (!File.Exists(destPath))
                {
                    File.Move(sourcePath, destPath, overwrite: false);
                }
                else
                {
                    // Destination exists; keep destination and remove source to avoid duplication
                    try { File.Delete(sourcePath); } catch { }
                }
            }
        }
        catch { /* ignore */ }
    }

    private static void TryMoveDirectory(string sourceDir, string destDir)
    {
        try
        {
            if (!Directory.Exists(sourceDir))
                return;

            // If destination doesn't exist or is empty, try a simple move
            if (!Directory.Exists(destDir) || Directory.GetFileSystemEntries(destDir).Length == 0)
            {
                try
                {
                    // Ensure parent exists
                    Directory.CreateDirectory(Path.GetDirectoryName(destDir)!);
                    Directory.Move(sourceDir, destDir);
                    return;
                }
                catch
                {
                    // Fall back to per-file move
                }
            }

            // Move contents recursively
            Directory.CreateDirectory(destDir);

            foreach (var dir in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(sourceDir, dir);
                Directory.CreateDirectory(Path.Combine(destDir, relative));
            }

            foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(sourceDir, file);
                var target = Path.Combine(destDir, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                if (!File.Exists(target))
                {
                    try { File.Move(file, target, overwrite: false); } catch { }
                }
                else
                {
                    // Destination already has file; prefer destination and delete source
                    try { File.Delete(file); } catch { }
                }
            }

            // Attempt to delete the now-empty source directory tree
            TryDeleteIfEmpty(sourceDir);
        }
        catch { /* ignore */ }
    }

    private static void TryDeleteIfEmpty(string dir)
    {
        try
        {
            if (Directory.Exists(dir) && Directory.GetFileSystemEntries(dir).Length == 0)
            {
                Directory.Delete(dir);
            }
        }
        catch { /* ignore */ }
    }

}
