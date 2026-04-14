using System;
using System.Collections.Generic;
using CovertActionTools.Core.Models.Executables.Records;

namespace CovertActionTools.Core.Models.Executables.Records.Tac
{
    /// <summary>
    /// 17-byte palette remap record consumed by <c>FUN_1000_00ca</c>. The record lays
    /// out as 16 palette bytes followed by one trailing byte. Vanilla records are
    /// near-identity with index 5 aliased to 0 because TAC's break-in minigame has no
    /// disguise system, so the player-clothing slot is swapped out.
    ///
    /// The trailing <see cref="UnknownTrailerByte"/> (always 0 in vanilla) is part of
    /// the 17-byte record stride but its exact role in <c>FUN_1000_00ca</c> has not
    /// been conclusively identified — pending further investigation.
    /// </summary>
    public class VgaPaletteRemapRecord : IExecutableRecord
    {
        public const int RecordSize = 17;
        public const int PaletteLength = 16;

        public Dictionary<byte, byte> Palette { get; set; } = new Dictionary<byte, byte>();
        public byte UnknownTrailerByte { get; set; }

        public int ReadBytes(byte[] fullPayload, int startingOffset)
        {
            Palette = new Dictionary<byte, byte>(PaletteLength);
            for (var i = 0; i < PaletteLength; i++)
            {
                var value = fullPayload[startingOffset + i];
                ValidateColorValue(value, i);
                Palette[(byte)i] = value;
            }
            UnknownTrailerByte = fullPayload[startingOffset + PaletteLength];
            return RecordSize;
        }

        public byte[] WriteBytes()
        {
            var result = new byte[RecordSize];
            for (var i = 0; i < PaletteLength; i++)
            {
                var key = (byte)i;
                if (!Palette.TryGetValue(key, out var value))
                    throw new Exception($"VgaPaletteRemapRecord: missing entry for palette index {i}.");
                ValidateColorValue(value, i);
                result[i] = value;
            }
            result[PaletteLength] = UnknownTrailerByte;
            return result;
        }

        public VgaPaletteRemapRecord Clone()
        {
            var result = new VgaPaletteRemapRecord
            {
                UnknownTrailerByte = UnknownTrailerByte,
                Palette = new Dictionary<byte, byte>(PaletteLength)
            };
            foreach (var kvp in Palette)
            {
                result.Palette[kvp.Key] = kvp.Value;
            }
            return result;
        }

        private static void ValidateColorValue(byte value, int index)
        {
            if (value > 15)
                throw new Exception(
                    $"VgaPaletteRemapRecord: value {value} at palette index {index} is not a valid color (must be 0-15).");
        }
    }
}
