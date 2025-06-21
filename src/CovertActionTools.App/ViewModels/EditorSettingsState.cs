using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.App.ViewModels;

public class EditorSettingsState : IViewModel
{
    private const int CurrentVersion = 1;
    private class FileData
    {
        public int Version { get; set; }
        public List<string> RecentlyOpenedProjects { get; set; } = new();
    }
    
    private static readonly JsonSerializerOptions JsonOptions = JsonSerializerOptions.Default;
    
    private readonly ILogger<EditorSettingsState> _logger;

    private FileData _fileData = new()
    {
        Version = CurrentVersion
    };
    
    public EditorSettingsState(ILogger<EditorSettingsState> logger)
    {
        _logger = logger;
        ReadFromFile();
    }

    public void AddRecentlyOpenedProject(string path)
    {
        var realPath = Path.GetFullPath(path);
        var index = _fileData.RecentlyOpenedProjects.IndexOf(realPath);
        if (index != -1)
        {
            _fileData.RecentlyOpenedProjects.RemoveAt(index);
        }
        _fileData.RecentlyOpenedProjects.Add(realPath);

        _fileData.Version = CurrentVersion;
        SaveToFile();
    }

    public IEnumerable<string> GetRecentlyOpenedProjects()
    {
        return _fileData.RecentlyOpenedProjects.ToList();
    }

    private void ReadFromFile()
    {
        var path = GetPath();
        try
        {
            if (!File.Exists(path))
            {
                return;
            }

            var rawData = File.ReadAllText(path);
            var parsedData = JsonSerializer.Deserialize<FileData>(rawData, JsonOptions);
            if (parsedData == null)
            {
                //delete the file
                File.Delete(path);
                _logger.LogWarning($"Unable to parse file, deleted and starting fresh: {path}");
                return;
            }

            if (parsedData.Version != CurrentVersion)
            {
                throw new Exception($"Unable to parse file version {parsedData.Version}, expecting {CurrentVersion}");
            }

            _fileData = parsedData;
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Exception while reading from file: {path}");
        }
    }

    private void SaveToFile()
    {
        var path = GetPath();
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            var serialisedData = JsonSerializer.Serialize(_fileData, JsonOptions);
            File.WriteAllText(path, serialisedData);
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Exception while writing to file: {path}");
        }
    }

    private string GetPath()
    {
        return Path.Combine(Path.GetTempPath(), "CovertActionTools.json");
    }
}