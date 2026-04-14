using System;
using System.Linq;
using CovertActionTools.Core.Models.Executables.Records.Shared;

namespace CovertActionTools.Core.Models.Executables.Sections.Tac
{
    public class SignToCompassDirectionSection : IExecutableSection
    {
        private const int EntryCount = 9;
        private const int ByteSize = EntryCount * 2;

        public CompassDirection[] Directions { get; set; } = new CompassDirection[EntryCount];

        public bool Viewable()
        {
            return false;
        }

        public bool Editable()
        {
            return false;
        }

        public static int ToIndex(int signDx, int signDy)
        {
            return (signDx + 1) * 3 + (signDy + 1);
        }

        public int ReadBytes(byte[] fullPayload, int startingOffset)
        {
            Directions = new CompassDirection[EntryCount];
            var offset = startingOffset;
            for (var dx = -1; dx <= 1; dx++)
            {
                for (var dy = -1; dy <= 1; dy++)
                {
                    var value = BitConverter.ToUInt16(fullPayload, offset);
                    Directions[ToIndex(dx, dy)] = (CompassDirection)value;
                    offset += 2;
                }
            }
            return ByteSize;
        }

        public byte[] WriteBytes()
        {
            var result = new byte[ByteSize];
            var offset = 0;
            for (var dx = -1; dx <= 1; dx++)
            {
                for (var dy = -1; dy <= 1; dy++)
                {
                    var value = (ushort)Directions[ToIndex(dx, dy)];
                    result[offset] = (byte)(value & 0xFF);
                    result[offset + 1] = (byte)((value >> 8) & 0xFF);
                    offset += 2;
                }
            }
            return result;
        }

        public SignToCompassDirectionSection Clone()
        {
            return new SignToCompassDirectionSection
            {
                Directions = Directions.ToArray()
            };
        }
    }
}
