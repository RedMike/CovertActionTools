using System;

namespace CovertActionTools.Core.Models.Executables.Records.Tac
{
    public class TacScreenRectRecord : IExecutableRecord
    {
        public const int RecordSize = 8;

        public ushort X1 { get; set; }
        public ushort Y1 { get; set; }
        public ushort X2 { get; set; }
        public ushort Y2 { get; set; }

        public int ReadBytes(byte[] fullPayload, int startingOffset)
        {
            X1 = BitConverter.ToUInt16(fullPayload, startingOffset);
            Y1 = BitConverter.ToUInt16(fullPayload, startingOffset + 2);
            X2 = BitConverter.ToUInt16(fullPayload, startingOffset + 4);
            Y2 = BitConverter.ToUInt16(fullPayload, startingOffset + 6);
            return RecordSize;
        }

        public byte[] WriteBytes()
        {
            var result = new byte[RecordSize];
            result[0] = (byte)(X1 & 0xFF); result[1] = (byte)((X1 >> 8) & 0xFF);
            result[2] = (byte)(Y1 & 0xFF); result[3] = (byte)((Y1 >> 8) & 0xFF);
            result[4] = (byte)(X2 & 0xFF); result[5] = (byte)((X2 >> 8) & 0xFF);
            result[6] = (byte)(Y2 & 0xFF); result[7] = (byte)((Y2 >> 8) & 0xFF);
            return result;
        }

        public TacScreenRectRecord Clone()
        {
            return new TacScreenRectRecord { X1 = X1, Y1 = Y1, X2 = X2, Y2 = Y2 };
        }
    }
}
