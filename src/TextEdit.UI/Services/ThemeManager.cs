using System;
using Microsoft.JSInterop;

namespace TextEdit.UI.Services;

/// <summary>
/// Manages theme application to UI components by updating CSS custom properties.
/// </summary>
public class ThemeManager
{
    private readonly IJSRuntime? _js;
    private int _themeVersion = 0;

    // Parameterless constructor for tests and non-interactive contexts
    public ThemeManager()
    {
        _js = null;
    }

    // Runtime constructor for Blazor/Electron with JS interop available
    public ThemeManager(IJSRuntime js)
    {
        _js = js;
    }

    /// <summary>
    /// Apply the specified theme mode to the UI by setting data-theme attribute.
    /// This will be implemented to inject JavaScript to update document.documentElement.dataset.theme
    /// </summary>
    /// <param name="themeMode">"Light", "Dark", or resolved from "System"</param>
    public void ApplyTheme(string themeMode)
    {
        // Normalize and store (actual DOM update happens in App.razor via JS)
        var normalized = string.IsNullOrWhiteSpace(themeMode) ? "Light" : themeMode;
        CurrentTheme = normalized;
        _themeVersion++; // Increment version to signal change
    }

    /// <summary>
    /// Current active theme ("Light" or "Dark")
    /// </summary>
    public string CurrentTheme { get; private set; } = "Light";
    
    /// <summary>
    /// Version counter incremented on each theme change
    /// </summary>
    public int ThemeVersion => _themeVersion;
}
