using System;
using System.IO;
using System.Text.Json;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.Core.Importing.Importers
{
    public interface ISimpleImageImporter
    {
        SimpleImageModel Import(string path, string filename);
    }
    
    internal class SimpleImageImporter : ISimpleImageImporter
    {
        private readonly ILogger<SimpleImageImporter> _logger;

        public SimpleImageImporter(ILogger<SimpleImageImporter> logger)
        {
            _logger = logger;
        }

        public SimpleImageModel Import(string path, string filename)
        {
            var model = new SimpleImageModel();
            model.Key = filename;
            //TODO: arbitrary sizes?
            model.Width = 320; //legacy width
            model.Height = 200; //legacy height
            model.ExtraData = ReadMetadata(path, filename);
            //TODO: read in raw image data
            //TODO: read in modern image data
            return model;
        }

        private SimpleImageModel.Metadata ReadMetadata(string path, string filename)
        {
            var filePath = Path.Combine(path, $"{filename}.json");
            if (!File.Exists(filePath))
            {
                throw new Exception($"Missing JSON file: {filename}");
            }

            var json = File.ReadAllText(filePath);
            var metadata = JsonSerializer.Deserialize<SimpleImageModel.Metadata>(json);
            if (metadata == null)
            {
                throw new Exception($"Unparseable JSON file: {filename}");
            }
            return metadata;
        }
    }
}