using System;
using CovertActionTools.Core.Models.Executables;
using CovertActionTools.Core.Models.Executables.Sections;

namespace CovertActionTools.Core.Models.Executables.Sections.Tac
{
    /// <summary>
    /// Two in-game menu strings at DS:0x1C46..0x1C76 followed by a one-byte
    /// word-alignment pad at DS:0x1C77 that word-aligns the direction-scancode table at
    /// DS:0x1C78 in <see cref="TacInputConfigSection"/>. Each string lives in a fixed
    /// byte slot; edits must produce an encoded byte sequence no longer than
    /// <c>slotSize - 1</c> (the final byte is reserved for the null terminator). The
    /// section throws from <see cref="WriteBytes"/> if an edit overflows its slot, and
    /// from <see cref="ReadBytes"/> if an input slot lacks a null terminator.
    ///
    ///   <see cref="PauseBanner"/> slot (8 bytes at 0x1C46): "PAUSED\n\0" in vanilla —
    ///     pointer-loaded by FUN_10e8_0f4c.
    ///   <see cref="QuitDialog"/> slot (41 bytes at 0x1C4E): "Are you sure\nyou want
    ///     to Quit?\n No\n Yes\n\0" in vanilla — pointer-loaded by FUN_10e8_0f4c.
    /// Word-alignment padding after the section is handled by <see cref="IPaddedToWord"/>.
    /// </summary>
    public class TacMenuStringsSection : IExecutableSection, IPaddedToWord
    {
        public const int PauseBannerSlotSize = 8;
        public const int QuitDialogSlotSize = 41;
        public const int SectionSize = PauseBannerSlotSize + QuitDialogSlotSize;

        public string PauseBanner { get; set; } = string.Empty;
        public string QuitDialog { get; set; } = string.Empty;

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
            PauseBanner = ReadFixedString(fullPayload, offset, PauseBannerSlotSize, nameof(PauseBanner));
            offset += PauseBannerSlotSize;
            QuitDialog = ReadFixedString(fullPayload, offset, QuitDialogSlotSize, nameof(QuitDialog));
            offset += QuitDialogSlotSize;
            return offset - startingOffset;
        }

        public byte[] WriteBytes()
        {
            var result = new byte[SectionSize];
            var offset = 0;
            WriteFixedString(result, offset, PauseBanner, PauseBannerSlotSize, nameof(PauseBanner));
            offset += PauseBannerSlotSize;
            WriteFixedString(result, offset, QuitDialog, QuitDialogSlotSize, nameof(QuitDialog));
            return result;
        }

        public TacMenuStringsSection Clone()
        {
            return new TacMenuStringsSection
            {
                PauseBanner = PauseBanner,
                QuitDialog = QuitDialog
            };
        }

        private static string ReadFixedString(byte[] data, int offset, int slotSize, string fieldName)
        {
            var end = offset;
            var slotEnd = offset + slotSize;
            while (end < slotEnd && data[end] != 0) end++;
            if (end >= slotEnd)
            {
                throw new InvalidOperationException(
                    $"{nameof(TacMenuStringsSection)}.{fieldName}: fixed slot at offset 0x{offset:X4} " +
                    $"(size {slotSize}) has no null terminator within its bounds.");
            }
            return DataSegmentHelper.DecodeControlString(data, offset, end - offset);
        }

        private static void WriteFixedString(byte[] dest, int offset, string value, int slotSize, string fieldName)
        {
            var encoded = DataSegmentHelper.EncodeControlString(value);
            var maxContent = slotSize - 1;
            if (encoded.Length > maxContent)
            {
                throw new InvalidOperationException(
                    $"{nameof(TacMenuStringsSection)}.{fieldName}: encoded length {encoded.Length} " +
                    $"exceeds the fixed slot's content capacity ({maxContent} bytes + 1 terminator). " +
                    "Shorten the edit — the original byte slot size cannot grow.");
            }
            Array.Copy(encoded, 0, dest, offset, encoded.Length);
            // Remaining bytes stay at 0 from the pre-zeroed destination buffer, which
            // both null-terminates the written string and zero-pads the slot tail.
        }
    }
}
