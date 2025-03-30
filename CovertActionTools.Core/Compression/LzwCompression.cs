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

        private int _state;
        private int _offset;
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
            _state = 0;
            _offset = 0;
            
            Reset();
        }
        
        private void WriteBytes(byte[] bytes, int data, byte bitsToWrite)
        {
            _partial |= data << _state;
            _state += bitsToWrite;
            while (_state >= 8)
            {
                bytes[_offset] = (byte)(_partial & 0xFF);
                //_logger.LogError($"test 1 {_offset}: {bytes[_offset]:X} {data:X} {bitsToWrite}");
                _partial >>= 8;
                _state -= 8;
                _offset += 1;
            }

            bytes[_offset] = (byte)(_partial & 0xFF);
            //_logger.LogError($"test 3 {_offset}: {bytes[_offset]:X} {data:X} {bitsToWrite}");
            // writer.Write((byte)(_partial & 0xFF));
            // writer.Seek(-1, SeekOrigin.Current);
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
                //_logger.LogError($"{_offset} {_state}: {s}");
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
            //to test pixel splitting
            //duplicatedBytes.LogDebugFirstBytes(_logger, 10, 10);

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
                    encodingWriter.Write((byte)0x90);
                    encodingWriter.Write((byte)(repeats + 2));
                    continue;
                }

                if (repeats == 0)
                {
                    //_logger.LogError($"Writing single pixel: {pixel:X2}");
                    encodingWriter.Write(pixel);
                    if (pixel == 0x90)
                    {
                        //the only way to write 0x90 is to RLE encode it with a repeat of 0
                        encodingWriter.Write((byte)0);
                    }
                }
                else if (repeats == 1)
                {
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
                    //we just write it twice
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
                    //_logger.LogError($"Writing RLE: {lastPixel:X2} for {repeats + 1}");
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

            //to test RLE
            //File.WriteAllBytes(@"", encodingMemStream.ToArray());
            //encodingMemStream.ToArray().LogDebugFirstBytes(_logger, 16, 10, 300);

            //lastly we apply LZW
            var bytes = new byte[_data.Length]; //TODO: lower size

            using var readStream = new MemoryStream(encodingMemStream.ToArray());
            using var reader = new BinaryReader(readStream);

            //Temporarily disable LZW, by doing the worst case (single codes)
            // while (readStream.Position < readStream.Length)
            // {
            //     var next = reader.ReadByte();
            //     WriteBytes(bytes, next, _wordWidth);
            //     var nextId = GetDictNextId();
            //     _dict[Guid.NewGuid().ToString()] = nextId;
            //     if (nextId >= _wordMask)
            //     {
            //         _wordWidth += 1;
            //         _wordMask <<= 1;
            //         _wordMask |= 1;
            //     }
            //     
            //     if (_wordWidth > _maxWordWidth)
            //     {
            //         Reset();
            //     }
            // }

            var first = true;
            var wordCreations = 0;
            var dictResets = 0;
            var debug = false;
            var justReset = false;
            var _testBytes = new List<byte>();
            while (readStream.Position < readStream.Length)
            {
                var next = reader.ReadByte();
                if (_offset == 2198)
                {
                   //_logger.LogError($"next: {GetDictNextId():X4} {string.Join(" ", _currentWord.Select(x => $"{x:X2}"))} = {TryGetDict(new List<byte>(_currentWord)):X4}");
                }

                if (first)
                {
                    //we add this fake first entry
                    SetDict(new List<byte>() { 0, next }, GetDictNextId());
                    first = false;
                }

                if (justReset)
                {
                    //_logger.LogError($"next id: {GetDictNextId()}");
                }

                if (justReset && GetDictNextId() == 0x105)
                {
                    justReset = false;
                    _logger.LogError($"next: {next:X2} - {string.Join(" ", _currentWord.Select(x => $"{x:X2}"))}");
                    for (ushort i = (ushort)(GetDictNextId() - 5); i < GetDictNextId(); i++)
                    {
                        _logger.LogError($"! {i:X4}: {string.Join(" ", _dict.First(x => x.Value == i).Key)}");    
                    }
                }
                //_logger.LogError($"next byte: {next:X2}");
                
                var nextId = GetDictNextId();
                if (nextId == 0x100)
                {
                    readStream.Seek(-2, SeekOrigin.Current);
                    var t1 = reader.ReadByte();
                    var t2 = reader.ReadByte();
                    _logger.LogError($"!! {next:X2} {t1:X2} {t2:X2} - {string.Join(" ", _currentWord.Select(x => $"{x:X2}"))}");
                }

                var potentialNextWord = _currentWord.ToList();
                potentialNextWord.Add(next);
                
                var index = TryGetDict(potentialNextWord);
                if (index != null)
                {
                    //it's an existing word
                    //_logger.LogError($"existing word: {string.Join("", potentialNextWord.Select(x => $"{x:X2}"))}");
                    _currentWord = potentialNextWord.ToList();
                    //_logger.LogError($"new word2 {nextId:X4}: {string.Join(" ", potentialNextWord.Select(x => $"{x:X2}"))}");
                }
                else
                {
                    //it's a new word
                    // var reversedWord = potentialNextWord.ToList();
                    // reversedWord.Reverse();
                    // _logger.LogError($"new word {nextId:X4}: {string.Join(" ", reversedWord.Select(x => $"{x:X2}"))}");
                    SetDict(potentialNextWord, nextId);
                    wordCreations += 1;
                    //to test word creation
                    // testBytes.Add((byte)((nextId >> 8) & 0xFF));
                    // testBytes.Add((byte)(nextId & 0xFF));
                    
                    var lastIndex = TryGetDict(_currentWord);
                    //_logger.LogError($"new word writing existing: {string.Join("", _currentWord.Select(x => $"{x:X2}"))}");
                    if (lastIndex == null)
                    {
                        throw new Exception($"Last index missing: {string.Join("", _currentWord.Select(x => $"{x:X2}"))}");
                    }
                    
                    WriteBytes(bytes, lastIndex.Value, _wordWidth);
                    //to test LZW without bit-shift
                    _testBytes.Add((byte)((lastIndex.Value >> 8) & 0xFF));
                    _testBytes.Add((byte)(lastIndex.Value & 0xFF));
                    if (_testBytes.Count == 3638)
                    {
                        _testBytes.ToArray().LogDebugFirstBytes(_logger, 16, 8, _testBytes.Count-128);
                        //_logger.LogError($"{_offset} {_state}");
                        // for (ushort i = 0x00FF; i < 0x0105; i++)
                        // {
                        //     _logger.LogError($"{i:X4}: {string.Join(" ", _dict.First(x => x.Value == i).Key)}");    
                        // }
                        //229 is where it goes wrong
                        //_logger.LogError($"last: {string.Join(" ", _testBytes.Skip(_testBytes.Count - 8).Select(x => $"{x:X2}"))}");
                    }

                    // if (nextId < 260 && _wordWidth == 9)
                    // {
                    //     var reversedWord = potentialNextWord.ToList();x
                    //     reversedWord.Reverse();
                    //     // if (nextId == 258)
                    //     // {
                    //     //     _logger.LogError($"256: word: {_dict.First(x => x.Value == 0x100)}");
                    //     //     _logger.LogError($"257: word: {_dict.First(x => x.Value == 0x101)}");
                    //     //     _logger.LogError($"258: word: {_dict.First(x => x.Value == 0x102)}");
                    //     // }
                    //
                    //     _logger.LogError($"Reached ID {nextId} at offset: {_offset} {_state}; word: {string.Join(" ", reversedWord.Select(x => $"{x:X2}"))}");
                    // }
                    
                    _currentWord = new List<byte>() { next };
                    if (nextId > _wordMask)
                    {
                        _logger.LogError($"Reached ID {nextId} at offset: {_offset} {_state}, bumping word width to {_wordWidth + 1}");
                        // for (ushort i = (ushort)(nextId - 5); i < nextId; i++)
                        // {
                        //     _logger.LogError($"!! {i:X4}: {string.Join(" ", _dict.First(x => x.Value == i).Key)}");    
                        // }
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
                        _testBytes.ToArray().LogDebugFirstBytes(_logger, 16, 2, _testBytes.Count-32);
                        _logger.LogError($"Resetting {next:X2} width {_wordWidth} at offset: {_offset} {_state} next ID: {nextId:X4} prev word: {string.Join(" ", _currentWord.Select(x => $"{x:X2}"))}");
                        // _testBytes.Add(0xDE);
                        // _testBytes.Add(0xAD);
                        // _testBytes.Add(0xBE);
                        // _testBytes.Add(0xEF);
                        for (ushort i = (ushort)(nextId - 5); i < nextId; i++)
                        {
                            _logger.LogError($"! {i:X4}: {string.Join(" ", _dict.First(x => x.Value == i).Key)}");    
                        }
                        Reset();
                        first = true;
                        justReset = true;
                        _currentWord = new List<byte>() { };
                        dictResets += 1;
                        readStream.Seek(-1, SeekOrigin.Current);
                    }
                }
            }
            
            var finalIndex = TryGetDict(_currentWord);
            if (finalIndex != null)
            {
                WriteBytes(bytes, finalIndex.Value, _wordWidth);
                //to test LZW without bit-shift
                _testBytes.Add((byte)((finalIndex.Value >> 8) & 0xFF));
                _testBytes.Add((byte)(finalIndex.Value & 0xFF));
            }
            
            //to test LZW without bit-shift
            //_testBytes.ToArray().LogDebugFirstBytes(_logger, 16, 10, 3944);
            _logger.LogError($"words {wordCreations} resets {dictResets}");
            
            var compressedBytes = bytes.Take(_offset + (_state > 0 ? 1 : 0)).ToArray();

            _logger.LogInformation($"Compressed from {_data.Length} bytes to {compressedBytes.Length}");
            return compressedBytes;
        }
    }
}