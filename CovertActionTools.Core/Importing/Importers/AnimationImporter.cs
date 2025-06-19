using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using CovertActionTools.Core.Conversion;
using CovertActionTools.Core.Importing.Shared;
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

        public override void SetResult(PackageModel model)
        {
            model.Animations = GetResult();
        }

        protected override bool CheckIfValidForImportInternal(string path)
        {
            if (Directory.GetFiles(GetPath(path), "*.json").Length == 0)
            {
                return false;
            }

            return true;
        }

        protected override int GetTotalItemCountInPath()
        {
            return GetKeys(GetPath(Path)).Count;
        }

        protected override int RunImportStepInternal()
        {
            var nextKey = _keys[_index];

            var animationPath = System.IO.Path.Combine(GetPath(Path), nextKey);
            _result[nextKey] = Import(animationPath, nextKey);

            return _index++;
        }

        protected override Dictionary<string, AnimationModel> GetResultInternal()
        {
            return _result;
        }

        protected override void OnImportStart()
        {
            _keys.AddRange(GetKeys(GetPath(Path)));
            _index = 0;
        }
        
        private List<string> GetKeys(string path)
        {
            return Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly)
                .Select(System.IO.Path.GetFileName)
                .ToList();
        }

        private List<int> GetImages(string path, string key)
        {
            return Directory.GetFiles(path, $"{key}_*_VGA.png")
                .Select(System.IO.Path.GetFileNameWithoutExtension)
                .Select(x => int.Parse(x.Replace($"{key}_", "").Replace("_VGA", "")))
                .ToList();
        }
        
        private SharedMetadata ReadMetadata(string path, string key)
        {
            var filePath = System.IO.Path.Combine(path, $"{key}_metadata.json");
            if (!File.Exists(filePath))
            {
                throw new Exception($"Missing JSON file: {key}");
            }

            var rawData = File.ReadAllText(filePath);
            
            return JsonSerializer.Deserialize<SharedMetadata>(rawData, JsonOptions) ?? throw new Exception("Invalid animation model"); 
        }

        private AnimationModel.GlobalData ReadGlobalData(string path, string key)
        {
            var filePath = System.IO.Path.Combine(path, $"{key}_global.json");
            if (!File.Exists(filePath))
            {
                throw new Exception($"Missing JSON file: {key}");
            }

            var rawData = File.ReadAllText(filePath);
            
            return JsonSerializer.Deserialize<AnimationModel.GlobalData>(rawData, JsonOptions) ?? throw new Exception("Invalid animation model"); 
        }
        
        private AnimationModel.ControlData ReadControlData(string path, string key)
        {
            var instructionFilePath = System.IO.Path.Combine(path, $"{key}_instructions.txt");
            if (!File.Exists(instructionFilePath))
            {
                throw new Exception($"Missing Instructions file: {key}");
            }
            var rawInstructionData = File.ReadAllText(instructionFilePath);
            
            var stepFilePath = System.IO.Path.Combine(path, $"{key}_steps.txt");
            if (!File.Exists(stepFilePath))
            {
                throw new Exception($"Missing Steps file: {key}");
            }
            var rawStepData = File.ReadAllText(stepFilePath);

            var data = new AnimationModel.ControlData();
            data.ParseInstructionsAndSteps(rawInstructionData, rawStepData);
            return data;
        }

        private AnimationModel Import(string path, string key)
        {
            var model = new AnimationModel()
            {
                Key = key
            };
            
            model.Metadata = ReadMetadata(path, key);
            model.Data = ReadGlobalData(path, key);
            model.Control = ReadControlData(path, key);

            var imagePath = System.IO.Path.Combine(path, "images");
            var images = GetImages(imagePath, key);
            foreach (var image in images)
            {
                model.Images[image] = ImportImage(imagePath, $"{key}_{image}");
            }

            return model;
        }
        
        private SimpleImageModel ImportImage(string path, string filename)
        {
            var model = new SimpleImageModel();
            model.Key = filename;
            model.ExtraData = _imageImporter.ReadImageData(path, filename, "VGA_metadata");
            (model.RawVgaImageData, model.VgaImageData) = _imageImporter.ReadVgaImageData(path, filename, model.ExtraData.LegacyWidth, model.ExtraData.LegacyHeight);
            model.CgaImageData = Array.Empty<byte>();
            if (model.ExtraData.LegacyColorMappings != null)
            {
                var rawCgaImageData = ImageConversion.VgaToCgaTexture(model.ExtraData.LegacyWidth, model.ExtraData.LegacyHeight, model.RawVgaImageData, model.ExtraData.LegacyColorMappings);
                using var skBitmap = SKBitmap.Decode(rawCgaImageData, new SKImageInfo(model.ExtraData.LegacyWidth, model.ExtraData.LegacyHeight, SKColorType.Rgba8888, SKAlphaType.Premul));
                var textureBytes = skBitmap.Bytes.ToArray();
                model.CgaImageData = textureBytes;
            }
            return model;
        }

        private string GetPath(string path)
        {
            return System.IO.Path.Combine(path, "animation");
        }
    }
}