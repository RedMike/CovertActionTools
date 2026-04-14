using System.Collections.Generic;
using System.Linq;
using CovertActionTools.Core.Models.Executables.Records.Tac;
using CovertActionTools.Core.Models.Executables.Sections;

namespace CovertActionTools.Core.Models.Executables.Sections.Tac
{
    /// <summary>
    /// VGA palette remap region at DS:0x1BCE..0x1C23 (86 bytes). Contains 5 × 17-byte
    /// palette records. Word-alignment padding after the section is handled by
    /// <see cref="IPaddedToWord"/>.
    ///
    /// Consumer: <c>FUN_10e8_06f6(int index)</c> — a 23-byte thunk that computes
    /// <c>index * 0x11 + 0x1BCE</c> (0x11 = 17 = record stride) and forwards the
    /// resulting pointer to <c>FUN_1000_00ca</c> in CODE_0, which applies the palette.
    /// 12 call sites across FUN_10e8_0018, FUN_10e8_0bcb, FUN_10e8_225d, FUN_10e8_26b0,
    /// FUN_10e8_2d10, FUN_10e8_3081, and FUN_10e8_1b32 pass index values 0..4 — exactly
    /// the 5 record indices present in the region.
    /// </summary>
    public class VgaPaletteRemapSection : IExecutableSection, IPaddedToWord
    {
        public const int RecordCount = 5;
        public const int SectionSize = RecordCount * VgaPaletteRemapRecord.RecordSize;

        public List<VgaPaletteRemapRecord> Records { get; set; } = new();

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
            return offset - startingOffset;
        }

        public byte[] WriteBytes()
        {
            var parts = new List<byte>();
            foreach (var record in Records)
            {
                parts.AddRange(record.WriteBytes());
            }
            return parts.ToArray();
        }

        public VgaPaletteRemapSection Clone()
        {
            return new VgaPaletteRemapSection
            {
                Records = Records.Select(r => r.Clone()).ToList()
            };
        }
    }
}
