using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using CovertActionTools.Core.Importing.Shared;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.Core.Importing.Importers
{
    internal class FontsImporter : BaseImporter<FontsModel>
    {
        private readonly ILogger<SimpleImageImporter> _logger;
        private readonly SharedImageImporter _imageImporter;
        
        private FontsModel _result = new FontsModel();
        private bool _done = false;

        public FontsImporter(ILogger<SimpleImageImporter> logger, SharedImageImporter imageImporter)
        {
            _logger = logger;
            _imageImporter = imageImporter;
        }

        protected override string Message => "Processing fonts..";

        public override void SetResult(PackageModel model)
        {
            model.Fonts = GetResult();
        }

        protected override bool CheckIfValidForImportInternal(string path)
        {
            if (Directory.GetFiles(GetPath(path), "FONTS.json").Length == 0)
            {
                return false;
            }

            return true;
        }

        protected override int GetTotalItemCountInPath()
        {
            return Directory.GetFiles(GetPath(Path), "FONTS.json").Length;
        }

        protected override int RunImportStepInternal()
        {
            if (_done)
            {
                return 1;
            }

            _result = Import(GetPath(Path));
            _done = true;
            return 1;
        }

        protected override FontsModel GetResultInternal()
        {
            return _result;
        }

        protected override void OnImportStart()
        {
            _done = false;
        }
        
        private FontsModel Import(string path)
        {
            var filePath = System.IO.Path.Combine(path, $"FONTS.json");
            if (!File.Exists(filePath))
            {
                throw new Exception($"Missing JSON file: FONTS.json");
            }

            var rawData = File.ReadAllText(filePath);
            var metadata = JsonSerializer.Deserialize<FontsModel.FontData>(rawData);
            if (metadata == null)
            {
                throw new Exception($"Unable to parse JSON");
            }

            var model = new FontsModel()
            {
                Data = metadata
            };

            foreach (var fontId in model.Data.Fonts.Keys)
            {
                var fontMetadata = model.Data.Fonts[fontId];
                var firstAsciiCode = fontMetadata.FirstAsciiValue;
                var lastAsciiCode = fontMetadata.LastAsciiValue;
                var fontImages = new Dictionary<char, SharedImageModel>(); 
                for (var b = firstAsciiCode; b <= lastAsciiCode; b++)
                {
                    var filename = $"FONTS_{fontId}_{b}";
                    var fontImage = _imageImporter.ReadImage(path, filename, "VGA_metadata");

                    fontImages[(char)b] = fontImage;
                }
                
                model.Fonts.Add(new FontsModel.Font()
                {
                    CharacterImages = fontImages
                });
            }

            return model;
        }
        
        private string GetPath(string path)
        {
            return System.IO.Path.Combine(path, "font");
        }
    }
}