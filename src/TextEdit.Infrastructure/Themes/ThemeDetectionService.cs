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
    private const int PollIntervalMs = 30000; // 30s polling to minimize noise

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
    /// Start watching OS theme changes with low-frequency polling (30s) to minimize nativeTheme logs.
    /// </summary>
    /// <param name="callback">Callback invoked with "Light" or "Dark" when OS theme changes</param>
    public void WatchThemeChanges(Action<string> callback)
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

        _ = Task.Run(async () =>
        {
            while (_isWatching)
            {
                await Task.Delay(PollIntervalMs);
                var osTheme = await GetCurrentOsThemeAsync();
                _themeChangedCallback?.Invoke(osTheme);
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
