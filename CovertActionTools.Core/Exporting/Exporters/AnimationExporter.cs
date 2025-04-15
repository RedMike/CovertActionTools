using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using CovertActionTools.Core.Importing;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.Core.Exporting.Exporters
{
    /// <summary>
    /// Given a loaded model for an Animation, returns multiple assets to save:
    ///   * JSON file _animation (metadata)
    ///   * multiple PNG files _animation_X
    /// </summary>
    internal class AnimationExporter : BaseExporter<Dictionary<string, AnimationModel>>
    {
#if DEBUG
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions()
        {
            WriteIndented = true
        };
#else
        private static readonly JsonSerializerOptions JsonOptions = JsonSerializerOptions.Default;
#endif
        
        private readonly ILogger<AnimationExporter> _logger;
        private readonly SharedImageExporter _imageExporter;
        
        private readonly List<string> _keys = new();
        private int _index = 0;

        public AnimationExporter(ILogger<AnimationExporter> logger, SharedImageExporter imageExporter)
        {
            _logger = logger;
            _imageExporter = imageExporter;
        }

        protected override string Message => "Processing animations..";
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
            _logger.LogInformation($"Starting export of animations: {_keys.Count}");
        }
        
        private List<string> GetKeys()
        {
            return Data.Keys.ToList();
        }

        private IDictionary<(string filename, bool publish), byte[]> Export(AnimationModel animation)
        {
            var dict = new Dictionary<(string filename, bool publish), byte[]>()
            {
                [($"{animation.Key}_animation.json", false)] = GetMetadata(animation),
            };
            foreach (var key in animation.Images.Keys)
            {
                dict.Add(($"{animation.Key}_animation_{key}.png", false), _imageExporter.GetModernImageData(animation.Images[key]));    
            }
            return dict;
        }
        
        private byte[] GetMetadata(AnimationModel animation)
        {
            var serialisedMetadata = JsonSerializer.Serialize(animation.ExtraData, JsonOptions);
            var bytes = Encoding.UTF8.GetBytes(serialisedMetadata);
            return bytes;
        }
    }
}