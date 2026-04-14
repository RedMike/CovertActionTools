using System;
using System.Collections.Generic;
using CovertActionTools.Core.Models.Executables.Records;

namespace CovertActionTools.Core.Models.Executables.Records.Tac
{
    public class CgaDitherPair
    {
        public byte Low { get; set; }
        public byte High { get; set; }

        public CgaDitherPair() { }

        public CgaDitherPair(byte low, byte high)
        {
            Low = low;
            High = high;
        }

        public CgaDitherPair Clone()
        {
            return new CgaDitherPair(Low, High);
        }
    }

    /// <summary>
    /// 16-byte CGA color remap record. Each byte maps one of the game's 16 VGA palette
    /// indices to a CGA dither pair: the low nibble is one CGA color (0-3) and the high
    /// nibble is the other, alternated on adjacent pixels to approximate the VGA color
    /// within CGA's 4-color limitation. Consumed by overlay stub 27 (CGRAPHIC only;
    /// no-op on EGA/MCGA/Tandy) to build framebuffer pixel lookup tables.
    /// </summary>
    public class CgaColorRemapRecord : IExecutableRecord
    {
        public const int RecordSize = 16;
        public const int EntryCount = 16;

        public Dictionary<byte, CgaDitherPair> ColorMap { get; set; } = new Dictionary<byte, CgaDitherPair>();

        public int ReadBytes(byte[] fullPayload, int startingOffset)
        {
            ColorMap = new Dictionary<byte, CgaDitherPair>(EntryCount);
            for (var i = 0; i < EntryCount; i++)
            {
                var packed = fullPayload[startingOffset + i];
                var low = (byte)(packed & 0x0F);
                var high = (byte)((packed >> 4) & 0x0F);
                ValidateCgaColor(low, i, "low");
                ValidateCgaColor(high, i, "high");
                ColorMap[(byte)i] = new CgaDitherPair(low, high);
            }
            return RecordSize;
        }

        public byte[] WriteBytes()
        {
            var result = new byte[RecordSize];
            for (var i = 0; i < EntryCount; i++)
            {
                var key = (byte)i;
                if (!ColorMap.TryGetValue(key, out var pair))
                    throw new Exception($"CgaColorRemapRecord: missing entry for VGA index {i}.");
                ValidateCgaColor(pair.Low, i, "low");
                ValidateCgaColor(pair.High, i, "high");
                result[i] = (byte)((pair.High << 4) | pair.Low);
            }
            return result;
        }

        public CgaColorRemapRecord Clone()
        {
            var result = new CgaColorRemapRecord
            {
                ColorMap = new Dictionary<byte, CgaDitherPair>(EntryCount)
            };
            foreach (var kvp in ColorMap)
            {
                result.ColorMap[kvp.Key] = kvp.Value.Clone();
            }
            return result;
        }

        private static void ValidateCgaColor(byte value, int index, string nibbleName)
        {
            if (value > 3)
                throw new Exception(
                    $"CgaColorRemapRecord: {nibbleName} nibble for VGA index {index} is {value}, must be 0-3.");
        }
    }
}
