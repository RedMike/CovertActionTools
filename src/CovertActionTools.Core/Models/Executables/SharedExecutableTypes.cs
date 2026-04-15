using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CovertActionTools.Core.Models.Executables.Sections;

namespace CovertActionTools.Core.Models.Executables
{
    internal static class DataSegmentHelper
    {
        public static byte[] Slice(byte[] data, int offset, int length)
        {
            if (length <= 0 || offset >= data.Length) return Array.Empty<byte>();
            if (offset + length > data.Length) length = data.Length - offset;
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

        public static byte[] Concatenate(params IExecutableSection[] sections)
        {
            var parts = new List<byte>();
            foreach (var section in sections)
            {
                var bytes = section.WriteBytes();
                parts.AddRange(bytes);
                if (section is IPaddedToParagraph && parts.Count % 16 != 0)
                {
                    var padding = 16 - (parts.Count % 16);
                    for (var i = 0; i < padding; i++) parts.Add(0);
                }
                else if (section is IPaddedToDword && parts.Count % 4 != 0)
                {
                    var padding = 4 - (parts.Count % 4);
                    for (var i = 0; i < padding; i++) parts.Add(0);
                }
                else if (section is IPaddedToWord && parts.Count % 2 != 0)
                {
                    parts.Add(0);
                }
            }
            return parts.ToArray();
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
                result[i] = DecodeControlString(data, pos, end - pos);
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
                strings.Add(DecodeControlString(data, pos, strEnd - pos));
                pos = strEnd + 1;
            }
            return strings.ToArray();
        }

        // TODO: Variable-size string serialization requires patching ALL DS-relative references
        // in the code segment (hardcoded immediate values in MOV/LEA/PUSH instructions).
        // Without code segment relocation, changing string sizes shifts downstream data and
        // crashes the game. For now, all non-pointer-table strings use fixed-size slots.
        // To implement properly:
        //   1. Scan code segment for instructions with DS-relative immediate operands
        //   2. Build a relocation map (code offset -> DS offset referenced)
        //   3. After data segment rebuild, compute deltas and patch code references
        // Pointer-table-backed strings (CharacterNames, ClueRelPhrases, MonthNames,
        // RankNames/EvidenceTypes/EvidenceItems) can already resize freely.

        public static byte[] NullTerminatedStringsToBytes(string[] strings)
        {
            var parts = new List<byte>();
            foreach (var s in strings)
            {
                parts.AddRange(DataSegmentHelper.EncodeControlString(s));
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
                strings[i] = DecodeControlString(data, pos, end - pos);
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
                strings.Add(DecodeControlString(data, pos, strEnd - pos));
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
                var strBytes = EncodeControlString(strings[i]);
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
                var bytes = EncodeControlString(s);
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

        /// <summary>
        /// Computes DS-relative pointer values for null-terminated strings starting at baseOffset.
        /// Returns one pointer per string, each pointing to the start of that string.
        /// </summary>
        public static ushort[] ComputeStringPointers(string[] strings, int baseOffset)
        {
            var pointers = new ushort[strings.Length];
            var pos = baseOffset;
            for (var i = 0; i < strings.Length; i++)
            {
                pointers[i] = (ushort)pos;
                pos += EncodeControlString(strings[i]).Length + 1; // string + null terminator
            }
            return pointers;
        }

        /// <summary>
        /// Given DS-relative pointer values and the full data segment bytes, finds the byte range
        /// [start, end) of the contiguous string block that the pointers reference.
        /// Skips zero-valued pointers (empty/padding entries).
        /// </summary>
        public static (int start, int end) FindStringBlockBounds(ushort[] pointers, byte[] dataSegment)
        {
            var validPointers = new List<int>();
            foreach (var p in pointers)
            {
                if (p > 0 && p < dataSegment.Length) validPointers.Add(p);
            }
            if (validPointers.Count == 0) return (0, 0);

            var start = validPointers.Min();

            // Find the end of the last string (scan past the highest pointer to the null terminator)
            var maxPtr = validPointers.Max();
            var end = maxPtr;
            while (end < dataSegment.Length && dataSegment[end] != 0) end++;
            end++; // include null terminator

            return (start, end);
        }

        /// <summary>
        /// Extracts null-terminated strings from the data segment at the positions indicated by
        /// DS-relative pointer values. Skips zero-valued pointers (returns empty string for those).
        /// </summary>
        public static string[] ExtractStringsFromPointers(ushort[] pointers, byte[] dataSegment)
        {
            var result = new string[pointers.Length];
            for (var i = 0; i < pointers.Length; i++)
            {
                var ptr = pointers[i];
                if (ptr == 0 || ptr >= dataSegment.Length)
                {
                    result[i] = string.Empty;
                    continue;
                }
                var end = (int)ptr;
                while (end < dataSegment.Length && dataSegment[end] != 0) end++;
                result[i] = DataSegmentHelper.DecodeControlString(dataSegment, (int)ptr, end - (int)ptr);
            }
            return result;
        }

        #region Control-byte string encoding

        /// <summary>
        /// Named control byte tokens used in the game's text rendering engine.
        /// Bytes >= 0x80 are special formatting codes; these provide human-readable
        /// display names for editing.
        /// </summary>
        /// <summary>
        /// Returns the list of known control byte tokens with descriptions, for UI display.
        /// </summary>
        public static (string token, string description)[] GetControlByteTokenInfo()
        {
            var result = new (string, string)[ControlByteTokens.Length + 1];
            for (var i = 0; i < ControlByteTokens.Length; i++)
                result[i] = (ControlByteTokens[i].token, ControlByteTokens[i].description);
            result[ControlByteTokens.Length] = ("[0xNN]", "Arbitrary hex byte value");
            return result;
        }

        // All bytes >= 0x80 are color changes: low nibble (byte & 0x0F) = VGA palette index.
        // High nibble is ignored by the renderer. Named tokens are provided for commonly
        // used values; all others render as [0xNN] and work identically.
        // VGA palette: 0=black, 1=blue, 2=green, 3=cyan, 4=red, 5=magenta, 6=brown,
        //   7=light grey, 8=dark grey, 9=light blue, 10=light green, 11=light cyan,
        //   12=light red, 13=light magenta*, 14=yellow*, 15=white
        //   * Colors 13 and 14 are dynamically replaced by player/enemy clothing colors
        private static readonly (byte value, string token, string description)[] ControlByteTokens =
        {
            (0x80, "[black]", "Color 0 — black"),
            (0x81, "[blue]", "Color 1 — blue"),
            (0x82, "[green]", "Color 2 — green"),
            (0x83, "[cyan]", "Color 3 — cyan"),
            (0x84, "[red]", "Color 4 — red"),
            (0x85, "[magenta]", "Color 5 — magenta"),
            (0x86, "[brown]", "Color 6 — brown"),
            (0x87, "[grey]", "Color 7 — light grey"),
            (0x88, "[dkgrey]", "Color 8 — dark grey"),
            (0x89, "[ltblue]", "Color 9 — light blue"),
            (0x8A, "[ltgreen]", "Color 10 — light green"),
            (0x8B, "[ltcyan]", "Color 11 — light cyan"),
            (0x8C, "[ltred]", "Color 12 — light red"),
            (0x8D, "[ltmagenta]", "Color 13 — light magenta (player clothing color)"),
            (0x8E, "[yellow]", "Color 14 — yellow (enemy clothing color)"),
            (0x8F, "[white]", "Color 15 — white"),
        };

        public static int DecodeControlStringWithNullableTerminator(byte[] data, int offset, out string s)
        {
            var strLength = 0;
            while (data[offset + strLength] != 0) strLength++;
            s = DecodeControlString(data, offset, strLength);
            return strLength + 1; // include null terminator in offset
        }

        /// <summary>
        /// Decode a byte array containing control bytes (0x80+) into a string with
        /// human-readable tokens (e.g. [tab], [b]). Preserves all bytes faithfully
        /// for round-trip via EncodeControlString.
        /// </summary>
        public static string DecodeControlString(byte[] data, int offset, int length)
        {
            var sb = new StringBuilder();
            for (var i = offset; i < offset + length; i++)
            {
                var b = data[i];
                if (b == 0) break;
                if (b < 0x80)
                {
                    sb.Append((char)b);
                }
                else
                {
                    var found = false;
                    for (var t = 0; t < ControlByteTokens.Length; t++)
                    {
                        if (ControlByteTokens[t].value == b)
                        {
                            sb.Append(ControlByteTokens[t].token);
                            found = true;
                            break;
                        }
                    }

                    if (!found) sb.Append($"[0x{b:X2}]");
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Encode a string containing control tokens (e.g. [tab], [b], [0xAB]) back
        /// into a byte array. Inverse of DecodeControlString.
        /// </summary>
        public static byte[] EncodeControlString(string text)
        {
            var result = new List<byte>();
            var i = 0;
            while (i < text.Length)
            {
                if (text[i] == '[')
                {
                    var end = text.IndexOf(']', i);
                    if (end > i)
                    {
                        var token = text.Substring(i, end - i + 1);
                        var matched = false;
                        for (var t = 0; t < ControlByteTokens.Length; t++)
                        {
                            if (ControlByteTokens[t].token == token)
                            {
                                result.Add(ControlByteTokens[t].value);
                                matched = true;
                                break;
                            }
                        }
                        if (!matched && token.StartsWith("[0x") && token.Length == 6)
                        {
                            if (byte.TryParse(token.Substring(3, 2), System.Globalization.NumberStyles.HexNumber, null, out var val))
                            {
                                result.Add(val);
                                matched = true;
                            }
                        }
                        if (matched)
                        {
                            i = end + 1;
                            continue;
                        }
                    }
                }
                result.Add((byte)text[i]);
                i++;
            }
            return result.ToArray();
        }

        /// <summary>
        /// Parse a byte region as control-byte-aware null-terminated strings.
        /// Returns strings with control tokens and their original byte sizes.
        /// </summary>
        public static (string[] strings, int[] byteSizes) ControlStringsFromBytes(byte[] data, int offset, int length)
        {
            var strings = new List<string>();
            var sizes = new List<int>();
            var pos = offset;
            var end = offset + length;
            while (pos < end)
            {
                var strEnd = pos;
                while (strEnd < end && data[strEnd] != 0) strEnd++;
                strings.Add(DecodeControlString(data, pos, strEnd - pos));
                sizes.Add(strEnd - pos + 1);
                pos = strEnd + 1;
            }
            return (strings.ToArray(), sizes.ToArray());
        }

        /// <summary>
        /// Parse a fixed number of control-byte-aware null-terminated strings.
        /// </summary>
        public static (string[] strings, int[] byteSizes) ControlStringsFromBytes(byte[] data, int offset, int count, bool countBased)
        {
            var strings = new string[count];
            var sizes = new int[count];
            var pos = offset;
            for (var i = 0; i < count; i++)
            {
                var end = pos;
                while (end < data.Length && data[end] != 0) end++;
                strings[i] = DecodeControlString(data, pos, end - pos);
                sizes[i] = end - pos + 1;
                pos = end + 1;
            }
            return (strings, sizes);
        }

        /// <summary>
        /// Serialize control-byte-aware strings back to bytes with fixed slot sizes.
        /// </summary>
        public static byte[] ControlStringsToFixedBytes(string[] strings, int[] originalByteSizes)
        {
            var parts = new List<byte>();
            for (var i = 0; i < strings.Length; i++)
            {
                var encoded = EncodeControlString(strings[i]);
                var slotSize = i < originalByteSizes.Length ? originalByteSizes[i] : encoded.Length + 1;
                var slot = new byte[slotSize];
                Array.Copy(encoded, 0, slot, 0, Math.Min(encoded.Length, slotSize));
                parts.AddRange(slot);
            }
            return parts.ToArray();
        }

        #endregion
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
