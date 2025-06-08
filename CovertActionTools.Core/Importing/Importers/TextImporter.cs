using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.Core.Importing.Importers
{
    internal class TextImporter : BaseImporter<Dictionary<string, TextModel>>
    {
        private readonly ILogger<TextImporter> _logger;
        
        private Dictionary<string, TextModel> _result = new Dictionary<string, TextModel>();
        private bool _done = false;

        public TextImporter(ILogger<TextImporter> logger)
        {
            _logger = logger;
        }

        protected override string Message => "Processing texts..";

        public override void SetResult(PackageModel model)
        {
            model.Texts = GetResult();
        }

        protected override bool CheckIfValidForImportInternal(string path)
        {
            if (Directory.GetFiles(path, "TEXT.json").Length == 0)
            {
                return false;
            }

            return true;
        }

        protected override int GetTotalItemCountInPath()
        {
            return Directory.GetFiles(Path, "TEXT.json").Length;
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

        protected override Dictionary<string, TextModel> GetResultInternal()
        {
            return _result;
        }

        protected override void OnImportStart()
        {
            _done = false;
        }
        
        private Dictionary<string, TextModel> Import(string path)
        {
            var filePath = System.IO.Path.Combine(path, $"TEXT.json");
            if (!File.Exists(filePath))
            {
                throw new Exception($"Missing JSON file: TEXT.json");
            }

            var rawData = File.ReadAllText(filePath);
            var model = JsonSerializer.Deserialize<Dictionary<string, TextModel>>(rawData);
            return model ?? throw new Exception("Invalid text model");
        }
    }
}