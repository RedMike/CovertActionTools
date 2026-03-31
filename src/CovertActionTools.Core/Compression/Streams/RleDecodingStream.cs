using System;
using System.IO;

namespace CovertActionTools.Core.Compression.Streams
{
    internal class RleDecodingStream : Stream
    {
        private readonly Stream _inner;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        private byte _pixel;
        private uint _rleCount;

        public RleDecodingStream(Stream inner)
        {
            _inner = inner;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            if (offset + count > buffer.Length) throw new ArgumentException("offset + count exceeds buffer length");

            var bytesWritten = 0;

            while (bytesWritten < count)
            {
                if (_rleCount > 0)
                {
                    buffer[offset + bytesWritten] = _pixel;
                    _rleCount--;
                    bytesWritten++;
                    continue;
                }

                var data = _inner.ReadByte();
                if (data < 0)
                    break;

                if ((byte)data != 0x90)
                {
                    _pixel = (byte)data;
                    buffer[offset + bytesWritten] = _pixel;
                    bytesWritten++;
                }
                else
                {
                    var repeat = _inner.ReadByte();
                    if (repeat < 0)
                        break;

                    if (repeat == 0)
                    {
                        // Literal 0x90
                        _pixel = 0x90;
                        buffer[offset + bytesWritten] = _pixel;
                        bytesWritten++;
                    }
                    else
                    {
                        if (repeat < 2)
                            throw new Exception($"Invalid RLE repeat byte: {repeat}");

                        // Current iteration emits one copy, rleCount covers the rest
                        _rleCount = (uint)(repeat - 2);
                        buffer[offset + bytesWritten] = _pixel;
                        bytesWritten++;
                    }
                }
            }

            return bytesWritten;
        }
    }
}
