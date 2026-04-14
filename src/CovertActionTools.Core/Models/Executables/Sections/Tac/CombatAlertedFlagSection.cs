using System;
using CovertActionTools.Core.Models.Executables.Sections;

namespace CovertActionTools.Core.Models.Executables.Sections.Tac
{
    /// <summary>
    /// Single word variable at DS:0x1C8A toggled by FUN_10e8_1b32 via
    /// <c>MOV word ptr [0x1c8a], 0</c> / <c>MOV word ptr [0x1c8a], 1</c>
    /// and gated on with <c>CMP word ptr [0x1c8a], 0</c>. Pure runtime
    /// state — the EXE's stored bytes are not meaningful to view or edit.
    /// </summary>
    public class CombatAlertedFlagSection : IExecutableSection
    {
        public const int SectionSize = 2;

        public ushort Value { get; set; }

        public bool Viewable()
        {
            return false;
        }

        public bool Editable()
        {
            return false;
        }

        public int ReadBytes(byte[] fullPayload, int startingOffset)
        {
            Value = BitConverter.ToUInt16(fullPayload, startingOffset);
            return SectionSize;
        }

        public byte[] WriteBytes()
        {
            return new[]
            {
                (byte)(Value & 0xFF),
                (byte)((Value >> 8) & 0xFF)
            };
        }

        public CombatAlertedFlagSection Clone()
        {
            return new CombatAlertedFlagSection { Value = Value };
        }
    }
}
