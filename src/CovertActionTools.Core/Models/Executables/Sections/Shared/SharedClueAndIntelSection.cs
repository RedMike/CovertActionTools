using System;
using CovertActionTools.Core.Models.Executables.Records.Shared;

namespace CovertActionTools.Core.Models.Executables.Sections.Shared
{
    /// <summary>
    /// Composite section that bundles the four clue/intel string tables, the 40-entry clue
    /// phrase pointer table, an opaque 16-byte block whose reader has not been identified,
    /// the 32-byte popcount lookup table, and the 12-entry month pointer table. Layout
    /// (TAC DS):
    ///   0x226C phrases
    ///   0x248C month abbreviations
    ///   0x24BC intel headers
    ///   0x251E intel phrases
    ///   0x2542 phrase pointer table (80 bytes, 40 entries)
    ///   0x2592 UnknownClueData (16 bytes)
    ///   0x25A2 CluePopcountTables (32 bytes, 4 sub-tables of 8)
    ///   0x25C2 month pointer table (24 bytes, 12 entries, regenerated on write)
    ///   0x25DA end
    /// GAME/FINAL/BUG carry the same structure at different DS bases.
    /// </summary>
    public class SharedClueAndIntelSection : IExecutableSection
    {
        /// <summary>
        /// DS-relative offset of the first clue relationship phrase. Used as the base for
        /// the generated phrase pointer table; defaults to TAC's layout and can be set by
        /// the owning data segment if reused by another EXE with a different location.
        /// </summary>
        public int PhraseBaseOffset { get; set; } = 0x226C;

        /// <summary>
        /// DS-relative offset of the first month abbreviation. Used as the base for the
        /// generated month pointer table; defaults to TAC's layout and can be set by the
        /// owning data segment for other EXEs.
        /// </summary>
        public int MonthBaseOffset { get; set; } = 0x248C;

        public ClueRelationshipPhrasesSection ClueRelationshipPhrases { get; set; } = new();
        public MonthAbbreviationsSection MonthAbbreviations { get; set; } = new();
        public IntelHeadersSection IntelHeaders { get; set; } = new();
        public IntelPhrasesSection IntelPhrases { get; set; } = new();

        // TODO: UnknownClueData --  16 opaque bytes whose reader we have not been able to
        // identify. On-disk values look like per-clue-pair bit flags (1/2/4/8 single-bit
        // masks arranged as 8 pairs), but exhaustive raw-byte scans of every plausible
        // instruction encoding -- direct `[disp16]`, register-immediate `MOV reg,imm16`,
        // `PUSH imm16`, stored pointer words, segment-prefixed variants, far-pointer LDS/
        // LES target data, and indexed `[reg+disp16]` with the literal displacement in
        // range -- turn up no reference in TAC, GAME, FINAL, or BUG. The block is present
        // only in the four EXEs that can show a clue screen (absent from CHASE and CODE),
        // so it almost certainly does get read; we just have not located the access path
        // yet. Possible remaining access paths (none yet confirmed): overlay code running
        // under the host EXE's DS, runtime-computed pointer arithmetic from a nearby base
        // (0x2542 or 0x25A2), or a runtime REP MOVS copy to a BSS buffer that is then
        // read elsewhere. Preserved verbatim so a future investigation can pick it up.
        /// <summary>Opaque 16-byte block following the phrase pointer table. See TODO
        /// above for the investigation state.</summary>
        public BlobRecord UnknownClueData { get; set; } = new(UnknownClueDataSize);

        /// <summary>32-byte popcount lookup table. 4 sub-tables of 8 bytes each:
        /// popcount(0..7)+0, popcount(0..7)+1, popcount(0..7)+1 (duplicate of the
        /// previous), popcount(0..7)+2. Read by TAC's <c>FUN_10e8_8106</c> via
        /// <c>[BX + 0x25A2]</c> with <c>BX &amp; 0x1f</c>, so the high 2 bits of the
        /// masked index select the sub-table and the low 3 bits are the popcount input.
        /// Identical across TAC/GAME/FINAL/BUG (same bit-count math).</summary>
        public BlobRecord CluePopcountTables { get; set; } = new(CluePopcountTablesSize);

        private const int PointerTableSizeBytes = ClueRelationshipPhrasesSection.PhraseCount * 2;
        private const int UnknownClueDataSize = 16;
        private const int CluePopcountTablesSize = 32;
        private const int MonthPointerTableSize = MonthAbbreviationsSection.MonthCount * 2;

        public bool Viewable() => true;
        public bool Editable() => true;

        public int ReadBytes(byte[] fullPayload, int startingOffset)
        {
            var offset = startingOffset;
            offset += ClueRelationshipPhrases.ReadBytes(fullPayload, offset);
            offset += MonthAbbreviations.ReadBytes(fullPayload, offset);
            offset += IntelHeaders.ReadBytes(fullPayload, offset);
            offset += IntelPhrases.ReadBytes(fullPayload, offset);
            // Pointer tables are recomputed on write from the phrase/month slot layouts.
            offset += PointerTableSizeBytes;
            offset += UnknownClueData.ReadBytes(fullPayload, offset);
            offset += CluePopcountTables.ReadBytes(fullPayload, offset);
            offset += MonthPointerTableSize;
            return offset - startingOffset;
        }

        public byte[] WriteBytes()
        {
            var phrases = ClueRelationshipPhrases.WriteBytes();
            var months = MonthAbbreviations.WriteBytes();
            var headers = IntelHeaders.WriteBytes();
            var intelPhrases = IntelPhrases.WriteBytes();
            var phrasePointers = ClueRelationshipPhrases.ComputePointers(PhraseBaseOffset);
            var unknown = UnknownClueData.WriteBytes();
            var popcount = CluePopcountTables.WriteBytes();
            var monthPointers = MonthAbbreviations.ComputePointers(MonthBaseOffset);

            var total = phrases.Length + months.Length + headers.Length
                + intelPhrases.Length + PointerTableSizeBytes
                + unknown.Length + popcount.Length + MonthPointerTableSize;
            var result = new byte[total];
            var pos = 0;
            Array.Copy(phrases, 0, result, pos, phrases.Length); pos += phrases.Length;
            Array.Copy(months, 0, result, pos, months.Length); pos += months.Length;
            Array.Copy(headers, 0, result, pos, headers.Length); pos += headers.Length;
            Array.Copy(intelPhrases, 0, result, pos, intelPhrases.Length); pos += intelPhrases.Length;
            for (var i = 0; i < ClueRelationshipPhrasesSection.PhraseCount; i++)
            {
                result[pos] = (byte)(phrasePointers[i] & 0xFF);
                result[pos + 1] = (byte)((phrasePointers[i] >> 8) & 0xFF);
                pos += 2;
            }
            Array.Copy(unknown, 0, result, pos, unknown.Length); pos += unknown.Length;
            Array.Copy(popcount, 0, result, pos, popcount.Length); pos += popcount.Length;
            for (var i = 0; i < MonthAbbreviationsSection.MonthCount; i++)
            {
                result[pos] = (byte)(monthPointers[i] & 0xFF);
                result[pos + 1] = (byte)((monthPointers[i] >> 8) & 0xFF);
                pos += 2;
            }
            return result;
        }

        public SharedClueAndIntelSection Clone()
        {
            return new SharedClueAndIntelSection
            {
                PhraseBaseOffset = PhraseBaseOffset,
                MonthBaseOffset = MonthBaseOffset,
                ClueRelationshipPhrases = ClueRelationshipPhrases.Clone(),
                MonthAbbreviations = MonthAbbreviations.Clone(),
                IntelHeaders = IntelHeaders.Clone(),
                IntelPhrases = IntelPhrases.Clone(),
                UnknownClueData = UnknownClueData.Clone(),
                CluePopcountTables = CluePopcountTables.Clone(),
            };
        }
    }
}
