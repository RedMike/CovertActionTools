using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CovertActionTools.Core.Compression
{
    internal static class ExepackUtilities
    {
        private static readonly byte[] ErrorString = Encoding.ASCII.GetBytes("Packed file is corrupt");

        #region MZ Header

        public static MzHeader ParseMzHeader(byte[] data)
        {
            if (data == null || data.Length < 28 || data[0] != (byte)'M' || data[1] != (byte)'Z')
            {
                return null;
            }

            return new MzHeader
            {
                LastPageBytes = BitConverter.ToUInt16(data, 2),
                Pages = BitConverter.ToUInt16(data, 4),
                RelocCount = BitConverter.ToUInt16(data, 6),
                HeaderParagraphs = BitConverter.ToUInt16(data, 8),
                MinExtra = BitConverter.ToUInt16(data, 10),
                MaxExtra = BitConverter.ToUInt16(data, 12),
                InitSS = BitConverter.ToUInt16(data, 14),
                InitSP = BitConverter.ToUInt16(data, 16),
                Checksum = BitConverter.ToUInt16(data, 18),
                InitIP = BitConverter.ToUInt16(data, 20),
                InitCS = BitConverter.ToUInt16(data, 22),
                RelocOffset = BitConverter.ToUInt16(data, 24),
                Overlay = BitConverter.ToUInt16(data, 26)
            };
        }

        #endregion

        #region EXEPACK Detection

        public static ExepackHeader DetectExepack(byte[] fileData, MzHeader header)
        {
            var payload = new byte[fileData.Length - header.HeaderSize];
            Array.Copy(fileData, header.HeaderSize, payload, 0, payload.Length);

            var cs = header.InitCS;
            var ip = header.InitIP;
            var exepackSegOff = cs * 16;

            if (exepackSegOff + 16 > payload.Length)
            {
                return null;
            }

            // Check for "RB" signature at CS:000E
            if (payload[exepackSegOff + 0x0E] != (byte)'R' || payload[exepackSegOff + 0x0F] != (byte)'B')
            {
                return null;
            }

            // Verify entry IP is 0x10 (standard EXEPACK stub entry)
            if (ip != 0x10)
            {
                return null;
            }

            var packedData = new byte[exepackSegOff];
            Array.Copy(payload, 0, packedData, 0, exepackSegOff);

            var exepackSize = BitConverter.ToUInt16(payload, exepackSegOff + 0x06);
            var exepackSegment = new byte[exepackSize];
            Array.Copy(payload, exepackSegOff, exepackSegment, 0, Math.Min(exepackSize, payload.Length - exepackSegOff));

            return new ExepackHeader
            {
                RealIP = BitConverter.ToUInt16(payload, exepackSegOff + 0x00),
                RealCS = BitConverter.ToUInt16(payload, exepackSegOff + 0x02),
                MemStart = BitConverter.ToUInt16(payload, exepackSegOff + 0x04),
                ExepackSize = exepackSize,
                RealSP = BitConverter.ToUInt16(payload, exepackSegOff + 0x08),
                RealSS = BitConverter.ToUInt16(payload, exepackSegOff + 0x0A),
                DestLen = BitConverter.ToUInt16(payload, exepackSegOff + 0x0C),
                SegmentOffset = exepackSegOff,
                PackedData = packedData,
                ExepackSegment = exepackSegment
            };
        }

        #endregion

        #region Stub Extraction

        public static byte[] ExtractStub(byte[] exepackSegment)
        {
            var errorPos = FindErrorString(exepackSegment);
            if (errorPos < 0)
            {
                return null;
            }

            // Stub is from offset 0x10 (after header) to the error string
            var stubLength = errorPos - 0x10;
            var stub = new byte[stubLength];
            Array.Copy(exepackSegment, 0x10, stub, 0, stubLength);
            return stub;
        }

        #endregion

        #region Relocations

        public static ushort[] ExtractRelocations(byte[] exepackSegment)
        {
            var errorPos = FindErrorString(exepackSegment);
            if (errorPos < 0)
            {
                throw new InvalidOperationException("Cannot find 'Packed file is corrupt' in EXEPACK segment");
            }

            var pos = errorPos + ErrorString.Length;
            var relocations = new List<ushort>();

            for (var block = 0; block < 16; block++)
            {
                var segBase = (ushort)(block * 0x1000);

                if (pos + 2 > exepackSegment.Length)
                {
                    break;
                }

                var count = BitConverter.ToUInt16(exepackSegment, pos);
                pos += 2;

                for (var i = 0; i < count; i++)
                {
                    if (pos + 2 > exepackSegment.Length)
                    {
                        break;
                    }

                    var offset = BitConverter.ToUInt16(exepackSegment, pos);
                    pos += 2;

                    relocations.Add(segBase);
                    if (offset == 0xFFFF)
                    {
                        // Cross-segment relocation
                        relocations.Add(0xFFF0);
                    }
                    else
                    {
                        relocations.Add(offset);
                    }
                }

                if (segBase == 0xF000)
                {
                    break;
                }
            }

            return relocations.ToArray();
        }

        public static byte[] BuildExepackRelocationTable(ushort[] relocations)
        {
            // Group relocations by 0x1000 block
            var blocks = new Dictionary<int, List<ushort>>();

            for (var i = 0; i < relocations.Length - 1; i += 2)
            {
                var seg = relocations[i];
                var off = relocations[i + 1];

                var blockIndex = seg / 0x1000;
                var blockSeg = blockIndex * 0x1000;

                // Compute the offset within the block
                var relOffset = (seg - blockSeg) * 16 + off;

                if (!blocks.ContainsKey(blockSeg))
                {
                    blocks[blockSeg] = new List<ushort>();
                }

                if (relOffset > 0xFFFE)
                {
                    // Cross-segment: use 0xFFFF marker
                    blocks[blockSeg].Add(0xFFFF);
                }
                else
                {
                    blocks[blockSeg].Add((ushort)relOffset);
                }
            }

            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);

            for (var block = 0; block < 16; block++)
            {
                var segBase = block * 0x1000;
                if (blocks.TryGetValue(segBase, out var entries))
                {
                    writer.Write((ushort)entries.Count);
                    foreach (var off in entries)
                    {
                        writer.Write(off);
                    }
                }
                else
                {
                    writer.Write((ushort)0);
                }
            }

            writer.Flush();
            return ms.ToArray();
        }

        #endregion

        #region Packed EXE Assembly

        public static byte[] BuildPackedExe(
            byte[] deadZone,
            byte[] compressedPayload,
            byte[] stub,
            byte[] originalMzHeader,
            ushort[] relocations,
            ushort realCs,
            ushort realIp,
            ushort realSs,
            ushort realSp,
            int fullPayloadLength)
        {
            // Build EXEPACK relocation table
            var relocTable = BuildExepackRelocationTable(relocations);

            // Build EXEPACK segment: [16-byte header] [stub] [error string] [reloc table]
            var segmentData = new byte[16 + stub.Length + ErrorString.Length + relocTable.Length];
            Array.Copy(stub, 0, segmentData, 16, stub.Length);
            Array.Copy(ErrorString, 0, segmentData, 16 + stub.Length, ErrorString.Length);
            Array.Copy(relocTable, 0, segmentData, 16 + stub.Length + ErrorString.Length, relocTable.Length);

            var exepackSize = (ushort)segmentData.Length;
            var destLen = (ushort)((fullPayloadLength + 15) / 16);

            // Fill in the EXEPACK header
            WriteUInt16(segmentData, 0x00, realIp);
            WriteUInt16(segmentData, 0x02, realCs);
            WriteUInt16(segmentData, 0x04, 0); // mem_start (filled at runtime)
            WriteUInt16(segmentData, 0x06, exepackSize);
            WriteUInt16(segmentData, 0x08, realSp);
            WriteUInt16(segmentData, 0x0A, realSs);
            WriteUInt16(segmentData, 0x0C, destLen);
            segmentData[0x0E] = (byte)'R';
            segmentData[0x0F] = (byte)'B';

            // Build packed data region: [dead_zone] [compressed] [FF padding]
            // Strip trailing 0xFF from the compressor output (it adds 1-2 padding bytes),
            // then compute CS from the actual data. Paragraph-alignment padding provides
            // the trailing 0xFF bytes that the EXEPACK stub scans for.
            var trimmedLength = compressedPayload.Length;
            while (trimmedLength > 0 && compressedPayload[trimmedLength - 1] == 0xFF)
            {
                trimmedLength--;
            }

            var trimmedRegionLength = deadZone.Length + trimmedLength;
            var exepackCs = (trimmedRegionLength + 15) / 16;
            var packedRegionLength = deadZone.Length + compressedPayload.Length;

            // Calculate FF padding. May be negative if the compressor's trailing 0xFF
            // extends past the CS boundary (trimmed region fit but untrimmed doesn't).
            // In that case, truncate the compressed data to fit — the trailing 0xFF
            // bytes are padding, not commands, so truncating them is safe.
            var paddingLength = exepackCs * 16 - packedRegionLength;
            var compressedBytesToCopy = compressedPayload.Length;
            if (paddingLength < 0)
            {
                compressedBytesToCopy += paddingLength; // reduce by overshoot
                paddingLength = 0;
            }

            // Assemble packed payload: [dead zone][compressed][FF padding][EXEPACK segment]
            var packedPayload = new byte[exepackCs * 16 + segmentData.Length];
            Array.Copy(deadZone, 0, packedPayload, 0, deadZone.Length);
            Array.Copy(compressedPayload, 0, packedPayload, deadZone.Length, compressedBytesToCopy);
            for (var i = 0; i < paddingLength; i++)
            {
                packedPayload[deadZone.Length + compressedBytesToCopy + i] = 0xFF;
            }
            Array.Copy(segmentData, 0, packedPayload, exepackCs * 16, segmentData.Length);

            // Build MZ header
            var mzHeaderSize = 512;
            var totalSize = mzHeaderSize + packedPayload.Length;
            var pages = (totalSize + 511) / 512;
            var lastPageBytes = totalSize % 512;

            var header = new byte[mzHeaderSize];

            if (originalMzHeader != null)
            {
                // Copy the original MZ header as base (including padding bytes)
                var copyLen = Math.Min(originalMzHeader.Length, mzHeaderSize);
                Array.Copy(originalMzHeader, 0, header, 0, copyLen);

                // Update fields that depend on compressed output
                WriteUInt16(header, 2, (ushort)lastPageBytes);
                WriteUInt16(header, 4, (ushort)pages);

                // min_extra: cover memory from end of file image to stack top
                var filePayloadParas = (totalSize - mzHeaderSize) / 16;
                var stackTopParas = (realSs * 16 + realSp + 15) / 16;
                var minExtra = Math.Max(0, stackTopParas - filePayloadParas);
                WriteUInt16(header, 10, (ushort)minExtra);
                WriteUInt16(header, 22, (ushort)exepackCs);
            }
            else
            {
                header[0] = (byte)'M';
                header[1] = (byte)'Z';
                WriteUInt16(header, 2, (ushort)lastPageBytes);
                WriteUInt16(header, 4, (ushort)pages);
                WriteUInt16(header, 6, 0);
                WriteUInt16(header, 8, (ushort)(mzHeaderSize / 16));
                WriteUInt16(header, 10, (ushort)(destLen - exepackCs));
                WriteUInt16(header, 12, 0xFFFF);
                WriteUInt16(header, 14, 0);
                WriteUInt16(header, 16, 0);
                WriteUInt16(header, 18, 0);
                WriteUInt16(header, 20, 0x10);
                WriteUInt16(header, 22, (ushort)exepackCs);
                WriteUInt16(header, 24, 0);
                WriteUInt16(header, 26, 0);
            }

            var result = new byte[header.Length + packedPayload.Length];
            Array.Copy(header, 0, result, 0, header.Length);
            Array.Copy(packedPayload, 0, result, header.Length, packedPayload.Length);
            return result;
        }

        #endregion

        #region Helpers

        private static int FindErrorString(byte[] data)
        {
            for (var i = 0; i <= data.Length - ErrorString.Length; i++)
            {
                var found = true;
                for (var j = 0; j < ErrorString.Length; j++)
                {
                    if (data[i + j] != ErrorString[j])
                    {
                        found = false;
                        break;
                    }
                }
                if (found)
                {
                    return i;
                }
            }
            return -1;
        }

        private static void WriteUInt16(byte[] buffer, int offset, ushort value)
        {
            buffer[offset] = (byte)(value & 0xFF);
            buffer[offset + 1] = (byte)((value >> 8) & 0xFF);
        }

        #endregion
    }
}
