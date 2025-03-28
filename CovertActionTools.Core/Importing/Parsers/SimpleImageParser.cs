using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.Core.Importing.Parsers
{
    public interface ISimpleImageParser
    {
        SimpleImageModel Parse(byte[] rawData);
    }
    
    internal class SimpleImageParser : ISimpleImageParser
    {
        private readonly ILogger<SimpleImageParser> _logger;

        public SimpleImageParser(ILogger<SimpleImageParser> logger)
        {
            _logger = logger;
        }

        public SimpleImageModel Parse(byte[] rawData)
        {
            using var memStream = new MemoryStream(rawData);
            using var reader = new BinaryReader(memStream);

            var formatFlag = reader.ReadUInt16();
            var width = reader.ReadUInt16();
            var height = reader.ReadUInt16();
            
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
            //TODO: parse
            
            _logger.LogInformation($"Read image: {width}x{height}, Legacy Color Mapping = {legacyColorMappings != null}");
            return new SimpleImageModel()
            {
                Width = width,
                Height = height,
                LegacyColorMappings = legacyColorMappings,
            };
        }
    }
}