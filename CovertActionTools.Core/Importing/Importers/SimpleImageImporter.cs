using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            if (Directory.GetFiles(path, "*_image.json").Length == 0)
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

            _result[nextKey] = Import(Path, nextKey);

            return _index++;
        }

        protected override Dictionary<string, SimpleImageModel> GetResultInternal()
        {
            return _result;
        }

        protected override void OnImportStart()
        {
            _keys.AddRange(GetKeys(Path));
            _index = 0;
            _logger.LogInformation($"Starting import of images: {_keys.Count} images");
        }

        private List<string> GetKeys(string path)
        {
            return Directory.GetFiles(path, "*_image.json")
                .Select(System.IO.Path.GetFileNameWithoutExtension)
                .Select(x => x.Replace("_image", ""))
                .ToList();
        }

        private SimpleImageModel Import(string path, string filename)
        {
            var model = new SimpleImageModel();
            model.Key = filename;
            model.ExtraData = _imageImporter.ReadMetadata(path, filename, "image");
            (model.RawVgaImageData, model.VgaImageData) = _imageImporter.ReadVgaImageData(path, filename, model.ExtraData.LegacyWidth, model.ExtraData.LegacyHeight);
            model.CgaImageData = Array.Empty<byte>();
            if (model.ExtraData.LegacyColorMappings != null)
            {
                var rawCgaImageData = ImageConversion.VgaToCgaTexture(model.ExtraData.LegacyWidth, model.ExtraData.LegacyHeight, model.RawVgaImageData, model.ExtraData.LegacyColorMappings);
                using var skBitmap = SKBitmap.Decode(rawCgaImageData, new SKImageInfo(model.ExtraData.LegacyWidth, model.ExtraData.LegacyHeight, SKColorType.Rgba8888, SKAlphaType.Premul));
                var textureBytes = skBitmap.Bytes.ToArray();
                model.CgaImageData = textureBytes;
            }
            model.ModernImageData = _imageImporter.ReadModernImageData(path, filename, model.ExtraData.Width, model.ExtraData.Height);
            return model;
        }
    }
}