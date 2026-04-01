using System;
using System.IO;
using System.Text;

namespace CovertActionTools.UnitTests.Core.Parsers.Data;

/// <summary>
/// Builds valid CRIME*.DTA binary data for unit tests.
/// </summary>
public static class CrimeTestDataGenerator
{
    #region Participant helpers

    public static byte[] BuildParticipant(
        ushort exposure = 100,
        string role = "TestRole",
        ushort unknown1 = 1,
        byte unknown2 = 0x48,
        byte participantType = 0,
        ushort unknown3 = 0,
        byte clueType = 0,
        ushort rank = 1,
        ushort unknown4 = 0x0600,
        byte unknown5 = 0)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write((ushort)0xFFFF);
        writer.Write(exposure);
        WriteFixedString(writer, role, 32);
        writer.Write(unknown1);
        writer.Write(unknown2);
        writer.Write(participantType);
        writer.Write(unknown3);
        writer.Write(clueType);
        writer.Write(rank);
        writer.Write(unknown4);
        writer.Write(unknown5);

        return ms.ToArray();
    }

    #endregion

    #region Event helpers

    /// <summary>
    /// Builds an individual event (low nibble 0, no pairing needed).
    /// </summary>
    public static byte[] BuildIndividualEvent(
        ushort sourceParticipantId = 0,
        ushort messageId = 1,
        string description = "Test event",
        byte receivedObjectBitmask = 0,
        byte destroyedObjectBitmask = 0,
        ushort score = 0,
        byte eventTypeByte = 0)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write(sourceParticipantId);
        writer.Write((ushort)0x0000);
        writer.Write(messageId);
        WriteFixedString(writer, description, 32);
        writer.Write((byte)0); // targetParticipant = 0 means null
        writer.Write(eventTypeByte);
        writer.Write(receivedObjectBitmask);
        writer.Write(destroyedObjectBitmask);
        writer.Write(score);

        return ms.ToArray();
    }

    /// <summary>
    /// Builds a paired send event (e.g. SentMessage = 2).
    /// </summary>
    public static byte[] BuildPairedSendEvent(
        ushort sourceParticipantId,
        ushort messageId,
        string description,
        byte targetParticipantId,
        byte eventTypeByte,
        byte receivedObjectBitmask = 0,
        byte destroyedObjectBitmask = 0,
        ushort score = 0)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write(sourceParticipantId);
        writer.Write((ushort)0x0000);
        writer.Write(messageId);
        WriteFixedString(writer, description, 32);
        writer.Write(targetParticipantId);
        writer.Write(eventTypeByte);
        writer.Write(receivedObjectBitmask);
        writer.Write(destroyedObjectBitmask);
        writer.Write(score);

        return ms.ToArray();
    }

    /// <summary>
    /// Builds an ignored event (sourceParticipantId = 0xFF).
    /// </summary>
    public static byte[] BuildIgnoredEvent(
        ushort messageId = 0,
        string description = "Ignored")
    {
        return BuildIndividualEvent(
            sourceParticipantId: 0x00FF,
            messageId: messageId,
            description: description);
    }

    #endregion

    #region Object helpers

    /// <summary>
    /// Builds a real object (pictureId != 0xFF).
    /// </summary>
    public static byte[] BuildObject(string name = "TestItem", byte pictureId = 0)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        WriteFixedString(writer, name, 16);
        writer.Write(pictureId);
        writer.Write((byte)0xFF); // validation byte

        return ms.ToArray();
    }

    /// <summary>
    /// Builds a blank/skip object (pictureId = 0xFF).
    /// </summary>
    public static byte[] BuildBlankObject()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        WriteFixedString(writer, "", 16);
        writer.Write((byte)0xFF); // pictureId = skip
        writer.Write((byte)0xFF); // extra byte read after skip

        return ms.ToArray();
    }

    #endregion

    #region Full file helpers

    /// <summary>
    /// Builds a complete CRIME DTA file with the given components.
    /// </summary>
    public static byte[] BuildCrimeFile(
        byte[][] participants,
        byte[][] events,
        byte[][]? objects = null)
    {
        // Default to 4 blank objects if not specified
        if (objects == null)
        {
            objects = new byte[4][];
            for (var i = 0; i < 4; i++)
            {
                objects[i] = BuildBlankObject();
            }
        }

        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write((ushort)participants.Length);
        writer.Write((ushort)events.Length);

        foreach (var p in participants)
        {
            writer.Write(p);
        }

        foreach (var e in events)
        {
            writer.Write(e);
        }

        // Always exactly 4 objects
        for (var i = 0; i < 4; i++)
        {
            writer.Write(objects[i]);
        }

        return ms.ToArray();
    }

    /// <summary>
    /// Builds a minimal crime file with one participant, zero events, and 4 blank objects.
    /// </summary>
    public static byte[] BuildMinimalCrimeFile()
    {
        return BuildCrimeFile(
            new[] { BuildParticipant() },
            Array.Empty<byte[]>());
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
