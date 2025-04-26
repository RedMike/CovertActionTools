using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.Core.Importing.Importers
{
    internal class AnimationImporter : BaseImporter<Dictionary<string, AnimationModel>>
    {
        private readonly ILogger<AnimationImporter> _logger;
        
        private readonly List<string> _keys = new();
        private readonly Dictionary<string, AnimationModel> _result = new Dictionary<string, AnimationModel>();
        
        private int _index = 0;

        public AnimationImporter(ILogger<AnimationImporter> logger)
        {
            _logger = logger;
        }

        protected override string Message => "Processing animations..";
        protected override bool CheckIfValidForImportInternal(string path)
        {
            if (Directory.GetFiles(path, "*_animation.json").Length == 0)
            {
                return false;
            }

            return true;
        }

        protected override int GetTotalItemCountInPath()
        {
            return GetKeys(Path).Count;
        }

        protected override int RunImportStepInternal()
        {
            var nextKey = _keys[_index];

            _result[nextKey] = Import(Path, nextKey);

            return _index++;
        }

        protected override Dictionary<string, AnimationModel> GetResultInternal()
        {
            return _result;
        }

        protected override void OnImportStart()
        {
            _keys.AddRange(GetKeys(Path));
            _index = 0;
        }
        
        private List<string> GetKeys(string path)
        {
            return Directory.GetFiles(path, "*_animation.json")
                .Select(System.IO.Path.GetFileNameWithoutExtension)
                .Select(x => x.Replace("_animation", ""))
                .ToList();
        }

        private AnimationModel Import(string path, string key)
        {
            
            var filePath = System.IO.Path.Combine(path, $"{key}_animation.json");
            if (!File.Exists(filePath))
            {
                throw new Exception($"Missing JSON file: {key}");
            }

            var rawData = File.ReadAllText(filePath);
            var model = JsonSerializer.Deserialize<AnimationModel>(rawData);
            return model ?? throw new Exception("Invalid animation model");
        }
    }
}