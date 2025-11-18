using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using TextEdit.Core.Preferences;
using Microsoft.Extensions.Logging;

namespace TextEdit.Infrastructure.Persistence;

/// <summary>
/// Persists <see cref="UserPreferences"/> to JSON file in OS application data directory with atomic writes.
/// </summary>
public class PreferencesRepository : IPreferencesRepository
{
    private readonly string _preferencesPath;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly TextEdit.Core.Abstractions.IAppLogger? _logger;
    private readonly Microsoft.Extensions.Logging.ILogger<PreferencesRepository>? _msLogger;

    public PreferencesRepository(TextEdit.Core.Abstractions.IAppLogger? logger = null, Microsoft.Extensions.Logging.ILogger<PreferencesRepository>? msLogger = null)
    {
        _logger = logger;
        _msLogger = msLogger;
        // Centralized location for preferences
        Directory.CreateDirectory(AppPaths.BaseDir);
        _preferencesPath = AppPaths.PreferencesPath;
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };
    }

    /// <summary>
    /// Load preferences from disk; return defaults if file doesn't exist or is corrupt.
    /// </summary>
    public async Task<UserPreferences> LoadAsync()
    {
        try
        {
            _logger?.LogInformation("Attempting to load preferences from {Path}", _preferencesPath);
            _msLogger?.LogInformation("Attempting to load preferences from {Path}", _preferencesPath);
            if (!File.Exists(_preferencesPath))
            {
                _logger?.LogInformation("Preferences file not found, using defaults");
                _msLogger?.LogInformation("Preferences file not found, using defaults");
                return new UserPreferences();
            }

            var json = await File.ReadAllTextAsync(_preferencesPath);
            var prefs = JsonSerializer.Deserialize<UserPreferences>(json, _jsonOptions);
            
            if (prefs == null)
            {
                _logger?.LogWarning("Preferences deserialized to null; using defaults");
                return new UserPreferences();
            }

            // Normalize and validate extensions
            prefs.NormalizeExtensions();
            var (isValid, invalidEntry) = prefs.ValidateExtensions();
            if (!isValid)
            {
                _logger?.LogWarning("Preferences file contains invalid extension entry: {InvalidEntry}", invalidEntry ?? "<null>");
                _msLogger?.LogWarning("Preferences file contains invalid extension entry: {InvalidEntry}", invalidEntry ?? "<null>");
            }

            return prefs;
        }
        catch (JsonException ex)
        {
            // Corrupt JSON - return defaults and let user reconfigure
            _logger?.LogWarning(ex, "Preferences JSON invalid: {Path}", _preferencesPath);
            _msLogger?.LogWarning(ex, "Preferences JSON invalid: {Path}", _preferencesPath);
            return new UserPreferences();
        }
        catch (IOException)
        {
            // File I/O error - return defaults
            _logger?.LogWarning("Preferences I/O error reading file: {Path}", _preferencesPath);
            _msLogger?.LogWarning("Preferences I/O error reading file: {Path}", _preferencesPath);
            return new UserPreferences();
        }
    }

    /// <summary>
    /// Save preferences atomically using temp file + rename pattern.
    /// </summary>
    public async Task SaveAsync(UserPreferences preferences)
    {
        if (preferences == null)
            throw new ArgumentNullException(nameof(preferences));

        // Normalize before save
        preferences.NormalizeExtensions();
        
        // Validate extensions
        var (isValid, invalidEntry) = preferences.ValidateExtensions();
        if (!isValid)
        {
            throw new ArgumentException($"Invalid file extension format: {invalidEntry}", nameof(preferences));
        }

        // Atomic write: temp file + rename
        var tempPath = _preferencesPath + ".tmp";
        try
        {
            // Ensure directory exists for both temp and final paths
            var dir = Path.GetDirectoryName(_preferencesPath)!;
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            var json = JsonSerializer.Serialize(preferences, _jsonOptions);
            await File.WriteAllTextAsync(tempPath, json);
            _logger?.LogInformation("Wrote preferences temp file to {TempPath}", tempPath);
            _msLogger?.LogInformation("Wrote preferences temp file to {TempPath}", tempPath);
            
            // Atomic rename (overwrites existing)
            File.Move(tempPath, _preferencesPath, overwrite: true);
            _logger?.LogInformation("Moved temp preferences to {Path}", _preferencesPath);
            _msLogger?.LogInformation("Moved temp preferences to {Path}", _preferencesPath);
        }
        catch (IOException ex)
        {
            // Clean up temp file if it exists
            if (File.Exists(tempPath))
            {
                try { File.Delete(tempPath); } catch { }
            }
            throw new InvalidOperationException("Failed to save preferences", ex);
        }
    }
}
