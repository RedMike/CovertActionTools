using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using CovertActionTools.Core.Conversion;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace CovertActionTools.Core.Importing.Importers
{
    internal class AnimationImporter : BaseImporter<Dictionary<string, AnimationModel>>
    {
#if DEBUG
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions()
        {
            WriteIndented = true,
            Converters = { 
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
            }
        };
#else
        private static readonly JsonSerializerOptions JsonOptions = JsonSerializerOptions.Default;
#endif
        
        private readonly ILogger<AnimationImporter> _logger;
        private readonly SharedImageImporter _imageImporter;
        
        private readonly List<string> _keys = new();
        private readonly Dictionary<string, AnimationModel> _result = new Dictionary<string, AnimationModel>();
        
        private int _index = 0;

        public AnimationImporter(ILogger<AnimationImporter> logger, SharedImageImporter imageImporter)
        {
            _logger = logger;
            _imageImporter = imageImporter;
        }

        protected override string Message => "Processing animations..";
        protected override bool CheckIfValidForImportInternal(string path)
        {
            if (Directory.GetFiles(path, "*_animation.json").Length == 0)
            {
                return false;
            }

            return true;
        }

        protected override int GetTotalItemCountInPath()
        {
            return GetKeys(Path).Count;
        }

        protected override int RunImportStepInternal()
        {
            var nextKey = _keys[_index];

            _result[nextKey] = Import(Path, nextKey);

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
            return Directory.GetFiles(path, "*_animation.json")
                .Select(System.IO.Path.GetFileNameWithoutExtension)
                .Select(x => x.Replace("_animation", ""))
                .ToList();
        }

        private List<int> GetImages(string path, string key)
        {
            return Directory.GetFiles(path, $"{key}_*_VGA.png")
                .Select(System.IO.Path.GetFileNameWithoutExtension)
                .Select(x => int.Parse(x.Replace($"{key}_", "").Replace("_VGA", "")))
                .ToList();
        }

        private AnimationModel Import(string path, string key)
        {
            var filePath = System.IO.Path.Combine(path, $"{key}_animation.json");
            if (!File.Exists(filePath))
            {
                throw new Exception($"Missing JSON file: {key}");
            }

            var rawData = File.ReadAllText(filePath);
            var model = new AnimationModel()
            {
                Key = key
            };
            model.ExtraData = JsonSerializer.Deserialize<AnimationModel.Metadata>(rawData, JsonOptions) ?? throw new Exception("Invalid animation model");
            
            var images = GetImages(path, key);
            foreach (var image in images)
            {
                model.Images[image] = ImportImage(path, $"{key}_{image}");
            }

            return model;
        }
        
        private SimpleImageModel ImportImage(string path, string filename)
        {
            var model = new SimpleImageModel();
            model.Key = filename;
            model.ExtraData = _imageImporter.ReadMetadata(path, filename, "animation_img");
            (model.RawVgaImageData, model.VgaImageData) = _imageImporter.ReadVgaImageData(path, filename, model.ExtraData.LegacyWidth, model.ExtraData.LegacyHeight);
            model.CgaImageData = Array.Empty<byte>();
            if (model.ExtraData.LegacyColorMappings != null)
            {
                var rawCgaImageData = ImageConversion.VgaToCgaTexture(model.ExtraData.LegacyWidth, model.ExtraData.LegacyHeight, model.RawVgaImageData, model.ExtraData.LegacyColorMappings);
                using var skBitmap = SKBitmap.Decode(rawCgaImageData, new SKImageInfo(model.ExtraData.LegacyWidth, model.ExtraData.LegacyHeight, SKColorType.Rgba8888, SKAlphaType.Premul));
                var textureBytes = skBitmap.Bytes.ToArray();
                model.CgaImageData = textureBytes;
            }
            //model.ModernImageData = _imageImporter.ReadModernImageData(path, filename, model.ExtraData.Width, model.ExtraData.Height);
            return model;
        }
    }
}