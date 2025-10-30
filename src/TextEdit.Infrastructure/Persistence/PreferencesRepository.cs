using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using TextEdit.Core.Preferences;

namespace TextEdit.Infrastructure.Persistence;

/// <summary>
/// Persists <see cref="UserPreferences"/> to JSON file in OS application data directory with atomic writes.
/// </summary>
public class PreferencesRepository : IPreferencesRepository
{
    private readonly string _preferencesPath;
    private readonly JsonSerializerOptions _jsonOptions;

    public PreferencesRepository()
    {
        // Get OS-specific app data directory
        var appDataDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var scrappyDir = Path.Combine(appDataDir, "Scrappy");
        
        // Ensure directory exists
        Directory.CreateDirectory(scrappyDir);
        
        _preferencesPath = Path.Combine(scrappyDir, "preferences.json");
        
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
            if (!File.Exists(_preferencesPath))
            {
                return new UserPreferences();
            }

            var json = await File.ReadAllTextAsync(_preferencesPath);
            var prefs = JsonSerializer.Deserialize<UserPreferences>(json, _jsonOptions);
            
            if (prefs == null)
            {
                return new UserPreferences();
            }

            // Normalize and validate extensions
            prefs.NormalizeExtensions();
            var (isValid, invalidEntry) = prefs.ValidateExtensions();
            if (!isValid)
            {
                // Log warning but continue with normalized extensions
                Console.WriteLine($"Warning: Invalid extension '{invalidEntry}' found, using defaults");
            }

            return prefs;
        }
        catch (JsonException)
        {
            // Corrupt JSON - return defaults and let user reconfigure
            return new UserPreferences();
        }
        catch (IOException)
        {
            // File I/O error - return defaults
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
            var json = JsonSerializer.Serialize(preferences, _jsonOptions);
            await File.WriteAllTextAsync(tempPath, json);
            
            // Atomic rename (overwrites existing)
            File.Move(tempPath, _preferencesPath, overwrite: true);
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
