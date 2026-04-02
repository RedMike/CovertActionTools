using System;
using System.Collections.Generic;

namespace CovertActionTools.UnitTests.Core.Compression.Data
{
    internal static class ExepackTestDataGenerator
    {
        /// <summary>
        /// Generates a payload of uniform bytes (all the same value).
        /// This should compress entirely as FILL commands.
        /// </summary>
        public static byte[] GenerateUniformPayload(int size, byte value = 0x00)
        {
            var data = new byte[size];
            for (var i = 0; i < size; i++)
            {
                data[i] = value;
            }
            return data;
        }

        /// <summary>
        /// Generates a payload of varied bytes using a simple PRNG.
        /// No runs of >= 8 identical bytes, so this should compress entirely as COPY commands.
        /// </summary>
        public static byte[] GenerateVariedPayload(int size, int seed = 42)
        {
            var data = new byte[size];
            var rng = new Random(seed);
            for (var i = 0; i < size; i++)
            {
                data[i] = (byte)(rng.Next(1, 256)); // 1-255 to avoid long runs of 0
            }

            // Break up any accidental runs of >= 8
            for (var i = 0; i < size - 1; i++)
            {
                var runLen = 1;
                while (i + runLen < size && data[i + runLen] == data[i] && runLen < 8)
                {
                    runLen++;
                }
                if (runLen >= 8)
                {
                    data[i + 7] = (byte)((data[i + 7] + 1) % 256);
                }
            }

            return data;
        }

        /// <summary>
        /// Generates a payload mixing runs (for FILL) and varied data (for COPY).
        /// </summary>
        public static byte[] GenerateMixedPayload()
        {
            var parts = new List<byte>();

            // 16 bytes of 0xAA (FILL)
            for (var i = 0; i < 16; i++) parts.Add(0xAA);

            // 10 varied bytes (COPY)
            for (var i = 0; i < 10; i++) parts.Add((byte)(0x10 + i));

            // 32 bytes of 0x55 (FILL)
            for (var i = 0; i < 32; i++) parts.Add(0x55);

            // 20 varied bytes (COPY)
            for (var i = 0; i < 20; i++) parts.Add((byte)(0x30 + i));

            // 8 bytes of 0xFF (FILL - exactly at MIN_RUN boundary)
            for (var i = 0; i < 8; i++) parts.Add(0xFF);

            return parts.ToArray();
        }

        /// <summary>
        /// Builds a minimal hand-crafted EXEPACK compressed data buffer containing
        /// a single FILL command with STOP bit.
        /// </summary>
        public static byte[] BuildSingleFillCommand(byte fillByte, int length)
        {
            // Format (low to high): fill_byte, length_lo, length_hi, command
            // Command 0xB1 = FILL + STOP
            return new byte[]
            {
                fillByte,
                (byte)(length & 0xFF),
                (byte)((length >> 8) & 0xFF),
                0xB1
            };
        }

        /// <summary>
        /// Builds a minimal hand-crafted EXEPACK compressed data buffer containing
        /// a single COPY command with STOP bit.
        /// </summary>
        public static byte[] BuildSingleCopyCommand(byte[] data)
        {
            // Format (low to high): data_bytes, length_lo, length_hi, command
            // Command 0xB3 = COPY + STOP
            var result = new byte[data.Length + 3];
            Array.Copy(data, 0, result, 0, data.Length);
            result[data.Length] = (byte)(data.Length & 0xFF);
            result[data.Length + 1] = (byte)((data.Length >> 8) & 0xFF);
            result[data.Length + 2] = 0xB3;
            return result;
        }

        /// <summary>
        /// Builds compressed data with two commands: a COPY (with STOP) followed by a FILL.
        /// The decompressor reads from high to low, so it processes FILL first, then COPY+STOP.
        /// </summary>
        public static byte[] BuildTwoCommands(byte fillByte, int fillLength, byte[] copyData)
        {
            var parts = new List<byte>();

            // First command in memory (last processed = STOP): COPY
            parts.AddRange(copyData);
            parts.Add((byte)(copyData.Length & 0xFF));
            parts.Add((byte)((copyData.Length >> 8) & 0xFF));
            parts.Add(0xB3); // COPY + STOP

            // Second command in memory (first processed): FILL
            parts.Add(fillByte);
            parts.Add((byte)(fillLength & 0xFF));
            parts.Add((byte)((fillLength >> 8) & 0xFF));
            parts.Add(0xB0); // FILL (no STOP)

            return parts.ToArray();
        }

        /// <summary>
        /// Builds a valid minimal MZ header (28 bytes).
        /// </summary>
        public static byte[] BuildMinimalMzHeader()
        {
            var header = new byte[28];
            header[0] = (byte)'M';
            header[1] = (byte)'Z';
            // pages = 1
            header[4] = 1;
            header[5] = 0;
            // header_paras = 2 (32 bytes, padded)
            header[8] = 2;
            header[9] = 0;
            return header;
        }
    }
}
