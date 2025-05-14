using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.Core.Compression
{
    internal class LzwDecompressor
    {
        private readonly ILogger _logger;
        private readonly int _maxWordWidth;
        private readonly BinaryReader _reader;
        
        private byte _bitOffset;
        private int _byteOffset;
        private byte? _curByte = null;
        
        private readonly Dictionary<ushort, List<byte>> _dict = new();
        private readonly Stack<byte> _stack = new();
        private byte _wordWidth;
        private int _wordMask;
        private ushort _prevIndex;
        private byte _prevData;

        public LzwDecompressor(ILogger logger, int maxWordWidth, BinaryReader reader)
        {
            _logger = logger;
            _maxWordWidth = maxWordWidth;
            _reader = reader;
            Reset();
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
        
        private ushort ReadBytes(byte bitsToRead)
        {
            ushort value = 0;
            byte bitsReadSoFar = 0;
            
            while (bitsReadSoFar != bitsToRead)
            {
                value = (ushort)(((short)value) >> 1); //we want arithmetic shift

                if (_curByte == null)
                {
                    _curByte = _reader.ReadByte();
                }
                byte data = _curByte.Value;
                if ((data & (1 << _bitOffset)) != 0)
                {
                    value = (ushort)(value | 1 << (bitsToRead - 1));
                }

                _bitOffset += 1;

                if (_bitOffset == 8)
                {
                    _bitOffset = 0;
                    _byteOffset += 1;
                    _curByte = null;
                }

                bitsReadSoFar += 1;
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
        
        public byte[] Decompress(int width, int height)
        {
            using var memStream = new MemoryStream();
            using var writer = new BinaryWriter(memStream);
            
            uint rleCount = 0;
            byte pixel = 0;

            for (var y = 0; y < height; y++)
            {
                var stride = width;
                //each byte has 2 pixels, if the width is -1 we have to append a fake pixel to keep it on the same line
                //but not last line because that can just end suddenly
                if (y < height - 1 && width % 2 == 1)
                {
                    stride = width + 1;
                }
                for (var x = 0; x < stride; x++)
                {
                    if (rleCount > 0)
                    {
                        rleCount--;
                    }
                    else
                    {
                        var data = ReadNext();

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

                            if (repeat == 0)
                            {
                                //we're just encoding 0x90
                                pixel = 0x90;
                            }
                            else
                            {
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
                    x++;
                    //but if it's the padding byte to keep the stride, we don't want to actually add it to the data
                    if (x < width)
                    {
                        writer.Write((byte)((pixel >> 4) & 0x0f));
                    }
                }
            }
           
            var decompressedBytes = memStream.ToArray();
            return decompressedBytes;
        }
    }

    public interface ILzwDecompression
    {
        byte[] Decompress(int width, int height, int maxWordWidth, BinaryReader reader);
    }
    
    internal class LzwDecompression : ILzwDecompression
    {
        private readonly ILogger _logger;

        public LzwDecompression(ILogger<LzwDecompression> logger)
        {
            _logger = logger;
        }

        public byte[] Decompress(int width, int height, int maxWordWidth, BinaryReader reader)
        {
            var decompressor = new LzwDecompressor(_logger, maxWordWidth, reader);
            return decompressor.Decompress(width, height);
        }
    }
}