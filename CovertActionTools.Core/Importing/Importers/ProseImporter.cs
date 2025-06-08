using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.Core.Importing.Importers
{
    internal class ProseImporter : BaseImporter<Dictionary<string, ProseModel>>
    {
        private readonly ILogger<ProseImporter> _logger;
        
        private Dictionary<string, ProseModel> _result = new Dictionary<string, ProseModel>();
        private bool _done = false;

        public ProseImporter(ILogger<ProseImporter> logger)
        {
            _logger = logger;
        }

        protected override string Message => "Processing prose..";

        public override void SetResult(PackageModel model)
        {
            model.Prose = GetResult();
        }

        protected override bool CheckIfValidForImportInternal(string path)
        {
            if (Directory.GetFiles(path, "PROSE.json").Length == 0)
            {
                return false;
            }

            return true;
        }

        protected override int GetTotalItemCountInPath()
        {
            return Directory.GetFiles(Path, "PROSE.json").Length;
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

        protected override Dictionary<string, ProseModel> GetResultInternal()
        {
            return _result;
        }

        protected override void OnImportStart()
        {
            _done = false;
        }
        
        private Dictionary<string, ProseModel> Import(string path)
        {
            var filePath = System.IO.Path.Combine(path, $"PROSE.json");
            if (!File.Exists(filePath))
            {
                throw new Exception($"Missing JSON file: PROSE.json");
            }

            var rawData = File.ReadAllText(filePath);
            var model = JsonSerializer.Deserialize<Dictionary<string, ProseModel>>(rawData);
            return model ?? throw new Exception("Invalid text model");
        }
    }
}