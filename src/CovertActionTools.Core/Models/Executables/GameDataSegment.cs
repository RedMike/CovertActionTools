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

        // MidSectionPreClue: guard alertness
        private const int GuardAlertnessLabelCount = 6;        // quiet, unsuspecting, cautious, suspicious, nervous, very nervous

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

        /// <summary>6 guard alertness level labels: quiet, unsuspecting, cautious,
        /// suspicious, nervous, very nervous. Displayed on the city map for each
        /// building's alert state.</summary>
        public string[] GuardAlertnessLabels { get; set; } = Array.Empty<string>();
        /// <summary>Original byte sizes for GuardAlertnessLabels slots.</summary>
        public int[] GuardAlertnessLabelSizes { get; set; } = Array.Empty<int>();

        /// <summary>Gameplay event strings (part 2): briefing farewell text,
        /// ".pic", "cities.cat".</summary>
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

        /// <summary>Clue display formatting and detail strings: Source/Method/Related Clues labels,
        /// $KEY/$NAME/$ORG/$CITY/$ROLE tokens, *C0000 tags, codenames, suspect info labels.
        /// Contains control bytes: [b]=bold, [tab]=field separator, [hdr]=section header.</summary>
        public string[] ClueFormattingStrings { get; set; } = Array.Empty<string>();
        public int[] ClueFormattingSizes { get; set; } = Array.Empty<int>();

        /// <summary>Message log display: message decoding, Rcvd/Sent msg formatters.</summary>
        public string[] MessageLogStrings { get; set; } = Array.Empty<string>();
        public int[] MessageLogStringSizes { get; set; } = Array.Empty<int>();

        /// <summary>Hotel menu: hotel.pic, hotel options (Leave/Lounge/Sleep/Save/Load), *lounge tag.</summary>
        public string[] HotelMenuStrings { get; set; } = Array.Empty<string>();
        public int[] HotelMenuStringSizes { get; set; } = Array.Empty<int>();

        /// <summary>Data files menu: Clues/Suspects/Inside Information/News/Org/City/Activity.</summary>
        public string[] DataFilesMenuStrings { get; set; } = Array.Empty<string>();
        public int[] DataFilesMenuStringSizes { get; set; } = Array.Empty<int>();

        /// <summary>CIA building menus: floor selection, Intelligence/Data/Crypto sections,
        /// double agent accusation, local agent check results.</summary>
        public string[] CiaMenuStrings { get; set; } = Array.Empty<string>();
        public int[] CiaMenuStringSizes { get; set; } = Array.Empty<int>();

        /// <summary>Activity/wiretap display: report summary, wiretap labels, org/allies formatters.</summary>
        public string[] ActivityWiretapStrings { get; set; } = Array.Empty<string>();
        public int[] ActivityWiretapStringSizes { get; set; } = Array.Empty<int>();

        /// <summary>City/suspect display: city summaries, suspect files, locations, documents.
        /// Contains [hdr] control bytes for section formatting.</summary>
        public string[] CitySuspectStrings { get; set; } = Array.Empty<string>();
        public int[] CitySuspectSizes { get; set; } = Array.Empty<int>();

        /// <summary>Coded message display: FROM/TO/MESSAGE headers, NSYN Overflow, Chronology, News.</summary>
        public string[] CodedMessageStrings { get; set; } = Array.Empty<string>();
        public int[] CodedMessageStringSizes { get; set; } = Array.Empty<int>();

        /// <summary>Message substitution tokens: $VICTIM, $SNDORG, $RCVORG, $SNDLOC, $RCVLOC,
        /// $HLPORG, $OBJECT + format buffers.</summary>
        public string[] SubstitutionTokenStrings { get; set; } = Array.Empty<string>();
        public int[] SubstitutionTokenStringSizes { get; set; } = Array.Empty<int>();

        /// <summary>Clue lookup pointer table and format strings before the not-found message.
        /// Contains embedded DS-relative pointer values — stored as byte array.</summary>
        public byte[] CluePreMessageData { get; set; } = Array.Empty<byte>();

        /// <summary>"This information requires security clearance: " — displayed when a
        /// text file lookup fails to find the requested header.</summary>
        public string ClueNotFoundMessage { get; set; } = string.Empty;

        /// <summary>Tag+filename pairs for text.dta lookups (*SLOC00/text.dta through *MSG0000),
        /// facesf.pic/faces.pic references. Contains [tab]/[prompt] control bytes.</summary>
        public string[] CluePostMessageStrings { get; set; } = Array.Empty<string>();
        public int[] CluePostMessageSizes { get; set; } = Array.Empty<int>();

        /// <summary>Bulletin/surveillance reports: bulletins, INTERPOL, satellite intercept,
        /// wiretap reveals, airport surveillance, double agent reports.</summary>
        public string[] BulletinStrings { get; set; } = Array.Empty<string>();
        public int[] BulletinStringSizes { get; set; } = Array.Empty<int>();

        /// <summary>Chronology/status display: time template, ARRESTED/TURNED status, clue review.
        /// Contains [b]/[bullet] control bytes.</summary>
        public string[] ChronologyStatusStrings { get; set; } = Array.Empty<string>();
        public int[] ChronologyStatusSizes { get; set; } = Array.Empty<int>();

        /// <summary>Research assistant: officem.pan/officef.pan, advice text, org/city suggestions,
        /// arrest/investigate recommendations, *advicea-*advice5 tags.
        /// Contains [prompt] control bytes.</summary>
        public string[] ResearchAssistantStrings { get; set; } = Array.Empty<string>();
        public int[] ResearchAssistantSizes { get; set; } = Array.Empty<int>();

        /// <summary>Save/load system: Select Load/Save File, cv0.sve, rank display, difficulty
        /// labels, disk prompts. Contains [prompt] control bytes.</summary>
        public string[] SaveLoadStrings { get; set; } = Array.Empty<string>();
        public int[] SaveLoadSizes { get; set; } = Array.Empty<int>();

        /// <summary>Exe chain + disk swap: env.sve, final.exe/game.exe/tac.exe/hq.pan,
        /// disk insert prompts. Contains non-ASCII bytes — stored as byte array.</summary>
        public byte[] ExeChainData { get; set; } = Array.Empty<byte>();

        /// <summary>PANI headers, scene init, palette remaps, OK string, briefing.pan,
        /// animation.pan, joystick table, RastPort blocks. Binary game state data.</summary>
        public byte[] GameStateData { get; set; } = Array.Empty<byte>();

        /// <summary>Overlay manager strings, C runtime error messages, BSS zero fill.
        /// Not editable — preserved for binary roundtrip fidelity.</summary>
        public byte[] RuntimeTrailingData { get; set; } = Array.Empty<byte>();

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

                    // Part 2: first 6 strings are guard alertness labels, remainder is other strings
                    var alertPos = binaryEnd;
                    var (alertStrs, alertSzs) = DataSegmentHelper.NullTerminatedStringsWithSizesFromBytes(
                        dataSegment, alertPos, GuardAlertnessLabelCount);
                    segment.GuardAlertnessLabels = alertStrs;
                    segment.GuardAlertnessLabelSizes = alertSzs;

                    var alertEnd = alertPos + alertSzs.Sum();
                    var remaining2Size = eventEnd - alertEnd;
                    if (remaining2Size > 0)
                    {
                        var (eventStrs2, eventSzs2) = DataSegmentHelper.AllNullTerminatedStringsWithSizesFromBytes(
                            dataSegment, alertEnd, remaining2Size);
                        segment.GameplayEventStrings2 = eventStrs2;
                        segment.GameplayEventString2Sizes = eventSzs2;
                    }
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

            // --- Remaining trailing sections (marker-based parsing) ---
            ParseRemainingTrailingData(dataSegment, pos, segment);
        }

        private static void ParseRemainingTrailingData(byte[] dataSegment, int start, GameDataSegment segment)
        {
            var end = dataSegment.Length;

            // Clue formatting data (byte array, non-ASCII control codes): to "hotel.pic"
            var hotelStart = FindMarkerString(dataSegment, start, end, "hotel.pic");
            // Split clue formatting and clue details at the message log boundary
            // Message log starts after clue details, before hotel. Find by scanning backwards
            // from hotel for the message log section. Use "Rcvd msg" as marker.
            var msgLogStart = FindMarkerString(dataSegment, start, hotelStart, "Rcvd msg");
            // Walk msgLogStart backwards to find the true start of message log section
            // (there are shorter strings before "Rcvd msg"). Find the first string after clue details.
            // Use approach: clue details end where there's a run of strings without 0x80+ bytes.
            // Simpler: find the boundary between non-ASCII and ASCII sections.
            var asciiStart = start;
            while (asciiStart < hotelStart)
            {
                // Scan forward looking for a string without non-ASCII bytes
                var strStart = asciiStart;
                while (strStart < hotelStart && dataSegment[strStart] == 0) strStart++;
                if (strStart >= hotelStart) break;
                var strEnd = strStart;
                var hasNonAscii = false;
                while (strEnd < hotelStart && dataSegment[strEnd] != 0)
                {
                    if (dataSegment[strEnd] >= 0x80) hasNonAscii = true;
                    strEnd++;
                }
                if (!hasNonAscii && strEnd - strStart > 3)
                {
                    // Found first clean string > 3 chars. Check if previous region had non-ASCII.
                    // This is the boundary between clue data and message log.
                    // Back up to include any leading short clean strings that are part of message log.
                    break;
                }
                asciiStart = strEnd + 1;
            }
            // asciiStart is now at the first clean string after the non-ASCII clue data.
            // But we need to find the true section boundary. Use a simpler approach:
            // Split at known marker. The clue detail section ends before the single-quote chars.
            // After clue details: "'", "'", "(message not decoded)" - these are clean ASCII.
            var messageDecodedPos = FindMarkerString(dataSegment, start, hotelStart, "(message not decoded)");
            // The clean message log section starts a bit before "(message not decoded)"
            // Walk back to find "'" chars
            var quotePos = messageDecodedPos;
            while (quotePos > start && dataSegment[quotePos - 1] == 0) quotePos--;
            while (quotePos > start && dataSegment[quotePos - 1] != 0) quotePos--;
            while (quotePos > start && dataSegment[quotePos - 1] == 0) quotePos--;
            while (quotePos > start && dataSegment[quotePos - 1] != 0) quotePos--;

            // Clue formatting strings (control-byte-aware)
            var (clueFmtStrs, clueFmtSzs) = DataSegmentHelper.ControlStringsFromBytes(
                dataSegment, start, quotePos - start);
            segment.ClueFormattingStrings = clueFmtStrs;
            segment.ClueFormattingSizes = clueFmtSzs;

            // Message log strings: from quotePos to hotel.pic
            var msgLogSize = hotelStart - quotePos;
            var (msgStrs, msgSzs) = DataSegmentHelper.AllNullTerminatedStringsWithSizesFromBytes(
                dataSegment, quotePos, msgLogSize);
            segment.MessageLogStrings = msgStrs;
            segment.MessageLogStringSizes = msgSzs;

            // Hotel menu: from "hotel.pic" to "Data Files"
            var dataFilesStart = FindMarkerString(dataSegment, hotelStart, end, "Data Files");
            var hotelSize = dataFilesStart - hotelStart;
            var (hotelStrs, hotelSzs) = DataSegmentHelper.AllNullTerminatedStringsWithSizesFromBytes(
                dataSegment, hotelStart, hotelSize);
            segment.HotelMenuStrings = hotelStrs;
            segment.HotelMenuStringSizes = hotelSzs;

            // Data files menu: to "You are in the CIA"
            var ciaStart = FindMarkerString(dataSegment, dataFilesStart, end, "You are in the CIA");
            var dfSize = ciaStart - dataFilesStart;
            var (dfStrs, dfSzs) = DataSegmentHelper.AllNullTerminatedStringsWithSizesFromBytes(
                dataSegment, dataFilesStart, dfSize);
            segment.DataFilesMenuStrings = dfStrs;
            segment.DataFilesMenuStringSizes = dfSzs;

            // CIA menus: to "Activity Report Summary"
            var actStart = FindMarkerString(dataSegment, ciaStart, end, "Activity Report Summary");
            var ciaSize = actStart - ciaStart;
            var (ciaStrs, ciaSzs) = DataSegmentHelper.AllNullTerminatedStringsWithSizesFromBytes(
                dataSegment, ciaStart, ciaSize);
            segment.CiaMenuStrings = ciaStrs;
            segment.CiaMenuStringSizes = ciaSzs;

            // Activity/wiretap display: to "Which city" or city suspect section
            var cityStart = FindMarkerString(dataSegment, actStart, end, "Coded Messages");
            // There's a city/suspect section between activity and coded messages
            // Find it by looking for the transition
            var citySuspectStart = actStart;
            // Activity strings end before city/suspect data. Use a byte-count approach.
            // Find "Which organization" as boundary after activity section
            var whichOrgPos = FindMarkerString(dataSegment, actStart, cityStart, "Which organization");
            // Activity ends a few strings before "Which organization"
            // Actually "Which organization" IS an activity string. Let me use "Known Locations" instead
            var knownLocStart = FindMarkerString(dataSegment, whichOrgPos, cityStart, "Known Locations");
            // Go back further - the whole org/city/suspect display runs together.
            // Simpler approach: activity section ends, city/suspect starts at "Which city"
            var whichCityPos = FindMarkerString(dataSegment, actStart, cityStart, "Which city");

            var actSize = whichCityPos - actStart;
            var (actStrs, actSzs) = DataSegmentHelper.AllNullTerminatedStringsWithSizesFromBytes(
                dataSegment, actStart, actSize);
            segment.ActivityWiretapStrings = actStrs;
            segment.ActivityWiretapStringSizes = actSzs;

            // City/suspect data (byte array, has 0x8C control bytes)
            var codedMsgStart = FindMarkerString(dataSegment, whichCityPos, end, "Coded Messages");
            var (citySusStrs, citySusSzs) = DataSegmentHelper.ControlStringsFromBytes(
                dataSegment, whichCityPos, codedMsgStart - whichCityPos);
            segment.CitySuspectStrings = citySusStrs;
            segment.CitySuspectSizes = citySusSzs;

            // Coded messages: to "$VICTIM"
            var tokensStart = FindMarkerString(dataSegment, codedMsgStart, end, "$VICTIM");
            var codedSize = tokensStart - codedMsgStart;
            var (codedStrs, codedSzs) = DataSegmentHelper.AllNullTerminatedStringsWithSizesFromBytes(
                dataSegment, codedMsgStart, codedSize);
            segment.CodedMessageStrings = codedStrs;
            segment.CodedMessageStringSizes = codedSzs;

            // Substitution tokens: to clue lookup section (has non-ASCII, find "This information")
            var clueInfoStart = FindMarkerString(dataSegment, tokensStart, end, "This information");
            // Back up to include the fscanf format strings before "This information"
            // The clue lookup section starts with format strings containing 0x80+ bytes
            // Find the boundary by scanning backwards for non-ASCII
            var clueLookupStart = clueInfoStart;
            // Scan back past clean strings to find where tokens end
            var scanBack = clueInfoStart - 1;
            while (scanBack > tokensStart && dataSegment[scanBack] == 0) scanBack--;
            while (scanBack > tokensStart && dataSegment[scanBack] != 0) scanBack--;
            clueLookupStart = scanBack + 1;
            // Actually just use the format string "%[^\n" as marker
            var fmtStart = FindMarkerString(dataSegment, tokensStart, end, "%[^");
            if (fmtStart < clueInfoStart)
                clueLookupStart = fmtStart;

            // Scan forward from tokens to find first non-ASCII byte (pointer table / control codes)
            clueLookupStart = tokensStart;
            while (clueLookupStart < end && dataSegment[clueLookupStart] < 0x80) clueLookupStart++;
            // Back up to the preceding null terminator boundary
            while (clueLookupStart > tokensStart && dataSegment[clueLookupStart - 1] != 0) clueLookupStart--;

            var tokensSize = clueLookupStart - tokensStart;
            var (tokenStrs, tokenSzs) = DataSegmentHelper.AllNullTerminatedStringsWithSizesFromBytes(
                dataSegment, tokensStart, tokensSize);
            segment.SubstitutionTokenStrings = tokenStrs;
            segment.SubstitutionTokenStringSizes = tokenSzs;

            // Clue lookup data (byte array, non-ASCII): to "Bulletin: "
            var bulletinStart = FindMarkerString(dataSegment, clueLookupStart, end, "Bulletin: ");
            // Split clue lookup: pre-message data, not-found message, post-message strings
            var clueNotFoundPos = FindMarkerString(dataSegment, clueLookupStart, bulletinStart, "This information");
            segment.CluePreMessageData = DataSegmentHelper.Slice(dataSegment, clueLookupStart, clueNotFoundPos - clueLookupStart);

            var clueNotFoundEnd = clueNotFoundPos;
            while (clueNotFoundEnd < bulletinStart && dataSegment[clueNotFoundEnd] != 0) clueNotFoundEnd++;
            segment.ClueNotFoundMessage = Encoding.ASCII.GetString(dataSegment, clueNotFoundPos, clueNotFoundEnd - clueNotFoundPos);
            var postMessageStart = clueNotFoundEnd + 1;

            var (cluePostStrs, cluePostSzs) = DataSegmentHelper.ControlStringsFromBytes(
                dataSegment, postMessageStart, bulletinStart - postMessageStart);
            segment.CluePostMessageStrings = cluePostStrs;
            segment.CluePostMessageSizes = cluePostSzs;

            // Bulletin/surveillance strings: to chronology section
            // Find " 10" followed by "00:00 AM" as chronology marker
            var chronoStart = FindMarkerString(dataSegment, bulletinStart, end, "00:00 AM");
            // Back up to include the " 10" before it
            while (chronoStart > bulletinStart && dataSegment[chronoStart - 1] != 0) chronoStart--;
            while (chronoStart > bulletinStart && dataSegment[chronoStart - 1] == 0) chronoStart--;
            while (chronoStart > bulletinStart && dataSegment[chronoStart - 1] != 0) chronoStart--;
            chronoStart++; // include the " 10" string start... actually let me use simpler marker
            chronoStart = FindMarkerString(dataSegment, bulletinStart, end, "00:00 AM");
            // " 10\0" is 4 bytes before "00:00 AM"
            chronoStart -= 4;

            var bulletinSize = chronoStart - bulletinStart;
            var (bulStrs, bulSzs) = DataSegmentHelper.AllNullTerminatedStringsWithSizesFromBytes(
                dataSegment, bulletinStart, bulletinSize);
            segment.BulletinStrings = bulStrs;
            segment.BulletinStringSizes = bulSzs;

            // Chronology/status data (byte array, has control codes): to "officem.pan"
            var researchStart = FindMarkerString(dataSegment, chronoStart, end, "officem.pan");
            var (chronoStrs, chronoSzs) = DataSegmentHelper.ControlStringsFromBytes(
                dataSegment, chronoStart, researchStart - chronoStart);
            segment.ChronologyStatusStrings = chronoStrs;
            segment.ChronologyStatusSizes = chronoSzs;

            // Research assistant strings (control-byte-aware): to "Select Load File"
            var saveLoadStart = FindMarkerString(dataSegment, researchStart, end, "Select Load File");
            var (resStrs, resSzs) = DataSegmentHelper.ControlStringsFromBytes(
                dataSegment, researchStart, saveLoadStart - researchStart);
            segment.ResearchAssistantStrings = resStrs;
            segment.ResearchAssistantSizes = resSzs;

            // Save/load data (byte array, has 0x8F): to exe chain section
            // Exe chain starts after save/load. Find "final.exe" as marker.
            var exeChainStart = FindMarkerString(dataSegment, saveLoadStart, end, "final.exe");
            // Back up to include "env.sve" and "File Error:" before final.exe
            var fileErrorStart = FindMarkerString(dataSegment, saveLoadStart, exeChainStart + 1, "File Error:");
            // There are two "File Error:" strings. The first is in save/load, second in exe chain.
            // Find the second one
            var secondFileError = FindMarkerString(dataSegment, fileErrorStart + 11, end, "File Error:");
            // Exe chain starts before second "File Error:" - find "env.sve" near it
            var envSvePos = FindMarkerString(dataSegment, secondFileError - 20, end, "env.sve");
            exeChainStart = envSvePos;

            var (slStrs, slSzs) = DataSegmentHelper.ControlStringsFromBytes(
                dataSegment, saveLoadStart, exeChainStart - saveLoadStart);
            segment.SaveLoadStrings = slStrs;
            segment.SaveLoadSizes = slSzs;

            // Exe chain data (byte array): to "PANI"
            var paniStart = FindMarkerString(dataSegment, exeChainStart, end, "PANI");
            segment.ExeChainData = DataSegmentHelper.Slice(dataSegment, exeChainStart, paniStart - exeChainStart);

            // Game state data (PANI + RastPort, byte array): to "Allocated"
            var runtimeStart = FindMarkerString(dataSegment, paniStart, end, "Allocated");
            segment.GameStateData = DataSegmentHelper.Slice(dataSegment, paniStart, runtimeStart - paniStart);

            // Runtime/BSS trailing data (not editable)
            segment.RuntimeTrailingData = DataSegmentHelper.Slice(dataSegment, runtimeStart, end - runtimeStart);
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
                DataSegmentHelper.NullTerminatedStringsToFixedBytes(GuardAlertnessLabels, GuardAlertnessLabelSizes),
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

            // Serialize remaining sections
            var msgLogBytes = DataSegmentHelper.NullTerminatedStringsToFixedBytes(MessageLogStrings, MessageLogStringSizes);
            var hotelBytes = DataSegmentHelper.NullTerminatedStringsToFixedBytes(HotelMenuStrings, HotelMenuStringSizes);
            var dfBytes = DataSegmentHelper.NullTerminatedStringsToFixedBytes(DataFilesMenuStrings, DataFilesMenuStringSizes);
            var ciaBytes = DataSegmentHelper.NullTerminatedStringsToFixedBytes(CiaMenuStrings, CiaMenuStringSizes);
            var actBytes = DataSegmentHelper.NullTerminatedStringsToFixedBytes(ActivityWiretapStrings, ActivityWiretapStringSizes);
            var codedBytes = DataSegmentHelper.NullTerminatedStringsToFixedBytes(CodedMessageStrings, CodedMessageStringSizes);
            var tokenBytes = DataSegmentHelper.NullTerminatedStringsToFixedBytes(SubstitutionTokenStrings, SubstitutionTokenStringSizes);
            var bulletinBytes = DataSegmentHelper.NullTerminatedStringsToFixedBytes(BulletinStrings, BulletinStringSizes);

            return DataSegmentHelper.Concatenate(
                new byte[IntelPaddingSize],        // 3 null bytes
                intelTxtBytes,
                rankBytes,
                evTypeBytes,
                evItemBytes,
                new byte[EvidenceEndPaddingSize],   // 1 null byte
                evRankPtrPlaceholder,
                invMethodBytes,
                DataSegmentHelper.ControlStringsToFixedBytes(ClueFormattingStrings, ClueFormattingSizes),
                msgLogBytes,
                hotelBytes,
                dfBytes,
                ciaBytes,
                actBytes,
                DataSegmentHelper.ControlStringsToFixedBytes(CitySuspectStrings, CitySuspectSizes),
                codedBytes,
                tokenBytes,
                CluePreMessageData,
                Encoding.ASCII.GetBytes(ClueNotFoundMessage),
                new byte[] { 0 },
                DataSegmentHelper.ControlStringsToFixedBytes(CluePostMessageStrings, CluePostMessageSizes),
                bulletinBytes,
                DataSegmentHelper.ControlStringsToFixedBytes(ChronologyStatusStrings, ChronologyStatusSizes),
                DataSegmentHelper.ControlStringsToFixedBytes(ResearchAssistantStrings, ResearchAssistantSizes),
                DataSegmentHelper.ControlStringsToFixedBytes(SaveLoadStrings, SaveLoadSizes),
                ExeChainData,
                GameStateData,
                RuntimeTrailingData
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
                GuardAlertnessLabels = GuardAlertnessLabels.Select(s => s).ToArray(),
                GuardAlertnessLabelSizes = GuardAlertnessLabelSizes.ToArray(),
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
                ClueFormattingStrings = ClueFormattingStrings.Select(s => s).ToArray(),
                ClueFormattingSizes = ClueFormattingSizes.ToArray(),
                MessageLogStrings = MessageLogStrings.Select(s => s).ToArray(),
                MessageLogStringSizes = MessageLogStringSizes.ToArray(),
                HotelMenuStrings = HotelMenuStrings.Select(s => s).ToArray(),
                HotelMenuStringSizes = HotelMenuStringSizes.ToArray(),
                DataFilesMenuStrings = DataFilesMenuStrings.Select(s => s).ToArray(),
                DataFilesMenuStringSizes = DataFilesMenuStringSizes.ToArray(),
                CiaMenuStrings = CiaMenuStrings.Select(s => s).ToArray(),
                CiaMenuStringSizes = CiaMenuStringSizes.ToArray(),
                ActivityWiretapStrings = ActivityWiretapStrings.Select(s => s).ToArray(),
                ActivityWiretapStringSizes = ActivityWiretapStringSizes.ToArray(),
                CitySuspectStrings = CitySuspectStrings.Select(s => s).ToArray(),
                CitySuspectSizes = CitySuspectSizes.ToArray(),
                CodedMessageStrings = CodedMessageStrings.Select(s => s).ToArray(),
                CodedMessageStringSizes = CodedMessageStringSizes.ToArray(),
                SubstitutionTokenStrings = SubstitutionTokenStrings.Select(s => s).ToArray(),
                SubstitutionTokenStringSizes = SubstitutionTokenStringSizes.ToArray(),
                CluePreMessageData = CluePreMessageData.ToArray(),
                ClueNotFoundMessage = ClueNotFoundMessage,
                CluePostMessageStrings = CluePostMessageStrings.Select(s => s).ToArray(),
                CluePostMessageSizes = CluePostMessageSizes.ToArray(),
                BulletinStrings = BulletinStrings.Select(s => s).ToArray(),
                BulletinStringSizes = BulletinStringSizes.ToArray(),
                ChronologyStatusStrings = ChronologyStatusStrings.Select(s => s).ToArray(),
                ChronologyStatusSizes = ChronologyStatusSizes.ToArray(),
                ResearchAssistantStrings = ResearchAssistantStrings.Select(s => s).ToArray(),
                ResearchAssistantSizes = ResearchAssistantSizes.ToArray(),
                SaveLoadStrings = SaveLoadStrings.Select(s => s).ToArray(),
                SaveLoadSizes = SaveLoadSizes.ToArray(),
                ExeChainData = ExeChainData.ToArray(),
                GameStateData = GameStateData.ToArray(),
                RuntimeTrailingData = RuntimeTrailingData.ToArray()
            };
        }
    }
}
