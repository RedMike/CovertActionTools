using System;
using System.Collections.Generic;
using System.Linq;

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
    }
}
