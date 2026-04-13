using System;
using CovertActionTools.Core.Models.Executables.Sections;

namespace CovertActionTools.Core.Models.Executables.Sections.Tac
{
    /// <summary>
    /// Single word variable at DS:0x1C24 that FUN_10e8_0018 overwrites at startup with
    /// the return value of <c>thunk_EXT_FUN_0000_0000(0x10e8, 3)</c> (overlay stub 3,
    /// "pixel color swap"). Held in its own section because the value is pure runtime
    /// state and the EXE's stored bytes are not meaningful to view or edit.
    /// </summary>
    public class TacStubOutputCaptureSection : IExecutableSection
    {
        public const int SectionSize = 2;

        private ushort Value { get; set; }

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

        public TacStubOutputCaptureSection Clone()
        {
            return new TacStubOutputCaptureSection { Value = Value };
        }
    }
}
