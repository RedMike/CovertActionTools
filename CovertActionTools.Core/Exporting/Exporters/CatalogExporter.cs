using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using CovertActionTools.Core.Exporting.Shared;
using CovertActionTools.Core.Importing;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.Core.Exporting.Exporters
{
    /// <summary>
    /// Given a loaded model for a Catalog, returns multiple assets to save:
    ///   * JSON file _catalog (metadata)
    ///   * multiple PNG files _catalog
    /// </summary>
    internal class CatalogExporter : BaseExporter<Dictionary<string, CatalogModel>>
    {
#if DEBUG
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions()
        {
            WriteIndented = true
        };
#else
        private static readonly JsonSerializerOptions JsonOptions = JsonSerializerOptions.Default;
#endif
        
        private readonly ILogger<CatalogExporter> _logger;
        private readonly SharedImageExporter _imageExporter;
        
        private readonly List<string> _keys = new();
        private int _index = 0;

        public CatalogExporter(ILogger<CatalogExporter> logger, SharedImageExporter imageExporter)
        {
            _logger = logger;
            _imageExporter = imageExporter;
        }

        protected override string Message => "Processing catalogs..";

        protected override Dictionary<string, CatalogModel> GetFromModel(PackageModel model)
        {
            return model.Catalogs;
        }

        protected override void Reset()
        {
            _keys.Clear();
            _index = 0;
        }
        protected override int GetTotalItemCountInPath()
        {
            return _keys.Count;
        }

        protected override int RunExportStepInternal()
        {
            var nextKey = _keys[_index];

            var files = Export(Data[nextKey]);
            var path = GetPath(Path);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            foreach (var pair in files)
            {
                File.WriteAllBytes(System.IO.Path.Combine(path, pair.Key), pair.Value);
            }

            return _index++;
        }

        protected override void OnExportStart()
        {
            _keys.AddRange(GetKeys());
            _index = 0;
        }
        
        private List<string> GetKeys()
        {
            return Data.Keys.ToList();
        }
        
        private IDictionary<string, byte[]> Export(CatalogModel catalog)
        {
            var dict = new Dictionary<string, byte[]>
            {
                [$"{catalog.Key}_catalog.json"] = GetMetadata(catalog),
            };
            foreach (var entry in catalog.ExtraData.Keys)
            {
                var image = catalog.Entries[entry];
                dict.Add($"{image.Key}_catalog_img.json", _imageExporter.GetImageData(image));
                dict.Add($"{image.Key}_modern.png", _imageExporter.GetModernImageData(image));
                dict.Add($"{image.Key}_VGA.png", _imageExporter.GetVgaImageData(image));
            }
            return dict;
        }
        
        private byte[] GetMetadata(CatalogModel catalog)
        {
            var serialisedMetadata = JsonSerializer.Serialize(catalog.ExtraData, JsonOptions);
            var bytes = Encoding.UTF8.GetBytes(serialisedMetadata);
            return bytes;
        }
        
        private string GetPath(string path)
        {
            return System.IO.Path.Combine(path, "catalog");
        }
    }
}