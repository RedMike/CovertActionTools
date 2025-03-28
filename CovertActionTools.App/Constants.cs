using System.Reflection;

namespace CovertActionTools.App;

public static class Constants
{
#if DEBUG
    //when running locally, just default to the known path with the original
    public static readonly string DefaultParseSourcePath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "./", "../../../../../Original/MPS/COVERT"));
#else
    public static readonly string DefaultParseSourcePath = Path.GetFullPath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "./");
#endif
    public static readonly string DefaultParseDestinationPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "./", "published"));
}