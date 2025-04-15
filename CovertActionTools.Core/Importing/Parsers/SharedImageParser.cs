using System;
using System.Collections.Generic;
using System.IO;
using CovertActionTools.Core.Compression;
using CovertActionTools.Core.Conversion;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.Core.Importing.Parsers
{
    public class SharedImageParser
    {
        private readonly ILogger<SharedImageParser> _logger;
        private readonly ILoggerFactory _loggerFactory;

        public SharedImageParser(ILogger<SharedImageParser> logger, ILoggerFactory loggerFactory)
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
            if (width % 2 == 1)
            {
                //TODO: does this have a special meaning?
                width += 1;
            }
            
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
            byte[] imageUncompressedData;
            {
                var lzw = new LzwDecompression(_loggerFactory.CreateLogger(typeof(LzwDecompression)), lzwMaxWordWidth, imageCompressedData, key);
                imageUncompressedData = lzw.Decompress(width * height);
            }
            
            //the data is currently in VGA format, so convert to modern format
            var imageModernData = new byte[width * height * 4];
            for (var i = 0; i < width; i++)
            {
                for (var j = 0; j < height; j++)
                {
                    var pixel = imageUncompressedData[j * width + i];
                    if (!Constants.VgaColorMapping.TryGetValue(pixel, out var col))
                    {
                        throw new Exception($"Invalid pixel value: {pixel}");
                    }
                    var (r, g, b, a) = col;
                    imageModernData[(j * width + i) * 4 + 0] = r;
                    imageModernData[(j * width + i) * 4 + 1] = g;
                    imageModernData[(j * width + i) * 4 + 2] = b;
                    imageModernData[(j * width + i) * 4 + 3] = a;
                }
            }

            var fullByteSize = width * height * 4;
            _logger.LogInformation($"Read image '{key}': {width}x{height}, Legacy Color Mapping = {legacyColorMappings != null}, Compressed Bytes = {rawData.Length}, Compression: {(100.0f - (float)rawData.Length/fullByteSize * 100.0f):F0}%");
            byte[] cgaImageData = Array.Empty<byte>();
            if (legacyColorMappings != null)
            {
                cgaImageData = ImageConversion.VgaToCgaTexture(width, height, imageUncompressedData, legacyColorMappings);
            }

            return new SimpleImageModel()
            {
                Key = key,
                RawVgaImageData = imageUncompressedData,
                VgaImageData = ImageConversion.VgaToTexture(width, height, imageUncompressedData),
                CgaImageData = cgaImageData,
                ModernImageData = imageModernData,
                ExtraData = new SimpleImageModel.Metadata()
                {
                    //for legacy images, we populate data from the legacy info
                    Type = Constants.GetLikelyImageType(key),
                    Name = key,
                    Width = width,
                    Height = height,
                    LegacyWidth = width,
                    LegacyHeight = height,
                    Comment = "Legacy import",
                    LegacyColorMappings = legacyColorMappings,
                    CompressionDictionaryWidth = lzwMaxWordWidth
                }
            };
        }
    }
}