using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.Core.Compression
{
    internal interface IExepackCompression
    {
        byte[] Compress(byte[] payload);
    }

    internal class ExepackCompression : IExepackCompression
    {
        private const int MinRun = 8;

        private readonly ILogger _logger;

        public ExepackCompression(ILogger<ExepackCompression> logger)
        {
            _logger = logger;
        }

        public byte[] Compress(byte[] payload)
        {
            var commands = new List<(byte cmd, int length, byte[] data)>();

            var pos = 0;
            var payloadLength = payload.Length;

            while (pos < payloadLength)
            {
                // Check for a run of identical bytes
                var runByte = payload[pos];
                var runLen = 1;
                while (pos + runLen < payloadLength &&
                       payload[pos + runLen] == runByte &&
                       runLen < 0xFFFF)
                {
                    runLen++;
                }

                if (runLen >= MinRun)
                {
                    // Use fill command
                    commands.Add((0xB0, runLen, new[] { runByte }));
                    pos += runLen;
                }
                else
                {
                    // Collect literal bytes until we hit a run >= MinRun
                    var copyStart = pos;
                    while (pos < payloadLength)
                    {
                        var rb = payload[pos];
                        var rl = 1;
                        while (pos + rl < payloadLength &&
                               payload[pos + rl] == rb &&
                               rl < MinRun)
                        {
                            rl++;
                        }

                        if (rl >= MinRun)
                        {
                            break;
                        }

                        pos++;
                        if (pos - copyStart >= 0xFFFF)
                        {
                            break;
                        }
                    }

                    var copyLen = pos - copyStart;
                    if (copyLen > 0)
                    {
                        var copyData = new byte[copyLen];
                        Array.Copy(payload, copyStart, copyData, 0, copyLen);
                        commands.Add((0xB2, copyLen, copyData));
                    }
                }
            }

            // Set bit 0 on the FIRST command (lowest address in compressed output,
            // which is the LAST command read by the decompressor since it reads backward)
            if (commands.Count > 0)
            {
                var (cmd, ln, data) = commands[0];
                commands[0] = ((byte)(cmd | 1), ln, data);
            }

            // Build compressed output (stored low to high; decompressor reads high to low)
            var compressed = new List<byte>();
            foreach (var (cmd, ln, data) in commands)
            {
                if ((cmd & 0xFE) == 0xB0)
                {
                    // Fill: data_byte, length_lo, length_hi, command
                    compressed.Add(data[0]);
                    compressed.Add((byte)(ln & 0xFF));
                    compressed.Add((byte)((ln >> 8) & 0xFF));
                    compressed.Add(cmd);
                }
                else
                {
                    // Copy: copy_data, length_lo, length_hi, command
                    compressed.AddRange(data);
                    compressed.Add((byte)(ln & 0xFF));
                    compressed.Add((byte)((ln >> 8) & 0xFF));
                    compressed.Add(cmd);
                }
            }

            // Add 0xFF padding (pad to even length + 1 extra)
            while (compressed.Count % 2 != 0)
            {
                compressed.Add(0xFF);
            }
            compressed.Add(0xFF);

            _logger.LogDebug(
                "EXEPACK compressed {OriginalSize} bytes to {CompressedSize} bytes ({CommandCount} commands)",
                payload.Length, compressed.Count, commands.Count);

            return compressed.ToArray();
        }
    }
}
