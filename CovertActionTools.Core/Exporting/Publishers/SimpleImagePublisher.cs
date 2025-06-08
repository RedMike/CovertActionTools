using System.Collections.Generic;
using System.IO;
using System.Linq;
using CovertActionTools.Core.Exporting.Shared;
using CovertActionTools.Core.Importing;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.Core.Exporting.Publishers
{
    /// <summary>
    /// Given a loaded model for a SimpleImage, returns multiple assets to save:
    ///   * PIC file (legacy image)
    /// </summary>
    internal class SimpleImagePublisher : BaseExporter<Dictionary<string, SimpleImageModel>>, ILegacyPublisher
    {
        private readonly ILogger<SimpleImagePublisher> _logger;
        private readonly SharedImageExporter _imageExporter;
        
        private readonly List<string> _keys = new();
        private int _index = 0;

        public SimpleImagePublisher(ILogger<SimpleImagePublisher> logger, SharedImageExporter imageExporter)
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
            foreach (var pair in files)
            {
                File.WriteAllBytes(System.IO.Path.Combine(Path, pair.Key), pair.Value);
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
                [$"{image.Key}.PIC"] = _imageExporter.GetLegacyFileData(image) 
            };
            return dict;
        }
    }
}