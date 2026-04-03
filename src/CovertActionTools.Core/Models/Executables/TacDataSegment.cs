using System;
using System.Linq;

namespace CovertActionTools.Core.Models.Executables
{
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
        private const int UnknownEquipTableOffset = 0x2100; // 0x012F30 - 0x10E30
        private const int UnknownEquipTableSize = 96;       // 48 × uint16
        private const int RagdollCoordsOffset = 0x2160;     // 0x012F90 - 0x10E30
        private const int RagdollCoordCount = 44;           // 43 entries + (0,0) terminator
        private const int EquipSlotRectsOffset = 0x2214;    // 0x013044 - 0x10E30
        private const int EquipSlotRectCount = 11;
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

        /// <summary>Spritesheet config, directional offsets, RastPort blocks, BSS, CGA animation, all string data.</summary>
        public byte[] MidSection { get; set; } = Array.Empty<byte>();

        /// <summary>16 DS-relative pointers to equipment name strings.</summary>
        public ushort[] EquipmentNamePointers { get; set; } = Array.Empty<ushort>();

        /// <summary>48 × uint16 table (values 0-11), purpose undecoded.</summary>
        public byte[] Unknown2 { get; set; } = Array.Empty<byte>();

        /// <summary>43 screen coordinates for ragdoll item positions + (0,0) terminator.</summary>
        public TacScreenCoordinate[] RagdollCoordinates { get; set; } = Array.Empty<TacScreenCoordinate>();

        /// <summary>4-byte separator (0,0,0,0) between ragdoll coordinates and equipment slot rects.</summary>
        public byte[] Unknown3 { get; set; } = Array.Empty<byte>();

        /// <summary>11 TL/BR rectangle pairs for equipment slot UI positions.</summary>
        public TacScreenRect[] EquipmentSlotRects { get; set; } = Array.Empty<TacScreenRect>();

        /// <summary>Everything after equipment slot rects: clue phrases, item tables, character names, C runtime, BSS.</summary>
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

            segment.MidSection = DataSegmentHelper.Slice(dataSegment, objectsEnd, EquipmentPointersOffset - objectsEnd);

            segment.EquipmentNamePointers = DataSegmentHelper.BytesToUInt16Array(dataSegment, EquipmentPointersOffset, EquipmentPointerCount);

            segment.Unknown2 = DataSegmentHelper.Slice(dataSegment, UnknownEquipTableOffset, UnknownEquipTableSize);

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

            segment.TrailingData = DataSegmentHelper.Slice(dataSegment, equipRectsEnd, dataSegment.Length - equipRectsEnd);

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

            return DataSegmentHelper.Concatenate(
                PreRoomData,
                roomTypeBytes,
                Unknown1,
                objectBytes,
                MidSection,
                DataSegmentHelper.UInt16ArrayToBytes(EquipmentNamePointers),
                Unknown2,
                ragdollBytes,
                Unknown3,
                equipRectBytes,
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
                MidSection = MidSection.ToArray(),
                EquipmentNamePointers = EquipmentNamePointers.ToArray(),
                Unknown2 = Unknown2.ToArray(),
                RagdollCoordinates = RagdollCoordinates.Select(c => c.Clone()).ToArray(),
                Unknown3 = Unknown3.ToArray(),
                EquipmentSlotRects = EquipmentSlotRects.Select(r => r.Clone()).ToArray(),
                TrailingData = TrailingData.ToArray()
            };
        }
    }
}
