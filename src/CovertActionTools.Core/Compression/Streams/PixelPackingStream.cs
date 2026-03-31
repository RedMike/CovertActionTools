using System;
using System.IO;

namespace CovertActionTools.Core.Compression.Streams
{
    internal class PixelPackingStream : Stream
    {
        private readonly Stream _inner;
        private readonly int _width;
        private readonly int _height;

        private int _y;
        private int _x;
        private int _stride;
        private bool _eof;

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

        public PixelPackingStream(Stream inner, int width, int height)
        {
            _inner = inner;
            _width = width;
            _height = height;
            _stride = ComputeStride(0);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            if (offset + count > buffer.Length) throw new ArgumentException("offset + count exceeds buffer length");

            var bytesWritten = 0;

            while (bytesWritten < count && !_eof)
            {
                if (_y >= _height)
                {
                    _eof = true;
                    break;
                }

                // Read first pixel
                var b1 = _inner.ReadByte();
                if (b1 < 0) { _eof = true; break; }
                var p1 = (byte)b1;
                _x++;

                // Read second pixel (or use 0 for padding)
                byte p2 = 0;
                if (_x < _width)
                {
                    var b2 = _inner.ReadByte();
                    if (b2 < 0) { _eof = true; break; }
                    p2 = (byte)b2;
                }
                _x++;

                if (p1 > 16 || p2 > 16)
                    throw new Exception($"Pixel value too high: {p1:X} {p2:X}");

                buffer[offset + bytesWritten] = (byte)(((p2 & 0x0F) << 4) | (p1 & 0x0F));
                bytesWritten++;

                // Advance row
                if (_x >= _stride)
                {
                    _y++;
                    _x = 0;
                    _stride = ComputeStride(_y);
                }
            }

            return bytesWritten;
        }

        private int ComputeStride(int y)
        {
            var stride = _width;
            if (y < _height - 1 && _width % 2 == 1)
                stride = _width + 1;
            return stride;
        }
    }
}
