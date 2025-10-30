using System;
using System.Threading.Tasks;
using ElectronNET.API;
using ElectronNET.API.Entities;

namespace TextEdit.Infrastructure.Themes;

/// <summary>
/// Detects current OS theme and watches for theme changes using Electron NativeTheme API.
/// </summary>
public class ThemeDetectionService
{
    private Action<string>? _themeChangedCallback;
    private bool _isWatching;
    private DateTime _lastThemeChange = DateTime.MinValue;
    private const int DebounceDurationMs = 100;

    /// <summary>
    /// Get current OS theme preference.
    /// </summary>
    /// <returns>"Dark", "Light", or "Light" (default on error)</returns>
    public async Task<string> GetCurrentOsThemeAsync()
    {
        try
        {
            if (!HybridSupport.IsElectronActive)
            {
                return "Light"; // Fallback for non-Electron context (tests)
            }

            var shouldUseDark = await Electron.NativeTheme.ShouldUseDarkColorsAsync();
            return shouldUseDark ? "Dark" : "Light";
        }
        catch
        {
            return "Light"; // Fallback on error
        }
    }

    /// <summary>
    /// Start watching OS theme changes. Polls every 2 seconds for changes.
    /// TODO: Replace with event-based approach when Electron.NET API supports NativeTheme.updated event
    /// </summary>
    /// <param name="callback">Callback invoked with "Light" or "Dark" when OS theme changes</param>
    public async void WatchThemeChanges(Action<string> callback)
    {
        if (_isWatching)
        {
            return; // Already watching
        }

        _themeChangedCallback = callback;
        _isWatching = true;

        if (!HybridSupport.IsElectronActive)
        {
            return; // No-op in non-Electron context
        }

        // Poll for theme changes every 2 seconds
        var lastTheme = await GetCurrentOsThemeAsync();
        _ = Task.Run(async () =>
        {
            while (_isWatching)
            {
                await Task.Delay(2000);
                var currentTheme = await GetCurrentOsThemeAsync();
                if (currentTheme != lastTheme)
                {
                    lastTheme = currentTheme;
                    _themeChangedCallback?.Invoke(currentTheme);
                }
            }
        });
    }

    /// <summary>
    /// Stop watching theme changes.
    /// </summary>
    public void StopWatching()
    {
        _isWatching = false;
        _themeChangedCallback = null;
    }
}
