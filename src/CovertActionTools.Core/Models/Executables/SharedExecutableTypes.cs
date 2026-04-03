using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CovertActionTools.Core.Models.Executables
{
    #region TAC Types

    public class TacRoomTypeRecord
    {
        public const int RecordSize = 22;
        public const int NameLength = 16;

        /// <summary>
        /// Room type name, null-padded to 16 bytes.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Rarity weight — higher = less likely to be chosen for a room slot.
        /// </summary>
        public ushort Rarity { get; set; }

        /// <summary>
        /// Size constraint category: 1=small only, 2=no constraint, 4=large only.
        /// </summary>
        public ushort SizeConstraint { get; set; }

        /// <summary>
        /// Room enabled flag: 7=enabled, 0=disabled.
        /// </summary>
        public ushort Enabled { get; set; }

        public TacRoomTypeRecord Clone()
        {
            return new TacRoomTypeRecord
            {
                Name = Name,
                Rarity = Rarity,
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
                Rarity = BitConverter.ToUInt16(data, offset + NameLength),
                SizeConstraint = BitConverter.ToUInt16(data, offset + NameLength + 2),
                Enabled = BitConverter.ToUInt16(data, offset + NameLength + 4)
            };
        }

        public byte[] ToBytes()
        {
            var result = new byte[RecordSize];
            var nameBytes = Encoding.ASCII.GetBytes(Name);
            Array.Copy(nameBytes, 0, result, 0, Math.Min(nameBytes.Length, NameLength));
            result[NameLength] = (byte)(Rarity & 0xFF);
            result[NameLength + 1] = (byte)((Rarity >> 8) & 0xFF);
            result[NameLength + 2] = (byte)(SizeConstraint & 0xFF);
            result[NameLength + 3] = (byte)((SizeConstraint >> 8) & 0xFF);
            result[NameLength + 4] = (byte)(Enabled & 0xFF);
            result[NameLength + 5] = (byte)((Enabled >> 8) & 0xFF);
            return result;
        }
    }

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
        /// Object behaviour bitfield (openable, buggable, photographable, etc.).
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

    #endregion

    #region FINAL Types

    public class FinalMissionSetRecord
    {
        public const int RecordSize = 74;

        /// <summary>
        /// The full 74-byte record data. Name is embedded at the start (null-terminated).
        /// Further field decomposition will be done in a future update.
        /// </summary>
        public byte[] RecordData { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Convenience property: extracts the null-terminated name from RecordData.
        /// </summary>
        public string Name
        {
            get
            {
                if (RecordData == null || RecordData.Length == 0) return string.Empty;
                var end = Array.IndexOf(RecordData, (byte)0);
                if (end < 0) end = Math.Min(RecordData.Length, 20);
                return Encoding.ASCII.GetString(RecordData, 0, end);
            }
        }

        public FinalMissionSetRecord Clone()
        {
            return new FinalMissionSetRecord
            {
                RecordData = RecordData.ToArray()
            };
        }

        public static FinalMissionSetRecord FromBytes(byte[] data, int offset)
        {
            var record = new byte[RecordSize];
            Array.Copy(data, offset, record, 0, RecordSize);
            return new FinalMissionSetRecord { RecordData = record };
        }

        public byte[] ToBytes()
        {
            var result = new byte[RecordSize];
            Array.Copy(RecordData, 0, result, 0, Math.Min(RecordData.Length, RecordSize));
            return result;
        }
    }

    #endregion

    #region Helpers

    internal static class DataSegmentHelper
    {
        public static byte[] Slice(byte[] data, int offset, int length)
        {
            var result = new byte[length];
            Array.Copy(data, offset, result, 0, length);
            return result;
        }

        public static List<byte[]> Collect(params byte[][] segments)
        {
            return segments.ToList();
        }

        public static byte[] Concatenate(params byte[][] segments)
        {
            var totalLength = 0;
            foreach (var s in segments) totalLength += s.Length;
            var result = new byte[totalLength];
            var pos = 0;
            foreach (var s in segments)
            {
                Array.Copy(s, 0, result, pos, s.Length);
                pos += s.Length;
            }
            return result;
        }

        public static byte[] UInt16ArrayToBytes(ushort[] values)
        {
            var result = new byte[values.Length * 2];
            for (var i = 0; i < values.Length; i++)
            {
                result[i * 2] = (byte)(values[i] & 0xFF);
                result[i * 2 + 1] = (byte)((values[i] >> 8) & 0xFF);
            }
            return result;
        }

        public static ushort[] BytesToUInt16Array(byte[] data, int offset, int count)
        {
            var result = new ushort[count];
            for (var i = 0; i < count; i++)
            {
                result[i] = BitConverter.ToUInt16(data, offset + i * 2);
            }
            return result;
        }
    }

    #endregion
}
