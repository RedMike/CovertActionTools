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
            var nameBytes = new byte[NameLength];
            Array.Copy(data, offset, nameBytes, 0, NameLength);
            var name = Encoding.ASCII.GetString(nameBytes).TrimEnd('\0');

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
            var nameBytes = Encoding.ASCII.GetBytes(Name);
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
            var nameBytes = new byte[NameLength];
            Array.Copy(data, offset, nameBytes, 0, NameLength);
            var name = Encoding.ASCII.GetString(nameBytes).TrimEnd('\0');

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
            var nameBytes = Encoding.ASCII.GetBytes(Name);
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
        private const int RoomTypesOffset = 0x00B2;       // 0x010EE2 - 0x10E30
        private const int RoomTypeCount = 10;
        private const int ObjectsOffset = 0x018E;          // 0x010FBE - 0x10E30
        private const int ObjectCount = 62;
        private const int EquipmentPointersOffset = 0x20E0; // 0x012F10 - 0x10E30
        private const int EquipmentPointerCount = 16;
        private const int EquipNavTableOffset = 0x2100; // 0x012F30 - 0x10E30
        private const int EquipNavTableCount = 48;
        private const int RagdollCoordsOffset = 0x2160;     // 0x012F90 - 0x10E30
        private const int RagdollCoordCount = 44;           // 43 entries + (0,0) terminator
        private const int EquipSlotRectsOffset = 0x2214;    // 0x013044 - 0x10E30
        private const int EquipSlotRectCount = 11;
        private const int CharNamePointersOffset = 0x346C; // in TrailingData region
        private const int CharNamePointerCount = 192;
        #endregion

        #region Fields (in binary order)

        /// <summary>Data before room types: MSC runtime, file refs, padding.</summary>
        public byte[] PreRoomData { get; set; } = Array.Empty<byte>();

        /// <summary>10 room type records (22 bytes each): name, rarity, size constraint, enabled flag.</summary>
        public TacRoomTypeRecord[] RoomTypes { get; set; } = Array.Empty<TacRoomTypeRecord>();

        /// <summary>Gap between room types and object records.</summary>
        public byte[] Unknown1 { get; set; } = Array.Empty<byte>();

        /// <summary>62 object/furniture records (20 bytes each): name, sprite, behaviour flags, room placement.</summary>
        public TacObjectRecord[] Objects { get; set; } = Array.Empty<TacObjectRecord>();

        /// <summary>Data before equipment names: spritesheet config, directional offsets, RastPort blocks, BSS, CGA animation, string data.</summary>
        public byte[] MidSectionPreEquipNames { get; set; } = Array.Empty<byte>();

        /// <summary>16 equipment name strings (resolved from DS-relative pointers).</summary>
        public string[] EquipmentNames { get; set; } = Array.Empty<string>();

        /// <summary>Data after equipment names but before equipment pointer table position.</summary>
        public byte[] MidSectionPostEquipNames { get; set; } = Array.Empty<byte>();

        // EquipmentNamePointers are computed at serialization time from EquipmentNames positions.

        /// <summary>
        /// Equipment selection UI navigation table: 12 rows (one per equipment item) x 4 columns
        /// (Up, Down, Left, Right). Each cell is the equipment index to navigate to when that
        /// arrow key is pressed. Defines the cursor movement grid for the equipment selection screen.
        /// </summary>
        public ushort[] EquipmentNavTable { get; set; } = Array.Empty<ushort>();

        /// <summary>43 screen coordinates for ragdoll item positions + (0,0) terminator.</summary>
        public TacScreenCoordinate[] RagdollCoordinates { get; set; } = Array.Empty<TacScreenCoordinate>();

        /// <summary>4-byte separator (0,0,0,0) between ragdoll coordinates and equipment slot rects.</summary>
        public byte[] Unknown3 { get; set; } = Array.Empty<byte>();

        /// <summary>11 TL/BR rectangle pairs for equipment slot UI positions.</summary>
        public TacScreenRect[] EquipmentSlotRects { get; set; } = Array.Empty<TacScreenRect>();

        /// <summary>Data after equipment slot rects and before character names: clue phrases, item tables.</summary>
        public byte[] PreCharNameData { get; set; } = Array.Empty<byte>();

        /// <summary>192 character names (4 ethnic groups x female first / male first / male surname, 16 each).</summary>
        public string[] CharacterNames { get; set; } = Array.Empty<string>();

        /// <summary>Data between character names and character name pointer table.</summary>
        public byte[] PostCharNameData { get; set; } = Array.Empty<byte>();

        // CharacterNamePointers are computed at serialization time.

        /// <summary>Everything after character name pointers: C runtime, BSS.</summary>
        public byte[] TrailingData { get; set; } = Array.Empty<byte>();

        #endregion

        public static TacDataSegment FromBytes(byte[] dataSegment)
        {
            var roomTypesEnd = RoomTypesOffset + RoomTypeCount * TacRoomTypeRecord.RecordSize;
            var objectsEnd = ObjectsOffset + ObjectCount * TacObjectRecord.RecordSize;
            var ragdollEnd = RagdollCoordsOffset + RagdollCoordCount * TacScreenCoordinate.RecordSize;
            var equipRectsEnd = EquipSlotRectsOffset + EquipSlotRectCount * TacScreenRect.RecordSize;

            var segment = new TacDataSegment();

            segment.PreRoomData = DataSegmentHelper.Slice(dataSegment, 0, RoomTypesOffset);

            segment.RoomTypes = new TacRoomTypeRecord[RoomTypeCount];
            for (var i = 0; i < RoomTypeCount; i++)
            {
                segment.RoomTypes[i] = TacRoomTypeRecord.FromBytes(dataSegment, RoomTypesOffset + i * TacRoomTypeRecord.RecordSize);
            }

            segment.Unknown1 = DataSegmentHelper.Slice(dataSegment, roomTypesEnd, ObjectsOffset - roomTypesEnd);

            segment.Objects = new TacObjectRecord[ObjectCount];
            for (var i = 0; i < ObjectCount; i++)
            {
                segment.Objects[i] = TacObjectRecord.FromBytes(dataSegment, ObjectsOffset + i * TacObjectRecord.RecordSize);
            }

            // Read equipment name pointers to find and extract the strings from the mid section
            var equipPtrs = DataSegmentHelper.BytesToUInt16Array(dataSegment, EquipmentPointersOffset, EquipmentPointerCount);
            segment.EquipmentNames = DataSegmentHelper.ExtractStringsFromPointers(equipPtrs, dataSegment);

            // Split mid section around the equipment name string block
            var (blockStart, blockEnd) = DataSegmentHelper.FindStringBlockBounds(equipPtrs, dataSegment);
            segment.MidSectionPreEquipNames = DataSegmentHelper.Slice(dataSegment, objectsEnd, blockStart - objectsEnd);
            segment.MidSectionPostEquipNames = DataSegmentHelper.Slice(dataSegment, blockEnd, EquipmentPointersOffset - blockEnd);

            segment.EquipmentNavTable = DataSegmentHelper.BytesToUInt16Array(dataSegment, EquipNavTableOffset, EquipNavTableCount);

            segment.RagdollCoordinates = new TacScreenCoordinate[RagdollCoordCount];
            for (var i = 0; i < RagdollCoordCount; i++)
            {
                segment.RagdollCoordinates[i] = TacScreenCoordinate.FromBytes(dataSegment, RagdollCoordsOffset + i * TacScreenCoordinate.RecordSize);
            }

            segment.Unknown3 = DataSegmentHelper.Slice(dataSegment, ragdollEnd, EquipSlotRectsOffset - ragdollEnd);

            segment.EquipmentSlotRects = new TacScreenRect[EquipSlotRectCount];
            for (var i = 0; i < EquipSlotRectCount; i++)
            {
                segment.EquipmentSlotRects[i] = TacScreenRect.FromBytes(dataSegment, EquipSlotRectsOffset + i * TacScreenRect.RecordSize);
            }

            // Extract character names using pointer table
            var charPtrs = DataSegmentHelper.BytesToUInt16Array(dataSegment, CharNamePointersOffset, CharNamePointerCount);
            segment.CharacterNames = DataSegmentHelper.ExtractStringsFromPointers(charPtrs, dataSegment);

            var (charBlockStart, charBlockEnd) = DataSegmentHelper.FindStringBlockBounds(charPtrs, dataSegment);
            segment.PreCharNameData = DataSegmentHelper.Slice(dataSegment, equipRectsEnd, charBlockStart - equipRectsEnd);
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

            // Compute equipment name pointer values from actual string positions
            var equipNamesBaseOffset = PreRoomData.Length + roomTypeBytes.Length + Unknown1.Length
                + objectBytes.Length + MidSectionPreEquipNames.Length;
            var equipNamePointers = DataSegmentHelper.ComputeStringPointers(EquipmentNames, equipNamesBaseOffset);
            var equipNamesBytes = DataSegmentHelper.NullTerminatedStringsToBytes(EquipmentNames);

            // Compute character name pointer values
            var charNamesBaseOffset = equipNamesBaseOffset + equipNamesBytes.Length
                + MidSectionPostEquipNames.Length
                + DataSegmentHelper.UInt16ArrayToBytes(equipNamePointers).Length
                + DataSegmentHelper.UInt16ArrayToBytes(EquipmentNavTable).Length
                + ragdollBytes.Length + Unknown3.Length
                + equipRectBytes.Length + PreCharNameData.Length;
            var charNamePointers = DataSegmentHelper.ComputeStringPointers(CharacterNames, charNamesBaseOffset);
            var charNamesBytes = DataSegmentHelper.NullTerminatedStringsToBytes(CharacterNames);

            return DataSegmentHelper.Concatenate(
                PreRoomData,
                roomTypeBytes,
                Unknown1,
                objectBytes,
                MidSectionPreEquipNames,
                equipNamesBytes,
                MidSectionPostEquipNames,
                DataSegmentHelper.UInt16ArrayToBytes(equipNamePointers),
                DataSegmentHelper.UInt16ArrayToBytes(EquipmentNavTable),
                ragdollBytes,
                Unknown3,
                equipRectBytes,
                PreCharNameData,
                charNamesBytes,
                PostCharNameData,
                DataSegmentHelper.UInt16ArrayToBytes(charNamePointers),
                TrailingData
            );
        }

        public TacDataSegment Clone()
        {
            return new TacDataSegment
            {
                PreRoomData = PreRoomData.ToArray(),
                RoomTypes = RoomTypes.Select(r => r.Clone()).ToArray(),
                Unknown1 = Unknown1.ToArray(),
                Objects = Objects.Select(o => o.Clone()).ToArray(),
                MidSectionPreEquipNames = MidSectionPreEquipNames.ToArray(),
                EquipmentNames = EquipmentNames.Select(s => s).ToArray(),
                MidSectionPostEquipNames = MidSectionPostEquipNames.ToArray(),
                EquipmentNavTable = EquipmentNavTable.ToArray(),
                RagdollCoordinates = RagdollCoordinates.Select(c => c.Clone()).ToArray(),
                Unknown3 = Unknown3.ToArray(),
                EquipmentSlotRects = EquipmentSlotRects.Select(r => r.Clone()).ToArray(),
                PreCharNameData = PreCharNameData.ToArray(),
                CharacterNames = CharacterNames.Select(s => s).ToArray(),
                PostCharNameData = PostCharNameData.ToArray(),
                TrailingData = TrailingData.ToArray()
            };
        }
    }
}
