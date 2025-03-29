using System.Collections.Generic;

namespace CovertActionTools.Core
{
    internal class Constants
    {
        //VGA palette, with colour index 5 replaced with plain black at full alpha
        public static readonly Dictionary<byte, (byte r, byte g, byte b, byte a)> VgaColorMapping = new()
        {
            {0, (0, 0, 0, 0)},
            {1, (0, 0, 0xAA, 255)},
            {2, (0, 0xAA, 0, 255)},
            {3, (0, 0xAA, 0xAA, 255)},
            {4, (0xAA, 0, 0, 255)},
            {5, (0, 0, 0, 255)},
            {6, (0xAA, 0x55, 0, 255)},
            {7, (0xAA, 0xAA, 0xAA, 255)},
            {8, (0x55, 0x55, 0x55, 255)},
            {9, (0x55, 0x55, 0xFF, 255)},
            {10, (0x55, 0xFF, 0x55, 255)},
            {11, (0x55, 0xFF, 0xFF, 255)},
            {12, (0xFF, 0x55, 0x55, 255)},
            {13, (0xFF, 0x55, 0xFF, 255)},
            {14, (0xFF, 0xFF, 0x55, 255)},
            {15, (0xFF, 0xFF, 0xFF, 255)},
        };
    }
}