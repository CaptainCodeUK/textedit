namespace TextEdit.Core.Preferences;

/// <summary>
/// Persisted window geometry and state for restoring on next launch.
/// </summary>
public class WindowState
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; } = 1200;
    public int Height { get; set; } = 800;
    public bool IsMaximized { get; set; }
    public bool IsFullScreen { get; set; }

    /// <summary>
    /// Clamp size to sensible minimums to avoid tiny or invalid windows.
    /// </summary>
    public void ClampToMinimums(int minWidth = 800, int minHeight = 600)
    {
        if (Width < minWidth) Width = minWidth;
        if (Height < minHeight) Height = minHeight;
    }
}
