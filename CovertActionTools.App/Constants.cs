using System.Reflection;

namespace CovertActionTools.App;

public static class Constants
{
#if DEBUG
    //when running locally, just default to the known path with the original
    public static readonly string DefaultParseSourcePath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "./", "../../../../../Original/MPS/COVERT"));
    public static readonly string DefaultPublishPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "./", "../../../../../Original - Copy (2)/MPS/COVERT"));
    public static readonly string DefaultRunPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "./", "../../../../../Original - Copy (2)/dosbox_windows/start.bat"));
#else
    public static readonly string DefaultParseSourcePath = Path.GetFullPath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "./");
    public static readonly string DefaultPublishPath = Path.GetFullPath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "./");
    public static readonly string DefaultRunPath = "";
#endif
    public static readonly string DefaultParseDestinationPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "./", "published"));
}