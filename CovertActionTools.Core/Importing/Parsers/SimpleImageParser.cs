using System;
using System.Collections.Generic;
using System.IO;
using CovertActionTools.Core.Compression;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.Core.Importing.Parsers
{
    public interface ISimpleImageParser
    {
        SimpleImageModel Parse(string key, byte[] rawData);
    }
    
    internal class SimpleImageParser : ISimpleImageParser
    {
        private readonly ILogger<SimpleImageParser> _logger;
        private readonly ILoggerFactory _loggerFactory;

        public SimpleImageParser(ILogger<SimpleImageParser> logger, ILoggerFactory loggerFactory)
        {
            _logger = logger;
            _loggerFactory = loggerFactory;
        }

        public SimpleImageModel Parse(string key, byte[] rawData)
        {
            using var memStream = new MemoryStream(rawData);
            using var reader = new BinaryReader(memStream);

            //basic data
            var formatFlag = reader.ReadUInt16();
            var width = reader.ReadUInt16();
            var height = reader.ReadUInt16();
            
            //legacy CGA colour mapping
            Dictionary<byte, byte>? legacyColorMappings = null;
            switch (formatFlag)
            {
                case 0x0F:
                    legacyColorMappings = new Dictionary<byte, byte>();
                    var colorMappingBytes = reader.ReadBytes(16);
                    for (byte c1 = 0; c1 < 16; c1++)
                    {
                        var c2 = colorMappingBytes[c1];
                        legacyColorMappings[c1] = c2;
                    }
                    break;
                case 0x07:
                    //no conversion dict
                    break;
                default:
                    throw new Exception($"Unsupported format flag: {formatFlag:X}");
            }
            
            //LZW config
            var lzwMaxWordWidth = reader.ReadByte();
            
            //data compressed in LZW+RLE
            var imageCompressedData = reader.ReadBytes(rawData.Length);
            byte[] rawImageData;
            {
                using var lzw = new LzwDecompression(_loggerFactory.CreateLogger(typeof(LzwDecompression)), lzwMaxWordWidth, imageCompressedData);
                rawImageData = lzw.Decompress(width * height);
            }
            
            _logger.LogInformation($"Read image '{key}': {width}x{height}, Legacy Color Mapping = {legacyColorMappings != null}, Compressed Bytes = {rawData.Length}, Bytes = {rawImageData.Length}");
            return new SimpleImageModel()
            {
                Width = width,
                Height = height,
                LegacyColorMappings = legacyColorMappings,
                CompressionDictionaryWidth = lzwMaxWordWidth,
                RawImageData = rawImageData
            };
        }
    }
}