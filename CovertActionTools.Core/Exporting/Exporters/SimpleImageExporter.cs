using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using CovertActionTools.Core.Exporting.Shared;
using CovertActionTools.Core.Importing;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.Core.Exporting.Exporters
{
    /// <summary>
    /// Given a loaded model for a SimpleImage, returns multiple assets to save:
    ///   * PIC file (legacy image)
    ///   * JSON file _image (metadata)
    ///   * PNG file _modern (modern image)
    ///   * PNG file _VGA (VGA legacy image)
    /// </summary>
    internal class SimpleImageExporter : BaseExporter<Dictionary<string, SimpleImageModel>>
    {
        #if DEBUG
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions()
        {
            WriteIndented = true
        };
        #else
        private static readonly JsonSerializerOptions JsonOptions = JsonSerializerOptions.Default;
        #endif
        
        private readonly ILogger<SimpleImageExporter> _logger;
        private readonly SharedImageExporter _imageExporter;
        
        private readonly List<string> _keys = new();
        private int _index = 0;

        public SimpleImageExporter(ILogger<SimpleImageExporter> logger, SharedImageExporter imageExporter)
        {
            _logger = logger;
            _imageExporter = imageExporter;
        }

        protected override string Message => "Processing simple images..";
        
        public override ExportStatus.ExportStage GetStage() => ExportStatus.ExportStage.ProcessingSimpleImages;

        protected override Dictionary<string, SimpleImageModel> GetFromModel(PackageModel model)
        {
            return model.SimpleImages;
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
            foreach (var pair in files)
            {
                var exportPath = Path;
                if (!string.IsNullOrEmpty(PublishPath) || !pair.Key.publish)
                {
                    var publishPath = PublishPath ?? exportPath;
                    File.WriteAllBytes(System.IO.Path.Combine(pair.Key.publish ? publishPath : exportPath, pair.Key.filename), pair.Value);
                }
            }

            return _index++;
        }

        protected override void OnExportStart()
        {
            _keys.AddRange(GetKeys());
            _index = 0;
            _logger.LogInformation($"Starting export of images: {_keys.Count}");
        }
        
        private List<string> GetKeys()
        {
            return Data.Keys.ToList();
        }

        private IDictionary<(string filename, bool publish), byte[]> Export(SimpleImageModel image)
        {
            var dict = new Dictionary<(string filename, bool publish), byte[]>
            {
                [($"{image.Key}_image.json", false)] = _imageExporter.GetMetadata(image),
                [($"{image.Key}_modern.png", false)] = _imageExporter.GetModernImageData(image),
                [($"{image.Key}_VGA.png", false)] = _imageExporter.GetVgaImageData(image),
                [($"{image.Key}.PIC", true)] = _imageExporter.GetLegacyFileData(image) 
            };
            return dict;
        }
    }
}