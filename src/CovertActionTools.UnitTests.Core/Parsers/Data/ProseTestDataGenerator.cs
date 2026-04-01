using System.IO;
using System.Text;

namespace CovertActionTools.UnitTests.Core.Parsers.Data;

public static class ProseTestDataGenerator
{
    #region File builders

    /// <summary>
    /// Builds a PROSE.DTA file with optional leading bytes before '*', then the given entries.
    /// The last entry should be an "end" marker built via BuildEndEntry.
    /// </summary>
    public static byte[] BuildProseFile(byte[][] entries, byte[]? leadingBytes = null)
    {
        using var ms = new MemoryStream();
        if (leadingBytes != null)
        {
            ms.Write(leadingBytes, 0, leadingBytes.Length);
        }
        ms.WriteByte((byte)'*');
        foreach (var entry in entries)
        {
            ms.Write(entry, 0, entry.Length);
        }
        return ms.ToArray();
    }

    /// <summary>
    /// Builds a prose entry: {prefix}\n{message}*
    /// The prefix is the type identifier (e.g. "advice1", "nice0", "lounge", "followed", etc.)
    /// </summary>
    public static byte[] BuildProseEntry(string prefix, string message)
    {
        using var ms = new MemoryStream();
        var prefixBytes = Encoding.ASCII.GetBytes(prefix);
        ms.Write(prefixBytes, 0, prefixBytes.Length);
        ms.WriteByte((byte)'\n');
        var msgBytes = Encoding.ASCII.GetBytes(message);
        ms.Write(msgBytes, 0, msgBytes.Length);
        ms.WriteByte((byte)'*');
        return ms.ToArray();
    }

    /// <summary>
    /// Builds the "end" marker: end\n
    /// </summary>
    public static byte[] BuildEndEntry()
    {
        return Encoding.ASCII.GetBytes("end\n");
    }

    #endregion

    #region Temp directory helpers

    public static string WriteProseFile(string tempDir, byte[] data)
    {
        var filePath = Path.Combine(tempDir, "PROSE.DTA");
        File.WriteAllBytes(filePath, data);
        return filePath;
    }

    #endregion
}
