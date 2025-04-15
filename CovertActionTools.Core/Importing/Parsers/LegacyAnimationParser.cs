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
                throw new Exception($"Unexpected prefix: {string.Join("", prefix.Select(x => $"{x:X2}"))}");
            }

            var u1 = reader.ReadUInt16();
            var u2 = reader.ReadUInt16();
            var colorMapping = new Dictionary<byte, byte>();
            for (var i = 0; i < 16; i++)
            {
                colorMapping[(byte)i] = reader.ReadByte();
            }

            var u3 = reader.ReadUInt16();
            var u4 = reader.ReadUInt16();
            var u5 = reader.ReadByte();
            var aWidth = reader.ReadUInt16();
            var aHeight = reader.ReadUInt16();
            var f1 = reader.ReadUInt16();
            var f2 = reader.ReadByte();

            if (f1 != 1 || f2 != 1)
            {
                //first read until we have the first format byte to find the first image
                var b1 = 0x00;
                var b2 = 0x00;
                do
                {
                    b1 = reader.ReadByte();
                    b2 = reader.ReadByte();
                    memStream.Seek(-1, SeekOrigin.Current);
                } while ((b1 != 0x07 || b2 != 0x00) && (b1 != 0x0F || b2 != 0x00));

                memStream.Seek(-1, SeekOrigin.Current);
            }

            var imageBytes = new Dictionary<int, byte[]>();
            var tempBytes = new List<byte>(8000);
            var img = 0;
            do
            {
                var b = reader.ReadBytes(2);
                if (b[0] == 0xFF && b[1] == 0xFF)
                {
                    break;
                }
                if (b[0] == 0x07 && b[1] == 0x00)
                {
                    //it's a new image
                    if (tempBytes.Count > 0)
                    {
                        imageBytes[img] = tempBytes.ToArray();
                    }
                    tempBytes.Clear();
                    img++;
                }

                tempBytes.Add(b[0]);
                tempBytes.Add(b[1]);
            } while (true);

            var images = new Dictionary<int, SimpleImageModel>();
            foreach (var pair in imageBytes)
            {
                try
                {
                    images[pair.Key] = _imageParser.Parse($"{key}_{pair.Key}", pair.Value);
                }
                catch (Exception e)
                {
                    _logger.LogWarning($"Failed to parse image {key} {pair.Key}: {e}");
                }
            }
            
            var model = new AnimationModel()
            {
                Key = key,
                Images = images,
                ExtraData = new AnimationModel.Metadata()
                {
                    Name = key,
                    Comment = "Legacy import",
                    Format1 = f1,
                    Format2 = f2
                }
            };
            return model;
        }
    }
}