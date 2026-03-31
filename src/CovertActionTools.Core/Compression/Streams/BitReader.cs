using System;
using System.IO;

namespace CovertActionTools.Core.Compression.Streams
{
    internal class BitReader
    {
        private readonly Stream _source;
        private byte _bitOffset;
        private int _curByte = -1;

        public BitReader(Stream source)
        {
            _source = source;
        }

        public ushort ReadBits(byte bitsToRead)
        {
            ushort value = 0;
            byte bitsReadSoFar = 0;

            while (bitsReadSoFar != bitsToRead)
            {
                value = (ushort)(((short)value) >> 1); // arithmetic shift

                if (_curByte < 0)
                {
                    _curByte = _source.ReadByte();
                    if (_curByte < 0)
                        throw new EndOfStreamException();
                }

                if ((_curByte & (1 << _bitOffset)) != 0)
                {
                    value = (ushort)(value | (1 << (bitsToRead - 1)));
                }

                _bitOffset += 1;

                if (_bitOffset == 8)
                {
                    _bitOffset = 0;
                    _curByte = -1;
                }

                bitsReadSoFar += 1;
            }

            return value;
        }
    }
}
