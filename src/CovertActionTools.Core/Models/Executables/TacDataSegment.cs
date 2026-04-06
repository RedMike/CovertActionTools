using System;
using System.Linq;
using System.Text;

namespace CovertActionTools.Core.Models.Executables
{
    /// <summary>
    /// Room type record from TAC.EXE (22 bytes).
    /// Field interpretations are based on reverse engineering and may not be fully accurate.
    /// </summary>
    public class TacRoomTypeRecord
    {
        public const int RecordSize = 22;
        public const int NameLength = 16;

        /// <summary>
        /// Room type name, null-padded to 16 bytes.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Surveillance quality increase granted when a bug is placed in this room type.
        /// </summary>
        public ushort SurveillanceQuality { get; set; }

        /// <summary>
        /// Building size constraint bitfield, matched against the building's room grid area
        /// (width * height): bit 0 (1) = small (area &lt;= 40), bit 1 (2) = medium (area 41-72),
        /// bit 2 (4) = large (area &gt; 72). Only large rooms (bit 2) are valid as the local
        /// agent's spawn room in a building.
        /// </summary>
        public ushort SizeConstraint { get; set; }

        /// <summary>
        /// Room enabled flag. Only bit 0 is tested at runtime (mask is hardcoded to 1),
        /// so this is effectively boolean: 0 = disabled, non-zero = enabled.
        /// Vanilla data uses 7 for enabled rooms, but only bit 0 matters.
        /// </summary>
        public ushort Enabled { get; set; }

        public TacRoomTypeRecord Clone()
        {
            return new TacRoomTypeRecord
            {
                Name = Name,
                SurveillanceQuality = SurveillanceQuality,
                SizeConstraint = SizeConstraint,
                Enabled = Enabled
            };
        }

        public static TacRoomTypeRecord FromBytes(byte[] data, int offset)
        {
            var nameLen = 0;
            while (nameLen < NameLength && data[offset + nameLen] != 0) nameLen++;
            var name = DataSegmentHelper.DecodeControlString(data, offset, nameLen);

            return new TacRoomTypeRecord
            {
                Name = name,
                SurveillanceQuality = BitConverter.ToUInt16(data, offset + NameLength),
                SizeConstraint = BitConverter.ToUInt16(data, offset + NameLength + 2),
                Enabled = BitConverter.ToUInt16(data, offset + NameLength + 4)
            };
        }

        public byte[] ToBytes()
        {
            var result = new byte[RecordSize];
            var nameBytes = DataSegmentHelper.EncodeControlString(Name);
            Array.Copy(nameBytes, 0, result, 0, Math.Min(nameBytes.Length, NameLength));
            result[NameLength] = (byte)(SurveillanceQuality & 0xFF);
            result[NameLength + 1] = (byte)((SurveillanceQuality >> 8) & 0xFF);
            result[NameLength + 2] = (byte)(SizeConstraint & 0xFF);
            result[NameLength + 3] = (byte)((SizeConstraint >> 8) & 0xFF);
            result[NameLength + 4] = (byte)(Enabled & 0xFF);
            result[NameLength + 5] = (byte)((Enabled >> 8) & 0xFF);
            return result;
        }
    }

    /// <summary>
    /// Object/furniture record from TAC.EXE (20 bytes).
    /// Field interpretations are based on reverse engineering and may not be fully accurate.
    /// </summary>
    public class TacObjectRecord
    {
        public const int RecordSize = 20;
        public const int NameLength = 12;

        /// <summary>
        /// Object/furniture name, null-padded to 12 bytes.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// X pixel offset into the spritesheet.
        /// </summary>
        public ushort SpriteOffset { get; set; }

        /// <summary>
        /// Y pixel offset / sprite page selector.
        /// </summary>
        public ushort SpritePage { get; set; }

        /// <summary>
        /// Object behaviour bitfield:
        ///   bit 0 (0x001) = Blocks Movement — occupies floor space, tile is impassable.
        ///   bit 1 (0x002) = Openable — can be opened/closed; open sprite is SpritePage+1.
        ///   bit 2 (0x004) = Buggable — player can place a listening device.
        ///   bit 3 (0x008) = Photographable — player can photograph contents.
        ///   bit 4 (0x010) = Is Door — propagates open/closed state to adjacent tile (through
        ///                    wall) so pathfinding works from both sides. Direction determined
        ///                    by (object_index &amp; 3).
        ///   bit 5 (0x020) = Blocks LOS — fully blocks line-of-sight raycast (returns 0).
        ///                    Bit 0 objects only partially obstruct (returns 1). Also blocks
        ///                    movement.
        ///   bit 6 (0x040) = Multi-tile — object spans 2 tiles along the X axis. Even-indexed
        ///                    objects extend to X+1, odd to X-1. Paired object is index +/- 1.
        ///   bit 7 (0x080) = Unused — never tested at runtime. Only set on Table objects.
        ///   bit 8 (0x100) = Wall-Adjacent — during room generation, placed on wall tiles only
        ///                    (not freestanding on empty floor).
        ///   bit 9 (0x200) = Password Terminal — interactable as a cipher terminal.
        /// </summary>
        public ushort BehaviourFlags { get; set; }

        /// <summary>
        /// Room placement bitfield (which room types this object can appear in).
        /// </summary>
        public ushort RoomPlacement { get; set; }

        public TacObjectRecord Clone()
        {
            return new TacObjectRecord
            {
                Name = Name,
                SpriteOffset = SpriteOffset,
                SpritePage = SpritePage,
                BehaviourFlags = BehaviourFlags,
                RoomPlacement = RoomPlacement
            };
        }

        public static TacObjectRecord FromBytes(byte[] data, int offset)
        {
            var nameLen = 0;
            while (nameLen < NameLength && data[offset + nameLen] != 0) nameLen++;
            var name = DataSegmentHelper.DecodeControlString(data, offset, nameLen);

            return new TacObjectRecord
            {
                Name = name,
                SpriteOffset = BitConverter.ToUInt16(data, offset + NameLength),
                SpritePage = BitConverter.ToUInt16(data, offset + NameLength + 2),
                BehaviourFlags = BitConverter.ToUInt16(data, offset + NameLength + 4),
                RoomPlacement = BitConverter.ToUInt16(data, offset + NameLength + 6)
            };
        }

        public byte[] ToBytes()
        {
            var result = new byte[RecordSize];
            var nameBytes = DataSegmentHelper.EncodeControlString(Name);
            Array.Copy(nameBytes, 0, result, 0, Math.Min(nameBytes.Length, NameLength));
            WriteUInt16(result, NameLength, SpriteOffset);
            WriteUInt16(result, NameLength + 2, SpritePage);
            WriteUInt16(result, NameLength + 4, BehaviourFlags);
            WriteUInt16(result, NameLength + 6, RoomPlacement);
            return result;
        }

        private static void WriteUInt16(byte[] buf, int off, ushort val)
        {
            buf[off] = (byte)(val & 0xFF);
            buf[off + 1] = (byte)((val >> 8) & 0xFF);
        }
    }

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
        private const int BssBlockEnd = 0x1ABE;
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

        /// <summary>Data before room types: MSC runtime copyright, file refs, padding.</summary>
        public byte[] PreRoomData { get; set; } = Array.Empty<byte>();

        /// <summary>10 room type records (22 bytes each).</summary>
        public TacRoomTypeRecord[] RoomTypes { get; set; } = Array.Empty<TacRoomTypeRecord>();

        /// <summary>62 object/furniture records (20 bytes each).</summary>
        public TacObjectRecord[] Objects { get; set; } = Array.Empty<TacObjectRecord>();

        #region MidSection (between Objects and Equipment Name strings)

        /// <summary>
        /// Movement Pixel DX: 9 pixel-scale direction offsets (values +/-2, +/-3) used for walking movement.
        /// Entry 0 = stationary, entries 1-8 = 8 compass directions. Indexed by direction * 2.
        /// </summary>
        public short[] MovementPixelDX { get; set; } = Array.Empty<short>();

        /// <summary>
        /// Movement Pixel DY: 9 pixel-scale direction offsets (values +/-2, +/-3) used for walking movement.
        /// </summary>
        public short[] MovementPixelDY { get; set; } = Array.Empty<short>();

        /// <summary>
        /// Jumping Tile DX: 9 tile-scale direction offsets (values +/-1) used for jumping movement.
        /// Also reused for tile adjacency checks in NPC AI. Indexed by direction * 2.
        /// </summary>
        public short[] JumpingTileDX { get; set; } = Array.Empty<short>();

        /// <summary>
        /// Jumping Tile DY: 9 tile-scale direction offsets (values +/-1) used for jumping movement.
        /// </summary>
        public short[] JumpingTileDY { get; set; } = Array.Empty<short>();

        /// <summary>
        /// Tile Adjacency DX: 4 cardinal direction offsets (N/E/S/W) used for map generation
        /// and door propagation. Indexed by (direction &amp; 3) * 2.
        /// </summary>
        public short[] TileAdjacencyDX { get; set; } = Array.Empty<short>();

        /// <summary>
        /// Tile Adjacency DY: 4 cardinal direction offsets (N/E/S/W) used for map generation
        /// and door propagation.
        /// </summary>
        public short[] TileAdjacencyDY { get; set; } = Array.Empty<short>();

        /// <summary>2-byte gap between Tile Adjacency DX and DY arrays.</summary>
        public byte[] TileAdjacencyMidGap { get; set; } = Array.Empty<byte>();

        /// <summary>Unreferenced 6-byte gap between tile adjacency tables and sprite configs.</summary>
        public byte[] MovementTableGap { get; set; } = Array.Empty<byte>();

        /// <summary>3 sprite sheet configuration records (20 bytes each): RastPort-style rendering config.</summary>
        public byte[] SpriteSheetConfigs { get; set; } = Array.Empty<byte>();

        /// <summary>BSS (uninitialized data) block: all zeros at runtime, used as scratch memory.</summary>
        public byte[] BssBlock { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Post-BSS binary data: additional sprite configs, CGA animation frames, VGA palette
        /// remap tables, and gameplay menu/dialogue strings (interleaved with binary config data).
        /// </summary>
        public byte[] GameplayData { get; set; } = Array.Empty<byte>();

        #endregion

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
            var roomTypesEnd = RoomTypesOffset + RoomTypeCount * TacRoomTypeRecord.RecordSize;
            var objectsEnd = ObjectsOffset + ObjectCount * TacObjectRecord.RecordSize;
            var ragdollEnd = RagdollCoordsOffset + RagdollCoordCount * TacScreenCoordinate.RecordSize;

            var segment = new TacDataSegment();

            segment.PreRoomData = DataSegmentHelper.Slice(dataSegment, 0, RoomTypesOffset);

            segment.RoomTypes = new TacRoomTypeRecord[RoomTypeCount];
            for (var i = 0; i < RoomTypeCount; i++)
            {
                segment.RoomTypes[i] = TacRoomTypeRecord.FromBytes(dataSegment, RoomTypesOffset + i * TacRoomTypeRecord.RecordSize);
            }

            segment.Objects = new TacObjectRecord[ObjectCount];
            for (var i = 0; i < ObjectCount; i++)
            {
                segment.Objects[i] = TacObjectRecord.FromBytes(dataSegment, ObjectsOffset + i * TacObjectRecord.RecordSize);
            }

            // Read equipment name pointers to find and extract the strings from the mid section
            var equipPtrs = DataSegmentHelper.BytesToUInt16Array(dataSegment, EquipmentPointersOffset, EquipmentPointerCount);
            segment.EquipmentNames = DataSegmentHelper.ExtractStringsFromPointers(equipPtrs, dataSegment);

            var (blockStart, blockEnd) = DataSegmentHelper.FindStringBlockBounds(equipPtrs, dataSegment);

            #region Parse MidSection sub-sections

            #region Parse movement tables (6 arrays + gap)

            var pos = objectsEnd;

            segment.MovementPixelDX = new short[MovementPixelCount];
            for (var i = 0; i < MovementPixelCount; i++)
                segment.MovementPixelDX[i] = BitConverter.ToInt16(dataSegment, pos + i * 2);
            pos += MovementPixelCount * 2;

            segment.MovementPixelDY = new short[MovementPixelCount];
            for (var i = 0; i < MovementPixelCount; i++)
                segment.MovementPixelDY[i] = BitConverter.ToInt16(dataSegment, pos + i * 2);
            pos += MovementPixelCount * 2;

            segment.JumpingTileDX = new short[JumpingTileCount];
            for (var i = 0; i < JumpingTileCount; i++)
                segment.JumpingTileDX[i] = BitConverter.ToInt16(dataSegment, pos + i * 2);
            pos += JumpingTileCount * 2;

            segment.JumpingTileDY = new short[JumpingTileCount];
            for (var i = 0; i < JumpingTileCount; i++)
                segment.JumpingTileDY[i] = BitConverter.ToInt16(dataSegment, pos + i * 2);
            pos += JumpingTileCount * 2;

            segment.TileAdjacencyDX = new short[TileAdjacencyCount];
            for (var i = 0; i < TileAdjacencyCount; i++)
                segment.TileAdjacencyDX[i] = BitConverter.ToInt16(dataSegment, pos + i * 2);
            pos += TileAdjacencyCount * 2;

            segment.TileAdjacencyMidGap = DataSegmentHelper.Slice(dataSegment, pos, 2);
            pos += 2;

            segment.TileAdjacencyDY = new short[TileAdjacencyCount];
            for (var i = 0; i < TileAdjacencyCount; i++)
                segment.TileAdjacencyDY[i] = BitConverter.ToInt16(dataSegment, pos + i * 2);
            pos += TileAdjacencyCount * 2;

            segment.MovementTableGap = DataSegmentHelper.Slice(dataSegment, pos, UnreferencedGapSize);
            pos += UnreferencedGapSize;

            #endregion

            var spriteConfigStart = pos;
            segment.SpriteSheetConfigs = DataSegmentHelper.Slice(dataSegment, spriteConfigStart, SpriteConfigsSize);

            var bssStart = spriteConfigStart + SpriteConfigsSize;
            segment.BssBlock = DataSegmentHelper.Slice(dataSegment, bssStart, BssBlockEnd - bssStart);

            segment.GameplayData = DataSegmentHelper.Slice(dataSegment, BssBlockEnd, blockStart - BssBlockEnd);

            #endregion

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
            var roomTypeBytes = new byte[RoomTypeCount * TacRoomTypeRecord.RecordSize];
            for (var i = 0; i < RoomTypes.Length; i++)
            {
                Array.Copy(RoomTypes[i].ToBytes(), 0, roomTypeBytes, i * TacRoomTypeRecord.RecordSize, TacRoomTypeRecord.RecordSize);
            }

            var objectBytes = new byte[ObjectCount * TacObjectRecord.RecordSize];
            for (var i = 0; i < Objects.Length; i++)
            {
                Array.Copy(Objects[i].ToBytes(), 0, objectBytes, i * TacObjectRecord.RecordSize, TacObjectRecord.RecordSize);
            }

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

            // Serialize MidSection sub-sections
            var midSectionBytes = DataSegmentHelper.Concatenate(
                ShortArrayToBytes(MovementPixelDX),
                ShortArrayToBytes(MovementPixelDY),
                ShortArrayToBytes(JumpingTileDX),
                ShortArrayToBytes(JumpingTileDY),
                ShortArrayToBytes(TileAdjacencyDX),
                TileAdjacencyMidGap,
                ShortArrayToBytes(TileAdjacencyDY),
                MovementTableGap,
                SpriteSheetConfigs,
                BssBlock,
                GameplayData
            );

            // Compute equipment name pointer values from actual string positions
            var equipNamesBaseOffset = PreRoomData.Length + roomTypeBytes.Length
                + objectBytes.Length + midSectionBytes.Length;
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
                PreRoomData,
                roomTypeBytes,
                objectBytes,
                midSectionBytes,
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
                PreRoomData = PreRoomData.ToArray(),
                RoomTypes = RoomTypes.Select(r => r.Clone()).ToArray(),
                Objects = Objects.Select(o => o.Clone()).ToArray(),
                MovementPixelDX = MovementPixelDX.ToArray(),
                MovementPixelDY = MovementPixelDY.ToArray(),
                JumpingTileDX = JumpingTileDX.ToArray(),
                JumpingTileDY = JumpingTileDY.ToArray(),
                TileAdjacencyDX = TileAdjacencyDX.ToArray(),
                TileAdjacencyMidGap = TileAdjacencyMidGap.ToArray(),
                TileAdjacencyDY = TileAdjacencyDY.ToArray(),
                MovementTableGap = MovementTableGap.ToArray(),
                SpriteSheetConfigs = SpriteSheetConfigs.ToArray(),
                BssBlock = BssBlock.ToArray(),
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
