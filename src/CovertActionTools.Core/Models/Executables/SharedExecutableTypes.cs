using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CovertActionTools.Core.Models.Executables
{
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

        public static string[] NullTerminatedStringsFromBytes(byte[] data, int offset, int count)
        {
            var result = new string[count];
            var pos = offset;
            for (var i = 0; i < count; i++)
            {
                var end = pos;
                while (end < data.Length && data[end] != 0) end++;
                result[i] = Encoding.ASCII.GetString(data, pos, end - pos);
                pos = end + 1; // skip null terminator
            }
            return result;
        }

        public static string[] AllNullTerminatedStringsFromBytes(byte[] data, int offset, int length)
        {
            var strings = new List<string>();
            var pos = offset;
            var end = offset + length;
            while (pos < end)
            {
                var strEnd = pos;
                while (strEnd < end && data[strEnd] != 0) strEnd++;
                strings.Add(Encoding.ASCII.GetString(data, pos, strEnd - pos));
                pos = strEnd + 1;
            }
            return strings.ToArray();
        }

        public static byte[] NullTerminatedStringsToBytes(string[] strings)
        {
            var parts = new List<byte>();
            foreach (var s in strings)
            {
                parts.AddRange(Encoding.ASCII.GetBytes(s));
                parts.Add(0);
            }
            return parts.ToArray();
        }

        public static (string[] strings, int[] byteSizes) NullTerminatedStringsWithSizesFromBytes(byte[] data, int offset, int count)
        {
            var strings = new string[count];
            var sizes = new int[count];
            var pos = offset;
            for (var i = 0; i < count; i++)
            {
                var end = pos;
                while (end < data.Length && data[end] != 0) end++;
                strings[i] = Encoding.ASCII.GetString(data, pos, end - pos);
                sizes[i] = end - pos + 1; // string length + null terminator
                pos = end + 1;
            }
            return (strings, sizes);
        }

        public static (string[] strings, int[] byteSizes) AllNullTerminatedStringsWithSizesFromBytes(byte[] data, int offset, int length)
        {
            var strings = new List<string>();
            var sizes = new List<int>();
            var pos = offset;
            var end = offset + length;
            while (pos < end)
            {
                var strEnd = pos;
                while (strEnd < end && data[strEnd] != 0) strEnd++;
                strings.Add(Encoding.ASCII.GetString(data, pos, strEnd - pos));
                sizes.Add(strEnd - pos + 1);
                pos = strEnd + 1;
            }
            return (strings.ToArray(), sizes.ToArray());
        }

        public static byte[] NullTerminatedStringsToFixedBytes(string[] strings, int[] originalByteSizes)
        {
            var parts = new List<byte>();
            for (var i = 0; i < strings.Length; i++)
            {
                var slotSize = i < originalByteSizes.Length ? originalByteSizes[i] : strings[i].Length + 1;
                var slot = new byte[slotSize];
                var strBytes = Encoding.ASCII.GetBytes(strings[i]);
                Array.Copy(strBytes, 0, slot, 0, Math.Min(strBytes.Length, slotSize - 1));
                parts.AddRange(slot);
            }
            return parts.ToArray();
        }

        public static byte[] PadToSize(byte[] data, int size)
        {
            if (data.Length >= size) return DataSegmentHelper.Slice(data, 0, size);
            var result = new byte[size];
            Array.Copy(data, 0, result, 0, data.Length);
            return result;
        }

        public static byte[] NullTerminatedStringsToBytesFixedSize(string[] strings, int size)
        {
            var result = new byte[size];
            var pos = 0;
            foreach (var s in strings)
            {
                var bytes = Encoding.ASCII.GetBytes(s);
                var toCopy = Math.Min(bytes.Length, size - pos);
                if (toCopy > 0)
                {
                    Array.Copy(bytes, 0, result, pos, toCopy);
                    pos += toCopy;
                }
                if (pos < size)
                {
                    result[pos] = 0;
                    pos++;
                }
            }
            return result;
        }
    }

    /// <summary>
    /// Rectangle drawing record (12 bytes) found in BUG.EXE and GAME.EXE.
    /// Defines lines and filled rectangles for screen layout rendering.
    /// Field interpretations are based on reverse engineering and may not be fully accurate.
    /// </summary>
    public class RectDrawRecord
    {
        public const int RecordSize = 12;

        /// <summary>Padding byte (always 0x00).</summary>
        public byte Padding { get; set; }

        /// <summary>Drawing flag: 0=line, 1=filled rect, 2=control/group marker.</summary>
        public byte Flag { get; set; }

        /// <summary>X1 coordinate (or metadata field when Flag=2).</summary>
        public ushort X1 { get; set; }

        /// <summary>Y1 coordinate (or metadata field when Flag=2).</summary>
        public ushort Y1 { get; set; }

        /// <summary>X2 coordinate (or metadata field when Flag=2).</summary>
        public ushort X2 { get; set; }

        /// <summary>Y2 coordinate (or metadata field when Flag=2).</summary>
        public ushort Y2 { get; set; }

        /// <summary>VGA palette colour index (0-15).</summary>
        public ushort Colour { get; set; }

        public RectDrawRecord Clone()
        {
            return new RectDrawRecord
            {
                Padding = Padding,
                Flag = Flag,
                X1 = X1, Y1 = Y1,
                X2 = X2, Y2 = Y2,
                Colour = Colour
            };
        }

        public static RectDrawRecord FromBytes(byte[] data, int offset)
        {
            return new RectDrawRecord
            {
                Padding = data[offset],
                Flag = data[offset + 1],
                X1 = BitConverter.ToUInt16(data, offset + 2),
                Y1 = BitConverter.ToUInt16(data, offset + 4),
                X2 = BitConverter.ToUInt16(data, offset + 6),
                Y2 = BitConverter.ToUInt16(data, offset + 8),
                Colour = BitConverter.ToUInt16(data, offset + 10)
            };
        }

        public byte[] ToBytes()
        {
            var result = new byte[RecordSize];
            result[0] = Padding;
            result[1] = Flag;
            result[2] = (byte)(X1 & 0xFF); result[3] = (byte)((X1 >> 8) & 0xFF);
            result[4] = (byte)(Y1 & 0xFF); result[5] = (byte)((Y1 >> 8) & 0xFF);
            result[6] = (byte)(X2 & 0xFF); result[7] = (byte)((X2 >> 8) & 0xFF);
            result[8] = (byte)(Y2 & 0xFF); result[9] = (byte)((Y2 >> 8) & 0xFF);
            result[10] = (byte)(Colour & 0xFF); result[11] = (byte)((Colour >> 8) & 0xFF);
            return result;
        }
    }
}
