using System;
using System.Collections.Generic;
using System.Linq;

namespace CovertActionTools.Core.Models.Executables.Records.Shared
{
    /// <summary>
    /// A menu string in the format "header\n option1\n option2\n", where options
    /// are delimited by newline-space (0x0A 0x20). The record reads a fixed byte
    /// slot and splits into a header string and a fixed number of option strings.
    /// </summary>
    public class MenuStringRecord : IExecutableRecord
    {
        public int? FixedSize { get; }
        public int OptionCount { get; }

        public string Header { get; set; } = "";
        public string[] Options { get; set; } = Array.Empty<string>();

        public MenuStringRecord(int? fixedSize, int optionCount)
        {
            FixedSize = fixedSize;
            OptionCount = optionCount;
            Options = new string[optionCount];
            for (var i = 0; i < optionCount; i++)
            {
                Options[i] = "";
            }
        }

        public int ReadBytes(byte[] fullPayload, int startingOffset)
        {
            var size = FixedSize ?? FindContentEnd(fullPayload, startingOffset);
            var raw = new byte[size];
            Array.Copy(fullPayload, startingOffset, raw, 0, size);

            var optionMarkers = FindOptionMarkers(raw);

            if (optionMarkers.Count < OptionCount)
            {
                Header = DataSegmentHelper.DecodeControlString(raw, 0, raw.Length);
                Options = new string[OptionCount];
                for (var i = 0; i < OptionCount; i++) Options[i] = "";
                return size;
            }

            var firstMarker = optionMarkers[optionMarkers.Count - OptionCount];
            Header = DataSegmentHelper.DecodeControlString(raw, 0, firstMarker);

            Options = new string[OptionCount];
            for (var i = 0; i < OptionCount; i++)
            {
                var markerIdx = optionMarkers.Count - OptionCount + i;
                var textStart = optionMarkers[markerIdx] + 2;
                int textEnd;
                if (i < OptionCount - 1)
                {
                    textEnd = optionMarkers[markerIdx + 1];
                }
                else
                {
                    textEnd = FindContentLength(raw, textStart);
                }

                Options[i] = DataSegmentHelper.DecodeControlString(raw, textStart, textEnd - textStart);
            }

            return size;
        }

        public byte[] WriteBytes()
        {
            var parts = new List<byte>();
            parts.AddRange(DataSegmentHelper.EncodeControlString(Header));
            for (var i = 0; i < Options.Length; i++)
            {
                parts.Add(0x0A);
                parts.Add(0x20);
                parts.AddRange(DataSegmentHelper.EncodeControlString(Options[i]));
            }
            parts.Add(0x0A);
            parts.Add(0x00);

            if (FixedSize.HasValue)
            {
                var result = new byte[FixedSize.Value];
                Array.Copy(parts.ToArray(), 0, result, 0, Math.Min(parts.Count, FixedSize.Value));
                return result;
            }

            return parts.ToArray();
        }

        public MenuStringRecord Clone()
        {
            var result = new MenuStringRecord(FixedSize, OptionCount)
            {
                Header = Header,
                Options = Options.ToArray()
            };
            return result;
        }

        private static List<int> FindOptionMarkers(byte[] data)
        {
            var markers = new List<int>();
            for (var i = 0; i < data.Length - 1; i++)
            {
                if (data[i] == 0x0A && data[i + 1] == 0x20)
                {
                    markers.Add(i);
                }
            }
            return markers;
        }

        private static int FindContentLength(byte[] data, int start)
        {
            var pos = start;
            while (pos < data.Length && data[pos] != 0x0A && data[pos] != 0x00)
            {
                pos++;
            }
            return pos;
        }

        private static int FindContentEnd(byte[] data, int startingOffset)
        {
            var pos = startingOffset;
            while (pos < data.Length && data[pos] != 0x00) pos++;
            return pos - startingOffset + 1;
        }
    }
}
