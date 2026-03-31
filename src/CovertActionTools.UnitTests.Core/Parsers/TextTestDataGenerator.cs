using System.IO;
using System.Text;

namespace CovertActionTools.UnitTests.Core.Parsers;

public static class TextTestDataGenerator
{
    #region File builders

    /// <summary>
    /// Builds a TEXT.DTA file with the given entries.
    /// The file starts with '*' and the caller must include an END entry as the last entry.
    /// </summary>
    public static byte[] BuildTextFile(params byte[][] entries)
    {
        using var ms = new MemoryStream();
        ms.WriteByte((byte)'*');
        foreach (var entry in entries)
        {
            ms.Write(entry, 0, entry.Length);
        }
        return ms.ToArray();
    }

    /// <summary>
    /// Builds a non-MSG entry: {prefix3}{fourthChar}{id:2}{message}*
    /// prefix3 is e.g. "SOR", "ROR", "SLO", "RLO", "FLU", "ALR", "AID"
    /// fourthChar is the 4th byte of the prefix (e.g. 'G' for SORG)
    /// </summary>
    public static byte[] BuildNonMsgEntry(string prefix3, char fourthChar, int id, string message)
    {
        using var ms = new MemoryStream();
        var header = $"{prefix3}{fourthChar}";
        ms.Write(Encoding.ASCII.GetBytes(header), 0, header.Length);
        var idStr = $"{id:D2}";
        ms.Write(Encoding.ASCII.GetBytes(idStr), 0, idStr.Length);
        var msgBytes = Encoding.ASCII.GetBytes(message);
        ms.Write(msgBytes, 0, msgBytes.Length);
        ms.WriteByte((byte)'*');
        return ms.ToArray();
    }

    /// <summary>
    /// Builds a MSG entry: MSG{crimeIdChar1}{crimeIdChar2}{id:2}{message}*
    /// The parser reads 4 chars ("MSGx") then 1 more char, so crimeId is parsed from those 2 chars.
    /// </summary>
    public static byte[] BuildMsgEntry(int crimeId, int id, string message)
    {
        using var ms = new MemoryStream();
        var crimeIdStr = $"{crimeId:D2}";
        // First 4 chars: MSG + first digit of crimeId
        var prefix = $"MSG{crimeIdStr[0]}";
        ms.Write(Encoding.ASCII.GetBytes(prefix), 0, prefix.Length);
        // 5th char: second digit of crimeId (read separately by parser)
        ms.WriteByte((byte)crimeIdStr[1]);
        var idStr = $"{id:D2}";
        ms.Write(Encoding.ASCII.GetBytes(idStr), 0, idStr.Length);
        var msgBytes = Encoding.ASCII.GetBytes(message);
        ms.Write(msgBytes, 0, msgBytes.Length);
        ms.WriteByte((byte)'*');
        return ms.ToArray();
    }

    /// <summary>
    /// Builds the END marker entry (4 chars: "END" + any char).
    /// </summary>
    public static byte[] BuildEndEntry()
    {
        return Encoding.ASCII.GetBytes("END\0");
    }

    #endregion

    #region Temp directory helpers

    public static string WriteTextFile(string tempDir, byte[] data)
    {
        var filePath = Path.Combine(tempDir, "TEXT.DTA");
        File.WriteAllBytes(filePath, data);
        return filePath;
    }

    #endregion
}
