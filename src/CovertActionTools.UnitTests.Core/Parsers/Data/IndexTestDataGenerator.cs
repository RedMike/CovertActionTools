using System.IO;

namespace CovertActionTools.UnitTests.Core.Parsers.Data;

internal static class IndexTestDataGenerator
{
    /// <summary>
    /// Creates an empty COVERT.EXE file in the given directory.
    /// The LegacyIndexParser only checks for the file's existence.
    /// </summary>
    public static void CreateCovertExe(string directory)
    {
        File.WriteAllBytes(Path.Combine(directory, "COVERT.EXE"), new byte[0]);
    }
}
