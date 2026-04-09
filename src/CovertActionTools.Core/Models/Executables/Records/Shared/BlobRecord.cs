using System;
using System.Linq;

namespace CovertActionTools.Core.Models.Executables.Records.Shared
{
    public class BlobRecord : IExecutableRecord
    {
        private readonly int _size;
        public byte[] Data { get; set; }

        public BlobRecord(int size)
        {
            _size = size;
            Data = new byte[_size];
        }

        public int ReadBytes(byte[] fullPayload, int startingOffset)
        {
            var offset = startingOffset;
            Array.Copy(fullPayload, offset, Data, 0, _size);
            offset += _size;
            return offset - startingOffset;
        }

        public byte[] WriteBytes()
        {
            return Data.ToArray();
        }

        public BlobRecord Clone()
        {
            var result = new BlobRecord(_size);
            Array.Copy(Data, result.Data, _size);
            return result;
        }
    }
}