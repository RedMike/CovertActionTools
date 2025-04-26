using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.Core.Importing.Parsers
{
    public class LegacyAnimationParser : BaseImporter<Dictionary<string, AnimationModel>>
    {
        private readonly ILogger<LegacyAnimationParser> _logger;
        private readonly SharedImageParser _imageParser;

        private readonly List<string> _keys = new();
        private readonly Dictionary<string, AnimationModel> _result = new Dictionary<string, AnimationModel>();
        
        private int _index = 0;
        
        public LegacyAnimationParser(ILogger<LegacyAnimationParser> logger, SharedImageParser imageParser)
        {
            _logger = logger;
            _imageParser = imageParser;
        }

        protected override string Message => "Processing animations..";
        protected override bool CheckIfValidForImportInternal(string path)
        {
            if (Directory.GetFiles(path, "*.PAN").Length == 0)
            {
                return false;
            }

            return true;
        }

        protected override int GetTotalItemCountInPath()
        {
            return _keys.Count;
        }

        protected override int RunImportStepInternal()
        {
            var nextKey = _keys[_index];

            _result[nextKey] = Parse(Path, nextKey);

            return _index++;
        }

        protected override Dictionary<string, AnimationModel> GetResultInternal()
        {
            return _result;
        }

        protected override void OnImportStart()
        {
            _keys.AddRange(GetKeys(Path));
            _index = 0;
        }
        
        private List<string> GetKeys(string path)
        {
            return Directory.GetFiles(path, "*.PAN")
                .Select(System.IO.Path.GetFileNameWithoutExtension)
                .ToList();
        }

        private AnimationModel Parse(string path, string key)
        {
            var filePath = System.IO.Path.Combine(path, $"{key}.PAN");
            if (!File.Exists(filePath))
            {
                throw new Exception($"Missing PAN file: {key}");
            }

            var rawData = File.ReadAllBytes(filePath);
            var model = ParseAnimation(key, rawData);
            return model;
        }

        private AnimationModel ParseAnimation(string key, byte[] rawData)
        {
            using var memStream = new MemoryStream(rawData);
            using var reader = new BinaryReader(memStream);

            var prefix = reader.ReadBytes(4);
            if (prefix[0] != 0x50 || prefix[1] != 0x41 || prefix[2] != 0x4E || prefix[3] != 0x49)
            {
                throw new Exception($"Unexpected prefix: {string.Join(" ", prefix.Select(x => $"{x:X2}"))}");
            }

            var tag = reader.ReadBytes(5);
            if (tag[0] != 0x03 || tag[1] != 0x01 || tag[2] != 0x01 || tag[3] != 0x00 || tag[4] != 0x03)
            {
                throw new Exception($"Unexpected tag: {string.Join(" ", tag.Select(x => $"{x:X2}"))}");
            }
            
            var colorMapping = new Dictionary<byte, byte>();
            for (var i = 1; i < 16; i++)
            {
                colorMapping[(byte)i] = reader.ReadByte();
            }

            var padding = reader.ReadBytes(5);
            if (padding.Any(x => x != 0x00))
            {
                throw new Exception($"Unexpected padding: {string.Join(" ", padding.Select(x => $"{x:X2}"))}");
            }
            
            var aWidth = reader.ReadUInt16(); //width - 1
            var aHeight = reader.ReadUInt16(); //height - 1
            var subAnimationCount = reader.ReadUInt16();
            var backgroundType = (AnimationModel.BackgroundType)reader.ReadByte();
            
            //header is always a fixed size
            var headerLength = backgroundType == AnimationModel.BackgroundType.PreviousAnimation ? 500 : 502;
            if (backgroundType == AnimationModel.BackgroundType.ClearToImage && subAnimationCount == 3)
            {
                //TODO: this is the case for BUSTOUT which makes no sense?
                headerLength = 501;
            }

            //for ClearToImage, there is an image before the header, otherwise it's straight to the header
            var images = new Dictionary<int, SimpleImageModel>();
            var img = 0;
            if (backgroundType == AnimationModel.BackgroundType.ClearToImage)
            {
                //there is one image before the header
                var image = ReadImage(rawData, reader, memStream, key, img, withOffset:true, offsetLength:headerLength);
                if (image == null)
                {
                    throw new Exception("Missing first image");
                }

                images[img++] = image;
            }

            var headerData = reader.ReadBytes(headerLength);
            
            //there are a variable number of images, ends with 00 05 00
            while (rawData[memStream.Position] == 0x07)
            {
                var image = ReadImage(rawData, reader, memStream, key, img);
                if (image == null)
                {
                    throw new Exception("Unparseable image");
                }

                images[img++] = image;
            }

            while (rawData[memStream.Position] == 0x00) //00 is used as padding sometimes
            {
                reader.ReadByte();
            }

            if (rawData[memStream.Position] != 0x05)
            {
                throw new Exception($"Invalid start to data section for {key}: {memStream.Position:X} {rawData[memStream.Position]:X2}");
            }
            
            //we read the data section into a buffer to use for pointers
            var originalOffset = memStream.Position;
            var dataSectionBytes = reader.ReadBytes(64000);
            memStream.Seek(originalOffset, SeekOrigin.Begin);
            
            //data section has a number of setup records, which include pointers to sets of instructions
            //the records are separated by 05 00, the distance between them dictates what type they are
            //setup records end when there is a 05 05 at which point the rest of the file is instructions
            //instructions are referenced by the setup records
            var done = false;
            var records = new List<AnimationModel.SetupRecord>();
            try
            {
                while (!done)
                {
                    var offset = memStream.Position + 2; //skip the 05 00
                    while (
                        rawData[offset] != 0x05 && (
                               (rawData[offset + 1] != 0x05 && rawData[offset + 2] != 0x00) || 
                               rawData[offset + 1] != 0x00
                        )
                    )
                    {
                        offset++;
                    }

                    if (rawData[offset + 1] == 0x05)
                    {
                        //it's 05 05 00, so it's the end of the section, we still handle the last instruction though
                        done = true;
                    }
                    
                    //skip 05 00 separator
                    reader.ReadBytes(2);
                    
                    //just because the first separator is X away doesn't mean the record is only X long
                    //e.g. Animation type has 2 bytes to the first separator, but the record has 6 groups of them
                    var length = offset - memStream.Position;
                    if (length < 1)
                    {
                        continue;
                    }
                    var recordType = (AnimationModel.SetupRecord.SetupType)length;
                    AnimationModel.SetupRecord record;
                    if (recordType == AnimationModel.SetupRecord.SetupType.Animation)
                    {
                        record = AnimationModel.SetupRecord.AsAnimation(reader, dataSectionBytes);
                    }
                    else
                    {
                        //for now assume that every other type has no extra bytes, any pattern should be obvious later
                        var bytes = reader.ReadBytes((int)length);
                        record = new AnimationModel.UnknownRecord()
                        {
                            Type = recordType,
                            Data = bytes
                        };
                    }

                    //TODO: skip some types like empty 00 records?
                    records.Add(record);
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"Failed to parse data section: {e}");
            }

            var model = new AnimationModel()
            {
                Key = key,
                Images = images,
                ExtraData = new AnimationModel.Metadata()
                {
                    Name = key,
                    Comment = "Legacy import",
                    SubAnimationCount = subAnimationCount,
                    BackgroundType = backgroundType,
                    BoundingWidth = aWidth,
                    BoundingHeight = aHeight,
                    ColorMapping = colorMapping,
                    HeaderData = headerData,
                    Records = records
                }
            };
            return model;
        }

        private SimpleImageModel? ReadImage(byte[] rawData, BinaryReader reader, MemoryStream memStream, string key, int img, bool withOffset = false, int offsetLength = 502)
        {
            //because we don't do piece-meal parsing, we have to read a ton of extra bytes and pass them over first
            //but parsing the image will return the actual offset
            var originalOffset = memStream.Position;
            var bytes = reader.ReadBytes(16000); //no image can be longer than this anyway
            SimpleImageModel? model = null;
            try
            {
                model = _imageParser.Parse($"{key}_{img}", bytes, out var byteOffset);
                memStream.Seek(originalOffset + byteOffset, SeekOrigin.Begin);
            }
            catch (Exception e)
            {
                _logger.LogWarning($"Failed to parse image {key} {img}: {e}");
            }
            
            //TODO: the LZW decompression is not returning the correct byte offset, so we have to correct it here
            //we'll temporarily just try to find the next 07 or 05, then end right before it
            while ((!withOffset && (rawData[memStream.Position] != 0x07 || rawData[memStream.Position+1] != 0x00) && rawData[memStream.Position] != 0x05) ||
                   (withOffset && (rawData[memStream.Position + offsetLength] != 0x07 || rawData[memStream.Position + offsetLength + 1] != 0x00)))
            {
                reader.ReadByte();
            }
            //now rewind one
            //memStream.Seek(-1, SeekOrigin.Current);

            return model;
        }
    }
}