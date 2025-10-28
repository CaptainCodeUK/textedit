namespace TextEdit.Infrastructure.Autosave;

using System;
using System.Timers;

/// <summary>
/// Periodically triggers autosave operations every 30 seconds.
/// </summary>
public class AutosaveService : IDisposable
{
    private readonly Timer _timer;
    private DateTime _lastAutosave = DateTime.MinValue;

    public event Func<Task>? AutosaveRequested;
    public DateTime LastAutosave => _lastAutosave;

    public AutosaveService(int intervalMs = 30000)
    {
        _timer = new Timer(intervalMs) { AutoReset = true, Enabled = false };
        _timer.Elapsed += OnTimerElapsed;
    }

    public void Start()
    {
        _timer.Start();
    }

    public void Stop()
    {
        _timer.Stop();
    }

    private async void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        try
        {
            if (AutosaveRequested != null)
            {
                await AutosaveRequested.Invoke();
                _lastAutosave = DateTime.UtcNow;
            }
        }
        catch
        {
            // Swallow exceptions by design; autosave failures should not crash the app
        }
    }

    public void Dispose()
    {
        _timer.Dispose();
    }
}
