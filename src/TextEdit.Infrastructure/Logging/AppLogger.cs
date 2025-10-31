using Microsoft.Extensions.Logging;
using TextEdit.Core.Preferences;
using TextEdit.Core.Abstractions;

namespace TextEdit.Infrastructure.Logging;

/// <summary>
/// Application logger that respects LoggingEnabled preference for detailed logging.
/// Always logs errors/warnings, but detailed Debug/Information only when enabled.
/// </summary>
public class AppLogger : IAppLogger
{
    private readonly ILogger _logger;
    private readonly Func<bool> _isLoggingEnabled;

    public AppLogger(ILogger logger, Func<bool> isLoggingEnabled)
    {
        _logger = logger;
        _isLoggingEnabled = isLoggingEnabled;
    }

    /// <summary>
    /// Log detailed information (only when LoggingEnabled = true)
    /// </summary>
    public void LogInformation(string message, params object[] args)
    {
        if (_isLoggingEnabled())
        {
            _logger.LogInformation(message, args);
        }
    }

    /// <summary>
    /// Log debug information (only when LoggingEnabled = true)
    /// </summary>
    public void LogDebug(string message, params object[] args)
    {
        if (_isLoggingEnabled())
        {
            _logger.LogDebug(message, args);
        }
    }

    /// <summary>
    /// Always log warnings regardless of LoggingEnabled setting
    /// </summary>
    public void LogWarning(string message, params object[] args)
    {
        _logger.LogWarning(message, args);
    }

    /// <summary>
    /// Always log warnings regardless of LoggingEnabled setting
    /// </summary>
    public void LogWarning(Exception exception, string message, params object[] args)
    {
        _logger.LogWarning(exception, message, args);
    }

    /// <summary>
    /// Always log errors regardless of LoggingEnabled setting
    /// </summary>
    public void LogError(string message, params object[] args)
    {
        _logger.LogError(message, args);
    }

    /// <summary>
    /// Always log errors regardless of LoggingEnabled setting
    /// </summary>
    public void LogError(Exception? exception, string message, params object[] args)
    {
        _logger.LogError(exception, message, args);
    }

    /// <summary>
    /// Always log critical errors regardless of LoggingEnabled setting
    /// </summary>
    public void LogCritical(string message, params object[] args)
    {
        _logger.LogCritical(message, args);
    }

    /// <summary>
    /// Always log critical errors regardless of LoggingEnabled setting
    /// </summary>
    public void LogCritical(Exception? exception, string message, params object[] args)
    {
        _logger.LogCritical(exception, message, args);
    }
}

/// <summary>
/// Factory for creating AppLogger instances
/// </summary>
public interface IAppLoggerFactory
{
    IAppLogger CreateLogger(string categoryName);
    IAppLogger CreateLogger<T>();
}

/// <summary>
/// Implementation of AppLogger factory
/// </summary>
public class AppLoggerFactory : IAppLoggerFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly Func<bool> _isLoggingEnabled;

    public AppLoggerFactory(ILoggerFactory loggerFactory, Func<bool> isLoggingEnabled)
    {
        _loggerFactory = loggerFactory;
        _isLoggingEnabled = isLoggingEnabled;
    }

    public IAppLogger CreateLogger(string categoryName)
    {
        var logger = _loggerFactory.CreateLogger(categoryName);
        return new AppLogger(logger, _isLoggingEnabled);
    }

    public IAppLogger CreateLogger<T>()
    {
        return CreateLogger(typeof(T).FullName ?? typeof(T).Name);
    }
}
