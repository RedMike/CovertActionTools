using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CovertActionTools.Core.Compression.Streams
{
    internal class LzwCompressingStream : Stream
    {
        private readonly Stream _inner;
        private readonly int _maxWordWidth;
        private readonly BitWriter _bitWriter = new BitWriter();

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

        private readonly Dictionary<string, ushort> _dict = new Dictionary<string, ushort>();
        private byte _wordWidth;
        private int _wordMask;
        private List<byte> _currentWord = new List<byte>();

        private bool _firstAfterReset;
        private bool _inputExhausted;
        private bool _finalFlushed;

        private bool _hasPendingByte;
        private byte _pendingByte;

        public LzwCompressingStream(Stream inner, int maxWordWidth)
        {
            _inner = inner;
            _maxWordWidth = maxWordWidth;
            Reset();
            _firstAfterReset = true;
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
                // 1. Drain BitWriter output
                if (_bitWriter.AvailableBytes > 0)
                {
                    bytesWritten += _bitWriter.CopyTo(buffer, offset + bytesWritten, count - bytesWritten);
                    continue;
                }

                // 2. If we've flushed everything, done
                if (_finalFlushed)
                    break;

                // 3. If input exhausted, write final code
                if (_inputExhausted)
                {
                    var finalIndex = TryGetDict(_currentWord);
                    if (finalIndex != null)
                    {
                        _bitWriter.WriteBits(finalIndex.Value, _wordWidth);
                    }
                    _bitWriter.FlushPartial();
                    _finalFlushed = true;
                    continue;
                }

                // 4. Read next byte
                byte next;
                if (_hasPendingByte)
                {
                    next = _pendingByte;
                    _hasPendingByte = false;
                }
                else
                {
                    var b = _inner.ReadByte();
                    if (b < 0)
                    {
                        _inputExhausted = true;
                        continue;
                    }
                    next = (byte)b;
                }

                // 5. LZW algorithm (mirrors original exactly)
                if (_firstAfterReset)
                {
                    var id = GetDictNextId();
                    SetDict(new List<byte> { 0, next }, id);
                    _firstAfterReset = false;
                }

                var nextId = GetDictNextId();
                var potentialNextWord = new List<byte>(_currentWord);
                potentialNextWord.Add(next);

                var index = TryGetDict(potentialNextWord);
                if (index != null)
                {
                    _currentWord = potentialNextWord;
                }
                else
                {
                    SetDict(potentialNextWord, nextId);

                    var lastIndex = TryGetDict(_currentWord);
                    if (lastIndex == null)
                    {
                        throw new Exception(
                            $"Last index missing: {string.Join("", _currentWord.Select(x => $"{x:X2}"))}");
                    }

                    _bitWriter.WriteBits(lastIndex.Value, _wordWidth);

                    _currentWord = new List<byte> { next };
                    if (nextId > _wordMask)
                    {
                        _wordWidth += 1;
                        _wordMask <<= 1;
                        _wordMask |= 1;
                    }
                    if (_wordWidth > _maxWordWidth)
                    {
                        Reset();
                        _firstAfterReset = true;
                        _currentWord = new List<byte>();
                        // Re-process this byte after reset (replaces Seek(-1) in original)
                        _pendingByte = next;
                        _hasPendingByte = true;
                    }
                }
            }

            return bytesWritten;
        }

        private void Reset()
        {
            _wordWidth = 9;
            _wordMask = (1 << _wordWidth) - 1;
            _dict.Clear();
            for (ushort i = 0; i < 0x100; i++)
            {
                var b = (byte)i;
                _dict[$"{b:X2}"] = i;
            }
            _currentWord.Clear();
        }

        private ushort? TryGetDict(List<byte> word)
        {
            var s = string.Join("", word.Select(x => $"{x:X2}"));
            if (!_dict.TryGetValue(s, out var index))
                return null;
            // Bug in their implementation: 0x100 is never actually used
            if (index == 0x100)
                return null;
            return index;
        }

        private void SetDict(List<byte> word, ushort index)
        {
            if (index > 2048)
                throw new Exception($"Writing beyond dictionary limit: {index}");

            var s = string.Join("", word.Select(x => $"{x:X2}"));
            // Bug in their implementation: 0x100 is never actually used
            if (_dict.TryGetValue(s, out var potentialIndex) && potentialIndex != 0x100)
                throw new Exception($"Found duplicate value for {s}");
            _dict[s] = index;
        }

        private ushort GetDictNextId()
        {
            return (ushort)(_dict.Values.DefaultIfEmpty((ushort)0xFF).Max() + 1);
        }
    }
}
