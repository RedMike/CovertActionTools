using System;
using System.IO;
using System.Text;

namespace CovertActionTools.IntegrationTests.Core.Parsers.Data;

/// <summary>
/// Builds valid WORLD*.DTA binary data for integration tests.
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
        writer.Write((ushort)0x0000);
        writer.Write((ushort)0x0000);
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
    /// Builds a world file with 3 cities and 2 organisations.
    /// Used as the standard snapshot test case.
    /// </summary>
    public static byte[] BuildStandardWorldFile()
    {
        var cities = new[]
        {
            BuildCity(name: "London", country: "England", mapX: 128, mapY: 45,
                unknown1: 0x0100, unknown2: 0x0200),
            BuildCity(name: "Paris", country: "France", mapX: 132, mapY: 50),
            BuildCity(name: "Berlin", country: "Germany", mapX: 140, mapY: 48,
                unknown1: 0x0300, unknown2: 0x0400)
        };

        var organisations = new[]
        {
            BuildOrganisation(shortName: "MI6", longName: "Secret Intel Svc",
                uniqueId: 10, unknown1: 0x0100),
            BuildOrganisation(shortName: "KGB", longName: "Committee State",
                uniqueId: 20, unknown2: 0x0500)
        };

        return BuildWorldFile(cities, organisations);
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
