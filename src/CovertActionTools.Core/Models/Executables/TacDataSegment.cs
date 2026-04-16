using System;
using System.Linq;
using System.Text;
using CovertActionTools.Core.Models.Executables.Sections.Shared;
using CovertActionTools.Core.Models.Executables.Sections.Tac;

namespace CovertActionTools.Core.Models.Executables
{
    /// <summary>
    /// Structured data segment for TAC.EXE.
    /// Only the regions the editor actively understands are parsed as sections; everything
    /// else (headers, movement/rendering tables, pointer tables, trailing runtime data) is
    /// preserved as the original DS byte image and written back verbatim. Retained sections
    /// live at fixed DS-relative offsets and are overlaid onto the original image on write.
    /// </summary>
    public class TacDataSegment
    {
        /// <summary>DS paragraph value for TAC.EXE.</summary>
        public const int DsParagraph = 0x10E3;

        #region Retained section offsets (DS-relative)
        private const int RoomTypesOffset = 0x00B2;
        private const int MapObjectTypesOffset = 0x018E;
        private const int CgaColorRemapOffset = 0x1AE8;
        private const int VgaPaletteRemapOffset = 0x1BCE;
        private const int MissionStateBlockOffset = 0x1C26;
        private const int MenuStringsOffset = 0x1C46;
        private const int InputConfigOffset = 0x1C78;
        private const int TargetReticleColorsOffset = 0x1C8C;
        private const int GameplayActionMenusOffset = 0x1C91;
        private const int StatusLineActionStringsOffset = 0x1CE4;
        private const int StatusLineStatusStringsOffset = 0x1D87;
        private const int GameplayEndingStringsOffset = 0x1D94;
        private const int GameplayPopupStringsOffset = 0x1E78;
        private const int FloorSafeInventoryItemRewardsOffset = 0x1F36;
        private const int PasswordDialogTextsOffset = 0x1F67;
        private const int WallTileDirectionSpriteOffset = 0x1FF0;
        private const int InventoryItemNamesOffset = 0x2034;
        private const int InventoryItemSelectionNavigationOffset = 0x2100;
        private const int InventoryItemRagdollCoordinatesOffset = 0x2160;
        private const int InventoryItemSelectionRectanglesOffset = 0x220C;

        private const int ClueRelationshipPhrasesOffset = 0x226C;
        private const int MonthAbbreviationsOffset = 0x248C;
        private const int IntelHeadersOffset = 0x24BC;
        private const int IntelPhrasesOffset = 0x251E;
        private const int IntelReportTextsOffset = 0x25DC;
        private const int RankNamesOffset = 0x274C;
        private const int EvidenceTypeAbbreviationsOffset = 0x279F;
        private const int EvidenceItemNamesOffset = 0x27BB;
        private const int InvestigationMethodsOffset = 0x2AC6;
        private const int ClueHeaderStringsOffset = 0x2B4C;
        private const int ClueTargetStringsOffset = 0x2BC8;
        private const int SuspectFileStringsOffset = 0x2C0D;
        private const int LoadFailedStringOffset = 0x2E46;
        private const int BuildingNamesOffset = 0x2F2A;
        private const int UnknownAgentTemplateStringOffset = 0x2F59;
        private const int FoundDocumentStringsOffset = 0x35EC;
        private const int TimeTemplateStringOffset = 0x364C;
        private const int LoadingMessageStringOffset = 0x3656;
        private const int QuitMenuOffset = 0x366B;

        private const int CharacterNamePointersOffset = 0x346C;
        private const int CharacterNamePointerCount = 192;
        #endregion

        /// <summary>
        /// The DS bytes exactly as they were parsed. Unparsed regions (headers, pointer
        /// tables, runtime scratch, everything else we do not model) round-trip through
        /// this buffer. On write, the retained section properties are overlaid onto a
        /// clone of these bytes.
        /// </summary>
        public byte[] OriginalDsBytes { get; set; } = Array.Empty<byte>();

        #region Retained sections
        public RoomTypeSection RoomTypes { get; set; } = new();
        public MapObjectTypeSection MapObjectTypes { get; set; } = new();
        public CgaColorRemapSection CgaColorRemap { get; set; } = new();
        public VgaPaletteRemapSection VgaPaletteRemap { get; set; } = new();
        public DoorEntryStringsSection MissionStateBlock { get; set; } = new();
        public TacMenuStringsSection MenuStrings { get; set; } = new();
        public TacInputConfigSection InputConfig { get; set; } = new();
        public TargetReticleColorsSection TargetReticleColors { get; set; } = new();
        public GameplayActionMenusSection GameplayActionMenus { get; set; } = new();
        public StatusLineActionStringsSection StatusLineActionStrings { get; set; } = new();
        public StatusLineStatusStringsSection StatusLineStatusStrings { get; set; } = new();
        public GameplayEndingStringsSection GameplayEndingStrings { get; set; } = new();
        public GameplayPopupStringsSection GameplayPopupStrings { get; set; } = new();
        public FloorSafeInventoryItemRewardSection FloorSafeInventoryItemRewards { get; set; } = new();
        public PasswordDialogTextsSection PasswordDialogTexts { get; set; } = new();
        public WallTileDirectionSpriteSection WallTileDirectionSprite { get; set; } = new();
        public InventoryItemNamesSection InventoryItemNames { get; set; } = new();
        public InventoryItemSelectionNavigationSection InventoryItemSelectionNavigation { get; set; } = new();
        public InventoryItemRagdollCoordinatesSection InventoryItemRagdollCoordinates { get; set; } = new();
        public InventoryItemSelectionRectanglesSection InventoryItemSelectionRectangles { get; set; } = new();

        public ClueRelationshipPhrasesSection ClueRelationshipPhrases { get; set; } = new();
        public MonthAbbreviationsSection MonthAbbreviations { get; set; } = new();
        public IntelHeadersSection IntelHeaders { get; set; } = new();
        public IntelPhrasesSection IntelPhrases { get; set; } = new();
        public IntelReportTextsSection IntelReportTexts { get; set; } = new();
        public RankNamesSection RankNames { get; set; } = new();
        public EvidenceTypeAbbreviationsSection EvidenceTypeAbbreviations { get; set; } = new();
        public EvidenceItemNamesSection EvidenceItemNames { get; set; } = new();
        public InvestigationMethodsSection InvestigationMethods { get; set; } = new();
        public ClueHeaderStringsSection ClueHeaderStrings { get; set; } = new();
        public ClueTargetStringsSection ClueTargetStrings { get; set; } = new();
        public SuspectFileStringsSection SuspectFileStrings { get; set; } = new();
        public LoadFailedStringSection LoadFailedString { get; set; } = new();
        public BuildingNamesSection BuildingNames { get; set; } = new();
        public UnknownAgentTemplateStringSection UnknownAgentTemplateString { get; set; } = new();
        public FoundDocumentStringsSection FoundDocumentStrings { get; set; } = new();
        public TimeTemplateStringSection TimeTemplateString { get; set; } = new();
        public LoadingMessageStringSection LoadingMessageString { get; set; } = new();
        public QuitMenuSection QuitMenu { get; set; } = new();

        /// <summary>192 character names (4 ethnic groups x female first / male first / male surname, 16 each).</summary>
        public string[] CharacterNames { get; set; } = Array.Empty<string>();
        #endregion

        public static TacDataSegment FromBytes(byte[] dataSegment)
        {
            var segment = new TacDataSegment
            {
                OriginalDsBytes = (byte[])dataSegment.Clone()
            };

            segment.RoomTypes.ReadBytes(dataSegment, RoomTypesOffset);
            segment.MapObjectTypes.ReadBytes(dataSegment, MapObjectTypesOffset);
            segment.CgaColorRemap.ReadBytes(dataSegment, CgaColorRemapOffset);
            segment.VgaPaletteRemap.ReadBytes(dataSegment, VgaPaletteRemapOffset);
            segment.MissionStateBlock.ReadBytes(dataSegment, MissionStateBlockOffset);
            segment.MenuStrings.ReadBytes(dataSegment, MenuStringsOffset);
            segment.InputConfig.ReadBytes(dataSegment, InputConfigOffset);
            segment.TargetReticleColors.ReadBytes(dataSegment, TargetReticleColorsOffset);
            segment.GameplayActionMenus.ReadBytes(dataSegment, GameplayActionMenusOffset);
            segment.StatusLineActionStrings.ReadBytes(dataSegment, StatusLineActionStringsOffset);
            segment.StatusLineStatusStrings.ReadBytes(dataSegment, StatusLineStatusStringsOffset);
            segment.GameplayEndingStrings.ReadBytes(dataSegment, GameplayEndingStringsOffset);
            segment.GameplayPopupStrings.ReadBytes(dataSegment, GameplayPopupStringsOffset);
            segment.FloorSafeInventoryItemRewards.ReadBytes(dataSegment, FloorSafeInventoryItemRewardsOffset);
            segment.PasswordDialogTexts.ReadBytes(dataSegment, PasswordDialogTextsOffset);
            segment.WallTileDirectionSprite.ReadBytes(dataSegment, WallTileDirectionSpriteOffset);
            segment.InventoryItemNames.ReadBytes(dataSegment, InventoryItemNamesOffset);
            segment.InventoryItemSelectionNavigation.ReadBytes(dataSegment, InventoryItemSelectionNavigationOffset);
            segment.InventoryItemRagdollCoordinates.ReadBytes(dataSegment, InventoryItemRagdollCoordinatesOffset);
            segment.InventoryItemSelectionRectangles.ReadBytes(dataSegment, InventoryItemSelectionRectanglesOffset);

            segment.ClueRelationshipPhrases.ReadBytes(dataSegment, ClueRelationshipPhrasesOffset);
            segment.MonthAbbreviations.ReadBytes(dataSegment, MonthAbbreviationsOffset);
            segment.IntelHeaders.ReadBytes(dataSegment, IntelHeadersOffset);
            segment.IntelPhrases.ReadBytes(dataSegment, IntelPhrasesOffset);
            segment.IntelReportTexts.ReadBytes(dataSegment, IntelReportTextsOffset);
            segment.RankNames.ReadBytes(dataSegment, RankNamesOffset);
            segment.EvidenceTypeAbbreviations.ReadBytes(dataSegment, EvidenceTypeAbbreviationsOffset);
            segment.EvidenceItemNames.ReadBytes(dataSegment, EvidenceItemNamesOffset);
            segment.InvestigationMethods.ReadBytes(dataSegment, InvestigationMethodsOffset);
            segment.ClueHeaderStrings.ReadBytes(dataSegment, ClueHeaderStringsOffset);
            segment.ClueTargetStrings.ReadBytes(dataSegment, ClueTargetStringsOffset);
            segment.SuspectFileStrings.ReadBytes(dataSegment, SuspectFileStringsOffset);
            segment.LoadFailedString.ReadBytes(dataSegment, LoadFailedStringOffset);
            segment.BuildingNames.ReadBytes(dataSegment, BuildingNamesOffset);
            segment.UnknownAgentTemplateString.ReadBytes(dataSegment, UnknownAgentTemplateStringOffset);
            segment.FoundDocumentStrings.ReadBytes(dataSegment, FoundDocumentStringsOffset);
            segment.TimeTemplateString.ReadBytes(dataSegment, TimeTemplateStringOffset);
            segment.LoadingMessageString.ReadBytes(dataSegment, LoadingMessageStringOffset);
            segment.QuitMenu.ReadBytes(dataSegment, QuitMenuOffset);

            var charPtrs = DataSegmentHelper.BytesToUInt16Array(
                dataSegment, CharacterNamePointersOffset, CharacterNamePointerCount);
            segment.CharacterNames = DataSegmentHelper.ExtractStringsFromPointers(charPtrs, dataSegment);

            return segment;
        }

        public byte[] ToBytes()
        {
            var result = (byte[])OriginalDsBytes.Clone();

            Overlay(result, RoomTypesOffset, RoomTypes.WriteBytes());
            Overlay(result, MapObjectTypesOffset, MapObjectTypes.WriteBytes());
            Overlay(result, CgaColorRemapOffset, CgaColorRemap.WriteBytes());
            Overlay(result, VgaPaletteRemapOffset, VgaPaletteRemap.WriteBytes());
            Overlay(result, MissionStateBlockOffset, MissionStateBlock.WriteBytes());
            Overlay(result, MenuStringsOffset, MenuStrings.WriteBytes());
            Overlay(result, InputConfigOffset, InputConfig.WriteBytes());
            Overlay(result, TargetReticleColorsOffset, TargetReticleColors.WriteBytes());
            Overlay(result, GameplayActionMenusOffset, GameplayActionMenus.WriteBytes());
            Overlay(result, StatusLineActionStringsOffset, StatusLineActionStrings.WriteBytes());
            Overlay(result, StatusLineStatusStringsOffset, StatusLineStatusStrings.WriteBytes());
            Overlay(result, GameplayEndingStringsOffset, GameplayEndingStrings.WriteBytes());
            Overlay(result, GameplayPopupStringsOffset, GameplayPopupStrings.WriteBytes());
            Overlay(result, FloorSafeInventoryItemRewardsOffset, FloorSafeInventoryItemRewards.WriteBytes());
            Overlay(result, PasswordDialogTextsOffset, PasswordDialogTexts.WriteBytes());
            Overlay(result, WallTileDirectionSpriteOffset, WallTileDirectionSprite.WriteBytes());
            Overlay(result, InventoryItemNamesOffset, InventoryItemNames.WriteBytes());
            Overlay(result, InventoryItemSelectionNavigationOffset, InventoryItemSelectionNavigation.WriteBytes());
            Overlay(result, InventoryItemRagdollCoordinatesOffset, InventoryItemRagdollCoordinates.WriteBytes());
            Overlay(result, InventoryItemSelectionRectanglesOffset, InventoryItemSelectionRectangles.WriteBytes());

            Overlay(result, ClueRelationshipPhrasesOffset, ClueRelationshipPhrases.WriteBytes());
            Overlay(result, MonthAbbreviationsOffset, MonthAbbreviations.WriteBytes());
            Overlay(result, IntelHeadersOffset, IntelHeaders.WriteBytes());
            Overlay(result, IntelPhrasesOffset, IntelPhrases.WriteBytes());
            Overlay(result, IntelReportTextsOffset, IntelReportTexts.WriteBytes());
            Overlay(result, RankNamesOffset, RankNames.WriteBytes());
            Overlay(result, EvidenceTypeAbbreviationsOffset, EvidenceTypeAbbreviations.WriteBytes());
            Overlay(result, EvidenceItemNamesOffset, EvidenceItemNames.WriteBytes());
            Overlay(result, InvestigationMethodsOffset, InvestigationMethods.WriteBytes());
            Overlay(result, ClueHeaderStringsOffset, ClueHeaderStrings.WriteBytes());
            Overlay(result, ClueTargetStringsOffset, ClueTargetStrings.WriteBytes());
            Overlay(result, SuspectFileStringsOffset, SuspectFileStrings.WriteBytes());
            Overlay(result, LoadFailedStringOffset, LoadFailedString.WriteBytes());
            Overlay(result, BuildingNamesOffset, BuildingNames.WriteBytes());
            Overlay(result, UnknownAgentTemplateStringOffset, UnknownAgentTemplateString.WriteBytes());
            Overlay(result, FoundDocumentStringsOffset, FoundDocumentStrings.WriteBytes());
            Overlay(result, TimeTemplateStringOffset, TimeTemplateString.WriteBytes());
            Overlay(result, LoadingMessageStringOffset, LoadingMessageString.WriteBytes());
            Overlay(result, QuitMenuOffset, QuitMenu.WriteBytes());

            OverlayCharacterNames(result);

            return result;
        }

        private static void Overlay(byte[] destination, int offset, byte[] source)
        {
            Array.Copy(source, 0, destination, offset, source.Length);
        }

        // The character-name pointer table is part of the opaque blob (it lives in
        // OriginalDsBytes and is never recomputed). Each name is written back at the
        // offset its pointer targets, with a null terminator; bytes beyond the null
        // and before the next name's start are left as whatever was already in the
        // blob. Names that grow past their original slot are truncated -- the editor
        // UI caps the edit length to the slot size to prevent this silently.
        private void OverlayCharacterNames(byte[] destination)
        {
            if (CharacterNames.Length == 0) return;

            var charPtrs = DataSegmentHelper.BytesToUInt16Array(
                OriginalDsBytes, CharacterNamePointersOffset, CharacterNamePointerCount);

            for (var i = 0; i < CharacterNames.Length && i < charPtrs.Length; i++)
            {
                var target = charPtrs[i];
                if (target == 0 || target >= destination.Length) continue;

                var maxSlotLen = GetCharacterNameSlotLength(charPtrs, i, destination.Length);
                var bytes = Encoding.ASCII.GetBytes(CharacterNames[i] ?? string.Empty);
                var writeLen = Math.Min(bytes.Length, maxSlotLen - 1); // leave room for null

                for (var j = 0; j < writeLen; j++) destination[target + j] = bytes[j];
                destination[target + writeLen] = 0;
            }
        }

        private static int GetCharacterNameSlotLength(ushort[] pointers, int index, int segmentLength)
        {
            // Slot length = distance from this pointer to the next non-zero pointer.
            for (var j = index + 1; j < pointers.Length; j++)
            {
                if (pointers[j] != 0 && pointers[j] > pointers[index])
                {
                    return pointers[j] - pointers[index];
                }
            }
            return Math.Max(1, segmentLength - pointers[index]);
        }

        public TacDataSegment Clone()
        {
            return new TacDataSegment
            {
                OriginalDsBytes = OriginalDsBytes.ToArray(),
                RoomTypes = RoomTypes.Clone(),
                MapObjectTypes = MapObjectTypes.Clone(),
                CgaColorRemap = CgaColorRemap.Clone(),
                VgaPaletteRemap = VgaPaletteRemap.Clone(),
                MissionStateBlock = MissionStateBlock.Clone(),
                MenuStrings = MenuStrings.Clone(),
                InputConfig = InputConfig.Clone(),
                TargetReticleColors = TargetReticleColors.Clone(),
                GameplayActionMenus = GameplayActionMenus.Clone(),
                StatusLineActionStrings = StatusLineActionStrings.Clone(),
                StatusLineStatusStrings = StatusLineStatusStrings.Clone(),
                GameplayEndingStrings = GameplayEndingStrings.Clone(),
                GameplayPopupStrings = GameplayPopupStrings.Clone(),
                FloorSafeInventoryItemRewards = FloorSafeInventoryItemRewards.Clone(),
                PasswordDialogTexts = PasswordDialogTexts.Clone(),
                WallTileDirectionSprite = WallTileDirectionSprite.Clone(),
                InventoryItemNames = InventoryItemNames.Clone(),
                InventoryItemSelectionNavigation = InventoryItemSelectionNavigation.Clone(),
                InventoryItemRagdollCoordinates = InventoryItemRagdollCoordinates.Clone(),
                InventoryItemSelectionRectangles = InventoryItemSelectionRectangles.Clone(),
                ClueRelationshipPhrases = ClueRelationshipPhrases.Clone(),
                MonthAbbreviations = MonthAbbreviations.Clone(),
                IntelHeaders = IntelHeaders.Clone(),
                IntelPhrases = IntelPhrases.Clone(),
                IntelReportTexts = IntelReportTexts.Clone(),
                RankNames = RankNames.Clone(),
                EvidenceTypeAbbreviations = EvidenceTypeAbbreviations.Clone(),
                EvidenceItemNames = EvidenceItemNames.Clone(),
                InvestigationMethods = InvestigationMethods.Clone(),
                ClueHeaderStrings = ClueHeaderStrings.Clone(),
                ClueTargetStrings = ClueTargetStrings.Clone(),
                SuspectFileStrings = SuspectFileStrings.Clone(),
                LoadFailedString = LoadFailedString.Clone(),
                BuildingNames = BuildingNames.Clone(),
                UnknownAgentTemplateString = UnknownAgentTemplateString.Clone(),
                FoundDocumentStrings = FoundDocumentStrings.Clone(),
                TimeTemplateString = TimeTemplateString.Clone(),
                LoadingMessageString = LoadingMessageString.Clone(),
                QuitMenu = QuitMenu.Clone(),
                CharacterNames = CharacterNames.Select(s => s).ToArray(),
            };
        }
    }
}
