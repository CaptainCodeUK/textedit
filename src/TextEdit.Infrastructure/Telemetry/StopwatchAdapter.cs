namespace TextEdit.Infrastructure.Telemetry;

using System.Diagnostics;

/// <summary>
/// Production implementation of IStopwatch using System.Diagnostics.Stopwatch.
/// </summary>
public class StopwatchAdapter : IStopwatch
{
    private readonly Stopwatch _stopwatch;

    public StopwatchAdapter()
    {
        _stopwatch = Stopwatch.StartNew();
    }

    public long ElapsedMilliseconds => _stopwatch.ElapsedMilliseconds;

    public void Stop() => _stopwatch.Stop();
}
