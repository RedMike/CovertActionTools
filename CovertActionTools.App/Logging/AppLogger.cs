using Microsoft.Extensions.Logging;

namespace CovertActionTools.App.Logging;

public class AppLogger : ILogger
{
    /// <summary>
    /// Just to provide a more readable name, maps to `LogLevel`
    /// </summary>
    private enum InternalLogLevel
    {
        Trace = 0,
        Debug = 1,
        Info = 2,
        Warn = 3,
        Error = 4,
        Fatal = 5,
        None = 6
    }

    private readonly string _categoryName;
    private readonly Action<string> _logEvent;

    public AppLogger(string categoryName, Action<string> logEvent)
    {
        _categoryName = categoryName;
        _logEvent = logEvent;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        var parsedLogLevel = (InternalLogLevel)(int)logLevel;
        var msg = formatter(state, exception);
        var fullString = $"[{parsedLogLevel.ToString().ToUpperInvariant(),-6}] [{_categoryName}] {msg}";
        _logEvent(fullString);
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }
}