using System;
using System.Collections.Generic;
using System.Linq;
using CovertActionTools.Core.Models.Executables.Records.Tac;
using CovertActionTools.Core.Models.Executables.Sections;

namespace CovertActionTools.Core.Models.Executables.Sections.Tac
{
    /// <summary>
    /// VGA palette remap region at DS:0x1BCE..0x1C23 (86 bytes). Contains 5 × 17-byte
    /// palette records followed by a 1-byte word-alignment pad (the next field in the
    /// segment is a word variable at DS:0x1C24).
    ///
    /// Consumer: <c>FUN_10e8_06f6(int index)</c> — a 23-byte thunk that computes
    /// <c>index * 0x11 + 0x1BCE</c> (0x11 = 17 = record stride) and forwards the
    /// resulting pointer to <c>FUN_1000_00ca</c> in CODE_0, which applies the palette.
    /// 12 call sites across FUN_10e8_0018, FUN_10e8_0bcb, FUN_10e8_225d, FUN_10e8_26b0,
    /// FUN_10e8_2d10, FUN_10e8_3081, and FUN_10e8_1b32 pass index values 0..4 — exactly
    /// the 5 record indices present in the region. The specific game-UI context that
    /// triggers each index is not yet known; individual record semantics are TBD. This
    /// is why earlier scans targeted at hardcoded pointer loads came up empty: the
    /// reference is an immediate inside an <c>ADD AX, 0x1bce</c> arithmetic step, not a
    /// <c>MOV/PUSH/LEA</c> pointer load, so only an exhaustive scalar-operand scan
    /// catches it.
    ///
    /// Record stride is 17 bytes — the first 16 are the palette indices, the 17th
    /// is an unidentified trailing byte (see <see cref="VgaPaletteRemapRecord"/>). A
    /// one-byte word-alignment pad follows the 5 records at DS:0x1C23; it is kept
    /// inside the section's Read/Write plumbing and not surfaced as a public field.
    /// </summary>
    public class VgaPaletteRemapSection : IExecutableSection
    {
        public const int RecordCount = 5;
        private const int TrailingPaddingSize = 1;
        public const int SectionSize = RecordCount * VgaPaletteRemapRecord.RecordSize + TrailingPaddingSize;

        public List<VgaPaletteRemapRecord> Records { get; set; } = new();

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
            Records = new List<VgaPaletteRemapRecord>(RecordCount);
            var offset = startingOffset;
            for (var i = 0; i < RecordCount; i++)
            {
                var record = new VgaPaletteRemapRecord();
                offset += record.ReadBytes(fullPayload, offset);
                Records.Add(record);
            }
            _trailingAlignmentPadding = fullPayload[offset];
            offset += TrailingPaddingSize;
            return offset - startingOffset;
        }

        public byte[] WriteBytes()
        {
            var result = new byte[SectionSize];
            var offset = 0;
            foreach (var record in Records)
            {
                var bytes = record.WriteBytes();
                Array.Copy(bytes, 0, result, offset, bytes.Length);
                offset += bytes.Length;
            }
            result[offset] = _trailingAlignmentPadding;
            return result;
        }

        public VgaPaletteRemapSection Clone()
        {
            return new VgaPaletteRemapSection
            {
                Records = Records.Select(r => r.Clone()).ToList(),
                _trailingAlignmentPadding = _trailingAlignmentPadding
            };
        }
    }
}
