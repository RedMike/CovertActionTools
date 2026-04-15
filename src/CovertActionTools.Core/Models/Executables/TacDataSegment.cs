using System;
using System.Linq;
using System.Text;
using CovertActionTools.Core.Models.Executables.Records.Tac;
using CovertActionTools.Core.Models.Executables.Sections;
using CovertActionTools.Core.Models.Executables.Sections.Shared;
using CovertActionTools.Core.Models.Executables.Sections.Tac;

namespace CovertActionTools.Core.Models.Executables
{
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
        private const int InventoryItemSelectionNavigationOffset = 0x2100;
        private const int InventoryItemRagdollCoordinatesOffset = 0x2160;
        private const int InventoryItemSelectionRectanglesOffset = 0x220C;
        private const int CharNamePointersOffset = 0x346C;
        private const int CharNamePointerCount = 192;
        #endregion

        #region MidSection Sub-offsets (DS-relative)
        private const int MovementPixelCount = 9;
        private const int JumpingTileCount = 9;
        private const int TileAdjacencyCount = 4;
        private const int UnreferencedGapSize = 6;
        private const int SpriteConfigsSize = 60;
        #endregion

        #region PreCharNameData Sub-offsets (DS-relative)
        // IntelReportTexts begins at 0x25DC; the two bytes at 0x25DA-0x25DB are
        // dword-alignment padding owned by SharedClueAndIntel (IPaddedToDword).
        // The byte at 0x25DC is a zero which acts as an empty-string entry used by
        // the intel formatter to reset the destination buffer before appending a
        // suffix -- it is the first slot of IntelReportTexts, not padding.
        private const int IntelTextsStart = 0x25DC;
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
        public DoorEntryStringsSection MissionStateBlock { get; set; } = new();
        public TacPlayerDirectionStateSection PlayerDirectionState { get; set; } = new();
        public TacMenuStringsSection MenuStrings { get; set; } = new();
        public TacInputConfigSection InputConfig { get; set; } = new();
        public CombatAlertedFlagSection CombatAlertedFlag { get; set; } = new();
        public TargetReticleColorsSection TargetReticleColors { get; set; } = new();
        public GameplayActionMenusSection GameplayActionMenus { get; set; } = new();
        public StatusLineActionStringsSection StatusLineActionStrings { get; set; } = new();
        public StatusLineStatusStringsSection StatusLineStatusStrings { get; set; } = new();
        public GameplayEndingStringsSection GameplayEndingStrings { get; set; } = new();
        public GameplayPopupStringsSection GameplayPopupStrings { get; set; } = new();
        public FloorSafeInventoryItemRewardSection FloorSafeInventoryItemRewards { get; set; } = new();
        public PasswordGenerationSection PasswordGeneration { get; set; } = new();
        public PasswordDialogTextsSection PasswordDialogTexts { get; set; } = new();
        public WallTileDirectionSpriteSection WallTileDirectionSprite { get; set; } = new();
        public TacGraphicsFilenamesSection GraphicsFilenames { get; set; } = new();
        public SignToCompassDirectionSection SignToCompassDirection { get; set; } = new();
        public CachedRoomDistanceTargetSection CachedRoomDistanceTarget { get; set; } = new();
        public InventoryItemNamesSection InventoryItemNames { get; set; } = new();
        public InventoryItemSelectionNavigationSection InventoryItemSelectionNavigation { get; set; } = new();
        public InventoryItemRagdollCoordinatesSection InventoryItemRagdollCoordinates { get; set; } = new();
        public InventoryItemSelectionRectanglesSection InventoryItemSelectionRectangles { get; set; } = new();

        #region PreCharNameData (between Equipment Slot Rects and Character Names)

        // TODO: The clue/intel string tables inside SharedClueAndIntel are duplicated across
        // TAC, FINAL, GAME and BUG. Each EXE currently carries its own section instance; long
        // term the editor should present them once and fan edits out to every host EXE so
        // they cannot drift apart.
        /// <summary>Clue relationship phrases, month abbreviations, intel headers, intel
        /// phrases, the 40-entry clue phrase pointer table, the opaque UnknownClueData
        /// block, the popcount lookup tables, and the month pointer table (0x226C-0x25DA).</summary>
        public SharedClueAndIntelSection SharedClueAndIntel { get; set; } = new();

        // TODO: Investigate string identification for IntelReportTexts -- the region from 0x25DC to
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
            var segment = new TacDataSegment();

            var offset = 0;
            offset = ReadSectionWithPadding(segment.Header, dataSegment, offset);
            offset = ReadSectionWithPadding(segment.HeaderFilenames, dataSegment, offset);
            offset = ReadSectionWithPadding(segment.RoomTypes, dataSegment, offset);
            offset = ReadSectionWithPadding(segment.MapObjectTypes, dataSegment, offset);
            offset = ReadSectionWithPadding(segment.Movement, dataSegment, offset);
            offset = ReadSectionWithPadding(segment.Rendering, dataSegment, offset);
            offset = ReadSectionWithPadding(segment.CgaColorRemap, dataSegment, offset);
            offset = ReadSectionWithPadding(segment.GameSettings, dataSegment, offset);
            offset = ReadSectionWithPadding(segment.VgaPaletteRemap, dataSegment, offset);
            offset = ReadSectionWithPadding(segment.StubOutputCapture, dataSegment, offset);
            offset = ReadSectionWithPadding(segment.MissionStateBlock, dataSegment, offset);
            offset = ReadSectionWithPadding(segment.PlayerDirectionState, dataSegment, offset);
            offset = ReadSectionWithPadding(segment.MenuStrings, dataSegment, offset);
            offset = ReadSectionWithPadding(segment.InputConfig, dataSegment, offset);
            offset = ReadSectionWithPadding(segment.CombatAlertedFlag, dataSegment, offset);
            offset = ReadSectionWithPadding(segment.TargetReticleColors, dataSegment, offset);
            offset = ReadSectionWithPadding(segment.GameplayActionMenus, dataSegment, offset);
            offset = ReadSectionWithPadding(segment.StatusLineActionStrings, dataSegment, offset);
            offset = ReadSectionWithPadding(segment.StatusLineStatusStrings, dataSegment, offset);
            offset = ReadSectionWithPadding(segment.GameplayEndingStrings, dataSegment, offset);
            offset = ReadSectionWithPadding(segment.GameplayPopupStrings, dataSegment, offset);
            offset = ReadSectionWithPadding(segment.FloorSafeInventoryItemRewards, dataSegment, offset);
            offset = ReadSectionWithPadding(segment.PasswordGeneration, dataSegment, offset);
            offset = ReadSectionWithPadding(segment.PasswordDialogTexts, dataSegment, offset);
            offset = ReadSectionWithPadding(segment.WallTileDirectionSprite, dataSegment, offset);
            offset = ReadSectionWithPadding(segment.GraphicsFilenames, dataSegment, offset);
            offset = ReadSectionWithPadding(segment.SignToCompassDirection, dataSegment, offset);
            offset = ReadSectionWithPadding(segment.CachedRoomDistanceTarget, dataSegment, offset);
            offset = ReadSectionWithPadding(segment.InventoryItemNames, dataSegment, offset);
            offset = ReadSectionWithPadding(segment.InventoryItemSelectionNavigation, dataSegment, offset);
            offset = ReadSectionWithPadding(segment.InventoryItemRagdollCoordinates, dataSegment, offset);
            offset = ReadSectionWithPadding(segment.InventoryItemSelectionRectangles, dataSegment, offset);
            offset = ReadSectionWithPadding(segment.SharedClueAndIntel, dataSegment, offset);

            #region Parse PreCharNameData sub-sections

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
            var preCharNameBytes = DataSegmentHelper.Concatenate(
                DataSegmentHelper.ControlStringsToFixedBytes(IntelReportTexts, IntelReportTextSizes),
                DataSegmentHelper.ControlStringsToFixedBytes(RankNames, RankNameSizes),
                DataSegmentHelper.ControlStringsToFixedBytes(EvidenceTypeAbbreviations, EvidenceTypeSizes),
                DataSegmentHelper.ControlStringsToFixedBytes(EvidenceItemNames, EvidenceItemSizes),
                EvidenceRankPointerTable,
                DataSegmentHelper.ControlStringsToFixedBytes(InvestigationMethods, InvestigationMethodSizes),
                ClueSystemData
            );

            var allSectionBytes = DataSegmentHelper.Concatenate(
                Header,
                HeaderFilenames,
                RoomTypes,
                MapObjectTypes,
                Movement,
                Rendering,
                CgaColorRemap,
                GameSettings,
                VgaPaletteRemap,
                StubOutputCapture,
                MissionStateBlock,
                PlayerDirectionState,
                MenuStrings,
                InputConfig,
                CombatAlertedFlag,
                TargetReticleColors,
                GameplayActionMenus,
                StatusLineActionStrings,
                StatusLineStatusStrings,
                GameplayEndingStrings,
                GameplayPopupStrings,
                FloorSafeInventoryItemRewards,
                PasswordGeneration,
                PasswordDialogTexts,
                WallTileDirectionSprite,
                GraphicsFilenames,
                SignToCompassDirection,
                CachedRoomDistanceTarget,
                InventoryItemNames,
                InventoryItemSelectionNavigation,
                InventoryItemRagdollCoordinates,
                InventoryItemSelectionRectangles,
                SharedClueAndIntel
            );

            var charNamesBaseOffset = allSectionBytes.Length + preCharNameBytes.Length;
            var charNamePointers = DataSegmentHelper.ComputeStringPointers(CharacterNames, charNamesBaseOffset);
            var charNamesBytes = DataSegmentHelper.NullTerminatedStringsToBytes(CharacterNames);

            return DataSegmentHelper.Concatenate(
                allSectionBytes,
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
            if (section is IPaddedToParagraph && offset % 16 != 0)
                offset += 16 - (offset % 16);
            else if (section is IPaddedToDword && offset % 4 != 0)
                offset += 4 - (offset % 4);
            else if (section is IPaddedToWord && offset % 2 != 0)
                offset++;
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
                CombatAlertedFlag = CombatAlertedFlag.Clone(),
                TargetReticleColors = TargetReticleColors.Clone(),
                GameplayActionMenus = GameplayActionMenus.Clone(),
                StatusLineActionStrings = StatusLineActionStrings.Clone(),
                StatusLineStatusStrings = StatusLineStatusStrings.Clone(),
                GameplayEndingStrings = GameplayEndingStrings.Clone(),
                GameplayPopupStrings = GameplayPopupStrings.Clone(),
                FloorSafeInventoryItemRewards = FloorSafeInventoryItemRewards.Clone(),
                PasswordGeneration = PasswordGeneration.Clone(),
                PasswordDialogTexts = PasswordDialogTexts.Clone(),
                WallTileDirectionSprite = WallTileDirectionSprite.Clone(),
                GraphicsFilenames = GraphicsFilenames.Clone(),
                SignToCompassDirection = SignToCompassDirection.Clone(),
                CachedRoomDistanceTarget = CachedRoomDistanceTarget.Clone(),
                InventoryItemNames = InventoryItemNames.Clone(),
                InventoryItemSelectionNavigation = InventoryItemSelectionNavigation.Clone(),
                InventoryItemRagdollCoordinates = InventoryItemRagdollCoordinates.Clone(),
                InventoryItemSelectionRectangles = InventoryItemSelectionRectangles.Clone(),
                SharedClueAndIntel = SharedClueAndIntel.Clone(),
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
