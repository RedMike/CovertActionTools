using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CovertActionTools.Core.Exporting.Shared;
using CovertActionTools.Core.Importing;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.Core.Exporting.Exporters
{
    /// <summary>
    /// Given a loaded model for an Animation, returns multiple assets to save:
    ///   * JSON file _animation (metadata)
    ///   * multiple PNG files _X_VGA
    ///   * multiple JSON files _X_animation_img
    /// </summary>
    internal class AnimationExporter : BaseExporter<Dictionary<string, AnimationModel>>
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

        protected override Dictionary<string, AnimationModel> GetFromModel(PackageModel model)
        {
            return model.Animations;
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

        private IDictionary<string, byte[]> Export(AnimationModel animation)
        {
            var dict = new Dictionary<string, byte[]>()
            {
                [$"{animation.Key}_metadata.json"] = GetMetadata(animation),
                [$"{animation.Key}_global.json"] = GetGlobalData(animation),
                [$"{animation.Key}_instructions.txt"] = GetInstructionData(animation),
                [$"{animation.Key}_steps.txt"] = GetStepData(animation),
            };
            foreach (var key in animation.Images.Keys)
            {
                var image = animation.Images[key];
                dict.Add($"{animation.Key}_{key}_VGA_metadata.json", _imageExporter.GetMetadata(image));
                dict.Add($"{animation.Key}_{key}_VGA.png", _imageExporter.GetVgaImageData(image));
                //TODO: export game-specific color mapping for CGA
            }
            return dict;
        }
        
        private byte[] GetMetadata(AnimationModel animation)
        {
            var data = JsonSerializer.Serialize(animation.Metadata, JsonOptions);
            var bytes = Encoding.UTF8.GetBytes(data);
            return bytes;
        }
        
        private byte[] GetGlobalData(AnimationModel animation)
        {
            var data = JsonSerializer.Serialize(animation.Data, JsonOptions);
            var bytes = Encoding.UTF8.GetBytes(data);
            return bytes;
        }
        
        private byte[] GetInstructionData(AnimationModel animation)
        {
            var data = animation.Control.GetSerialisedInstructions();
            var bytes = Encoding.UTF8.GetBytes(data);
            return bytes;
        }
        
        private byte[] GetStepData(AnimationModel animation)
        {
            var data = animation.Control.GetSerialisedSteps();
            var bytes = Encoding.UTF8.GetBytes(data);
            return bytes;
        }
        
        private string GetPath(string path)
        {
            return System.IO.Path.Combine(path, "animation");
        }
    }
}