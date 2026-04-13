using System;
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

        public byte[] Palette { get; set; } = new byte[PaletteLength];
        public byte UnknownTrailerByte { get; set; }

        public int ReadBytes(byte[] fullPayload, int startingOffset)
        {
            Array.Copy(fullPayload, startingOffset, Palette, 0, PaletteLength);
            UnknownTrailerByte = fullPayload[startingOffset + PaletteLength];
            return RecordSize;
        }

        public byte[] WriteBytes()
        {
            var result = new byte[RecordSize];
            Array.Copy(Palette, 0, result, 0, PaletteLength);
            result[PaletteLength] = UnknownTrailerByte;
            return result;
        }

        public VgaPaletteRemapRecord Clone()
        {
            var result = new VgaPaletteRemapRecord
            {
                UnknownTrailerByte = UnknownTrailerByte,
                Palette = new byte[PaletteLength]
            };
            Array.Copy(Palette, result.Palette, PaletteLength);
            return result;
        }
    }
}
