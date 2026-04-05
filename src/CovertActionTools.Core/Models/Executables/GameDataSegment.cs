using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CovertActionTools.Core.Models.Executables
{
    /// <summary>
    /// Structured data segment for GAME.EXE.
    /// Field boundaries and interpretations are based on reverse engineering and may not
    /// be fully accurate. Unknown regions are preserved as raw byte arrays.
    /// </summary>
    public class GameDataSegment
    {
        /// <summary>DS paragraph value for GAME.EXE.</summary>
        public const int DsParagraph = 0x1206;

        #region Layout Constants (DS-relative offsets)
        private const int CharNamePointerCount = 192;
        private const int ClueRelPtrCount = 40;
        private const int ClueCategoryDataSize = 48;
        private const int MonthNamePtrCount = 12;
        private const int EvidenceRankPointerTableSize = 160;

        // Original binary offsets (used for initial parse)
        private const int CharNamePointersOffset = 0x1C2A; // 0x013C8A - 0x12060
        private const int ClueRelPtrsOffset = 0x2B3E;      // 0x014B9E - 0x12060
        private const int ClueCategoryDataOffset = 0x2B8E;     // 0x014BEE - 0x12060
        private const int MonthNamePtrsOffset = 0x2BBE;     // 0x014C1E - 0x12060

        // PreCharNameData sub-section layout (DS-relative offsets)
        private const int PreStCRuntimeSize = 0x0070;          // DS:0x0000-0x006F
        private const int PreStInitialStringsOffset = 0x0070;  // DS:0x0070
        private const int PreStInitialStringsEnd = 0x0094;     // BSS starts at DS:0x0094
        private const int PreStRastPortOffset = 0x1454;        // DS:0x1454
        private const int PreStRastPortSize = 44;              // 2x20 RastPort + 2x2 pointer
        private const int PreStCgaOffset = 0x1480;             // DS:0x1480
        private const int PreStCgaSize = 576;                  // GAME has larger CGA data than FINAL
        private const int PreStAnimInitPrefixSize = 2;
        private const int PreStAnimBufferSize = 14;            // "animation.pan\0"
        private const int PreStEnvSentinelSize = 8;            // "env.sve\0"

        // PreCharNameData sub-section sizes
        private const int PreStRuntimePrefixSize = 6;          // 6 bytes of runtime metadata (includes 0xF7) before env.sve

        // MidSectionPreClue sub-section byte sizes (from original binary)
        private const int GameStatusLabelsByteSize = 120;      // DS:0x1DAA-0x1E21
        private const int ScreenLayoutDataByteSize = 588;      // DS:0x1E22-0x206D

        // TrailingData sub-section layout
        private const int IntelPaddingSize = 3;                // 3 null bytes at start
        private const int EvidenceEndPaddingSize = 1;          // 1 null byte after evidence items
        #endregion

        #region Fields (in binary order)

        #region PreCharNameData sub-sections (DS:0x0000 to character names start)

        /// <summary>C runtime copyright, flags, filename template, zeros (DS:0x0000-0x006F, 112 bytes).</summary>
        public byte[] PreStringTableCRuntimeData { get; set; } = Array.Empty<byte>();

        /// <summary>Runtime metadata prefix before initial strings (DS:0x0070-0x0075, 6 bytes).
        /// Contains non-ASCII bytes (0x1A EOF marker, 0xF7 flag) that must be preserved as raw bytes.</summary>
        public byte[] InitialGameRuntimePrefix { get; set; } = Array.Empty<byte>();

        /// <summary>Initial game strings: env.sve sentinel, "One moment please..." loading message
        /// (DS:0x0076-0x0093).</summary>
        public string[] InitialGameStrings { get; set; } = Array.Empty<string>();
        /// <summary>Original byte sizes for InitialGameStrings slots.</summary>
        public int[] InitialGameStringSizes { get; set; } = Array.Empty<int>();

        // BSS zero fill (DS:0x0094 to PreStRastPortOffset) is not stored — emitted as zeros in ToBytes().

        /// <summary>2x RastPort config blocks + config pointers (DS:0x1454-0x147F, 44 bytes).</summary>
        public byte[] PreStringTableRastPortData { get; set; } = Array.Empty<byte>();

        /// <summary>CGA animation pixel data: 2bpp sprite + nibble tables + CGA-to-VGA palette
        /// (DS:0x1480-0x16BF, 576 bytes).</summary>
        public byte[] CgaAnimationData { get; set; } = Array.Empty<byte>();

        /// <summary>2-byte prefix before animation.pan buffer (DS:0x16C0-0x16C1).</summary>
        public byte[] AnimationInitPrefix { get; set; } = Array.Empty<byte>();

        // animation.pan buffer (14 bytes): overwritten at runtime with actual PAN filename.
        // env.sve sentinel (8 bytes): sentinel string, never opened as a file.
        // Both are emitted as fixed strings in ToBytes().

        /// <summary>HQ screen display strings: ad.pic filename, location type labels, default agent name
        /// (DS:0x16D8 to character names start). Used by the main game HQ display.</summary>
        public string[] HqDisplayStrings { get; set; } = Array.Empty<string>();
        /// <summary>Original byte sizes for HqDisplayStrings slots.</summary>
        public int[] HqDisplayStringSizes { get; set; } = Array.Empty<int>();

        #endregion

        /// <summary>192 character names (4 ethnic groups x female first / male first / male surname, 16 each).</summary>
        public string[] CharacterNames { get; set; } = Array.Empty<string>();

        /// <summary>Data between character names and character name pointer table.</summary>
        public byte[] PostCharNameData { get; set; } = Array.Empty<byte>();

        // CharacterNamePointers are computed at serialization time.

        #region MidSectionPreClue sub-sections (between char name pointers and clue phrases)

        /// <summary>Game status UI labels: "Master Plan", "--SECRET--", "ARRESTED", "IN HIDING",
        /// " TURNED", "Personnel File", " Action Team", ", ", "(No activity)", "00:00:00",
        /// "Incoming Msg" (120 bytes in original binary).</summary>
        public string[] GameStatusLabels { get; set; } = Array.Empty<string>();
        /// <summary>Original byte sizes for GameStatusLabels slots.</summary>
        public int[] GameStatusLabelSizes { get; set; } = Array.Empty<int>();

        /// <summary>Screen layout rect draw data: coordinates and attributes for game UI regions
        /// (588 bytes in original binary). Binary data, not directly editable.</summary>
        public byte[] ScreenLayoutData { get; set; } = Array.Empty<byte>();

        /// <summary>Gameplay event strings (part 1): jail breaks, escapes, building approach,
        /// interrogation, briefing text, travel menus, city map filenames.
        /// Core gameplay text strings up to the binary lookup table.</summary>
        public string[] GameplayEventStrings { get; set; } = Array.Empty<string>();
        /// <summary>Original byte sizes for GameplayEventStrings slots.</summary>
        public int[] GameplayEventStringSizes { get; set; } = Array.Empty<int>();

        /// <summary>Binary gameplay lookup table: difficulty/guard data between gameplay
        /// string groups (26 bytes in original binary). Contains 0xFF bytes.</summary>
        public byte[] GameplayBinaryLookup { get; set; } = Array.Empty<byte>();

        /// <summary>Gameplay event strings (part 2): guard alertness labels ("quiet",
        /// "unsuspecting", etc.), briefing farewell text, ".pic", "cities.cat".</summary>
        public string[] GameplayEventStrings2 { get; set; } = Array.Empty<string>();
        /// <summary>Original byte sizes for GameplayEventStrings2 slots.</summary>
        public int[] GameplayEventString2Sizes { get; set; } = Array.Empty<int>();

        #endregion

        /// <summary>40 clue relationship phrases.</summary>
        public string[] ClueRelationshipPhrases { get; set; } = Array.Empty<string>();

        /// <summary>12 month name abbreviations.</summary>
        public string[] MonthNames { get; set; } = Array.Empty<string>();

        /// <summary>Intel report header strings: "CODED MESSAGE:", "MEETING NOTES:", etc.
        /// Formatting strings used in intelligence report display (134 bytes in original binary).</summary>
        public string[] IntelHeaders { get; set; } = Array.Empty<string>();
        /// <summary>Original byte sizes for IntelHeaders slots.</summary>
        public int[] IntelHeaderSizes { get; set; } = Array.Empty<int>();

        // ClueRelationshipPointers (40) and MonthNamePointers (12) are computed at serialization time.

        /// <summary>
        /// 48-byte clue category and popcount lookup table. Identical across FINAL/TAC/GAME.
        /// Bytes 0-15: category bit flags per clue pair (values 1/2/4/8 = single-bit masks).
        /// Bytes 16-47: four 8-entry popcount lookup sub-tables with offsets +0, +1, +1, +2.
        /// </summary>
        public byte[] ClueCategoryData { get; set; } = Array.Empty<byte>();

        #region TrailingData sub-sections (after month name pointers to end of data segment)

        /// <summary>Intel report text templates: participant identification, photo acquired,
        /// spotted in city, member of org, additional information, rank determination,
        /// recruiting information (370 bytes in original binary).</summary>
        public string[] IntelReportTexts { get; set; } = Array.Empty<string>();
        /// <summary>Original byte sizes for IntelReportTexts slots.</summary>
        public int[] IntelReportTextSizes { get; set; } = Array.Empty<int>();

        /// <summary>8 agent rank names: Recruit, Operative, Technician, Agent, Organizer,
        /// Special Agent, Group Leader, MasterMind.</summary>
        public string[] RankNames { get; set; } = Array.Empty<string>();
        /// <summary>Original byte sizes for RankNames slots.</summary>
        public int[] RankNameSizes { get; set; } = Array.Empty<int>();

        /// <summary>8 evidence type abbreviations: CAR, WPN, ADR, TKT, MSG, $, $, FCE.</summary>
        public string[] EvidenceTypeAbbreviations { get; set; } = Array.Empty<string>();
        /// <summary>Original byte sizes for EvidenceTypeAbbreviations slots.</summary>
        public int[] EvidenceTypeSizes { get; set; } = Array.Empty<int>();

        /// <summary>64 evidence item names: 8 cars, 8 weapons, 8 addresses, 8 airline tickets,
        /// 8 messages, 16 face descriptor placeholders ($), 8 passports.</summary>
        public string[] EvidenceItemNames { get; set; } = Array.Empty<string>();
        /// <summary>Original byte sizes for EvidenceItemNames slots.</summary>
        public int[] EvidenceItemSizes { get; set; } = Array.Empty<int>();

        // EvidenceRankPointerTable (80 uint16) is computed at serialization time.

        /// <summary>8 investigation/evidence-gathering method names: Clandestine Photo,
        /// Electronic WireTap, Covert Surveillance, File Record Search, Local Informant,
        /// INTERPOL Data Base, Local Authorities, + 1 label.</summary>
        public string[] InvestigationMethods { get; set; } = Array.Empty<string>();
        /// <summary>Original byte sizes for InvestigationMethods slots.</summary>
        public int[] InvestigationMethodSizes { get; set; } = Array.Empty<int>();

        /// <summary>Everything after investigation methods to end of data segment:
        /// clue system data, gameplay strings (hotel/CIA/data menus), save/load UI,
        /// exe chain data, RastPort blocks, overlay manager strings, C runtime error
        /// messages, BSS zero fill.</summary>
        public byte[] RemainingTrailingData { get; set; } = Array.Empty<byte>();

        #endregion

        #endregion

        #region Parsing

        public static GameDataSegment FromBytes(byte[] dataSegment)
        {
            var segment = new GameDataSegment();

            // --- PreCharNameData sub-sections ---
            ParsePreCharNameData(dataSegment, segment);

            // --- Character names ---
            var charPtrs = DataSegmentHelper.BytesToUInt16Array(dataSegment, CharNamePointersOffset, CharNamePointerCount);
            segment.CharacterNames = DataSegmentHelper.ExtractStringsFromPointers(charPtrs, dataSegment);

            var (charBlockStart, charBlockEnd) = DataSegmentHelper.FindStringBlockBounds(charPtrs, dataSegment);
            var postCharLen = CharNamePointersOffset - charBlockEnd;
            segment.PostCharNameData = postCharLen > 0
                ? DataSegmentHelper.Slice(dataSegment, charBlockEnd, postCharLen)
                : Array.Empty<byte>();

            var charPtrsEnd = CharNamePointersOffset + CharNamePointerCount * 2;

            // --- MidSectionPreClue sub-sections ---
            var cluePtrs = DataSegmentHelper.BytesToUInt16Array(dataSegment, ClueRelPtrsOffset, ClueRelPtrCount);
            segment.ClueRelationshipPhrases = DataSegmentHelper.ExtractStringsFromPointers(cluePtrs, dataSegment);

            var monthPtrs = DataSegmentHelper.BytesToUInt16Array(dataSegment, MonthNamePtrsOffset, MonthNamePtrCount);
            segment.MonthNames = DataSegmentHelper.ExtractStringsFromPointers(monthPtrs, dataSegment);

            var (clueBlockStart, _) = DataSegmentHelper.FindStringBlockBounds(cluePtrs, dataSegment);
            var (_, monthBlockEnd) = DataSegmentHelper.FindStringBlockBounds(monthPtrs, dataSegment);

            var midPreClueStart = charPtrsEnd;
            var midPreClueSize = clueBlockStart - midPreClueStart;
            ParseMidSectionPreClue(dataSegment, midPreClueStart, midPreClueSize, segment);

            // --- IntelHeaders (was MidSectionPostMonth) ---
            var intelHeaderStart = monthBlockEnd;
            var intelHeaderSize = ClueRelPtrsOffset - intelHeaderStart;
            var (intelHdrs, intelHdrSzs) = DataSegmentHelper.AllNullTerminatedStringsWithSizesFromBytes(
                dataSegment, intelHeaderStart, intelHeaderSize);
            segment.IntelHeaders = intelHdrs;
            segment.IntelHeaderSizes = intelHdrSzs;

            // --- ClueCategoryData ---
            segment.ClueCategoryData = DataSegmentHelper.Slice(dataSegment, ClueCategoryDataOffset, ClueCategoryDataSize);

            // --- TrailingData sub-sections ---
            var monthPtrsEnd = MonthNamePtrsOffset + MonthNamePtrCount * 2;
            ParseTrailingData(dataSegment, monthPtrsEnd, segment);

            return segment;
        }

        private static void ParsePreCharNameData(byte[] dataSegment, GameDataSegment segment)
        {
            // CRuntime header (DS:0x0000-0x006F)
            segment.PreStringTableCRuntimeData = DataSegmentHelper.Slice(dataSegment, 0, PreStCRuntimeSize);

            // Runtime prefix (6 bytes of non-ASCII metadata at DS:0x0070-0x0075)
            segment.InitialGameRuntimePrefix = DataSegmentHelper.Slice(dataSegment, PreStInitialStringsOffset, PreStRuntimePrefixSize);

            // Initial game strings (DS:0x0076-0x0093)
            var initStrStart = PreStInitialStringsOffset + PreStRuntimePrefixSize;
            var initStrSize = PreStInitialStringsEnd - initStrStart;
            var (initStrs, initSzs) = DataSegmentHelper.AllNullTerminatedStringsWithSizesFromBytes(
                dataSegment, initStrStart, initStrSize);
            segment.InitialGameStrings = initStrs;
            segment.InitialGameStringSizes = initSzs;

            // BSS zero fill is not stored

            // RastPort blocks (DS:0x1454-0x147F)
            segment.PreStringTableRastPortData = DataSegmentHelper.Slice(dataSegment, PreStRastPortOffset, PreStRastPortSize);

            // CGA animation data (DS:0x1480-0x16BF)
            segment.CgaAnimationData = DataSegmentHelper.Slice(dataSegment, PreStCgaOffset, PreStCgaSize);

            // Animation init prefix (2 bytes before animation.pan)
            var animInitOff = PreStCgaOffset + PreStCgaSize;
            segment.AnimationInitPrefix = DataSegmentHelper.Slice(dataSegment, animInitOff, PreStAnimInitPrefixSize);

            // animation.pan buffer and env.sve sentinel are fixed — not stored
            // HQ display strings start after env.sve sentinel
            var hqStringsOff = animInitOff + PreStAnimInitPrefixSize + PreStAnimBufferSize + PreStEnvSentinelSize;

            // Find where character names start (first character name pointer value)
            var charPtrs = DataSegmentHelper.BytesToUInt16Array(dataSegment, CharNamePointersOffset, CharNamePointerCount);
            var (charBlockStart, _) = DataSegmentHelper.FindStringBlockBounds(charPtrs, dataSegment);

            var hqStringsSize = charBlockStart - hqStringsOff;
            if (hqStringsSize > 0)
            {
                var (hqStrs, hqSzs) = DataSegmentHelper.AllNullTerminatedStringsWithSizesFromBytes(
                    dataSegment, hqStringsOff, hqStringsSize);
                segment.HqDisplayStrings = hqStrs;
                segment.HqDisplayStringSizes = hqSzs;
            }
        }

        private static void ParseMidSectionPreClue(byte[] dataSegment, int start, int totalSize, GameDataSegment segment)
        {
            // Status labels (120 bytes in original binary)
            var statusSize = Math.Min(GameStatusLabelsByteSize, totalSize);
            var (statusStrs, statusSzs) = DataSegmentHelper.AllNullTerminatedStringsWithSizesFromBytes(
                dataSegment, start, statusSize);
            segment.GameStatusLabels = statusStrs;
            segment.GameStatusLabelSizes = statusSzs;

            // Screen layout binary data (588 bytes in original binary)
            var layoutStart = start + statusSize;
            var layoutSize = Math.Min(ScreenLayoutDataByteSize, totalSize - statusSize);
            segment.ScreenLayoutData = DataSegmentHelper.Slice(dataSegment, layoutStart, layoutSize);

            // Gameplay event strings: split around binary lookup data containing 0xFF bytes.
            // Find "quiet" marker to locate the binary/string boundary.
            var eventStart = layoutStart + layoutSize;
            var eventEnd = start + totalSize;
            var eventSize = eventEnd - eventStart;

            if (eventSize > 0)
            {
                // Find "quiet" to determine where binary data ends and part 2 strings begin
                var quietPos = FindMarkerString(dataSegment, eventStart, eventEnd, "quiet");

                if (quietPos < eventEnd)
                {
                    // Scan backwards from quietPos to find where the binary data starts
                    // (last null byte before the binary run)
                    var binaryEnd = quietPos;
                    var binaryStart = binaryEnd;

                    // Walk backwards past null bytes and non-ASCII data to find the boundary.
                    // The binary data is preceded by the last string's null terminator.
                    // Find the start of the binary block by scanning backwards from quietPos
                    // looking for the end of the last string before the binary data.
                    var scanPos = quietPos - 1;
                    while (scanPos > eventStart && (dataSegment[scanPos] < 0x20 || dataSegment[scanPos] >= 0x80))
                    {
                        scanPos--;
                    }
                    // scanPos is now at the last printable ASCII char of the preceding string.
                    // Find the null terminator after it.
                    while (scanPos < quietPos && dataSegment[scanPos] != 0) scanPos++;
                    binaryStart = scanPos + 1; // just past the null terminator

                    var part1Size = binaryStart - eventStart;
                    var binarySize = binaryEnd - binaryStart;
                    var part2Size = eventEnd - binaryEnd;

                    var (eventStrs1, eventSzs1) = DataSegmentHelper.AllNullTerminatedStringsWithSizesFromBytes(
                        dataSegment, eventStart, part1Size);
                    segment.GameplayEventStrings = eventStrs1;
                    segment.GameplayEventStringSizes = eventSzs1;

                    segment.GameplayBinaryLookup = DataSegmentHelper.Slice(dataSegment, binaryStart, binarySize);

                    var (eventStrs2, eventSzs2) = DataSegmentHelper.AllNullTerminatedStringsWithSizesFromBytes(
                        dataSegment, binaryEnd, part2Size);
                    segment.GameplayEventStrings2 = eventStrs2;
                    segment.GameplayEventString2Sizes = eventSzs2;
                }
                else
                {
                    // Fallback: no binary data found, treat everything as strings
                    var (eventStrs, eventSzs) = DataSegmentHelper.AllNullTerminatedStringsWithSizesFromBytes(
                        dataSegment, eventStart, eventSize);
                    segment.GameplayEventStrings = eventStrs;
                    segment.GameplayEventStringSizes = eventSzs;
                }
            }
        }

        private static void ParseTrailingData(byte[] dataSegment, int start, GameDataSegment segment)
        {
            var pos = start;
            var end = dataSegment.Length;

            // Intel padding (3 null bytes)
            pos += IntelPaddingSize;

            // Intel report texts: ends before "Recruit\0"
            var rankStart = FindMarkerString(dataSegment, pos, end, "Recruit");
            var intelTextsSize = rankStart - pos;
            var (intelTexts, intelTextSzs) = DataSegmentHelper.AllNullTerminatedStringsWithSizesFromBytes(
                dataSegment, pos, intelTextsSize);
            segment.IntelReportTexts = intelTexts;
            segment.IntelReportTextSizes = intelTextSzs;
            pos = rankStart;

            // Rank names: 8 strings, ending before "CAR\0"
            var evTypesStart = FindMarkerString(dataSegment, pos, end, "CAR");
            var rankSize = evTypesStart - pos;
            var (ranks, rankSzs) = DataSegmentHelper.AllNullTerminatedStringsWithSizesFromBytes(
                dataSegment, pos, rankSize);
            segment.RankNames = ranks;
            segment.RankNameSizes = rankSzs;
            pos = evTypesStart;

            // Evidence type abbreviations: 8 strings, ending before "Ford Escort"
            var evItemsStart = FindMarkerString(dataSegment, pos, end, "Ford Escort");
            var evTypesSize = evItemsStart - pos;
            var (evTypes, evTypeSzs) = DataSegmentHelper.AllNullTerminatedStringsWithSizesFromBytes(
                dataSegment, pos, evTypesSize);
            segment.EvidenceTypeAbbreviations = evTypes;
            segment.EvidenceTypeSizes = evTypeSzs;
            pos = evItemsStart;

            // Evidence item names: strings until pointer table.
            // Find "Clandestine Photo" to locate investigation methods, then back-calculate.
            var invMethodsStart = FindMarkerString(dataSegment, pos, end, "Clandestine Photo");
            var evPtrTableStart = invMethodsStart - EvidenceRankPointerTableSize;
            var evItemsEndWithPadding = evPtrTableStart; // includes the 1-byte null padding
            var evItemsSize = evItemsEndWithPadding - EvidenceEndPaddingSize - pos;
            var (evItems, evItemSzs) = DataSegmentHelper.AllNullTerminatedStringsWithSizesFromBytes(
                dataSegment, pos, evItemsSize);
            segment.EvidenceItemNames = evItems;
            segment.EvidenceItemSizes = evItemSzs;
            pos = invMethodsStart; // skip padding + pointer table

            // Investigation methods: 8 strings
            var invPos = pos;
            for (var i = 0; i < 8; i++)
            {
                while (invPos < end && dataSegment[invPos] != 0) invPos++;
                invPos++; // skip null
            }
            var invSize = invPos - pos;
            var (invMethods, invMethodSzs) = DataSegmentHelper.AllNullTerminatedStringsWithSizesFromBytes(
                dataSegment, pos, invSize);
            segment.InvestigationMethods = invMethods;
            segment.InvestigationMethodSizes = invMethodSzs;
            pos = invPos;

            // Remaining trailing data (clue system, gameplay menus, save/load, exe chain,
            // RastPort blocks, overlay/C runtime, BSS)
            segment.RemainingTrailingData = DataSegmentHelper.Slice(dataSegment, pos, end - pos);
        }

        private static int FindMarkerString(byte[] data, int start, int end, string marker)
        {
            var markerBytes = Encoding.ASCII.GetBytes(marker);
            for (var i = start; i <= end - markerBytes.Length; i++)
            {
                var match = true;
                for (var j = 0; j < markerBytes.Length; j++)
                {
                    if (data[i + j] != markerBytes[j])
                    {
                        match = false;
                        break;
                    }
                }
                if (match) return i;
            }
            return end; // fallback
        }

        #endregion

        #region Serialization

        public byte[] ToBytes()
        {
            // Build pre-char-name data
            var preCharNameBytes = BuildPreCharNameData();

            // Serialize character names
            var charNamesBytes = DataSegmentHelper.NullTerminatedStringsToBytes(CharacterNames);

            // Compute character name pointers
            var charNamesBase = preCharNameBytes.Length;
            var charNamePointers = DataSegmentHelper.ComputeStringPointers(CharacterNames, charNamesBase);

            // Build mid-section pre-clue data
            var midPreClueBytes = BuildMidSectionPreClue();

            // Serialize clue phrases and month names
            var cluePhraseBytes = DataSegmentHelper.NullTerminatedStringsToBytes(ClueRelationshipPhrases);
            var monthNameBytes = DataSegmentHelper.NullTerminatedStringsToBytes(MonthNames);

            // Serialize intel headers
            var intelHeaderBytes = DataSegmentHelper.NullTerminatedStringsToFixedBytes(IntelHeaders, IntelHeaderSizes);

            // Compute clue and month pointer bases
            var clueBase = preCharNameBytes.Length + charNamesBytes.Length + PostCharNameData.Length
                + CharNamePointerCount * 2 + midPreClueBytes.Length;
            var cluePointers = DataSegmentHelper.ComputeStringPointers(ClueRelationshipPhrases, clueBase);

            var monthBase = clueBase + cluePhraseBytes.Length;
            var monthPointers = DataSegmentHelper.ComputeStringPointers(MonthNames, monthBase);

            // Build trailing data
            var trailingBytes = BuildTrailingData();

            // Assemble the full data segment
            var result = DataSegmentHelper.Concatenate(
                preCharNameBytes,
                charNamesBytes,
                PostCharNameData,
                DataSegmentHelper.UInt16ArrayToBytes(charNamePointers),
                midPreClueBytes,
                cluePhraseBytes,
                monthNameBytes,
                intelHeaderBytes,
                DataSegmentHelper.UInt16ArrayToBytes(cluePointers),
                ClueCategoryData,
                DataSegmentHelper.UInt16ArrayToBytes(monthPointers),
                trailingBytes
            );

            // Patch the evidence/rank pointer table in the trailing data
            PatchEvidencePointers(result);

            return result;
        }

        private byte[] BuildPreCharNameData()
        {
            var parts = new List<byte>();

            // CRuntime header
            parts.AddRange(PreStringTableCRuntimeData);

            // Runtime prefix (6 bytes of non-ASCII metadata)
            parts.AddRange(InitialGameRuntimePrefix);

            // Initial game strings
            parts.AddRange(DataSegmentHelper.NullTerminatedStringsToFixedBytes(InitialGameStrings, InitialGameStringSizes));

            // BSS zero fill: pad with zeros up to RastPort offset
            var zeroFillSize = PreStRastPortOffset - parts.Count;
            if (zeroFillSize > 0)
                parts.AddRange(new byte[zeroFillSize]);

            // RastPort blocks
            parts.AddRange(PreStringTableRastPortData);

            // CGA animation data
            parts.AddRange(CgaAnimationData);

            // Animation init prefix (2 bytes)
            parts.AddRange(AnimationInitPrefix);

            // animation.pan buffer (14 bytes) — emit fixed placeholder
            parts.AddRange(Encoding.ASCII.GetBytes("animation.pan"));
            parts.Add(0);

            // env.sve sentinel (8 bytes) — emit fixed sentinel
            parts.AddRange(Encoding.ASCII.GetBytes("env.sve"));
            parts.Add(0);

            // HQ display strings
            parts.AddRange(DataSegmentHelper.NullTerminatedStringsToFixedBytes(HqDisplayStrings, HqDisplayStringSizes));

            return parts.ToArray();
        }

        private byte[] BuildMidSectionPreClue()
        {
            return DataSegmentHelper.Concatenate(
                DataSegmentHelper.NullTerminatedStringsToFixedBytes(GameStatusLabels, GameStatusLabelSizes),
                ScreenLayoutData,
                DataSegmentHelper.NullTerminatedStringsToFixedBytes(GameplayEventStrings, GameplayEventStringSizes),
                GameplayBinaryLookup,
                DataSegmentHelper.NullTerminatedStringsToFixedBytes(GameplayEventStrings2, GameplayEventString2Sizes)
            );
        }

        private byte[] BuildTrailingData()
        {
            // Serialize all string sections
            var intelTxtBytes = DataSegmentHelper.NullTerminatedStringsToFixedBytes(IntelReportTexts, IntelReportTextSizes);
            var rankBytes = DataSegmentHelper.NullTerminatedStringsToFixedBytes(RankNames, RankNameSizes);
            var evTypeBytes = DataSegmentHelper.NullTerminatedStringsToFixedBytes(EvidenceTypeAbbreviations, EvidenceTypeSizes);
            var evItemBytes = DataSegmentHelper.NullTerminatedStringsToFixedBytes(EvidenceItemNames, EvidenceItemSizes);
            var invMethodBytes = DataSegmentHelper.NullTerminatedStringsToFixedBytes(InvestigationMethods, InvestigationMethodSizes);

            // Evidence/rank pointer table placeholder (patched after assembly)
            var evRankPtrPlaceholder = new byte[EvidenceRankPointerTableSize];

            return DataSegmentHelper.Concatenate(
                new byte[IntelPaddingSize],        // 3 null bytes
                intelTxtBytes,
                rankBytes,
                evTypeBytes,
                evItemBytes,
                new byte[EvidenceEndPaddingSize],   // 1 null byte
                evRankPtrPlaceholder,
                invMethodBytes,
                RemainingTrailingData
            );
        }

        private void PatchEvidencePointers(byte[] fullDataSegment)
        {
            // Find the start of trailing data in the full segment
            var preCharNameBytes = BuildPreCharNameData();
            var charNamesBytes = DataSegmentHelper.NullTerminatedStringsToBytes(CharacterNames);
            var midPreClueBytes = BuildMidSectionPreClue();
            var cluePhraseBytes = DataSegmentHelper.NullTerminatedStringsToBytes(ClueRelationshipPhrases);
            var monthNameBytes = DataSegmentHelper.NullTerminatedStringsToBytes(MonthNames);
            var intelHeaderBytes = DataSegmentHelper.NullTerminatedStringsToFixedBytes(IntelHeaders, IntelHeaderSizes);

            var trailingStart = preCharNameBytes.Length + charNamesBytes.Length + PostCharNameData.Length
                + CharNamePointerCount * 2 + midPreClueBytes.Length + cluePhraseBytes.Length
                + monthNameBytes.Length + intelHeaderBytes.Length + ClueRelPtrCount * 2
                + ClueCategoryDataSize + MonthNamePtrCount * 2;

            // Compute offsets within trailing data
            var intelTxtBytes = DataSegmentHelper.NullTerminatedStringsToFixedBytes(IntelReportTexts, IntelReportTextSizes);
            var rankBytes = DataSegmentHelper.NullTerminatedStringsToFixedBytes(RankNames, RankNameSizes);
            var evTypeBytes = DataSegmentHelper.NullTerminatedStringsToFixedBytes(EvidenceTypeAbbreviations, EvidenceTypeSizes);
            var evItemBytes = DataSegmentHelper.NullTerminatedStringsToFixedBytes(EvidenceItemNames, EvidenceItemSizes);

            var rankStart = trailingStart + IntelPaddingSize + intelTxtBytes.Length;
            var evTypeStart = rankStart + rankBytes.Length;
            var evItemStart = evTypeStart + evTypeBytes.Length;
            var evPtrStart = evItemStart + evItemBytes.Length + EvidenceEndPaddingSize;

            // Build combined pointer array: ranks + evidence types + evidence items
            var allEvStrings = new string[RankNames.Length + EvidenceTypeAbbreviations.Length + EvidenceItemNames.Length];
            Array.Copy(RankNames, 0, allEvStrings, 0, RankNames.Length);
            Array.Copy(EvidenceTypeAbbreviations, 0, allEvStrings, RankNames.Length, EvidenceTypeAbbreviations.Length);
            Array.Copy(EvidenceItemNames, 0, allEvStrings, RankNames.Length + EvidenceTypeAbbreviations.Length, EvidenceItemNames.Length);
            var evPtrs = DataSegmentHelper.ComputeStringPointers(allEvStrings, rankStart);

            for (var i = 0; i < evPtrs.Length && i * 2 + 1 < EvidenceRankPointerTableSize; i++)
            {
                fullDataSegment[evPtrStart + i * 2] = (byte)(evPtrs[i] & 0xFF);
                fullDataSegment[evPtrStart + i * 2 + 1] = (byte)((evPtrs[i] >> 8) & 0xFF);
            }
        }

        #endregion

        public GameDataSegment Clone()
        {
            return new GameDataSegment
            {
                PreStringTableCRuntimeData = PreStringTableCRuntimeData.ToArray(),
                InitialGameRuntimePrefix = InitialGameRuntimePrefix.ToArray(),
                InitialGameStrings = InitialGameStrings.Select(s => s).ToArray(),
                InitialGameStringSizes = InitialGameStringSizes.ToArray(),
                PreStringTableRastPortData = PreStringTableRastPortData.ToArray(),
                CgaAnimationData = CgaAnimationData.ToArray(),
                AnimationInitPrefix = AnimationInitPrefix.ToArray(),
                HqDisplayStrings = HqDisplayStrings.Select(s => s).ToArray(),
                HqDisplayStringSizes = HqDisplayStringSizes.ToArray(),
                CharacterNames = CharacterNames.Select(s => s).ToArray(),
                PostCharNameData = PostCharNameData.ToArray(),
                GameStatusLabels = GameStatusLabels.Select(s => s).ToArray(),
                GameStatusLabelSizes = GameStatusLabelSizes.ToArray(),
                ScreenLayoutData = ScreenLayoutData.ToArray(),
                GameplayEventStrings = GameplayEventStrings.Select(s => s).ToArray(),
                GameplayEventStringSizes = GameplayEventStringSizes.ToArray(),
                GameplayBinaryLookup = GameplayBinaryLookup.ToArray(),
                GameplayEventStrings2 = GameplayEventStrings2.Select(s => s).ToArray(),
                GameplayEventString2Sizes = GameplayEventString2Sizes.ToArray(),
                ClueRelationshipPhrases = ClueRelationshipPhrases.Select(s => s).ToArray(),
                MonthNames = MonthNames.Select(s => s).ToArray(),
                IntelHeaders = IntelHeaders.Select(s => s).ToArray(),
                IntelHeaderSizes = IntelHeaderSizes.ToArray(),
                ClueCategoryData = ClueCategoryData.ToArray(),
                IntelReportTexts = IntelReportTexts.Select(s => s).ToArray(),
                IntelReportTextSizes = IntelReportTextSizes.ToArray(),
                RankNames = RankNames.Select(s => s).ToArray(),
                RankNameSizes = RankNameSizes.ToArray(),
                EvidenceTypeAbbreviations = EvidenceTypeAbbreviations.Select(s => s).ToArray(),
                EvidenceTypeSizes = EvidenceTypeSizes.ToArray(),
                EvidenceItemNames = EvidenceItemNames.Select(s => s).ToArray(),
                EvidenceItemSizes = EvidenceItemSizes.ToArray(),
                InvestigationMethods = InvestigationMethods.Select(s => s).ToArray(),
                InvestigationMethodSizes = InvestigationMethodSizes.ToArray(),
                RemainingTrailingData = RemainingTrailingData.ToArray()
            };
        }
    }
}
