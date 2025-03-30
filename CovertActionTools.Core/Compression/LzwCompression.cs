using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.Core.Compression
{
    public class LzwCompression
    {
        private readonly ILogger _logger;
        private readonly int _maxWordWidth;
        private readonly byte[] _data;

        private int _bitOffset;
        private int _byteOffset;
        private int _partial;
        
        private readonly Dictionary<string, ushort> _dict = new();
        private byte _wordWidth;
        private int _wordMask;
        private List<byte> _currentWord = new();

        public LzwCompression(ILogger logger, int maxWordWidth, byte[] data)
        {
            _logger = logger;
            _maxWordWidth = maxWordWidth;
            _data = data;
            _logger.LogInformation($"Starting compression from {data.Length} bytes, max word width {maxWordWidth}");

            _partial = 0;
            _bitOffset = 0;
            _byteOffset = 0;
            
            Reset();
        }
        
        private void WriteBytes(byte[] bytes, int data, byte bitsToWrite)
        {
            _partial |= data << _bitOffset;
            _bitOffset += bitsToWrite;
            while (_bitOffset >= 8)
            {
                bytes[_byteOffset] = (byte)(_partial & 0xFF);
                _partial >>= 8;
                _bitOffset -= 8;
                _byteOffset += 1;
            }

            bytes[_byteOffset] = (byte)(_partial & 0xFF);
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
            {
                return null;
            }

            return index;
        }

        private void SetDict(List<byte> word, ushort index)
        {
            if (index > 2048)
            {
                throw new Exception($"Writing beyond dictionary limit: {index}");
            }
            
            var s = string.Join("", word.Select(x => $"{x:X2}"));
            if (_dict.ContainsKey(s))
            {
                throw new Exception($"Found duplicate value for {s}");
            }
            _dict[s] = index;
        }

        private ushort GetDictNextId()
        {
            return (ushort)(_dict.Values.DefaultIfEmpty((ushort)0xFF).Max() + 1);
        }

        public byte[] Compress()
        {
            //first turn two pixels (up to 16 values) into a single byte (up to 256)
            using var duplicatedMemStream = new MemoryStream();
            using var duplicatedWriter = new BinaryWriter(duplicatedMemStream);
            for (var i = 0; i < _data.Length; i++)
            {
                var p1 = _data[i];
                i++;
                byte p2 = 0;
                if (i < _data.Length)
                {
                    p2 = _data[i];
                }

                if (p1 > 16 || p2 > 16)
                {
                    throw new Exception($"Pixel value too high: {p1:X} {p2:X}");
                }

                var mixedPixel = (byte)(((p2 & 0x0F) << 4) | (p1 & 0x0F));
                duplicatedWriter.Write(mixedPixel);
            }

            var duplicatedBytes = duplicatedMemStream.ToArray();

            //then we apply RLE
            using var encodingMemStream = new MemoryStream();
            using var encodingWriter = new BinaryWriter(encodingMemStream);
            byte repeats = 0;
            bool started = false;
            byte lastPixel = 0;
            for (var i = 0; i < duplicatedBytes.Length; i++)
            {
                var pixel = duplicatedBytes[i];

                if (started &&
                    pixel == lastPixel &&
                    repeats < 250 && //prevent overflow (in legacy engine)
                    i < duplicatedBytes.Length - 1 //end of image, finish repeating
                   )
                {
                    repeats++;
                    continue;
                }

                if (i == duplicatedBytes.Length - 1)
                {
                    //if we're reaching the end, forcefully write the RLE end
                    encodingWriter.Write((byte)0x90);
                    encodingWriter.Write((byte)(repeats + 2));
                    continue;
                }

                if (repeats == 0)
                {
                    //it was a single pixel
                    encodingWriter.Write(pixel);
                    if (pixel == 0x90)
                    {
                        //the only way to write 0x90 is to RLE encode it with a repeat of 0
                        encodingWriter.Write((byte)0);
                    }
                }
                else if (repeats == 1)
                {
                    //we don't apply RLE for two pixels
                    encodingWriter.Write(lastPixel);
                    if (lastPixel == 0x90)
                    {
                        //the only way to write 0x90 is to RLE encode it with a repeat of 0
                        encodingWriter.Write((byte)0);
                    }
                    
                    encodingWriter.Write(pixel);
                    if (pixel == 0x90)
                    {
                        //the only way to write 0x90 is to RLE encode it with a repeat of 0
                        encodingWriter.Write((byte)0);
                    }
                }
                else if (repeats == 2)
                {
                    //we also don't apply RLE for three pixels because that's what the legacy engine does
                    encodingWriter.Write(lastPixel);
                    if (lastPixel == 0x90)
                    {
                        //the only way to write 0x90 is to RLE encode it with a repeat of 0
                        encodingWriter.Write((byte)0);
                    }
                    encodingWriter.Write(lastPixel);
                    if (lastPixel == 0x90)
                    {
                        //the only way to write 0x90 is to RLE encode it with a repeat of 0
                        encodingWriter.Write((byte)0);
                    }
                    encodingWriter.Write(pixel);
                    if (pixel == 0x90)
                    {
                        //the only way to write 0x90 is to RLE encode it with a repeat of 0
                        encodingWriter.Write((byte)0);
                    }
                }
                else
                {
                    //we apply RLE
                    encodingWriter.Write((byte)0x90);
                    encodingWriter.Write((byte)(repeats + 1));
                    encodingWriter.Write(pixel);
                    if (pixel == 0x90)
                    {
                        //the only way to write 0x90 is to RLE encode it with a repeat of 0
                        encodingWriter.Write((byte)0);
                    }
                }

                started = true;
                repeats = 0;
                lastPixel = pixel;
            }

            //lastly we apply LZW
            var bytes = new byte[_data.Length]; //TODO: lower size

            using var readStream = new MemoryStream(encodingMemStream.ToArray());
            using var reader = new BinaryReader(readStream);

            var first = true;
            while (readStream.Position < readStream.Length)
            {
                var next = reader.ReadByte();
                if (first)
                {
                    //we add this fake first entry
                    SetDict(new List<byte>() { 0, next }, GetDictNextId());
                    first = false;
                }
                
                var nextId = GetDictNextId();

                var potentialNextWord = _currentWord.ToList();
                potentialNextWord.Add(next);
                
                var index = TryGetDict(potentialNextWord);
                if (index != null)
                {
                    //it's an existing word
                    _currentWord = potentialNextWord.ToList();
                }
                else
                {
                    //it's a new word
                    SetDict(potentialNextWord, nextId);
                    
                    var lastIndex = TryGetDict(_currentWord);
                    if (lastIndex == null)
                    {
                        throw new Exception($"Last index missing: {string.Join("", _currentWord.Select(x => $"{x:X2}"))}");
                    }
                    
                    WriteBytes(bytes, lastIndex.Value, _wordWidth);
                    
                    _currentWord = new List<byte>() { next };
                    if (nextId > _wordMask)
                    {
                        _wordWidth += 1;
                        _wordMask <<= 1;
                        _wordMask |= 1;
                    }
                    if (_wordWidth > _maxWordWidth)
                    {
                        Reset();
                        first = true;
                        _currentWord = new List<byte>() { };
                        //for some reason we already processed the pixel once, just go back
                        readStream.Seek(-1, SeekOrigin.Current);
                    }
                }
            }
            
            var finalIndex = TryGetDict(_currentWord);
            if (finalIndex != null)
            {
                WriteBytes(bytes, finalIndex.Value, _wordWidth);
            }
            
            var compressedBytes = bytes.Take(_byteOffset + (_bitOffset > 0 ? 1 : 0)).ToArray();

            _logger.LogInformation($"Compressed from {_data.Length} bytes to {compressedBytes.Length}");
            return compressedBytes;
        }
    }
}