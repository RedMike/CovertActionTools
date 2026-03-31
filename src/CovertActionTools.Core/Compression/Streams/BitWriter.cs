using System;

namespace CovertActionTools.Core.Compression.Streams
{
    internal class BitWriter
    {
        private byte[] _buffer;
        private int _writeHead;
        private int _readHead;
        private int _partial;
        private int _bitOffset;
        private bool _flushed;

        public BitWriter(int initialCapacity = 256)
        {
            _buffer = new byte[initialCapacity];
        }

        public int AvailableBytes => _writeHead - _readHead;

        public void WriteBits(int data, byte bitCount)
        {
            _partial |= data << _bitOffset;
            _bitOffset += bitCount;
            while (_bitOffset >= 8)
            {
                EnsureCapacity();
                _buffer[_writeHead++] = (byte)(_partial & 0xFF);
                _partial >>= 8;
                _bitOffset -= 8;
            }
        }

        public void FlushPartial()
        {
            if (_flushed)
                return;
            if (_bitOffset > 0)
            {
                EnsureCapacity();
                _buffer[_writeHead++] = (byte)(_partial & 0xFF);
                _partial = 0;
                _bitOffset = 0;
            }
            _flushed = true;
        }

        public int CopyTo(byte[] destination, int offset, int count)
        {
            var available = AvailableBytes;
            var toCopy = Math.Min(count, available);
            if (toCopy > 0)
            {
                Array.Copy(_buffer, _readHead, destination, offset, toCopy);
                _readHead += toCopy;

                // Compact buffer when fully drained
                if (_readHead == _writeHead)
                {
                    _readHead = 0;
                    _writeHead = 0;
                }
            }
            return toCopy;
        }

        private void EnsureCapacity()
        {
            if (_writeHead < _buffer.Length)
                return;

            // Compact first
            if (_readHead > 0)
            {
                var remaining = _writeHead - _readHead;
                Array.Copy(_buffer, _readHead, _buffer, 0, remaining);
                _writeHead = remaining;
                _readHead = 0;
                return;
            }

            // Grow
            var newBuffer = new byte[_buffer.Length * 2];
            Array.Copy(_buffer, 0, newBuffer, 0, _writeHead);
            _buffer = newBuffer;
        }
    }
}
