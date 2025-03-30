using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.Core.Compression
{
    internal class LzwDecompression
    {
        private readonly ILogger _logger;
        private readonly int _maxWordWidth;
        private readonly byte[] _data;

        private int _offsetInBits;
        private int _offsetInBytes;
        
        private readonly Dictionary<ushort, List<byte>> _dict = new();
        private readonly Stack<byte> _stack = new();
        private byte _wordWidth;
        private int _wordMask;
        private ushort _prevIndex;
        private byte _prevData;
        

        public LzwDecompression(ILogger logger, int maxWordWidth, byte[] data)
        {
            _logger = logger;
            _maxWordWidth = maxWordWidth;
            _data = data;
            _logger.LogInformation($"Starting decompression from {data.Length} bytes, max word width {maxWordWidth}");

            _offsetInBits = 0;
            _offsetInBytes = 0;
            
            _stack.Clear();
            Reset();
        }

        private List<byte> GetDict(int index)
        {
            if (index > 2048)
            {
                throw new Exception($"Reading beyond dictionary limit: {index}");
            }
            
            var pIndex = (ushort)(index & 0xFFFF);
            if (_dict.TryGetValue(pIndex, out var val))
            {
                return val;
            }

            if (pIndex > 0xFF)
            {
                throw new Exception("Reading default value from beyond the defaults");
            }
            val = new List<byte>() { (byte)(index & 0xFF) };
            _dict[pIndex] = val;
            return val;
        }

        private void SetDict(int index, List<byte> bytes)
        {
            if (index > 2048)
            {
                throw new Exception($"Writing beyond dictionary limit: {index}");
            }
            var pIndex = (ushort)(index & 0xFFFF);
            _dict[pIndex] = bytes;
        }

        private ushort GetDictNextId()
        {
            return (ushort)(_dict.Keys.DefaultIfEmpty((ushort)0xFF).Max() + 1);
        }

        private void Reset()
        {
            _prevIndex = 0;
            _prevData = 0;
            _wordWidth = 9;
            _wordMask = (1 << _wordWidth) - 1;
            _dict.Clear();
        }

        private ushort ReadBytes(byte bitsToRead)
        {
            var origOffset = _offsetInBytes;
            ushort value = 0;
            byte bitsReadSoFar = 0;
            
            while (bitsReadSoFar != bitsToRead)
            {
                value = (ushort)(((short)value) >> 1); //we want arithmetic shift

                if (_offsetInBytes >= _data.Length || _offsetInBytes < 0)
                {
                    //TODO: why is this triggering?
                    //break;
                }
                byte data = _data[_offsetInBytes];
                if ((data & (1 << _offsetInBits)) != 0)
                {
                    value = (ushort)(value | 1 << (bitsToRead - 1));
                }

                _offsetInBits += 1;

                if (_offsetInBits == 8)
                {
                    _offsetInBits = 0;
                    _offsetInBytes += 1;
                }

                bitsReadSoFar += 1;
            }

            return value;
        }

        private List<byte> _testBytes = new();
        private int _wordCreations = 0;
        private int _dictResets = 0;

        private byte ReadNext()
        {
            ushort index = 0;

            if (_stack.Any())
            {
                //we're queueing a word
                return _stack.Pop();
            }

            index = ReadBytes(_wordWidth);
            //to test LZW without bit-shift
            _testBytes.Add((byte)((index >> 8) & 0xFF));
            _testBytes.Add((byte)(index & 0xFF));
            if (_offsetInBytes == 229)
            {
                _logger.LogError($"next: {GetDictNextId():X4} {string.Join(" ", GetDict(index).Select(x => $"{x:X2}"))} = {index:X4}");
                for (ushort i = (ushort)(GetDictNextId() - 10); i < GetDictNextId(); i++)
                {
                    _logger.LogError($"{i:X4}: {string.Join(" ", GetDict(i).Select(x => $"{x:X2}"))}");
                }
                _logger.LogError($"5555: {_dict.FirstOrDefault(x => x.Value.Count == 2 && x.Value[0] == 0x55 && x.Value[1] == 0x55).Key:X4}");
                // _logger.LogError($"next2: {string.Join(" ", GetDict(0x123).Select(x => $"{x:X2}"))} = {0x123:X4}");
                _logger.LogError($"last: {string.Join(" ", _testBytes.Skip(_testBytes.Count - 8).Select(x => $"{x:X2}"))}");
                _logger.LogError($"0101: {string.Join(" ", GetDict(0x101).Select(x => $"{x:X2}"))}");
                
            }

            List<byte> existingWord;
            var nextId = GetDictNextId();
            if (index >= nextId)
            {
                index = nextId;
                _stack.Push(_prevData);
                existingWord = GetDict(_prevIndex); //it's a new index, so load the previous word
            }
            else
            {
                existingWord = GetDict(index); //it's an existing index, so load the current word
            }
            //now push the previous/current word
            foreach (var b in existingWord)
            {
                _stack.Push(b);
            }

            _prevData = _stack.Peek();
            
            //and make a new word of the previous word plus the first character of the current word (or previous if new index)
            var word = GetDict(_prevIndex).ToList();
            word.Insert(0, _stack.Peek());
            SetDict(nextId, word);
            if (word.Count == 2 && word[0] == 0x55 && word[1] == 0x55)
            {
                _logger.LogError($"!: {_stack.Peek():X2} {nextId:X4}");
            }
            _wordCreations += 1;
            //_logger.LogError($"new word {nextId:X4}: {string.Join(" ", word.Select(x => $"{x:X2}"))}");
            //to test word creation
            // _testBytes.Add((byte)((nextId >> 8) & 0xFF));
            // _testBytes.Add((byte)(nextId & 0xFF));

            _prevIndex = index;
            
            // if (nextId < 260 && _wordWidth == 9)
            // {
            //     // if (nextId == 258)
            //     // {
            //     //     _logger.LogError($"256: word: {string.Join(" ", GetDict(0x100).Select(x => $"{x:X2}"))}");
            //     //     _logger.LogError($"257: word: {string.Join(" ", GetDict(0x101).Select(x => $"{x:X2}"))}");
            //     //     _logger.LogError($"258: word: {string.Join(" ", GetDict(0x102).Select(x => $"{x:X2}"))}");
            //     // }
            //
            //     _logger.LogError($"Reached ID {nextId} at offset: {_offsetInBytes} {_offsetInBits}; word: {string.Join(" ", word.Select(x => $"{x:X2}"))}");
            // }
            //if we've reached the limit of X bit indexes, increase by 1 bit
            if (nextId >= _wordMask)
            {
                _logger.LogError($"Reached ID {nextId} at offset: {_offsetInBytes} {_offsetInBits}, bumping word width to {_wordWidth + 1}");
                _wordWidth += 1;
                _wordMask <<= 1;
                _wordMask |= 1;
                // _testBytes.Add(0xDE);
                // _testBytes.Add(0xAD);
                // _testBytes.Add(0xBE);
                // _testBytes.Add(0xEF);
            }

            if (_wordWidth > _maxWordWidth)
            {
                _logger.LogError($"Resetting width {_wordWidth} at offset: {_offsetInBytes} {_offsetInBits} next ID: {nextId:X4} prev index: {_prevIndex:X4} prev word: {string.Join(" ", word.Select(x => $"{x:X2}"))}");
                // _testBytes.Add(0xDE);
                // _testBytes.Add(0xAD);
                // _testBytes.Add(0xBE);
                // _testBytes.Add(0xEF);
                _dictResets += 1;
                Reset();
            }
            return ReadNext(); //recurse, we have something in the stack already
        }

        public byte[] Decompress(int length)
        {
            using var memStream = new MemoryStream();
            using var writer = new BinaryWriter(memStream);
            
            uint rleCount = 0;
            byte pixel = 0;

            var testBytes2 = new List<byte>();
            for (var i = 0; i < length; i++)
            {
                if (rleCount > 0)
                {
                    rleCount--;
                }
                else
                {
                    var data = ReadNext();
                    //to test RLE
                    testBytes2.Add(data);
                    
                    //is it RLE?
                    if (data != 0x90)
                    {
                        //no
                        //_logger.LogInformation($"Non-RLE encoded bytes {pixel:X2}");
                        pixel = data;
                    }
                    else
                    {
                        //yes, check how many times
                        var repeat = ReadNext();
                        //to test RLE
                        testBytes2.Add(repeat);

                        if (repeat == 0)
                        {
                            //we're just encoding 0x90
                            pixel = 0x90;
                        }
                        else
                        {
                            //_logger.LogInformation($"RLE encoded of bytes {pixel:X2} for {repeat}");
                            if (repeat < 2)
                            {
                                throw new Exception($"Invalid RLE repeat byte: {repeat}");
                            }
                            rleCount = (uint)(repeat - 2);
                        }
                    }
                }

                //to test pixel splitting
                //testBytes.Add(pixel);

                //each byte is actually two pixels one after the other
                writer.Write((byte)(pixel & 0x0f));
                writer.Write((byte)((pixel >> 4) & 0x0f));
                i += 1;
            }
            
            //to test pixel splitting/RLE
            //File.WriteAllBytes(@"", testBytes2.ToArray());
            //testBytes2.ToArray().LogDebugFirstBytes(_logger, 16, 10, 300);
            //to test LZW without bit-shift
            _testBytes.ToArray().LogDebugFirstBytes(_logger, 16, 10, 402);
            _logger.LogError($"words {_wordCreations} resets {_dictResets}");

            var decompressedBytes = memStream.ToArray();
            _logger.LogInformation($"Decompressed from {_data.Length} bytes to {decompressedBytes.Length}");
            return decompressedBytes;
        }
    }
}