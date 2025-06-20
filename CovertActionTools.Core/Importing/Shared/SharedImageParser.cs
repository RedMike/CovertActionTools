using System;
using System.Collections.Generic;
using System.IO;
using CovertActionTools.Core.Compression;
using CovertActionTools.Core.Conversion;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.Core.Importing.Shared
{
    public class SharedImageParser
    {
        private readonly ILogger<SharedImageParser> _logger;
        private readonly ILzwDecompression _decompression;

        public SharedImageParser(ILogger<SharedImageParser> logger, ILzwDecompression decompression)
        {
            _logger = logger;
            _decompression = decompression;
        }

        public SharedImageModel Parse(string key, BinaryReader reader)
        {
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
            var imageUncompressedData = _decompression.Decompress(width, height, lzwMaxWordWidth, reader);

            _logger.LogDebug($"Read image '{key}': {width}x{height}, Legacy Color Mapping = {legacyColorMappings != null}");
            byte[] cgaImageData = Array.Empty<byte>();
            if (legacyColorMappings != null)
            {
                cgaImageData = ImageConversion.VgaToCgaTexture(width, height, imageUncompressedData, legacyColorMappings);
            }

            return new SharedImageModel()
                {
                    RawVgaImageData = imageUncompressedData,
                    VgaImageData = ImageConversion.VgaToTexture(width, height, imageUncompressedData),
                    CgaImageData = cgaImageData,
                    Data = new SharedImageModel.ImageData()
                    {
                        //for legacy images, we populate data from the legacy info
                        Type = Constants.GetLikelyImageType(key),
                        Width = width,
                        Height = height,
                        LegacyColorMappings = legacyColorMappings,
                        CompressionDictionaryWidth = lzwMaxWordWidth
                    }
                };
        }
    }
}