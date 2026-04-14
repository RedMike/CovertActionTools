using System;
using CovertActionTools.Core.Models.Executables.Records.Shared;
using CovertActionTools.Core.Models.Executables.Sections;

namespace CovertActionTools.Core.Models.Executables.Sections.Tac
{
    /// <summary>
    /// Two action menus at DS:0x1C91..0x1CE3: the set-trap menu (3 options) and
    /// the arrest confirmation dialog (header string + 2-option menu).
    /// </summary>
    public class GameplayActionMenusSection : IExecutableSection
    {
        private const int SetTrapSize = 48;
        private const int ArrestHeaderSize = 23;
        private const int ArrestMenuSize = 12;
        public const int SectionSize = SetTrapSize + ArrestHeaderSize + ArrestMenuSize;

        public MenuStringRecord SetTrapMenuStrings { get; set; }
        public string ArrestHeaderString { get; set; } = "";
        public MenuStringRecord ArrestMenuStrings { get; set; }

        public GameplayActionMenusSection()
        {
            SetTrapMenuStrings = new MenuStringRecord(SetTrapSize, 3);
            ArrestMenuStrings = new MenuStringRecord(ArrestMenuSize, 2);
        }

        public bool Viewable()
        {
            return true;
        }

        public bool Editable()
        {
            return true;
        }

        public int ReadBytes(byte[] fullPayload, int startingOffset)
        {
            var offset = startingOffset;
            offset += SetTrapMenuStrings.ReadBytes(fullPayload, offset);
            var headerLen = DataSegmentHelper.DecodeControlStringWithNullableTerminator(
                fullPayload, offset, out var headerStr);
            ArrestHeaderString = headerStr;
            offset += headerLen;
            offset += ArrestMenuStrings.ReadBytes(fullPayload, offset);
            return offset - startingOffset;
        }

        public byte[] WriteBytes()
        {
            var headerEncoded = DataSegmentHelper.EncodeControlString(ArrestHeaderString);
            var headerSlot = new byte[ArrestHeaderSize];
            Array.Copy(headerEncoded, 0, headerSlot, 0, Math.Min(headerEncoded.Length, ArrestHeaderSize - 1));

            return DataSegmentHelper.Concatenate(
                SetTrapMenuStrings.WriteBytes(),
                headerSlot,
                ArrestMenuStrings.WriteBytes()
            );
        }

        public GameplayActionMenusSection Clone()
        {
            return new GameplayActionMenusSection
            {
                SetTrapMenuStrings = SetTrapMenuStrings.Clone(),
                ArrestHeaderString = ArrestHeaderString,
                ArrestMenuStrings = ArrestMenuStrings.Clone()
            };
        }
    }
}
