using System;
using System.IO;

namespace CovertActionTools.Core.Compression.Streams
{
    internal class RleEncodingStream : ReadOnlyStream
    {
        private readonly Stream _inner;
        private readonly int _innerLength;

        private bool _started;
        private byte _lastPixel;
        private byte _repeats;
        private int _innerBytesRead;

        // Micro-buffer: one RLE emit can produce up to 6 bytes (repeats==2 with 0x90 values)
        private readonly byte[] _outBuf = new byte[8];
        private int _outPos;
        private int _outLen;

        private bool _flushed;

        public RleEncodingStream(Stream inner, int inputLength)
        {
            _inner = inner;
            _innerLength = inputLength;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var bytesWritten = 0;

            while (bytesWritten < count)
            {
                // 1. Drain micro-buffer
                if (_outPos < _outLen)
                {
                    var toCopy = Math.Min(count - bytesWritten, _outLen - _outPos);
                    Array.Copy(_outBuf, _outPos, buffer, offset + bytesWritten, toCopy);
                    _outPos += toCopy;
                    bytesWritten += toCopy;
                    continue;
                }

                // 2. If we've flushed everything, done
                if (_flushed)
                    break;

                // 3. Get next input byte
                var b = _inner.ReadByte();
                if (b < 0)
                {
                    // End of input — flush any accumulated repeat state
                    FlushRemainingRepeats();
                    _flushed = true;
                    continue;
                }
                var pixel = (byte)b;
                _innerBytesRead++;
                var isLastByte = _innerBytesRead >= _innerLength;

                // 4. Repeat tracking (mirrors original exactly)
                if (_started &&
                    pixel != 0x90 &&
                    pixel == _lastPixel &&
                    _repeats < 254 &&
                    !isLastByte)
                {
                    _repeats++;
                    continue;
                }

                // 5. Emit based on repeat count
                EmitRepeats(pixel, isLastByte);

                _started = true;
                _repeats = 0;
                _lastPixel = pixel;
            }

            return bytesWritten;
        }

        private void FlushRemainingRepeats()
        {
            // This is called when the inner stream is exhausted but we never
            // fell through the repeat-tracking condition (all bytes were identical
            // and we never hit the isLastByte=true path because EOF came after
            // tracking). This handles the edge case of a stream that's entirely
            // one repeated byte.
            if (!_started || _repeats == 0)
                return;

            // We need to emit the accumulated repeats. The "last pixel" is _lastPixel
            // and there's no new pixel breaking the run.
            _outPos = 0;
            _outLen = 0;

            if (_repeats == 1)
            {
                WriteBufByte(_lastPixel);
                if (_lastPixel == 0x90)
                    WriteBufByte(0);
            }
            else if (_repeats == 2)
            {
                WriteBufByte(_lastPixel);
                if (_lastPixel == 0x90)
                    WriteBufByte(0);
                WriteBufByte(_lastPixel);
                if (_lastPixel == 0x90)
                    WriteBufByte(0);
            }
            else
            {
                // repeats >= 3, end-of-image path: pixel == lastPixel
                WriteBufByte(0x90);
                WriteBufByte((byte)(_repeats + 1));
            }
        }

        private void EmitRepeats(byte pixel, bool isLastByte)
        {
            _outPos = 0;
            _outLen = 0;

            if (_repeats == 0)
            {
                WriteBufByte(pixel);
                if (pixel == 0x90)
                    WriteBufByte(0);
            }
            else if (_repeats == 1)
            {
                WriteBufByte(_lastPixel);
                if (_lastPixel == 0x90)
                    WriteBufByte(0);

                WriteBufByte(pixel);
                if (pixel == 0x90)
                    WriteBufByte(0);
            }
            else if (_repeats == 2)
            {
                WriteBufByte(_lastPixel);
                if (_lastPixel == 0x90)
                    WriteBufByte(0);
                WriteBufByte(_lastPixel);
                if (_lastPixel == 0x90)
                    WriteBufByte(0);

                WriteBufByte(pixel);
                if (pixel == 0x90)
                    WriteBufByte(0);
            }
            else
            {
                // repeats >= 3
                if (isLastByte && pixel == _lastPixel)
                {
                    // End-of-image: it's only an RLE code, not a second pixel
                    WriteBufByte(0x90);
                    WriteBufByte((byte)(_repeats + 2));
                }
                else
                {
                    WriteBufByte(0x90);
                    WriteBufByte((byte)(_repeats + 1));
                    WriteBufByte(pixel);
                    if (pixel == 0x90)
                        WriteBufByte(0);
                }
            }
        }

        private void WriteBufByte(byte b)
        {
            _outBuf[_outLen++] = b;
        }
    }
}
