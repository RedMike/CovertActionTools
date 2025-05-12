using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.Core.Compression
{
    public class LzwCompression
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
        private int _partial;
        
        private readonly Dictionary<string, ushort> _dict = new();
        private byte _wordWidth;
        private int _wordMask;
        private List<byte> _currentWord = new();

        private readonly string _key;

        private List<byte> _lzwBytes = new();

        public LzwCompression(ILogger logger, int maxWordWidth, byte[] data, string key)
        {
            _logger = logger;
            _maxWordWidth = maxWordWidth;
            _data = data;
            _key = key;
            _logger.LogInformation($"Starting compression from {data.Length} bytes, max word width {maxWordWidth}");

            _partial = 0;
            _bitOffset = 0;
            _byteOffset = 0;
            
            Reset();
        }
        
        private void WriteBytes(byte[] bytes, int data, byte bitsToWrite)
        {
            if (PrintDebugLzw)
            {
                _lzwBytes.Add((byte)((data >> 8) & 0xFF));
                _lzwBytes.Add((byte)(data & 0xFF));
            }
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
            //bug in their implementation, 0x100 is never actually used
            if (index == 0x100)
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
            //bug in their implementation, 0x100 is never actually used
            if (_dict.TryGetValue(s, out var potentialIndex) && potentialIndex != 0x100)
            {
                throw new Exception($"Found duplicate value for {s}");
            }
            _dict[s] = index;
        }

        private ushort GetDictNextId()
        {
            return (ushort)(_dict.Values.DefaultIfEmpty((ushort)0xFF).Max() + 1);
        }

        public byte[] Compress(int width, int height)
        {
            //first turn two pixels (up to 16 values) into a single byte (up to 256)
            using var duplicatedMemStream = new MemoryStream();
            using var duplicatedWriter = new BinaryWriter(duplicatedMemStream);
            var i = 0;
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
                    var p1 = _data[i];
                    i++;
                    x++;
                    byte p2 = 0;
                    if (x < width) //if we're reading the fake pixel, don't increment actual byte count
                    {
                        p2 = _data[i];
                        i++;
                    }

                    if (p1 > 16 || p2 > 16)
                    {
                        throw new Exception($"Pixel value too high: {p1:X} {p2:X}");
                    }

                    var mixedPixel = (byte)(((p2 & 0x0F) << 4) | (p1 & 0x0F));
                    duplicatedWriter.Write(mixedPixel);
                }
            }

            var duplicatedBytes = duplicatedMemStream.ToArray();

            //then we apply RLE
            using var encodingMemStream = new MemoryStream();
            using var encodingWriter = new BinaryWriter(encodingMemStream);
            byte repeats = 0;
            bool started = false;
            byte lastPixel = 0;
            for (i = 0; i < duplicatedBytes.Length; i++)
            {
                var pixel = duplicatedBytes[i];
                
                if (started &&
                    pixel != 0x90 &&
                    pixel == lastPixel &&
                    repeats < 254 && //prevent overflow (in legacy engine)
                    i < duplicatedBytes.Length - 1 //end of image, finish repeating
                   )
                {
                    repeats++;
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
                    if (i == duplicatedBytes.Length - 1 && pixel == lastPixel)
                    {
                        //it's only a RLE code, not a second pixel
                        encodingWriter.Write((byte)0x90);
                        encodingWriter.Write((byte)(repeats + 2));
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
                }

                started = true;
                repeats = 0;
                lastPixel = pixel;
            }

            var rleBytes = encodingMemStream.ToArray();
            
            //lastly we apply LZW
            var bytes = new byte[16000]; //TODO: lower size

            using var readStream = new MemoryStream(rleBytes);
            using var reader = new BinaryReader(readStream);

            var first = true;
            while (readStream.Position < readStream.Length)
            {
                var next = reader.ReadByte();
                if (first)
                {
                    //we add this fake first entry
                    var id = GetDictNextId();
                    var tempBytes = new List<byte>() { 0, next };
                    SetDict(tempBytes, id);
                    if (LogDebugLzwFirstDict && _logger.IsEnabled(LogLevel.Debug))
                    {
                        var reversedWord = tempBytes;
                        reversedWord.Reverse();
                        _logger.LogDebug($"Dict word {id:X4} at offset {_byteOffset} {_bitOffset}: {string.Join("", reversedWord.Select(x => $"{x:X2}"))}");
                    }
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
                    
                    if (LogDebugLzwFirstDict && nextId < 0x105)
                    {
                        if (_logger.IsEnabled(LogLevel.Debug))
                        {
                            var reversedWord = potentialNextWord.ToList();
                            reversedWord.Reverse();
                            _logger.LogDebug($"Dict word {nextId:X4} at offset {_byteOffset} {_bitOffset}: {string.Join("", reversedWord.Select(x => $"{x:X2}"))}");
                        }
                    }
                    
                    _currentWord = new List<byte>() { next };
                    if (nextId > _wordMask)
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
            
            if (PrintDebugKeys.Contains(_key))
            {
                if (PrintDebugRawData)
                {
                    File.WriteAllBytes(Path.Combine(PrintDebugFolder, $"{_key}_compress_raw.bin"), _data.ToArray());    
                }
                if (PrintDebugMergedPixels)
                {
                    File.WriteAllBytes(Path.Combine(PrintDebugFolder, $"{_key}_compress_merged.bin"), duplicatedBytes.ToArray());    
                }
                if (PrintDebugRle)
                {
                    File.WriteAllBytes(Path.Combine(PrintDebugFolder, $"{_key}_compress_RLE.bin"), encodingMemStream.ToArray());    
                }
                if (PrintDebugLzw)
                {
                    File.WriteAllBytes(Path.Combine(PrintDebugFolder, $"{_key}_compress_LZW.bin"), _lzwBytes.ToArray());    
                }
            }

            _logger.LogDebug($"Compressed from {_data.Length} bytes to {compressedBytes.Length}");
            return compressedBytes;
        }
    }
}