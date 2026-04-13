using System;
using System.Linq;
using System.Text;
using CovertActionTools.Core.Models.Executables.Records.Tac;
using CovertActionTools.Core.Models.Executables.Sections;
using CovertActionTools.Core.Models.Executables.Sections.Shared;
using CovertActionTools.Core.Models.Executables.Sections.Tac;

namespace CovertActionTools.Core.Models.Executables
{
    public class TacScreenCoordinate
    {
        public const int RecordSize = 4;

        public ushort X { get; set; }
        public ushort Y { get; set; }

        public TacScreenCoordinate Clone()
        {
            return new TacScreenCoordinate { X = X, Y = Y };
        }

        public static TacScreenCoordinate FromBytes(byte[] data, int offset)
        {
            return new TacScreenCoordinate
            {
                X = BitConverter.ToUInt16(data, offset),
                Y = BitConverter.ToUInt16(data, offset + 2)
            };
        }

        public byte[] ToBytes()
        {
            var result = new byte[RecordSize];
            result[0] = (byte)(X & 0xFF);
            result[1] = (byte)((X >> 8) & 0xFF);
            result[2] = (byte)(Y & 0xFF);
            result[3] = (byte)((Y >> 8) & 0xFF);
            return result;
        }
    }

    public class TacScreenRect
    {
        public const int RecordSize = 8;

        public ushort X1 { get; set; }
        public ushort Y1 { get; set; }
        public ushort X2 { get; set; }
        public ushort Y2 { get; set; }

        public TacScreenRect Clone()
        {
            return new TacScreenRect { X1 = X1, Y1 = Y1, X2 = X2, Y2 = Y2 };
        }

        public static TacScreenRect FromBytes(byte[] data, int offset)
        {
            return new TacScreenRect
            {
                X1 = BitConverter.ToUInt16(data, offset),
                Y1 = BitConverter.ToUInt16(data, offset + 2),
                X2 = BitConverter.ToUInt16(data, offset + 4),
                Y2 = BitConverter.ToUInt16(data, offset + 6)
            };
        }

        public byte[] ToBytes()
        {
            var result = new byte[RecordSize];
            result[0] = (byte)(X1 & 0xFF); result[1] = (byte)((X1 >> 8) & 0xFF);
            result[2] = (byte)(Y1 & 0xFF); result[3] = (byte)((Y1 >> 8) & 0xFF);
            result[4] = (byte)(X2 & 0xFF); result[5] = (byte)((X2 >> 8) & 0xFF);
            result[6] = (byte)(Y2 & 0xFF); result[7] = (byte)((Y2 >> 8) & 0xFF);
            return result;
        }
    }

    /// <summary>
    /// Structured data segment for TAC.EXE.
    /// Field boundaries and interpretations are based on reverse engineering and may not
    /// be fully accurate. Unknown regions are preserved as raw byte arrays.
    /// </summary>
    public class TacDataSegment
    {
        /// <summary>DS paragraph value for TAC.EXE.</summary>
        public const int DsParagraph = 0x10E3;

        #region Layout Constants (DS-relative offsets)
        private const int RoomTypesOffset = 0x00B2;
        private const int RoomTypeCount = 10;
        private const int ObjectsOffset = 0x018E;
        private const int ObjectCount = 62;
        private const int EquipmentPointersOffset = 0x20E0;
        private const int EquipmentPointerCount = 16;
        private const int EquipNavTableOffset = 0x2100;
        private const int EquipNavTableCount = 48;
        private const int RagdollCoordsOffset = 0x2160;
        private const int RagdollCoordCount = 44;
        private const int EquipSlotRectsOffset = 0x2214;
        private const int EquipSlotRectCount = 11;
        private const int CharNamePointersOffset = 0x346C;
        private const int CharNamePointerCount = 192;
        #endregion

        #region MidSection Sub-offsets (DS-relative)
        private const int MovementPixelCount = 9;
        private const int JumpingTileCount = 9;
        private const int TileAdjacencyCount = 4;
        private const int UnreferencedGapSize = 6;
        private const int SpriteConfigsSize = 60;
        // End of the Rendering section / start of CgaColorRemap. GameplayData begins
        // after all Rendering-followup typed sections (CgaColorRemap .. TacGameplayStrings)
        // at 0x1F36.
        private const int GameplayDataStart = 0x1F36;
        #endregion

        #region PreCharNameData Sub-offsets (DS-relative)
        private const int CluePhrasesStart = 0x226C;
        private const int MonthAbbrevsStart = 0x248C;
        private const int IntelHeadersStart = 0x24BC;
        private const int CluePhraseTableOffset = 0x2542;
        private const int CluePhraseTableSize = 80;
        private const int ClueCategoryDataOffset = 0x2592;
        private const int ClueCategoryDataSize = 48;
        private const int MonthTableOffset = 0x25C2;
        private const int MonthTableSize = 24;
        private const int IntelPaddingOffset = 0x25DA;
        private const int IntelPaddingSize = 3;
        private const int IntelTextsStart = 0x25DD;
        private const int RankNamesStart = 0x274C;
        private const int EvidenceTypesStart = 0x279F;
        private const int EvidenceItemsStart = 0x27BB;
        private const int EvidenceTableOffset = 0x2A26;
        private const int EvidenceTableSize = 160;
        private const int InvestMethodsStart = 0x2AC6;
        private const int ClueSystemStart = 0x2B4C;
        #endregion

        #region Fields (in binary order)
        public ExeFileHeaderSection Header { get; set; } = new();
        public TacHeaderFilenamesSection HeaderFilenames { get; set; } = new();
        public RoomTypeSection RoomTypes { get; set; } = new();
        public MapObjectTypeSection MapObjectTypes { get; set; } = new();
        public MovementSection Movement { get; set; } = new();
        public RenderingSection Rendering { get; set; } = new();
        public CgaColorRemapSection CgaColorRemap { get; set; } = new();
        public TacGameSettingsSection GameSettings { get; set; } = new();
        public VgaPaletteRemapSection VgaPaletteRemap { get; set; } = new();
        public TacStubOutputCaptureSection StubOutputCapture { get; set; } = new();
        public TacMissionStateBlockSection MissionStateBlock { get; set; } = new();
        public TacPlayerDirectionStateSection PlayerDirectionState { get; set; } = new();
        public TacMenuStringsSection MenuStrings { get; set; } = new();
        public TacInputConfigSection InputConfig { get; set; } = new();
        public TacAlertLevelColorsSection AlertLevelColors { get; set; } = new();
        public TacGameplayStringsSection GameplayStrings { get; set; } = new();

        /// <summary>
        /// Post-string-block binary data: additional sprite configs, CGA animation frames,
        /// VGA palette remap tables, and remaining binary config data between the gameplay
        /// dialogue strings and the equipment name string block.
        /// </summary>
        public byte[] GameplayData { get; set; } = Array.Empty<byte>();

        /// <summary>16 equipment name strings (resolved from DS-relative pointers).</summary>
        public string[] EquipmentNames { get; set; } = Array.Empty<string>();

        /// <summary>Data after equipment names but before equipment pointer table (equip2.pic filename).</summary>
        public byte[] MidSectionPostEquipNames { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Equipment selection UI navigation table: 12 rows x 4 columns (Up, Down, Left, Right).
        /// Each cell is the equipment index to navigate to when that arrow key is pressed.
        /// </summary>
        public ushort[] EquipmentNavTable { get; set; } = Array.Empty<ushort>();

        /// <summary>43 screen coordinates for ragdoll item positions + (0,0) terminator.</summary>
        public TacScreenCoordinate[] RagdollCoordinates { get; set; } = Array.Empty<TacScreenCoordinate>();

        /// <summary>4-byte padding between ragdoll coordinates and equipment slot rects (no direct code references).</summary>
        public byte[] RagdollRectPadding { get; set; } = Array.Empty<byte>();

        /// <summary>11 TL/BR rectangle pairs for equipment slot UI positions.</summary>
        public TacScreenRect[] EquipmentSlotRects { get; set; } = Array.Empty<TacScreenRect>();

        #region PreCharNameData (between Equipment Slot Rects and Character Names)

        // TODO: Clue relationship phrases are duplicated across multiple EXEs (TAC, FINAL, GAME, BUG).
        // These should be merged into a shared data segment model so editing in one EXE updates all.
        /// <summary>40 clue relationship phrases used in evidence connections (e.g. " tied to ", " registered to ").</summary>
        public string[] ClueRelationshipPhrases { get; set; } = Array.Empty<string>();
        /// <summary>Original byte sizes for clue phrase slots.</summary>
        public int[] CluePhraseSizes { get; set; } = Array.Empty<int>();

        /// <summary>12 month abbreviations: "Jan", "Feb", ... "Dec".</summary>
        public string[] MonthAbbreviations { get; set; } = Array.Empty<string>();
        /// <summary>Original byte sizes for month abbreviation slots.</summary>
        public int[] MonthSizes { get; set; } = Array.Empty<int>();

        // TODO: Investigate string identification for IntelHeaders -- the region from 0x24BC to
        // 0x2542 is extracted as null-terminated strings, but some entries are empty or single-char
        // filler values that may be binary data misidentified as strings. The boundary between
        // headers and the clue phrase pointer table needs verification.
        /// <summary>Intel report headers and filler text fragments ("CODED MESSAGE:", "MEETING NOTES:", etc.).</summary>
        public string[] IntelHeaders { get; set; } = Array.Empty<string>();
        /// <summary>Original byte sizes for intel header slots.</summary>
        public int[] IntelHeaderSizes { get; set; } = Array.Empty<int>();

        /// <summary>Pointer table for the 40 clue relationship phrases (DS-relative offsets, preserved as raw bytes).</summary>
        public byte[] CluePhrasePointerTable { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// 48-byte clue category and popcount lookup table. Identical across FINAL/TAC/GAME.
        /// Bytes 0-15: category bit flags per clue pair (values 1/2/4/8 = single-bit masks).
        /// Bytes 16-23: popcount(0-7) + 0 lookup.
        /// Bytes 24-31: popcount(0-7) + 1 lookup.
        /// Bytes 32-39: popcount(0-7) + 1 lookup (duplicate of 24-31).
        /// Bytes 40-47: popcount(0-7) + 2 lookup.
        /// TODO: investigate how the clue system uses this table — the bit flags likely map
        /// clue slots to evidence categories, and the popcount sub-tables count active categories
        /// for a given bitmask. Trace from GAME.EXE clue processing code to confirm.
        /// </summary>
        public byte[] ClueCategoryData { get; set; } = Array.Empty<byte>();

        /// <summary>Pointer table for the 12 month abbreviations (DS-relative offsets, preserved as raw bytes).</summary>
        public byte[] MonthPointerTable { get; set; } = Array.Empty<byte>();

        /// <summary>Padding bytes between month pointer table and intel report texts.</summary>
        public byte[] IntelMidPadding { get; set; } = Array.Empty<byte>();

        // TODO: Investigate string identification for IntelReportTexts -- the region from 0x25DD to
        // 0x274C is extracted as null-terminated strings, but some entries are empty or single-char
        // values that may be binary data or format control codes rather than displayable text.
        /// <summary>Intel report text templates used in the clue/intel display system.</summary>
        public string[] IntelReportTexts { get; set; } = Array.Empty<string>();
        /// <summary>Original byte sizes for intel report text slots.</summary>
        public int[] IntelReportTextSizes { get; set; } = Array.Empty<int>();

        /// <summary>8 agent rank names: "Recruit", "Operative", ... "MasterMind".</summary>
        public string[] RankNames { get; set; } = Array.Empty<string>();
        /// <summary>Original byte sizes for rank name slots.</summary>
        public int[] RankNameSizes { get; set; } = Array.Empty<int>();

        /// <summary>8 evidence type abbreviations: "CAR", "WPN", "ADR", "TKT", "MSG", "$", "$", "FCE".</summary>
        public string[] EvidenceTypeAbbreviations { get; set; } = Array.Empty<string>();
        /// <summary>Original byte sizes for evidence type slots.</summary>
        public int[] EvidenceTypeSizes { get; set; } = Array.Empty<int>();

        /// <summary>Evidence item name templates (cars, weapons, addresses, tickets, messages, money, IDs).</summary>
        public string[] EvidenceItemNames { get; set; } = Array.Empty<string>();
        /// <summary>Original byte sizes for evidence item name slots.</summary>
        public int[] EvidenceItemSizes { get; set; } = Array.Empty<int>();

        /// <summary>Combined pointer table for rank names, evidence types, and evidence items (preserved as raw bytes).</summary>
        public byte[] EvidenceRankPointerTable { get; set; } = Array.Empty<byte>();

        /// <summary>8 investigation method names: "Clandestine Photo", ... "Local Authorities", "Clue".</summary>
        public string[] InvestigationMethods { get; set; } = Array.Empty<string>();
        /// <summary>Original byte sizes for investigation method slots.</summary>
        public int[] InvestigationMethodSizes { get; set; } = Array.Empty<int>();

        /// <summary>Remaining clue system data: UI text, template variables, file references, text.dta lookups.</summary>
        public byte[] ClueSystemData { get; set; } = Array.Empty<byte>();

        #endregion

        /// <summary>192 character names (4 ethnic groups x female first / male first / male surname, 16 each).</summary>
        public string[] CharacterNames { get; set; } = Array.Empty<string>();

        /// <summary>Data between character names and character name pointer table.</summary>
        public byte[] PostCharNameData { get; set; } = Array.Empty<byte>();

        /// <summary>Everything after character name pointers: C runtime, BSS.</summary>
        public byte[] TrailingData { get; set; } = Array.Empty<byte>();

        #endregion

        public static TacDataSegment FromBytes(byte[] dataSegment)
        {
            var ragdollEnd = RagdollCoordsOffset + RagdollCoordCount * TacScreenCoordinate.RecordSize;

            var segment = new TacDataSegment();

            var offset = 0;
            offset += segment.Header.ReadBytes(dataSegment, offset);
            offset += segment.HeaderFilenames.ReadBytes(dataSegment, offset);
            offset += segment.RoomTypes.ReadBytes(dataSegment, offset);
            offset += segment.MapObjectTypes.ReadBytes(dataSegment, offset);
            offset += segment.Movement.ReadBytes(dataSegment, offset);
            offset += segment.Rendering.ReadBytes(dataSegment, offset);
            offset = ReadSectionWithPadding(segment.CgaColorRemap, dataSegment, offset);
            offset = ReadSectionWithPadding(segment.GameSettings, dataSegment, offset);
            offset = ReadSectionWithPadding(segment.VgaPaletteRemap, dataSegment, offset);
            offset = ReadSectionWithPadding(segment.StubOutputCapture, dataSegment, offset);
            offset = ReadSectionWithPadding(segment.MissionStateBlock, dataSegment, offset);
            offset = ReadSectionWithPadding(segment.PlayerDirectionState, dataSegment, offset);
            offset = ReadSectionWithPadding(segment.MenuStrings, dataSegment, offset);
            offset = ReadSectionWithPadding(segment.InputConfig, dataSegment, offset);
            offset = ReadSectionWithPadding(segment.AlertLevelColors, dataSegment, offset);
            offset = ReadSectionWithPadding(segment.GameplayStrings, dataSegment, offset);

            // Read equipment name pointers to find and extract the strings from the mid section
            var equipPtrs = DataSegmentHelper.BytesToUInt16Array(dataSegment, EquipmentPointersOffset, EquipmentPointerCount);
            segment.EquipmentNames = DataSegmentHelper.ExtractStringsFromPointers(equipPtrs, dataSegment);

            var (blockStart, blockEnd) = DataSegmentHelper.FindStringBlockBounds(equipPtrs, dataSegment);

            segment.GameplayData = DataSegmentHelper.Slice(dataSegment, GameplayDataStart, blockStart - GameplayDataStart);

            segment.MidSectionPostEquipNames = DataSegmentHelper.Slice(dataSegment, blockEnd, EquipmentPointersOffset - blockEnd);

            segment.EquipmentNavTable = DataSegmentHelper.BytesToUInt16Array(dataSegment, EquipNavTableOffset, EquipNavTableCount);

            segment.RagdollCoordinates = new TacScreenCoordinate[RagdollCoordCount];
            for (var i = 0; i < RagdollCoordCount; i++)
            {
                segment.RagdollCoordinates[i] = TacScreenCoordinate.FromBytes(dataSegment, RagdollCoordsOffset + i * TacScreenCoordinate.RecordSize);
            }

            segment.RagdollRectPadding = DataSegmentHelper.Slice(dataSegment, ragdollEnd, EquipSlotRectsOffset - ragdollEnd);

            segment.EquipmentSlotRects = new TacScreenRect[EquipSlotRectCount];
            for (var i = 0; i < EquipSlotRectCount; i++)
            {
                segment.EquipmentSlotRects[i] = TacScreenRect.FromBytes(dataSegment, EquipSlotRectsOffset + i * TacScreenRect.RecordSize);
            }

            #region Parse PreCharNameData sub-sections

            var (cluePhrases, clueSizes) = DataSegmentHelper.ControlStringsFromBytes(
                dataSegment, CluePhrasesStart, MonthAbbrevsStart - CluePhrasesStart);
            segment.ClueRelationshipPhrases = cluePhrases;
            segment.CluePhraseSizes = clueSizes;

            var (months, monthSzs) = DataSegmentHelper.ControlStringsFromBytes(
                dataSegment, MonthAbbrevsStart, IntelHeadersStart - MonthAbbrevsStart);
            segment.MonthAbbreviations = months;
            segment.MonthSizes = monthSzs;

            var (intelHdrs, intelHdrSzs) = DataSegmentHelper.ControlStringsFromBytes(
                dataSegment, IntelHeadersStart, CluePhraseTableOffset - IntelHeadersStart);
            segment.IntelHeaders = intelHdrs;
            segment.IntelHeaderSizes = intelHdrSzs;

            segment.CluePhrasePointerTable = DataSegmentHelper.Slice(dataSegment, CluePhraseTableOffset, CluePhraseTableSize);
            segment.ClueCategoryData = DataSegmentHelper.Slice(dataSegment, ClueCategoryDataOffset, ClueCategoryDataSize);
            segment.MonthPointerTable = DataSegmentHelper.Slice(dataSegment, MonthTableOffset, MonthTableSize);
            segment.IntelMidPadding = DataSegmentHelper.Slice(dataSegment, IntelPaddingOffset, IntelPaddingSize);

            var (intelTexts, intelTextSzs) = DataSegmentHelper.ControlStringsFromBytes(
                dataSegment, IntelTextsStart, RankNamesStart - IntelTextsStart);
            segment.IntelReportTexts = intelTexts;
            segment.IntelReportTextSizes = intelTextSzs;

            var (ranks, rankSzs) = DataSegmentHelper.ControlStringsFromBytes(
                dataSegment, RankNamesStart, EvidenceTypesStart - RankNamesStart);
            segment.RankNames = ranks;
            segment.RankNameSizes = rankSzs;

            var (evTypes, evTypeSzs) = DataSegmentHelper.ControlStringsFromBytes(
                dataSegment, EvidenceTypesStart, EvidenceItemsStart - EvidenceTypesStart);
            segment.EvidenceTypeAbbreviations = evTypes;
            segment.EvidenceTypeSizes = evTypeSzs;

            var (evItems, evItemSzs) = DataSegmentHelper.ControlStringsFromBytes(
                dataSegment, EvidenceItemsStart, EvidenceTableOffset - EvidenceItemsStart);
            segment.EvidenceItemNames = evItems;
            segment.EvidenceItemSizes = evItemSzs;

            segment.EvidenceRankPointerTable = DataSegmentHelper.Slice(dataSegment, EvidenceTableOffset, EvidenceTableSize);

            var (invMethods, invMethodSzs) = DataSegmentHelper.ControlStringsFromBytes(
                dataSegment, InvestMethodsStart, ClueSystemStart - InvestMethodsStart);
            segment.InvestigationMethods = invMethods;
            segment.InvestigationMethodSizes = invMethodSzs;

            // Extract character names using pointer table
            var charPtrs = DataSegmentHelper.BytesToUInt16Array(dataSegment, CharNamePointersOffset, CharNamePointerCount);
            segment.CharacterNames = DataSegmentHelper.ExtractStringsFromPointers(charPtrs, dataSegment);

            var (charBlockStart, charBlockEnd) = DataSegmentHelper.FindStringBlockBounds(charPtrs, dataSegment);
            segment.ClueSystemData = DataSegmentHelper.Slice(dataSegment, ClueSystemStart, charBlockStart - ClueSystemStart);

            #endregion

            var postCharLen = CharNamePointersOffset - charBlockEnd;
            segment.PostCharNameData = postCharLen > 0
                ? DataSegmentHelper.Slice(dataSegment, charBlockEnd, postCharLen)
                : Array.Empty<byte>();

            var charPtrsEnd = CharNamePointersOffset + CharNamePointerCount * 2;
            segment.TrailingData = DataSegmentHelper.Slice(dataSegment, charPtrsEnd, dataSegment.Length - charPtrsEnd);

            return segment;
        }

        public byte[] ToBytes()
        {
            var ragdollBytes = new byte[RagdollCoordCount * TacScreenCoordinate.RecordSize];
            for (var i = 0; i < RagdollCoordinates.Length; i++)
            {
                Array.Copy(RagdollCoordinates[i].ToBytes(), 0, ragdollBytes, i * TacScreenCoordinate.RecordSize, TacScreenCoordinate.RecordSize);
            }

            var equipRectBytes = new byte[EquipSlotRectCount * TacScreenRect.RecordSize];
            for (var i = 0; i < EquipmentSlotRects.Length; i++)
            {
                Array.Copy(EquipmentSlotRects[i].ToBytes(), 0, equipRectBytes, i * TacScreenRect.RecordSize, TacScreenRect.RecordSize);
            }

            var typedSectionBytes = DataSegmentHelper.Concatenate(
                CgaColorRemap,
                GameSettings,
                VgaPaletteRemap,
                StubOutputCapture,
                MissionStateBlock,
                PlayerDirectionState,
                MenuStrings,
                InputConfig,
                AlertLevelColors,
                GameplayStrings
            );

            // Compute equipment name pointer values from actual string positions. The base
            // offset accounts for all typed sections plus the remaining GameplayData blob.
            var equipNamesBaseOffset = GameplayDataStart + GameplayData.Length;
            var equipNamePointers = DataSegmentHelper.ComputeStringPointers(EquipmentNames, equipNamesBaseOffset);
            var equipNamesBytes = DataSegmentHelper.NullTerminatedStringsToBytes(EquipmentNames);

            // Serialize PreCharNameData sub-sections
            var preCharNameBytes = DataSegmentHelper.Concatenate(
                DataSegmentHelper.ControlStringsToFixedBytes(ClueRelationshipPhrases, CluePhraseSizes),
                DataSegmentHelper.ControlStringsToFixedBytes(MonthAbbreviations, MonthSizes),
                DataSegmentHelper.ControlStringsToFixedBytes(IntelHeaders, IntelHeaderSizes),
                CluePhrasePointerTable,
                ClueCategoryData,
                MonthPointerTable,
                IntelMidPadding,
                DataSegmentHelper.ControlStringsToFixedBytes(IntelReportTexts, IntelReportTextSizes),
                DataSegmentHelper.ControlStringsToFixedBytes(RankNames, RankNameSizes),
                DataSegmentHelper.ControlStringsToFixedBytes(EvidenceTypeAbbreviations, EvidenceTypeSizes),
                DataSegmentHelper.ControlStringsToFixedBytes(EvidenceItemNames, EvidenceItemSizes),
                EvidenceRankPointerTable,
                DataSegmentHelper.ControlStringsToFixedBytes(InvestigationMethods, InvestigationMethodSizes),
                ClueSystemData
            );

            // Compute character name pointer values
            var charNamesBaseOffset = equipNamesBaseOffset + equipNamesBytes.Length
                + MidSectionPostEquipNames.Length
                + DataSegmentHelper.UInt16ArrayToBytes(equipNamePointers).Length
                + DataSegmentHelper.UInt16ArrayToBytes(EquipmentNavTable).Length
                + ragdollBytes.Length + RagdollRectPadding.Length
                + equipRectBytes.Length + preCharNameBytes.Length;
            var charNamePointers = DataSegmentHelper.ComputeStringPointers(CharacterNames, charNamesBaseOffset);
            var charNamesBytes = DataSegmentHelper.NullTerminatedStringsToBytes(CharacterNames);

            return DataSegmentHelper.Concatenate(
                Header.WriteBytes(),
                HeaderFilenames.WriteBytes(),
                RoomTypes.WriteBytes(),
                MapObjectTypes.WriteBytes(),
                Movement.WriteBytes(),
                Rendering.WriteBytes(),
                typedSectionBytes,
                GameplayData,
                equipNamesBytes,
                MidSectionPostEquipNames,
                DataSegmentHelper.UInt16ArrayToBytes(equipNamePointers),
                DataSegmentHelper.UInt16ArrayToBytes(EquipmentNavTable),
                ragdollBytes,
                RagdollRectPadding,
                equipRectBytes,
                preCharNameBytes,
                charNamesBytes,
                PostCharNameData,
                DataSegmentHelper.UInt16ArrayToBytes(charNamePointers),
                TrailingData
            );
        }

        private static int ReadSectionWithPadding(IExecutableSection section, byte[] data, int offset)
        {
            offset += section.ReadBytes(data, offset);
            if (section is IPaddedToWord && offset % 2 != 0)
                offset++;
            else if (section is IPaddedToParagraph && offset % 16 != 0)
                offset += 16 - (offset % 16);
            return offset;
        }

        private static byte[] ShortArrayToBytes(short[] values)
        {
            var result = new byte[values.Length * 2];
            for (var i = 0; i < values.Length; i++)
            {
                result[i * 2] = (byte)(values[i] & 0xFF);
                result[i * 2 + 1] = (byte)((values[i] >> 8) & 0xFF);
            }
            return result;
        }

        public TacDataSegment Clone()
        {
            return new TacDataSegment
            {
                Header = Header.Clone(),
                HeaderFilenames = HeaderFilenames.Clone(),
                RoomTypes = RoomTypes.Clone(),
                MapObjectTypes = MapObjectTypes.Clone(),
                Movement = Movement.Clone(),
                Rendering = Rendering.Clone(),
                CgaColorRemap = CgaColorRemap.Clone(),
                GameSettings = GameSettings.Clone(),
                VgaPaletteRemap = VgaPaletteRemap.Clone(),
                StubOutputCapture = StubOutputCapture.Clone(),
                MissionStateBlock = MissionStateBlock.Clone(),
                PlayerDirectionState = PlayerDirectionState.Clone(),
                MenuStrings = MenuStrings.Clone(),
                InputConfig = InputConfig.Clone(),
                AlertLevelColors = AlertLevelColors.Clone(),
                GameplayStrings = GameplayStrings.Clone(),
                GameplayData = GameplayData.ToArray(),
                EquipmentNames = EquipmentNames.Select(s => s).ToArray(),
                MidSectionPostEquipNames = MidSectionPostEquipNames.ToArray(),
                EquipmentNavTable = EquipmentNavTable.ToArray(),
                RagdollCoordinates = RagdollCoordinates.Select(c => c.Clone()).ToArray(),
                RagdollRectPadding = RagdollRectPadding.ToArray(),
                EquipmentSlotRects = EquipmentSlotRects.Select(r => r.Clone()).ToArray(),
                ClueRelationshipPhrases = ClueRelationshipPhrases.ToArray(),
                CluePhraseSizes = CluePhraseSizes.ToArray(),
                MonthAbbreviations = MonthAbbreviations.ToArray(),
                MonthSizes = MonthSizes.ToArray(),
                IntelHeaders = IntelHeaders.ToArray(),
                IntelHeaderSizes = IntelHeaderSizes.ToArray(),
                CluePhrasePointerTable = CluePhrasePointerTable.ToArray(),
                ClueCategoryData = ClueCategoryData.ToArray(),
                MonthPointerTable = MonthPointerTable.ToArray(),
                IntelMidPadding = IntelMidPadding.ToArray(),
                IntelReportTexts = IntelReportTexts.ToArray(),
                IntelReportTextSizes = IntelReportTextSizes.ToArray(),
                RankNames = RankNames.ToArray(),
                RankNameSizes = RankNameSizes.ToArray(),
                EvidenceTypeAbbreviations = EvidenceTypeAbbreviations.ToArray(),
                EvidenceTypeSizes = EvidenceTypeSizes.ToArray(),
                EvidenceItemNames = EvidenceItemNames.ToArray(),
                EvidenceItemSizes = EvidenceItemSizes.ToArray(),
                EvidenceRankPointerTable = EvidenceRankPointerTable.ToArray(),
                InvestigationMethods = InvestigationMethods.ToArray(),
                InvestigationMethodSizes = InvestigationMethodSizes.ToArray(),
                ClueSystemData = ClueSystemData.ToArray(),
                CharacterNames = CharacterNames.Select(s => s).ToArray(),
                PostCharNameData = PostCharNameData.ToArray(),
                TrailingData = TrailingData.ToArray()
            };
        }
    }
}
