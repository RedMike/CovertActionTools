using System;
using System.Collections.Generic;
using System.IO;

namespace CovertActionTools.Core.Compression
{
    internal class LzwDecompression : IDisposable
    {
        private readonly int _maxWordWidth;
        private readonly byte[] _data;
        private readonly MemoryStream _memStream;
        private readonly BinaryWriter _writer;

        private int _state;
        private int _offset;
        
        private Dictionary<ushort, (byte data, ushort next)> _dict = new();
        private ushort[] _stack = new ushort[1024];
        private int _stackTop;
        private ushort _wordWidth;
        private ushort _wordMask;
        private ushort _dictTop;
        private ushort _prevIndex;
        private byte _prevData;
        

        public LzwDecompression(int maxWordWidth, byte[] data)
        {
            _maxWordWidth = maxWordWidth;
            _data = data;
            
            _memStream = new MemoryStream();
            _writer = new BinaryWriter(_memStream);

            _state = 0;
            _offset = 0;
            
            _stackTop = 0;
            Reset();
        }

        private void Reset()
        {
            _prevIndex = 0;
            _prevData = 0;
            _wordWidth = 9;
            _wordMask = (ushort)((1 << _wordWidth) - 1);
            _dictTop = 0x100;
            for (ushort i = 0; i < 2048; i++)
            {
                _dict[i] = ((byte)(i & 0xff), 0xffff);
            }
        }

        private int ReadBytes(int bits)
        {
            int read = 0;
            int bitsRead = 0;
            
            while (bitsRead != bits)
            {
                read >>= 1;

                if (_offset >= _data.Length || _offset < 0)
                {
                    //TODO: why is this triggering?
                    break;
                }
                int data = _data[_offset];
                if ((data & (1 << _state)) != 0)
                {
                    read |= (ushort)(1 << (bits - 1));
                }

                _state += 1;

                if (_state == 8)
                {
                    _state = 0;
                    _offset += 1;
                }

                bitsRead += 1;
            }

            return read;
        }

        private int ReadNext()
        {
            ushort tempIndex = 0;
            ushort index = 0;

            if (_stackTop == 0)
            {
                tempIndex = index = (ushort)ReadBytes(_wordWidth);

                if (index >= _dictTop)
                {
                    tempIndex = _dictTop;
                    index = _prevIndex;
                    _stack[_stackTop] = _prevData;
                    _stackTop += 1;
                }

                while (_dict[index].next != 0xFFFF)
                {
                    _stack[_stackTop] = (ushort)((index & 0xFF00) + _dict[index].data);
                    _stackTop += 1;
                    index = _dict[index].next;
                }

                _prevData = _dict[index].data;
                _stack[_stackTop] = _prevData;
                _stackTop += 1;

                _dict[_dictTop] = (_prevData, _prevIndex);

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

            _stackTop -= 1;
            return _stack[_stackTop];
        }

        public byte[] Decompress(int length)
        {
            var rleCount = 0;
            var pixel = 0;
            
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
                            rleCount = repeat - 2;
                        }
                    }
                }
                
                //each byte is actually two pixels one after the other
                _writer.Write((byte)(pixel & 0x0f));
                _writer.Write((byte)((pixel >> 4) & 0x0f));
            }
            
            return _memStream.ToArray();
        }

        public void Dispose()
        {
            _memStream.Dispose();
            _writer.Dispose();
        }
    }
}