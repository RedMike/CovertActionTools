using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.Core.Compression
{
    internal class LzwDecompression
    {
        private const bool PrintDebugRawData = false;
        private const bool PrintDebugMergedPixels = false;
        private const bool PrintDebugRle = false;
        private const bool PrintDebugLzw = false;

        private const bool LogDebugLzwFirstDict = false;
        
        private static readonly string PrintDebugFolder = @"";
        private static readonly HashSet<string> PrintDebugKeys = new HashSet<string>() { };
        
        private readonly ILogger _logger;
        private readonly int _maxWordWidth;
        private readonly byte[] _data;

        private int _bitOffset;
        private int _byteOffset;
        
        private readonly Dictionary<ushort, List<byte>> _dict = new();
        private readonly Stack<byte> _stack = new();
        private byte _wordWidth;
        private int _wordMask;
        private ushort _prevIndex;
        private byte _prevData;

        private readonly string _key;

        private readonly List<byte> _rawBytes = new();
        private readonly List<byte> _mergedBytes = new();
        private readonly List<byte> _rleBytes = new();
        private readonly List<byte> _lzwBytes = new();

        public LzwDecompression(ILogger logger, int maxWordWidth, byte[] data, string key)
        {
            _logger = logger;
            _maxWordWidth = maxWordWidth;
            _data = data;
            _key = key;
            _logger.LogInformation($"Starting decompression from {data.Length} bytes, max word width {maxWordWidth}");

            _bitOffset = 0;
            _byteOffset = 0;
            
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
            ushort value = 0;
            byte bitsReadSoFar = 0;
            
            while (bitsReadSoFar != bitsToRead)
            {
                value = (ushort)(((short)value) >> 1); //we want arithmetic shift

                byte data = _data[_byteOffset];
                if ((data & (1 << _bitOffset)) != 0)
                {
                    value = (ushort)(value | 1 << (bitsToRead - 1));
                }

                _bitOffset += 1;

                if (_bitOffset == 8)
                {
                    _bitOffset = 0;
                    _byteOffset += 1;
                }

                bitsReadSoFar += 1;
            }
            
            if (PrintDebugLzw)
            {
                _lzwBytes.Add((byte)((value >> 8) & 0xFF));
                _lzwBytes.Add((byte)(value & 0xFF));
            }
            return value;
        }

        private byte ReadNext()
        {
            ushort index = 0;

            if (_stack.Any())
            {
                //we're queueing a word
                return _stack.Pop();
            }

            index = ReadBytes(_wordWidth);

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
            if (LogDebugLzwFirstDict && nextId < 0x105)
            {
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug($"Dict word {nextId:X4} at offset {_byteOffset} {_bitOffset}: {string.Join("", word.Select(x => $"{x:X2}"))}");
                }
            }

            _prevIndex = index;
            //if we've reached the limit of X bit indexes, increase by 1 bit
            if (nextId >= _wordMask)
            {
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug($"Increasing word width to {_wordWidth} at offset {_byteOffset} {_bitOffset}");
                }
                _wordWidth += 1;
                _wordMask <<= 1;
                _wordMask |= 1;
            }

            if (_wordWidth > _maxWordWidth)
            {
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug($"Resetting dictionary at offset {_byteOffset} {_bitOffset}");
                }
                Reset();
            }
            return ReadNext(); //recurse, we have something in the stack already
        }

        public byte[] Decompress(int length, out int byteOffset)
        {
            using var memStream = new MemoryStream();
            using var writer = new BinaryWriter(memStream);
            
            uint rleCount = 0;
            byte pixel = 0;

            for (var i = 0; i < length; i++)
            {
                if (rleCount > 0)
                {
                    rleCount--;
                }
                else
                {
                    var data = ReadNext();
                    if (PrintDebugRle)
                    {
                        _rleBytes.Add(data);
                    }

                    //is it RLE?
                    if (data != 0x90)
                    {
                        //no
                        pixel = data;
                    }
                    else
                    {
                        //yes, check how many times
                        var repeat = ReadNext();
                        if (PrintDebugRle)
                        {
                            _rleBytes.Add(repeat);
                        }

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

                //each byte is actually two pixels one after the other
                writer.Write((byte)(pixel & 0x0f));
                writer.Write((byte)((pixel >> 4) & 0x0f));
                if (PrintDebugMergedPixels)
                {
                    _mergedBytes.Add((byte)pixel);
                }
                if (PrintDebugRawData)
                {
                    _rawBytes.Add((byte)(pixel & 0x0f));
                    _rawBytes.Add((byte)((pixel >> 4) & 0x0f));
                }
                i += 1;
            }

            if (PrintDebugKeys.Contains(_key))
            {
                if (PrintDebugRawData)
                {
                    File.WriteAllBytes(Path.Combine(PrintDebugFolder, $"{_key}_decompress_raw.bin"), _rawBytes.ToArray());    
                }
                if (PrintDebugMergedPixels)
                {
                    File.WriteAllBytes(Path.Combine(PrintDebugFolder, $"{_key}_decompress_merged.bin"), _mergedBytes.ToArray());    
                }
                if (PrintDebugRle)
                {
                    File.WriteAllBytes(Path.Combine(PrintDebugFolder, $"{_key}_decompress_RLE.bin"), _rleBytes.ToArray());    
                }
                if (PrintDebugLzw)
                {
                    File.WriteAllBytes(Path.Combine(PrintDebugFolder, $"{_key}_decompress_LZW.bin"), _lzwBytes.ToArray());    
                }
            }
            
            var decompressedBytes = memStream.ToArray();
            byteOffset = _byteOffset + (_bitOffset > 1 ? 1 : 0);
            //_logger.LogError($"{_key} offset {_byteOffset} bits {_bitOffset}");
            _logger.LogInformation($"Decompressed from {_data.Length} ({byteOffset}) bytes to {decompressedBytes.Length}");
            return decompressedBytes;
        }
    }
}