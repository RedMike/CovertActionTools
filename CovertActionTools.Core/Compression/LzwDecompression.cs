using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.Core.Compression
{
    //TODO: rework this to be more readable
    internal class LzwDecompression
    {
        private readonly ILogger _logger;
        private readonly int _maxWordWidth;
        private readonly byte[] _data;

        private int _offsetInBits;
        private int _offsetInBytes;
        
        private readonly Dictionary<ushort, (byte data, ushort next)> _dict = new();
        private ushort[] _stack = new ushort[1024];
        private int _stackTop;
        private int _wordWidth;
        private int _wordMask;
        private int _dictTop;
        private int _prevIndex;
        private int _prevData;
        

        public LzwDecompression(ILogger logger, int maxWordWidth, byte[] data)
        {
            _logger = logger;
            _maxWordWidth = maxWordWidth;
            _data = data;
            _logger.LogInformation($"Starting decompression from {data.Length} bytes, max word width {maxWordWidth}");

            _offsetInBits = 0;
            _offsetInBytes = 0;
            
            _stackTop = 0;
            Reset();
        }

        private (byte data, ushort next) GetDict(int index)
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

            val = ((byte)(index & 0xFF), 0xFFFF);
            _dict[pIndex] = val;
            return val;
        }

        private void SetDict(int index, byte data, ushort next)
        {
            if (index > 2048)
            {
                throw new Exception($"Writing beyond dictionary limit: {index}");
            }
            var pIndex = (ushort)(index & 0xFFFF);
            _dict[pIndex] = (data, next);
        }

        private void Reset()
        {
            //_logger.LogInformation($"Triggering reset from {_prevIndex} {_wordWidth} {_wordMask} {_dictTop}");
            _prevIndex = 0;
            _prevData = 0;
            _wordWidth = 9;
            _wordMask = (1 << _wordWidth) - 1;
            _dictTop = 0x100;
            _dict.Clear();
        }

        private int ReadBytes(int bitsToRead)
        {
            int value = 0;
            int bitsReadSoFar = 0;
            
            while (bitsReadSoFar != bitsToRead)
            {
                value >>= 1;

                if (_offsetInBytes >= _data.Length || _offsetInBytes < 0)
                {
                    //TODO: why is this triggering?
                    break;
                }
                byte data = _data[_offsetInBytes];
                if ((data & (1 << _offsetInBits)) != 0)
                {
                    value |= 1 << (bitsToRead - 1);
                }

                _offsetInBits += 1;

                if (_offsetInBits == 8)
                {
                    _offsetInBits = 0;
                    _offsetInBytes += 1;
                }

                bitsReadSoFar += 1;
            }

            //_logger.LogInformation($"ReadBytes({bitsToRead}) read {value:b8}");
            return value;
        }

        private byte ReadNext()
        {
            int tempIndex = 0;
            int index = 0;

            if (_stackTop == 0)
            {
                tempIndex = index = ReadBytes(_wordWidth);

                if (index >= _dictTop)
                {
                    //_logger.LogInformation($"Adding new entry to dict for index {_dictTop} starting with index {_prevIndex}");
                    tempIndex = _dictTop;
                    index = _prevIndex;
                    _stack[_stackTop] = (ushort)(_prevData & 0xFFFF);
                    _stackTop += 1;
                }

                var preStoredValues = "";
                while (GetDict(index).next != 0xFFFF)
                {
                    var (data, next) = GetDict(index);
                    if (next != 0xFFFF)
                    {
                        var val = (byte)(_stack[_stackTop] & 0xFF);
                        preStoredValues += $"{(byte)(val & 0x0F):X} ";
                        preStoredValues += $"{(byte)((val >> 4) & 0x0F):X} ";
                    }
                    _stack[_stackTop] = (ushort)((index & 0xFF00) + data);

                    _stackTop += 1;
                    index = next;
                }

                _prevData = GetDict(index).data;
                _stack[_stackTop] = (ushort)(_prevData & 0xFFFF);
                _stackTop += 1;
                
                // var lastVal = (byte)(_stack[_stackTop - 1] & 0xFF);
                // preStoredValues += $"{(byte)(lastVal & 0x0F):X} ";
                // preStoredValues += $"{(byte)((lastVal >> 4) & 0x0F):X} ";
                // var pos = $"{index}";
                // if (index <= 0xFF)
                // {
                //     pos += $" ({index & 0xFF:X2})";
                // }
                //_logger.LogInformation($"Retrieved from dict at pos {pos} '{preStoredValues}'");

                // var valuesToStore = "";
                // var q = _stackTop;
                // do
                // {
                //     q--;
                //     var nextVal = (byte)(_stack[q] & 0xFF);
                //     valuesToStore += $"{(byte)(nextVal & 0x0F):X} ";
                //     valuesToStore += $"{(byte)((nextVal >> 4) & 0x0F):X} ";
                // } while (q > 0);
                //_logger.LogInformation($"Storing in dict at pos {_dictTop} '{valuesToStore}'");
                SetDict(_dictTop, (byte)(_prevData & 0xFF), (ushort)(_prevIndex & 0xFFFF));

                _prevIndex = tempIndex;

                _dictTop += 1;
                if (_dictTop > _wordMask)
                {
                    _wordWidth += 1;
                    _wordMask <<= 1;
                    _wordMask |= 1;
                }

                if (_wordWidth > _maxWordWidth)
                {
                    Reset();
                }
            }
            // else
            // {
            //     _logger.LogInformation($"Reading from stack {_stackTop} {(byte)(_stack[_stackTop] & 0xFF):X}");
            // }

            _stackTop -= 1;
            var ret = (byte)(_stack[_stackTop] & 0xFF);
            //_logger.LogInformation($"Returning from stack {_stackTop} {ret:X}");
            return ret;
        }

        public byte[] Decompress(int length)
        {
            using var memStream = new MemoryStream();
            using var writer = new BinaryWriter(memStream);
            
            uint rleCount = 0;
            uint pixel = 0;
            
            for (var i = 0; i < length; i++)
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
                        //_logger.LogInformation($"Non-RLE encoded bytes {((byte)pixel & 0x0F):X} {((byte)(pixel >> 4) & 0x0F):X}");
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
                            //_logger.LogInformation($"RLE encoded of bytes {((byte)pixel & 0x0F):X} {((byte)(pixel >> 4) & 0x0F):X} for {repeat}");
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
            }

            var decompressedBytes = memStream.ToArray();
            _logger.LogInformation($"Decompressed from {_data.Length} bytes to {decompressedBytes.Length}");
            return decompressedBytes;
        }
    }
}