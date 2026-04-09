using System;
using System.Collections.Generic;

namespace CovertActionTools.Core.Models.Executables.Records.Tac
{
    public class CardinalDirectionRecord : IExecutableRecord
    {
        public int NorthDx { get; set; }
        public int NorthDy { get; set; }
        public int EastDx { get; set; }
        public int EastDy { get; set; }
        public int SouthDx { get; set; }
        public int SouthDy { get; set; }
        public int WestDx { get; set; }
        public int WestDy { get; set; }
        
        public int ReadBytes(byte[] fullPayload, int startingOffset)
        {
            var offset = startingOffset;
            NorthDx = BitConverter.ToInt16(fullPayload, offset);
            offset += 2;
            EastDx = BitConverter.ToInt16(fullPayload, offset);
            offset += 2;
            SouthDx = BitConverter.ToInt16(fullPayload, offset);
            offset += 2;
            WestDx = BitConverter.ToInt16(fullPayload, offset);
            offset += 2;
            var tmp = BitConverter.ToUInt16(fullPayload, offset);
            if (tmp != 0)
            {
                throw new Exception($"Expecting to find 0 but found real value: {tmp:X4} at offset {offset:X4}");
            }
            offset += 2;
            NorthDy = BitConverter.ToInt16(fullPayload, offset);
            offset += 2;
            EastDy = BitConverter.ToInt16(fullPayload, offset);
            offset += 2;
            SouthDy = BitConverter.ToInt16(fullPayload, offset);
            offset += 2;
            WestDy = BitConverter.ToInt16(fullPayload, offset);
            offset += 2;
            
            return offset - startingOffset;
        }

        public byte[] WriteBytes()
        {
            var result = new List<byte>();
            result.AddRange(BitConverter.GetBytes((short)NorthDx));
            result.AddRange(BitConverter.GetBytes((short)EastDx));
            result.AddRange(BitConverter.GetBytes((short)SouthDx));
            result.AddRange(BitConverter.GetBytes((short)WestDx));
            result.AddRange(BitConverter.GetBytes((short)0));
            result.AddRange(BitConverter.GetBytes((short)NorthDy));
            result.AddRange(BitConverter.GetBytes((short)EastDy));
            result.AddRange(BitConverter.GetBytes((short)SouthDy));
            result.AddRange(BitConverter.GetBytes((short)WestDy));
            return result.ToArray();
        }
        
        public CardinalDirectionRecord Clone()
        {
            return new CardinalDirectionRecord
            {
                NorthDx = NorthDx,
                NorthDy = NorthDy,
                EastDx = EastDx,
                EastDy = EastDy,
                SouthDx = SouthDx,
                SouthDy = SouthDy,
                WestDx = WestDx,
                WestDy = WestDy
            };
        }
    }
}