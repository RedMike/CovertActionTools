using System;
using System.IO;
using System.Text;

namespace CovertActionTools.UnitTests.Core.Parsers.Data;

/// <summary>
/// Builds valid WORLD*.DTA binary data for unit tests.
/// </summary>
public static class WorldTestDataGenerator
{
    #region City helpers

    public static byte[] BuildCity(
        string name = "TestCity",
        string country = "TestCountry",
        ushort unknown1 = 0,
        ushort unknown2 = 0,
        byte mapX = 100,
        byte mapY = 50)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        WriteFixedString(writer, name, 12);
        WriteFixedString(writer, country, 12);
        writer.Write(unknown1);
        writer.Write(unknown2);
        writer.Write((ushort)0x0000); // must be zero
        writer.Write((ushort)0x0000); // must be zero
        writer.Write(mapX);
        writer.Write(mapY);

        return ms.ToArray();
    }

    #endregion

    #region Organisation helpers

    public static byte[] BuildOrganisation(
        string shortName = "TST",
        string longName = "Test Org",
        ushort unknown1 = 0,
        ushort unknown2 = 0,
        ushort unknown3 = 0,
        ushort uniqueId = 1,
        ushort unknown4 = 0)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        WriteFixedString(writer, shortName, 6);
        WriteFixedString(writer, longName, 20);
        writer.Write(unknown1);
        writer.Write(unknown2);
        writer.Write(unknown3);
        writer.Write(uniqueId);
        writer.Write(unknown4);

        return ms.ToArray();
    }

    #endregion

    #region Full file helpers

    /// <summary>
    /// Builds a complete WORLD DTA file with the given components.
    /// </summary>
    public static byte[] BuildWorldFile(byte[][] cities, byte[][] organisations)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write((ushort)cities.Length);
        writer.Write((ushort)organisations.Length);

        foreach (var c in cities)
        {
            writer.Write(c);
        }

        foreach (var o in organisations)
        {
            writer.Write(o);
        }

        return ms.ToArray();
    }

    /// <summary>
    /// Builds a minimal world file with one city and one organisation.
    /// </summary>
    public static byte[] BuildMinimalWorldFile()
    {
        return BuildWorldFile(
            new[] { BuildCity() },
            new[] { BuildOrganisation() });
    }

    #endregion

    #region Private helpers

    private static void WriteFixedString(BinaryWriter writer, string value, int length)
    {
        var bytes = new byte[length];
        var encoded = Encoding.ASCII.GetBytes(value);
        Array.Copy(encoded, bytes, Math.Min(encoded.Length, length));
        writer.Write(bytes);
    }

    #endregion
}
