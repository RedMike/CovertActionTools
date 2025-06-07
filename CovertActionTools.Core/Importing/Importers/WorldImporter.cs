using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.Core.Importing.Importers
{
    internal class WorldImporter : BaseImporter<Dictionary<int, WorldModel>>
    {
        private readonly ILogger<WorldImporter> _logger;
        
        private readonly List<int> _keys = new();
        private readonly Dictionary<int, WorldModel> _result = new Dictionary<int, WorldModel>();
        
        private int _index = 0;

        public WorldImporter(ILogger<WorldImporter> logger)
        {
            _logger = logger;
        }


        protected override string Message => "Processing worlds..";
        
        public override ImportStatus.ImportStage GetStage() => ImportStatus.ImportStage.ProcessingWorlds;

        public override void SetResult(PackageModel model)
        {
            model.Worlds = GetResult();
        }

        protected override bool CheckIfValidForImportInternal(string path)
        {
            if (Directory.GetFiles(path, "*_world.json").Length == 0)
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

        protected override Dictionary<int, WorldModel> GetResultInternal()
        {
            return _result;
        }

        protected override void OnImportStart()
        {
            _keys.AddRange(GetKeys(Path));
            _index = 0;
        }
        
        private List<int> GetKeys(string path)
        {
            return Directory.GetFiles(path, "*_world.json")
                .Select(System.IO.Path.GetFileNameWithoutExtension)
                .Select(x => int.TryParse(x.Replace("_world", "").Replace("WORLD", ""), out var index) ? index : -1)
                .Where(x => x >= 0)
                .ToList();
        }
        
        private WorldModel Import(string path, int key)
        {
            var filePath = System.IO.Path.Combine(path, $"WORLD{key}_world.json");
            if (!File.Exists(filePath))
            {
                throw new Exception($"Missing JSON file: {key}");
            }

            var rawData = File.ReadAllText(filePath);
            var model = JsonSerializer.Deserialize<WorldModel>(rawData);
            return model ?? throw new Exception("Invalid world model");
        }
    }
}