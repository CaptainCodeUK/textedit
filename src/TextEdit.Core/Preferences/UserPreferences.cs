namespace TextEdit.Core.Preferences;

using System.Collections.Generic;

/// <summary>
/// Represents all user-configurable settings that persist across application sessions.
/// </summary>
public class UserPreferences
{
    public ThemeMode Theme { get; set; } = ThemeMode.System;

    /// <summary>
    /// Font family name. Empty string means use system monospace fallback.
    /// </summary>
    public string FontFamily { get; set; } = string.Empty;

    /// <summary>
    /// Font size in points. Clamped to 8..72 on set.
    /// </summary>
    private int _fontSize = 12;
    public int FontSize
    {
        get => _fontSize;
        set => _fontSize = value < 8 ? 8 : (value > 72 ? 72 : value);
    }

    /// <summary>
    /// List of recognized text file extensions. Stored in lowercase and without duplicates.
    /// </summary>
    public List<string> FileExtensions { get; set; } = new List<string> { ".txt", ".md", ".log", ".json", ".xml", ".csv", ".ini", ".cfg", ".conf" };

    public bool LoggingEnabled { get; set; } = false;

    public bool ToolbarVisible { get; set; } = true;

    /// <summary>
    /// Editor line numbers visibility setting.
    /// </summary>
    public bool ShowLineNumbers { get; set; } = true;

    /// <summary>
    /// Editor minimap visibility setting.
    /// </summary>
    public bool ShowMinimap { get; set; } = false;

    /// <summary>
    /// Auto-update preferences (check frequency, auto-download, etc).
    /// </summary>
    public TextEdit.Core.Updates.UpdatePreferences Updates { get; set; } = new();

    public UserPreferences()
    {
    }

    /// <summary>
    /// Normalize extensions (lowercase, ensure leading dot) and remove duplicates.
    /// Also ensures required defaults (.txt, .md) are present.
    /// </summary>
    public void NormalizeExtensions()
    {
        var set = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        foreach (var ext in FileExtensions)
        {
            if (string.IsNullOrWhiteSpace(ext)) continue;
            var e = ext.Trim();
            e = e.StartsWith(".") ? e : "." + e;
            e = e.ToLowerInvariant();
            set.Add(e);
        }

        // Ensure required defaults
        set.Add(".txt");
        set.Add(".md");

        FileExtensions = new List<string>(set);
    }

    /// <summary>
    /// Validate extensions using regex ^\.[a-zA-Z0-9-]+$; returns tuple (isValid, invalidEntry) where invalidEntry is null on success.
    /// </summary>
    public (bool IsValid, string? InvalidEntry) ValidateExtensions()
    {
        var rx = new System.Text.RegularExpressions.Regex("^\\.[a-zA-Z0-9-]+$");
        foreach (var ext in FileExtensions)
        {
            if (!rx.IsMatch(ext)) return (false, ext);
        }
        return (true, null);
    }
}

/// <summary>
/// Theme selection mode.
/// Placed in the same file per tasks instruction (no separate enum file).
/// </summary>
public enum ThemeMode
{
    Light = 0,
    Dark = 1,
    System = 2
}
