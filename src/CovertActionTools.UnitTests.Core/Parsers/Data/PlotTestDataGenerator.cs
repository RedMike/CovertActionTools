using System.IO;
using System.Text;

namespace CovertActionTools.UnitTests.Core.Parsers.Data;

public static class PlotTestDataGenerator
{
    #region File builders

    /// <summary>
    /// Builds a PLOT.TXT file with the given entries.
    /// The file starts with '*'. The last entry should use 0x1A as terminator.
    /// </summary>
    public static byte[] BuildPlotFile(params byte[][] entries)
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
    /// Builds a plot entry: PL{missionSetId:2}{crimeIndex}{messageNumberHex}{message}{terminator}
    /// terminator is '*' for non-last entries or 0x1A for the last entry.
    /// </summary>
    public static byte[] BuildPlotEntry(int missionSetId, int crimeIndex, int messageNumber, string message, bool isLast = false)
    {
        using var ms = new MemoryStream();
        var hexChar = messageNumber.ToString("X1");
        var prefix = $"PL{missionSetId:D2}{crimeIndex}{hexChar}";
        ms.Write(Encoding.ASCII.GetBytes(prefix), 0, prefix.Length);
        var msgBytes = Encoding.ASCII.GetBytes(message);
        ms.Write(msgBytes, 0, msgBytes.Length);
        ms.WriteByte(isLast ? (byte)0x1A : (byte)'*');
        return ms.ToArray();
    }

    #endregion

    #region Temp directory helpers

    public static string WritePlotFile(string tempDir, byte[] data)
    {
        var filePath = Path.Combine(tempDir, "PLOT.TXT");
        File.WriteAllBytes(filePath, data);
        return filePath;
    }

    #endregion
}
