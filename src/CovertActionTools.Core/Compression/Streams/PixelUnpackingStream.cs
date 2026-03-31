using System;
using System.IO;

namespace CovertActionTools.Core.Compression.Streams
{
    internal class PixelUnpackingStream : Stream
    {
        private readonly Stream _inner;
        private readonly int _width;
        private readonly int _height;

        private int _y;
        private int _x;
        private int _stride;
        private bool _eof;

        // One packed byte produces up to 2 pixels
        private bool _hasSecondPixel;
        private byte _secondPixel;

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

        public PixelUnpackingStream(Stream inner, int width, int height)
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

                // Emit buffered second pixel from previous packed byte
                if (_hasSecondPixel)
                {
                    buffer[offset + bytesWritten] = _secondPixel;
                    bytesWritten++;
                    _hasSecondPixel = false;
                    _x++;

                    if (_x >= _stride)
                    {
                        _y++;
                        _x = 0;
                        _stride = ComputeStride(_y);
                    }
                    continue;
                }

                // Read next packed byte
                var b = _inner.ReadByte();
                if (b < 0) { _eof = true; break; }
                var pixel = (byte)b;

                // First pixel: low nibble
                buffer[offset + bytesWritten] = (byte)(pixel & 0x0F);
                bytesWritten++;
                _x++;

                // Second pixel: high nibble (skip if padding byte)
                if (_x < _width)
                {
                    _secondPixel = (byte)((pixel >> 4) & 0x0F);
                    _hasSecondPixel = true;
                }
                else
                {
                    // Consume padding position without output
                    _x++;
                }

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
