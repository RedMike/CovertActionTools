using System;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.Core.Compression
{
    internal interface IExepackDecompression
    {
        ExepackDecompressionResult Decompress(byte[] packedData, int destLenParagraphs);
    }

    internal class ExepackDecompression : IExepackDecompression
    {
        private readonly ILogger _logger;

        public ExepackDecompression(ILogger<ExepackDecompression> logger)
        {
            _logger = logger;
        }

        public ExepackDecompressionResult Decompress(byte[] packedData, int destLenParagraphs)
        {
            var destSize = destLenParagraphs * 16;
            var output = new byte[destSize];

            // Skip trailing 0xFF padding
            var pos = packedData.Length - 1;
            while (pos >= 0 && packedData[pos] == 0xFF)
            {
                pos--;
            }

            if (pos < 0)
            {
                throw new InvalidOperationException("Packed data is all 0xFF padding");
            }

            var outPos = destSize; // write position (fill from end to start)

            while (pos >= 2)
            {
                // Read command byte
                var command = packedData[pos];
                pos--;

                // Read 16-bit length: high byte first (at higher address), then low byte
                var lengthHi = packedData[pos];
                pos--;
                var lengthLo = packedData[pos];
                pos--;
                var length = lengthLo | (lengthHi << 8);

                var cmdType = command & 0xFE;

                if (cmdType == 0xB0)
                {
                    // Fill: read one byte, repeat it length times
                    if (pos < 0)
                    {
                        throw new InvalidOperationException(
                            $"Unexpected end of packed data during fill command at pos {pos}");
                    }

                    var fillByte = packedData[pos];
                    pos--;
                    outPos -= length;

                    if (outPos < 0)
                    {
                        throw new InvalidOperationException(
                            $"Output underflow during fill: outPos={outPos}, length={length}");
                    }

                    for (var i = 0; i < length; i++)
                    {
                        output[outPos + i] = fillByte;
                    }
                }
                else if (cmdType == 0xB2)
                {
                    // Copy: copy length bytes from packed data
                    outPos -= length;

                    if (outPos < 0)
                    {
                        throw new InvalidOperationException(
                            $"Output underflow during copy: outPos={outPos}, length={length}");
                    }

                    if (pos - length + 1 < 0)
                    {
                        throw new InvalidOperationException(
                            $"Packed data underflow during copy: pos={pos}, length={length}");
                    }

                    for (var i = 0; i < length; i++)
                    {
                        output[outPos + length - 1 - i] = packedData[pos];
                        pos--;
                    }
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Unknown EXEPACK command: 0x{command:X2} at packed pos {pos + 3}");
                }

                // Bit 0 set means last command
                if ((command & 1) != 0)
                {
                    break;
                }
            }

            _logger.LogDebug(
                "EXEPACK decompressed {PackedSize} bytes to {DestSize} bytes, dead zone boundary at {DeadZone}",
                packedData.Length, destSize, outPos);

            return new ExepackDecompressionResult(output, outPos);
        }
    }
}
