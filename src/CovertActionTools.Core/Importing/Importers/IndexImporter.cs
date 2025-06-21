using System;
using System.IO;
using System.Text.Json;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.Core.Importing.Importers
{
    internal class IndexImporter : BaseImporter<PackageIndex>
    {
        private readonly ILogger<IndexImporter> _logger;
        
        private PackageIndex _result = new PackageIndex();
        private bool _done = false;

        public IndexImporter(ILogger<IndexImporter> logger)
        {
            _logger = logger;
        }


        protected override string Message => "Processing index..";
        public override void SetResult(PackageModel model)
        {
            model.Index = GetResult();
        }

        protected override bool CheckIfValidForImportInternal(string path)
        {
            if (Directory.GetFiles(path, "index.json").Length == 0)
            {
                return false;
            }

            return true;
        }

        protected override int GetTotalItemCountInPath()
        {
            return Directory.GetFiles(Path, "index.json").Length;
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

        protected override PackageIndex GetResultInternal()
        {
            return _result;
        }

        protected override void OnImportStart()
        {
            _done = false;
        }

        private PackageIndex Import(string path)
        {
            var filePath = System.IO.Path.Combine(path, $"index.json");
            if (!File.Exists(filePath))
            {
                throw new Exception($"Missing JSON file: index.json");
            }

            var rawData = File.ReadAllText(filePath);
            var model = JsonSerializer.Deserialize<PackageIndex>(rawData);
            return model ?? throw new Exception("Invalid plot model");
        }
    }
}