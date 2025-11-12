namespace TextEdit.Infrastructure.Telemetry;

/// <summary>
/// Abstraction for timing operations, allowing testable performance measurement.
/// </summary>
public interface IStopwatch
{
    /// <summary>
    /// Gets the elapsed time in milliseconds.
    /// </summary>
    long ElapsedMilliseconds { get; }

    /// <summary>
    /// Stops the stopwatch.
    /// </summary>
    void Stop();
}
