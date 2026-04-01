using System.IO;
using System.Text;

namespace CovertActionTools.UnitTests.Core.Parsers.Data;

public static class ClueTestDataGenerator
{
    #region File builders

    /// <summary>
    /// Builds a CLUES.TXT file with the given entries.
    /// Each entry is already formatted as raw bytes (without the leading '*' or trailing end marker).
    /// The file starts with '*' and ends with '\r\n\x1A'.
    /// </summary>
    public static byte[] BuildCluesFile(params byte[][] entries)
    {
        using var ms = new MemoryStream();
        ms.WriteByte((byte)'*');
        foreach (var entry in entries)
        {
            ms.Write(entry, 0, entry.Length);
        }
        // End marker: \r\n\x1A + 2 padding bytes (the parser reads 5 bytes for prefix)
        ms.WriteByte((byte)'\r');
        ms.WriteByte((byte)'\n');
        ms.WriteByte(0x1A);
        ms.WriteByte(0x00);
        ms.WriteByte(0x00);
        return ms.ToArray();
    }

    /// <summary>
    /// Builds a non-crime clue entry: C{type}{id}\r\n{source}{message}*
    /// </summary>
    public static byte[] BuildNonCrimeClueEntry(int type, int id, int source, string message)
    {
        using var ms = new MemoryStream();
        var prefix = $"C{type}{id}\r\n";
        ms.Write(Encoding.ASCII.GetBytes(prefix), 0, prefix.Length);
        ms.WriteByte((byte)(source + '0'));
        var msgBytes = Encoding.ASCII.GetBytes(message);
        ms.Write(msgBytes, 0, msgBytes.Length);
        ms.WriteByte((byte)'*');
        return ms.ToArray();
    }

    /// <summary>
    /// Builds a crime-specific clue entry: C{crimeId:2}{participantId:2}{whitespace}{source}{type}{message}*
    /// </summary>
    public static byte[] BuildCrimeClueEntry(int crimeId, int participantId, int source, int type, string message)
    {
        using var ms = new MemoryStream();
        var prefix = $"C{crimeId:D2}{participantId:D2}";
        ms.Write(Encoding.ASCII.GetBytes(prefix), 0, prefix.Length);
        // Whitespace separator
        ms.WriteByte((byte)'\r');
        ms.WriteByte((byte)'\n');
        ms.WriteByte((byte)(source + '0'));
        ms.WriteByte((byte)(type + '0'));
        var msgBytes = Encoding.ASCII.GetBytes(message);
        ms.Write(msgBytes, 0, msgBytes.Length);
        ms.WriteByte((byte)'*');
        return ms.ToArray();
    }

    /// <summary>
    /// Builds a duplicate crime-specific clue entry: C{crimeId:2}{participantId:2}{whitespace}*
    /// (next char after whitespace is '*' indicating duplicate)
    /// </summary>
    public static byte[] BuildDuplicateCrimeClueEntry(int crimeId, int participantId)
    {
        using var ms = new MemoryStream();
        var prefix = $"C{crimeId:D2}{participantId:D2}";
        ms.Write(Encoding.ASCII.GetBytes(prefix), 0, prefix.Length);
        ms.WriteByte((byte)'\r');
        ms.WriteByte((byte)'\n');
        ms.WriteByte((byte)'*');
        return ms.ToArray();
    }

    #endregion

    #region Temp directory helpers

    public static string WriteCluesFile(string tempDir, byte[] data)
    {
        var filePath = Path.Combine(tempDir, "CLUES.TXT");
        File.WriteAllBytes(filePath, data);
        return filePath;
    }

    #endregion
}
