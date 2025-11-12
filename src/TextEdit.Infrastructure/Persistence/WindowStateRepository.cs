using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using TextEdit.Core.Preferences;

namespace TextEdit.Infrastructure.Persistence;

/// <summary>
/// Stores and retrieves window state (position/size/maximize/fullscreen) in app data JSON.
/// </summary>
public class WindowStateRepository
{
    private readonly string _path;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    public WindowStateRepository(string? baseDir = null)
    {
        var dir = baseDir ?? AppPaths.BaseDir;
        Directory.CreateDirectory(dir);
        _path = Path.Combine(dir, "window-state.json");
    }

    public async Task<WindowState> LoadAsync()
    {
        try
        {
            if (!File.Exists(_path))
            {
                return new WindowState();
            }
            var json = await File.ReadAllTextAsync(_path);
            var state = JsonSerializer.Deserialize<WindowState>(json, _jsonOptions);
            return state ?? new WindowState();
        }
        catch
        {
            return new WindowState();
        }
    }

    public async Task SaveAsync(WindowState state)
    {
        // Ensure sensible minimums
        state.ClampToMinimums();
        var temp = _path + ".tmp";
        try
        {
            var json = JsonSerializer.Serialize(state, _jsonOptions);
            await File.WriteAllTextAsync(temp, json);
            File.Move(temp, _path, overwrite: true);
        }
        catch
        {
            try { if (File.Exists(temp)) File.Delete(temp); } catch { }
        }
    }
}
