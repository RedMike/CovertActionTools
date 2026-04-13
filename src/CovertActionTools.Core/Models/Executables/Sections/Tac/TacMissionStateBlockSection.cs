using System;
using CovertActionTools.Core.Models.Executables;
using CovertActionTools.Core.Models.Executables.Sections;

namespace CovertActionTools.Core.Models.Executables.Sections.Tac
{
    /// <summary>
    /// 28-byte block at DS:0x1C26..0x1C41 holding the door-picker dialog strings and an
    /// intentional empty-string slot that precedes them. All three door strings are
    /// stored in fixed byte slots; edits must encode to no more than
    /// <c>slotSize - 1</c> bytes (the final byte is reserved for the null terminator).
    /// A trailing one-byte word-alignment pad at DS:0x1C41 is kept inside the section's
    /// Read/Write plumbing and not surfaced as a public field.
    ///
    ///   - 0x1C26 <see cref="EmptyStringSlot1"/> (byte): an intentional empty-string
    ///     data slot. FUN_10e8_070e:0923 issues <c>MOV AX, 0x1c26</c> and passes the
    ///     address to FUN_10e8_f0f6 as a string pointer; dereferencing finds an
    ///     immediate null, yielding an empty string.
    ///   - 0x1C27 <see cref="DoorPromptHeader"/>: "Which door?  \n " — pushed into the
    ///     message buffer 0x8dda by FUN_10e8_070e:90 via <c>FUN_10e8_f0f6</c>.
    ///   - 0x1C37 <see cref="DoorLabel"/>: "Door #" — appended per-door in the
    ///     FUN_10e8_070e door-enumeration loop.
    ///   - 0x1C3E <see cref="DoorSeparator"/>: "\n " — trailing newline + space between
    ///     door entries in the same loop.
    /// </summary>
    public class TacMissionStateBlockSection : IExecutableSection
    {
        public const int EmptyStringSlot1Size = 1;
        public const int DoorPromptHeaderSlotSize = 16;
        public const int DoorLabelSlotSize = 7;
        public const int DoorSeparatorSlotSize = 3;
        private const int TrailingAlignmentPaddingSize = 1;
        public const int SectionSize =
            EmptyStringSlot1Size +
            DoorPromptHeaderSlotSize +
            DoorLabelSlotSize +
            DoorSeparatorSlotSize +
            TrailingAlignmentPaddingSize;

        public byte EmptyStringSlot1 { get; set; }
        public string DoorPromptHeader { get; set; } = string.Empty;
        public string DoorLabel { get; set; } = string.Empty;
        public string DoorSeparator { get; set; } = string.Empty;

        private byte _trailingAlignmentPadding;

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
            EmptyStringSlot1 = fullPayload[offset];
            offset += EmptyStringSlot1Size;
            DoorPromptHeader = ReadFixedString(fullPayload, offset, DoorPromptHeaderSlotSize, nameof(DoorPromptHeader));
            offset += DoorPromptHeaderSlotSize;
            DoorLabel = ReadFixedString(fullPayload, offset, DoorLabelSlotSize, nameof(DoorLabel));
            offset += DoorLabelSlotSize;
            DoorSeparator = ReadFixedString(fullPayload, offset, DoorSeparatorSlotSize, nameof(DoorSeparator));
            offset += DoorSeparatorSlotSize;
            _trailingAlignmentPadding = fullPayload[offset];
            offset += TrailingAlignmentPaddingSize;
            return offset - startingOffset;
        }

        public byte[] WriteBytes()
        {
            var result = new byte[SectionSize];
            var offset = 0;
            result[offset] = EmptyStringSlot1;
            offset += EmptyStringSlot1Size;
            WriteFixedString(result, offset, DoorPromptHeader, DoorPromptHeaderSlotSize, nameof(DoorPromptHeader));
            offset += DoorPromptHeaderSlotSize;
            WriteFixedString(result, offset, DoorLabel, DoorLabelSlotSize, nameof(DoorLabel));
            offset += DoorLabelSlotSize;
            WriteFixedString(result, offset, DoorSeparator, DoorSeparatorSlotSize, nameof(DoorSeparator));
            offset += DoorSeparatorSlotSize;
            result[offset] = _trailingAlignmentPadding;
            return result;
        }

        public TacMissionStateBlockSection Clone()
        {
            return new TacMissionStateBlockSection
            {
                EmptyStringSlot1 = EmptyStringSlot1,
                DoorPromptHeader = DoorPromptHeader,
                DoorLabel = DoorLabel,
                DoorSeparator = DoorSeparator,
                _trailingAlignmentPadding = _trailingAlignmentPadding
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
                    $"{nameof(TacMissionStateBlockSection)}.{fieldName}: fixed slot at offset 0x{offset:X4} " +
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
                    $"{nameof(TacMissionStateBlockSection)}.{fieldName}: encoded length {encoded.Length} " +
                    $"exceeds the fixed slot's content capacity ({maxContent} bytes + 1 terminator). " +
                    "Shorten the edit — the original byte slot size cannot grow.");
            }
            Array.Copy(encoded, 0, dest, offset, encoded.Length);
            // Remaining bytes are already 0 from the pre-zeroed result buffer, which
            // both null-terminates the written string and zero-pads the slot tail.
        }
    }
}
