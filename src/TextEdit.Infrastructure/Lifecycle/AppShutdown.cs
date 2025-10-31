namespace TextEdit.Infrastructure.Lifecycle;

/// <summary>
/// Global shutdown flag to let infrastructure and UI avoid late work (dialogs, timers) during app exit.
/// </summary>
public static class AppShutdown
{
    private static volatile bool _isShuttingDown;
    public static bool IsShuttingDown => _isShuttingDown;
    public static void Begin() => _isShuttingDown = true;
}
