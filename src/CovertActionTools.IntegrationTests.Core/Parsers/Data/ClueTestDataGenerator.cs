using System.IO;
using System.Text;

namespace CovertActionTools.IntegrationTests.Core.Parsers.Data;

public static class ClueTestDataGenerator
{
    #region File builders

    /// <summary>
    /// Builds a CLUES.TXT file with the given entries.
    /// The file starts with '*' and ends with '\r\n\x1A' end marker.
    /// </summary>
    public static byte[] BuildCluesFile(params byte[][] entries)
    {
        using var ms = new MemoryStream();
        ms.WriteByte((byte)'*');
        foreach (var entry in entries)
        {
            ms.Write(entry, 0, entry.Length);
        }
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
        ms.WriteByte((byte)'\r');
        ms.WriteByte((byte)'\n');
        ms.WriteByte((byte)(source + '0'));
        ms.WriteByte((byte)(type + '0'));
        var msgBytes = Encoding.ASCII.GetBytes(message);
        ms.Write(msgBytes, 0, msgBytes.Length);
        ms.WriteByte((byte)'*');
        return ms.ToArray();
    }

    #endregion
}
