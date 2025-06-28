using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using CovertActionTools.Core.Conversion;
using CovertActionTools.Core.Importing.Shared;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace CovertActionTools.Core.Importing.Importers
{
    internal class SimpleImageImporter : BaseImporter<Dictionary<string, SimpleImageModel>>
    {
        private readonly ILogger<SimpleImageImporter> _logger;
        private readonly SharedImageImporter _imageImporter;

        private readonly List<string> _keys = new();
        private readonly Dictionary<string, SimpleImageModel> _result = new Dictionary<string, SimpleImageModel>();
        
        private int _index = 0;

        public SimpleImageImporter(ILogger<SimpleImageImporter> logger, SharedImageImporter imageImporter)
        {
            _logger = logger;
            _imageImporter = imageImporter;
        }
        
        protected override string Message => "Processing simple images..";
        
        public override void SetResult(PackageModel model)
        {
            model.SimpleImages = GetResult();
        }

        protected override bool CheckIfValidForImportInternal(string path)
        {
            if (Directory.GetFiles(GetPath(path), "*_image.json").Length == 0)
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

            _result[nextKey] = Import(GetPath(Path), nextKey);

            return _index++;
        }

        protected override Dictionary<string, SimpleImageModel> GetResultInternal()
        {
            return _result;
        }

        protected override void OnImportStart()
        {
            _keys.AddRange(GetKeys(GetPath(Path)));
            _index = 0;
            _logger.LogInformation($"Starting import of images: {_keys.Count} images");
        }

        private List<string> GetKeys(string path)
        {
            return Directory.GetFiles(path, "*_VGA.png")
                .Select(System.IO.Path.GetFileNameWithoutExtension)
                .Select(x => x.Replace("_VGA", ""))
                .ToList();
        }

        private SimpleImageModel Import(string path, string filename)
        {
            var model = new SimpleImageModel();
            model.Key = filename;
            model.Metadata = _imageImporter.ReadMetadata(path, filename, "metadata");
            model.Image = _imageImporter.ReadImage(path, filename, "image");
            if (File.Exists(System.IO.Path.Combine(path, $"{filename}_sprites.json")))
            {
                model.SpriteSheet = ReadSpriteSheet(path, filename);
            }
            return model;
        }
        
        private SimpleImageModel.SpriteSheetData ReadSpriteSheet(string path, string key)
        {
            var filePath = System.IO.Path.Combine(path, $"{key}_sprites.json");
            if (!File.Exists(filePath))
            {
                throw new Exception($"Missing JSON file: {key}");
            }

            var json = File.ReadAllText(filePath);
            var data = JsonSerializer.Deserialize<SimpleImageModel.SpriteSheetData>(json);
            if (data == null)
            {
                throw new Exception($"Unparseable JSON file: {key}");
            }
            return data;
        }
        
        private string GetPath(string path)
        {
            return System.IO.Path.Combine(path, "image");
        }
    }
}