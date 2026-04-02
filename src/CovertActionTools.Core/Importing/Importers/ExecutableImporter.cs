using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.Core.Importing.Importers
{
    internal class ExecutableImporter : BaseImporter<Dictionary<string, ExecutableModel>>
    {
        private readonly ILogger<ExecutableImporter> _logger;

        private Dictionary<string, ExecutableModel> _result = new Dictionary<string, ExecutableModel>();
        private bool _done = false;

        public ExecutableImporter(ILogger<ExecutableImporter> logger)
        {
            _logger = logger;
        }

        protected override string Message => "Processing executables..";

        public override void SetResult(PackageModel model)
        {
            model.Executables = GetResult();
        }

        protected override bool CheckIfValidForImportInternal(string path)
        {
            return Directory.GetFiles(path, "EXECUTABLES.json").Length > 0;
        }

        protected override int GetTotalItemCountInPath()
        {
            return Directory.GetFiles(Path, "EXECUTABLES.json").Length;
        }

        protected override int RunImportStepInternal()
        {
            if (_done)
            {
                return 1;
            }

            var filePath = System.IO.Path.Combine(Path, "EXECUTABLES.json");
            if (!File.Exists(filePath))
            {
                throw new Exception("Missing JSON file: EXECUTABLES.json");
            }

            var rawData = File.ReadAllText(filePath);
            var model = JsonSerializer.Deserialize<Dictionary<string, ExecutableModel>>(rawData);
            _result = model ?? throw new Exception("Invalid executables model");
            _done = true;
            return 1;
        }

        protected override Dictionary<string, ExecutableModel> GetResultInternal()
        {
            return _result;
        }

        protected override void OnImportStart()
        {
            _done = false;
        }
    }
}
