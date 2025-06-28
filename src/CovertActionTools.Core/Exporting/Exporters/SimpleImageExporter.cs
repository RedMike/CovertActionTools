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
    /// Given a loaded model for a SimpleImage, returns multiple assets to save:
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

        private IDictionary<string, byte[]> Export(SimpleImageModel image)
        {
            var dict = new Dictionary<string, byte[]>
            {
                [$"{image.Key}_image.json"] = _imageExporter.GetImageData(image.Image),
                [$"{image.Key}_metadata.json"] = GetMetadata(image),
                [$"{image.Key}_VGA.png"] = _imageExporter.GetVgaImageData(image.Image) 
            };
            if (image.SpriteSheet != null && image.SpriteSheet.Sprites.Count > 0)
            {
                dict[$"{image.Key}_sprites.json"] = GetSpriteSheet(image);
            }
            return dict;
        }
        
        private byte[] GetMetadata(SimpleImageModel image)
        {
            var data = JsonSerializer.Serialize(image.Metadata, JsonOptions);
            var bytes = Encoding.UTF8.GetBytes(data);
            return bytes;
        }
        
        private byte[] GetSpriteSheet(SimpleImageModel image)
        {
            var data = JsonSerializer.Serialize(image.SpriteSheet, JsonOptions);
            var bytes = Encoding.UTF8.GetBytes(data);
            return bytes;
        }
        
        private string GetPath(string path)
        {
            return System.IO.Path.Combine(path, "image");
        }
    }
}