using System;
using System.IO;
using System.Text;

namespace CovertActionTools.IntegrationTests.Core.Parsers;

/// <summary>
/// Builds valid CRIME*.DTA binary data for integration tests.
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
        writer.Write((byte)0);
        writer.Write(eventTypeByte);
        writer.Write(receivedObjectBitmask);
        writer.Write(destroyedObjectBitmask);
        writer.Write(score);

        return ms.ToArray();
    }

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

    #endregion

    #region Object helpers

    public static byte[] BuildObject(string name = "TestItem", byte pictureId = 0)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        WriteFixedString(writer, name, 16);
        writer.Write(pictureId);
        writer.Write((byte)0xFF);

        return ms.ToArray();
    }

    public static byte[] BuildBlankObject()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        WriteFixedString(writer, "", 16);
        writer.Write((byte)0xFF);
        writer.Write((byte)0xFF);

        return ms.ToArray();
    }

    #endregion

    #region Full file helpers

    public static byte[] BuildCrimeFile(
        byte[][] participants,
        byte[][] events,
        byte[][]? objects = null)
    {
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

        for (var i = 0; i < 4; i++)
        {
            writer.Write(objects[i]);
        }

        return ms.ToArray();
    }

    /// <summary>
    /// Builds a crime file with 2 participants, 1 individual event, 1 paired message, and 2 objects.
    /// Used as the standard snapshot test case.
    /// </summary>
    public static byte[] BuildStandardCrimeFile()
    {
        var participants = new[]
        {
            BuildParticipant(exposure: 50, role: "Mastermind", participantType: 1,
                unknown1: 1, unknown2: 0x00, clueType: 2, rank: 3, unknown4: 0x0600),
            BuildParticipant(exposure: 80, role: "Courier", participantType: 0,
                unknown1: 1, unknown2: 0x48, clueType: 5, rank: 1, unknown4: 0x0600)
        };

        var events = new[]
        {
            BuildIndividualEvent(sourceParticipantId: 0, messageId: 1,
                description: "Initiated the plan", score: 100, eventTypeByte: 0x20),
            BuildPairedSendEvent(sourceParticipantId: 0, messageId: 5,
                description: "Sent orders", targetParticipantId: 1, eventTypeByte: 2),
            BuildPairedSendEvent(sourceParticipantId: 1, messageId: 5,
                description: "Received orders", targetParticipantId: 0, eventTypeByte: 3)
        };

        var objects = new[]
        {
            BuildObject("Secret Plans", 3),
            BuildObject("Ransom Money", 7),
            BuildBlankObject(),
            BuildBlankObject()
        };

        return BuildCrimeFile(participants, events, objects);
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
