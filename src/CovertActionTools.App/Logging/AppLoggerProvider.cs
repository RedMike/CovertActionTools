using System.Collections.Concurrent;
using CovertActionTools.App.ViewModels;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.App.Logging;

[ProviderAlias("App")]
public class AppLoggerProvider : ILoggerProvider
{
    private readonly string _rootNamespace = nameof(CovertActionTools);
    
    private readonly AppLoggingState _state;
    private readonly ConcurrentDictionary<string, AppLogger> _loggers = new(StringComparer.OrdinalIgnoreCase);

    public AppLoggerProvider(AppLoggingState state)
    {
        _state = state;
    }

    public ILogger CreateLogger(string categoryName)
    {
        var trimmedName = categoryName;
        if (categoryName.StartsWith(_rootNamespace))
        {
            trimmedName = categoryName.Substring(_rootNamespace.Length + 1);
            
        }
        var logger = _loggers.GetOrAdd(categoryName, name => new AppLogger(trimmedName, (message) => HandleLog(categoryName, message)));
        return logger;
    }

    private void HandleLog(string categoryName, string message)
    {
        _state.HandleLog(categoryName, message);
    }
    
    public void Dispose()
    {
        _loggers.Clear();
    }
}