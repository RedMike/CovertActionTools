using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using CovertActionTools.Core.Conversion;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace CovertActionTools.Core.Importing.Importers
{
    internal class CatalogImporter : BaseImporter<Dictionary<string, CatalogModel>>
    {
        private readonly ILogger<CatalogImporter> _logger;
        private readonly SharedImageImporter _imageImporter;

        private readonly List<string> _keys = new();
        private readonly Dictionary<string, CatalogModel> _result = new Dictionary<string, CatalogModel>();
        
        private int _index = 0;

        public CatalogImporter(ILogger<CatalogImporter> logger, SharedImageImporter imageImporter)
        {
            _logger = logger;
            _imageImporter = imageImporter;
        }

        protected override string Message => "Processing catalogs..";
        protected override bool CheckIfValidForImportInternal(string path)
        {
            if (Directory.GetFiles(path, "*_catalog.json").Length == 0)
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

        protected override Dictionary<string, CatalogModel> GetResultInternal()
        {
            return _result;
        }

        protected override void OnImportStart()
        {
            _keys.AddRange(GetKeys(Path));
            _index = 0;
            _logger.LogInformation($"Starting import of catalogs: {_keys.Count} catalogs");
        }
        
        private List<string> GetKeys(string path)
        {
            return Directory.GetFiles(path, "*_catalog.json")
                .Select(System.IO.Path.GetFileNameWithoutExtension)
                .Select(x => x.Replace("_catalog", ""))
                .ToList();
        }
        
        private CatalogModel Import(string path, string filename)
        {
            var filePath = System.IO.Path.Combine(path, $"{filename}_catalog.json");
            if (!File.Exists(filePath))
            {
                throw new Exception($"Missing JSON file: {filename}");
            }

            var rawData = File.ReadAllText(filePath);
            var extraData = JsonSerializer.Deserialize<CatalogModel.Metadata>(rawData);
            if (extraData == null)
            {
                throw new Exception($"Unable to parse JSON file: {filename}");
            }

            var model = new CatalogModel()
            {
                Key = filename,
                ExtraData = extraData
            };
            foreach (var entry in extraData.Keys)
            {
                var image = ImportImage(path, entry);
                model.Entries[entry] = image;
            }

            return model;
        }

        private SimpleImageModel ImportImage(string path, string filename)
        {
            var model = new SimpleImageModel();
            model.Key = filename;
            model.ExtraData = _imageImporter.ReadMetadata(path, filename, "catalog_img");
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