using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.Core.Importing.Importers
{
    public interface ITextImporter
    {
        Dictionary<string, TextModel> Import(string path, string filename);
    }
    
    internal class TextImporter : ITextImporter
    {
        private readonly ILogger<TextImporter> _logger;

        public TextImporter(ILogger<TextImporter> logger)
        {
            _logger = logger;
        }

        public Dictionary<string, TextModel> Import(string path, string filename)
        {
            var filePath = Path.Combine(path, $"{filename}.json");
            if (!File.Exists(filePath))
            {
                throw new Exception($"Missing JSON file: {filename}");
            }

            var rawData = File.ReadAllText(filePath);
            var model = JsonSerializer.Deserialize<Dictionary<string, TextModel>>(rawData);
            return model ?? throw new Exception("Invalid text model");
        }
    }
}