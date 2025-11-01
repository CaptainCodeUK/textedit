namespace TextEdit.Infrastructure.Autosave;

using System;
using System.Timers;

/// <summary>
/// Periodically triggers autosave operations at a configurable interval.
/// </summary>
public class AutosaveService : IDisposable
{
    private readonly Timer _timer;
    private DateTime _lastAutosave = DateTime.MinValue;

    /// <summary>
    /// Raised when an autosave tick occurs. Subscribers should perform persistence.
    /// Exceptions are swallowed in the timer callback to avoid process termination.
    /// </summary>
    public event Func<Task>? AutosaveRequested;

    /// <summary>
    /// Timestamp of the last successful autosave (UTC).
    /// </summary>
    public DateTime LastAutosave => _lastAutosave;

    /// <summary>
    /// Create a new autosave service.
    /// </summary>
    /// <param name="intervalMs">Timer interval in milliseconds. Default is 30 seconds.</param>
    public AutosaveService(int intervalMs = 30000)
    {
        _timer = new Timer(intervalMs) { AutoReset = true, Enabled = false };
        _timer.Elapsed += OnTimerElapsed;
    }

    /// <summary>
    /// Start the autosave timer.
    /// </summary>
    public void Start()
    {
        _timer.Start();
    }

    /// <summary>
    /// Stop the autosave timer.
    /// </summary>
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

    /// <summary>
    /// Dispose the underlying timer resources.
    /// </summary>
    public void Dispose()
    {
        _timer.Dispose();
    }
}
