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
        // ClueSystemData starts immediately after SharedClueAndIntel (which now
        // absorbs IntelReportTexts, RankNames, EvidenceTypeAbbreviations,
        // EvidenceItemNames, EvidenceRankPointerTable, and InvestigationMethods,
        // ending at 0x2B4C exactly).
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
        /// block, the popcount lookup tables, the month pointer table, the intel
        /// report text fragments, rank names, evidence type abbreviations, evidence
        /// item names, the evidence/rank pointer table, and investigation method
        /// names (0x226C-0x2B4C).</summary>
        public SharedClueAndIntelSection SharedClueAndIntel { get; set; } = new();

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
            var preCharNameBytes = ClueSystemData;

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
                ClueSystemData = ClueSystemData.ToArray(),
                CharacterNames = CharacterNames.Select(s => s).ToArray(),
                PostCharNameData = PostCharNameData.ToArray(),
                TrailingData = TrailingData.ToArray()
            };
        }
    }
}
