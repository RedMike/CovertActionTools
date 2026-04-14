using System;

namespace CovertActionTools.Core.Models.Executables.Records.Tac
{
    public class TacScreenCoordinateRecord : IExecutableRecord
    {
        public const int RecordSize = 4;

        public ushort X { get; set; }
        public ushort Y { get; set; }

        public int ReadBytes(byte[] fullPayload, int startingOffset)
        {
            X = BitConverter.ToUInt16(fullPayload, startingOffset);
            Y = BitConverter.ToUInt16(fullPayload, startingOffset + 2);
            return RecordSize;
        }

        public byte[] WriteBytes()
        {
            var result = new byte[RecordSize];
            result[0] = (byte)(X & 0xFF);
            result[1] = (byte)((X >> 8) & 0xFF);
            result[2] = (byte)(Y & 0xFF);
            result[3] = (byte)((Y >> 8) & 0xFF);
            return result;
        }

        public TacScreenCoordinateRecord Clone()
        {
            return new TacScreenCoordinateRecord { X = X, Y = Y };
        }
    }
}
