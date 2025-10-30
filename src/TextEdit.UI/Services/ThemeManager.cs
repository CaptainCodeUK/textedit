using System;

namespace TextEdit.UI.Services;

/// <summary>
/// Manages theme application to UI components by updating CSS custom properties.
/// </summary>
public class ThemeManager
{
    /// <summary>
    /// Apply the specified theme mode to the UI by setting data-theme attribute.
    /// This will be implemented to inject JavaScript to update document.documentElement.dataset.theme
    /// </summary>
    /// <param name="themeMode">"Light", "Dark", or resolved from "System"</param>
    public void ApplyTheme(string themeMode)
    {
        // This will be called by AppState when theme changes
        // Implementation will use IJSRuntime to set data-theme attribute on <html>
        // For now, store the current theme
        CurrentTheme = themeMode;
    }

    /// <summary>
    /// Current active theme ("Light" or "Dark")
    /// </summary>
    public string CurrentTheme { get; private set; } = "Light";
}
