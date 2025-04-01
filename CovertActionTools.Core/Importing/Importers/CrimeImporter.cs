using System;
using System.IO;
using System.Text.Json;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.Core.Importing.Importers
{
    public interface ICrimeImporter
    {
        CrimeModel Import(string path, string filename);
    }
    
    internal class CrimeImporter : ICrimeImporter
    {
        private readonly ILogger<CrimeImporter> _logger;

        public CrimeImporter(ILogger<CrimeImporter> logger)
        {
            _logger = logger;
        }

        public CrimeModel Import(string path, string filename)
        {
            var filePath = Path.Combine(path, $"{filename}_crime.json");
            if (!File.Exists(filePath))
            {
                throw new Exception($"Missing JSON file: {filename}");
            }

            var rawData = File.ReadAllText(filePath);
            var model = JsonSerializer.Deserialize<CrimeModel>(rawData);
            return model ?? throw new Exception("Invalid crime model");
        }
    }
}