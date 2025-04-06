using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.Core.Importing.Importers
{
    internal class PlotImporter : BaseImporter<Dictionary<string, PlotModel>>
    {
        private readonly ILogger<PlotImporter> _logger;
        
        private Dictionary<string, PlotModel> _result = new Dictionary<string, PlotModel>();
        private bool _done = false;

        public PlotImporter(ILogger<PlotImporter> logger)
        {
            _logger = logger;
        }

        protected override string Message => "Processing plots..";
        protected override bool CheckIfValidForImportInternal(string path)
        {
            if (Directory.GetFiles(path, "PLOT.json").Length == 0)
            {
                return false;
            }

            return true;
        }

        protected override int GetTotalItemCountInPath()
        {
            return Directory.GetFiles(Path, "PLOT.json").Length;
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

        protected override Dictionary<string, PlotModel> GetResultInternal()
        {
            return _result;
        }

        protected override void OnImportStart()
        {
            _done = false;
        }
        
        private Dictionary<string, PlotModel> Import(string path)
        {
            var filePath = System.IO.Path.Combine(path, $"PLOT.json");
            if (!File.Exists(filePath))
            {
                throw new Exception($"Missing JSON file: PLOT.json");
            }

            var rawData = File.ReadAllText(filePath);
            var model = JsonSerializer.Deserialize<Dictionary<string, PlotModel>>(rawData);
            return model ?? throw new Exception("Invalid plot model");
        }
    }
}