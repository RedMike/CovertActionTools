using System;
using System.Linq;

namespace CovertActionTools.Core.Models.Executables.Records.Shared
{
    public class BlobRecord : IExecutableRecord
    {
        public int Size { get; }
        public byte[] Data { get; set; }

        public BlobRecord(int size)
        {
            Size = size;
            Data = new byte[Size];
        }

        public int ReadBytes(byte[] fullPayload, int startingOffset)
        {
            var offset = startingOffset;
            Array.Copy(fullPayload, offset, Data, 0, Size);
            offset += Size;
            return offset - startingOffset;
        }

        public byte[] WriteBytes()
        {
            return Data.ToArray();
        }

        public BlobRecord Clone()
        {
            var result = new BlobRecord(Size);
            Array.Copy(Data, result.Data, Size);
            return result;
        }
    }
}