using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.Core.Importing.Importers
{
    internal class ClueImporter : BaseImporter<Dictionary<string, ClueModel>>
    {
        private readonly ILogger<ClueImporter> _logger;
        
        private Dictionary<string, ClueModel> _result = new Dictionary<string, ClueModel>();
        private bool _done = false;

        public ClueImporter(ILogger<ClueImporter> logger)
        {
            _logger = logger;
        }

        protected override string Message => "Processing clues..";
        protected override bool CheckIfValidForImportInternal(string path)
        {
            if (Directory.GetFiles(path, "CLUES.json").Length == 0)
            {
                return false;
            }

            return true;
        }

        protected override int GetTotalItemCountInPath()
        {
            return Directory.GetFiles(Path, "CLUES.json").Length;
        }

        protected override int RunImportStepInternal()
        {
            if (_done)
            {
                return 1;
            }

            _result = Import(Path);
            _done = true;
            return 1;
        }

        protected override Dictionary<string, ClueModel> GetResultInternal()
        {
            return _result;
        }

        protected override void OnImportStart()
        {
            _done = false;
        }
        
        private Dictionary<string, ClueModel> Import(string path)
        {
            var filePath = System.IO.Path.Combine(path, $"CLUES.json");
            if (!File.Exists(filePath))
            {
                throw new Exception($"Missing JSON file: CLUES.json");
            }

            var rawData = File.ReadAllText(filePath);
            var model = JsonSerializer.Deserialize<Dictionary<string, ClueModel>>(rawData);
            return model ?? throw new Exception("Invalid clue model");
        }
    }
}