using System.Collections.Concurrent;

namespace CovertActionTools.App.ViewModels;

public class AppLoggingState : IViewModel
{
    private const int MaxLogCount = 1000;

    private HashSet<string> Filters { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    public ConcurrentStack<string> Logs { get; set; } = new ConcurrentStack<string>();

    public void Clear(HashSet<string>? newFilters = null)
    {
        Filters = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (newFilters != null)
        {
            Filters.UnionWith(newFilters);
        }
        Logs.Clear();
    }
    
    public void HandleLog(string categoryName, string message)
    {
        if (Filters.Count != 0)
        {
            if (!Filters.Contains(categoryName))
            {
                return;
            }
        }

        Logs.Push(message);
        while (Logs.Count > MaxLogCount)
        {
            Logs.TryPop(out _);
        }
    }
}