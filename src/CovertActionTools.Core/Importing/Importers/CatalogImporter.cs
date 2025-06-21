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
    internal class CatalogImporter : BaseImporter<Dictionary<string, CatalogModel>>
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

        public override void SetResult(PackageModel model)
        {
            model.Catalogs = GetResult();
        }

        protected override bool CheckIfValidForImportInternal(string path)
        {
            if (Directory.GetFiles(GetPath(path), "*_catalog.json").Length == 0)
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

            var catalogPath = System.IO.Path.Combine(GetPath(Path), nextKey);
            _result[nextKey] = Import(catalogPath, nextKey);

            return _index++;
        }

        protected override Dictionary<string, CatalogModel> GetResultInternal()
        {
            return _result;
        }

        protected override void OnImportStart()
        {
            _keys.AddRange(GetKeys(GetPath(Path)));
            _index = 0;
            _logger.LogInformation($"Starting import of catalogs: {_keys.Count} catalogs");
        }
        
        private List<string> GetKeys(string path)
        {
            return Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly)
                .Select(System.IO.Path.GetFileName)
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
        
        private CatalogModel.CatalogData ReadCatalogData(string path, string key)
        {
            var filePath = System.IO.Path.Combine(path, $"{key}_catalog.json");
            if (!File.Exists(filePath))
            {
                throw new Exception($"Missing JSON file: {key}");
            }

            var rawData = File.ReadAllText(filePath);
            
            return JsonSerializer.Deserialize<CatalogModel.CatalogData>(rawData, JsonOptions) ?? throw new Exception("Invalid animation model"); 
        }
        
        private CatalogModel Import(string path, string filename)
        {
            var model = new CatalogModel()
            {
                Key = filename,
                Data = ReadCatalogData(path, filename),
                Metadata = ReadMetadata(path, filename)
            };
            
            var imagePath = System.IO.Path.Combine(path, "images");
            foreach (var entry in model.Data.Keys)
            {
                var image = ImportImage(imagePath, entry);
                model.Entries[entry] = image;
            }

            return model;
        }

        private SharedImageModel ImportImage(string path, string filename)
        {
            return _imageImporter.ReadImage(path, filename, "VGA_metadata");
        }
        
        private string GetPath(string path)
        {
            return System.IO.Path.Combine(path, "catalog");
        }
    }
}