using System;
using System.IO;

namespace CovertActionTools.Core.Compression
{
    internal class LzwDecompression : IDisposable
    {
        private readonly int _maxWordWidth;
        private BinaryReader _reader;

        private readonly MemoryStream _memStream;
        private readonly BinaryWriter _writer;

        public LzwDecompression(int maxWordWidth, BinaryReader reader)
        {
            _maxWordWidth = maxWordWidth;
            _reader = reader;
            
            _memStream = new MemoryStream();
            _writer = new BinaryWriter(_memStream);
        }

        public byte[] Decompress()
        {
            return _memStream.ToArray();
        }

        public void Dispose()
        {
            _memStream.Dispose();
            _writer.Dispose();
        }
    }
}