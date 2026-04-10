using System;
using System.Collections.Generic;

namespace CovertActionTools.Core.Models.Executables.Records.Tac
{
    public class FullDirectionRecord : IExecutableRecord
    {
        public int StationaryDx { get; set; }
        public int StationaryDy { get; set; }
        public int NorthDx { get; set; }
        public int NorthDy { get; set; }
        public int NorthEastDx { get; set; }
        public int NorthEastDy { get; set; }
        public int EastDx { get; set; }
        public int EastDy { get; set; }
        public int SouthEastDx { get; set; }
        public int SouthEastDy { get; set; }
        public int SouthDx { get; set; }
        public int SouthDy { get; set; }
        public int SouthWestDx { get; set; }
        public int SouthWestDy { get; set; }
        public int WestDx { get; set; }
        public int WestDy { get; set; }
        public int NorthWestDx { get; set; }
        public int NorthWestDy { get; set; }
        
        public int ReadBytes(byte[] fullPayload, int startingOffset)
        {
            var offset = startingOffset;
            StationaryDx = BitConverter.ToInt16(fullPayload, offset);
            offset += 2;
            NorthDx = BitConverter.ToInt16(fullPayload, offset);
            offset += 2;
            NorthEastDx = BitConverter.ToInt16(fullPayload, offset);
            offset += 2;
            EastDx = BitConverter.ToInt16(fullPayload, offset);
            offset += 2;
            SouthEastDx = BitConverter.ToInt16(fullPayload, offset);
            offset += 2;
            SouthDx = BitConverter.ToInt16(fullPayload, offset);
            offset += 2;
            SouthWestDx = BitConverter.ToInt16(fullPayload, offset);
            offset += 2;
            WestDx = BitConverter.ToInt16(fullPayload, offset);
            offset += 2;
            NorthWestDx = BitConverter.ToInt16(fullPayload, offset);
            offset += 2;
            StationaryDy = BitConverter.ToInt16(fullPayload, offset);
            offset += 2;
            NorthDy = BitConverter.ToInt16(fullPayload, offset);
            offset += 2;
            NorthEastDy = BitConverter.ToInt16(fullPayload, offset);
            offset += 2;
            EastDy = BitConverter.ToInt16(fullPayload, offset);
            offset += 2;
            SouthEastDy = BitConverter.ToInt16(fullPayload, offset);
            offset += 2;
            SouthDy = BitConverter.ToInt16(fullPayload, offset);
            offset += 2;
            SouthWestDy = BitConverter.ToInt16(fullPayload, offset);
            offset += 2;
            WestDy = BitConverter.ToInt16(fullPayload, offset);
            offset += 2;
            NorthWestDy = BitConverter.ToInt16(fullPayload, offset);
            offset += 2;
            
            return offset - startingOffset;
        }

        public byte[] WriteBytes()
        {
            var result = new List<byte>();
            result.AddRange(BitConverter.GetBytes((short)StationaryDx));
            result.AddRange(BitConverter.GetBytes((short)NorthDx));
            result.AddRange(BitConverter.GetBytes((short)NorthEastDx));
            result.AddRange(BitConverter.GetBytes((short)EastDx));
            result.AddRange(BitConverter.GetBytes((short)SouthEastDx));
            result.AddRange(BitConverter.GetBytes((short)SouthDx));
            result.AddRange(BitConverter.GetBytes((short)SouthWestDx));
            result.AddRange(BitConverter.GetBytes((short)WestDx));
            result.AddRange(BitConverter.GetBytes((short)NorthWestDx));
            result.AddRange(BitConverter.GetBytes((short)StationaryDy));
            result.AddRange(BitConverter.GetBytes((short)NorthDy));
            result.AddRange(BitConverter.GetBytes((short)NorthEastDy));
            result.AddRange(BitConverter.GetBytes((short)EastDy));
            result.AddRange(BitConverter.GetBytes((short)SouthEastDy));
            result.AddRange(BitConverter.GetBytes((short)SouthDy));
            result.AddRange(BitConverter.GetBytes((short)SouthWestDy));
            result.AddRange(BitConverter.GetBytes((short)WestDy));
            result.AddRange(BitConverter.GetBytes((short)NorthWestDy));

            return result.ToArray();
        }

        public FullDirectionRecord Clone()
        {
            return new FullDirectionRecord()
            {
                StationaryDx = StationaryDx,
                StationaryDy = StationaryDy,
                NorthDx = NorthDx,
                NorthDy = NorthDy,
                NorthEastDx = NorthEastDx,
                NorthEastDy = NorthEastDy,
                EastDx = EastDx,
                EastDy = EastDy,
                SouthEastDx = SouthEastDx,
                SouthEastDy = SouthEastDy,
                SouthDx = SouthDx,
                SouthDy = SouthDy,
                SouthWestDx = SouthWestDx,
                SouthWestDy = SouthWestDy,
                WestDx = WestDx,
                WestDy = WestDy,
                NorthWestDx = NorthWestDx,
                NorthWestDy = NorthWestDy
            };
        }
    }
}