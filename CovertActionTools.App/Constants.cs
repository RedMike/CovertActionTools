using System.Reflection;

namespace CovertActionTools.App;

public static class Constants
{
    public static readonly string DefaultParseSourcePath = Path.GetFullPath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "./");
    public static readonly string DefaultPublishPath = Path.GetFullPath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "./");
    public static readonly string DefaultParseDestinationPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "./", "published"));
}