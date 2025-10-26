namespace TextEdit.Infrastructure.Autosave;

using System.Timers;

/// <summary>
/// Periodically triggers autosave operations. No-op placeholder for Phase 2.
/// </summary>
public class AutosaveService : IDisposable
{
    private readonly Timer _timer;

    public AutosaveService()
    {
        _timer = new Timer(30000) { AutoReset = true, Enabled = false };
    }

    public void Start() => _timer.Start();
    public void Stop() => _timer.Stop();

    public void Dispose() => _timer.Dispose();
}
