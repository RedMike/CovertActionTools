using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CovertActionTools.Core.Compression.Streams
{
    internal class LzwDecompressingStream : ReadOnlyStream
    {
        private readonly int _maxWordWidth;
        private readonly BitReader _bitReader;

        private readonly Dictionary<ushort, List<byte>> _dict = new Dictionary<ushort, List<byte>>();
        private readonly Stack<byte> _stack = new Stack<byte>();
        private byte _wordWidth;
        private int _wordMask;
        private ushort _prevIndex;
        private byte _prevData;

        private bool _eof;

        public LzwDecompressingStream(Stream inner, int maxWordWidth)
        {
            _maxWordWidth = maxWordWidth;
            _bitReader = new BitReader(inner);
            Reset();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var bytesWritten = 0;

            while (bytesWritten < count && !_eof)
            {
                try
                {
                    buffer[offset + bytesWritten] = ReadNext();
                    bytesWritten++;
                }
                catch (EndOfStreamException)
                {
                    _eof = true;
                }
            }

            return bytesWritten;
        }

        private byte ReadNext()
        {
            if (_stack.Count > 0)
                return _stack.Pop();

            var index = _bitReader.ReadBits(_wordWidth);

            List<byte> existingWord;
            var nextId = GetDictNextId();
            if (index >= nextId)
            {
                index = nextId;
                _stack.Push(_prevData);
                existingWord = GetDict(_prevIndex);
            }
            else
            {
                existingWord = GetDict(index);
            }

            foreach (var b in existingWord)
            {
                _stack.Push(b);
            }

            _prevData = _stack.Peek();

            var word = GetDict(_prevIndex).ToList();
            word.Insert(0, _stack.Peek());

            SetDict(nextId, word);

            _prevIndex = index;

            if (nextId >= _wordMask)
            {
                _wordWidth += 1;
                _wordMask <<= 1;
                _wordMask |= 1;
            }

            if (_wordWidth > _maxWordWidth)
            {
                Reset();
            }

            return ReadNext();
        }

        private void Reset()
        {
            _prevIndex = 0;
            _prevData = 0;
            _wordWidth = 9;
            _wordMask = (1 << _wordWidth) - 1;
            _dict.Clear();
        }

        private List<byte> GetDict(int index)
        {
            if (index > 2048)
                throw new Exception($"Reading beyond dictionary limit: {index}");

            var pIndex = (ushort)(index & 0xFFFF);
            if (_dict.TryGetValue(pIndex, out var val))
                return val;

            if (pIndex > 0xFF)
                throw new Exception("Reading default value from beyond the defaults");

            val = new List<byte> { (byte)(index & 0xFF) };
            _dict[pIndex] = val;
            return val;
        }

        private void SetDict(int index, List<byte> bytes)
        {
            if (index > 2048)
                throw new Exception($"Writing beyond dictionary limit: {index}");

            var pIndex = (ushort)(index & 0xFFFF);
            _dict[pIndex] = bytes;
        }

        private ushort GetDictNextId()
        {
            return (ushort)(_dict.Keys.DefaultIfEmpty((ushort)0xFF).Max() + 1);
        }
    }
}
