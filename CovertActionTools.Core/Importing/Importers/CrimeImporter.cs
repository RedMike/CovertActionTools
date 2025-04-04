using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.Core.Importing.Importers
{
    internal class CrimeImporter : BaseImporter<Dictionary<int, CrimeModel>>
    {
        private readonly ILogger<CrimeImporter> _logger;
        
        private readonly List<int> _keys = new();
        private readonly Dictionary<int, CrimeModel> _result = new Dictionary<int, CrimeModel>();
        
        private int _index = 0;

        public CrimeImporter(ILogger<CrimeImporter> logger)
        {
            _logger = logger;
        }

        protected override string Message => "Processing crimes..";
        protected override bool CheckIfValidForImportInternal(string path)
        {
            if (Directory.GetFiles(path, "*_crime.json").Length == 0)
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

        protected override Dictionary<int, CrimeModel> GetResultInternal()
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
            return Directory.GetFiles(path, "*_crime.json")
                .Select(System.IO.Path.GetFileNameWithoutExtension)
                .Select(x => int.TryParse(x.Replace("_crime", "").Replace("CRIME", ""), out var index) ? index : -1)
                .Where(x => x >= 0)
                .ToList();
        }
        
        private CrimeModel Import(string path, int key)
        {
            var filePath = System.IO.Path.Combine(path, $"CRIME{key}_crime.json");
            if (!File.Exists(filePath))
            {
                throw new Exception($"Missing JSON file: {key}");
            }

            var rawData = File.ReadAllText(filePath);
            var model = JsonSerializer.Deserialize<CrimeModel>(rawData);
            return model ?? throw new Exception("Invalid crime model");
        }
    }
}