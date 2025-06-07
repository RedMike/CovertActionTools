using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CovertActionTools.Core.Importing.Shared;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.Core.Importing.Parsers
{
    internal class LegacySimpleImageParser : BaseImporter<Dictionary<string, SimpleImageModel>>, ILegacyParser
    {
        private readonly ILogger<LegacySimpleImageParser> _logger;
        private readonly SharedImageParser _imageParser;
        
        private readonly List<string> _keys = new();
        private readonly Dictionary<string, SimpleImageModel> _result = new Dictionary<string, SimpleImageModel>();
        
        private int _index = 0;

        public LegacySimpleImageParser(ILogger<LegacySimpleImageParser> logger, SharedImageParser imageParser)
        {
            _logger = logger;
            _imageParser = imageParser;
        }

        protected override string Message => "Processing simple images..";
        public override ImportStatus.ImportStage GetStage() => ImportStatus.ImportStage.ProcessingSimpleImages;

        public override void SetResult(PackageModel model)
        {
            model.SimpleImages = GetResult();
        }

        protected override bool CheckIfValidForImportInternal(string path)
        {
            if (Directory.GetFiles(path, "*.PIC").Length == 0)
            {
                return false;
            }

            return true;
        }

        protected override int GetTotalItemCountInPath()
        {
            return _keys.Count;
        }

        protected override int RunImportStepInternal()
        {
            var nextKey = _keys[_index];

            _result[nextKey] = Parse(Path, nextKey);

            return _index++;
        }

        protected override Dictionary<string, SimpleImageModel> GetResultInternal()
        {
            return _result;
        }

        protected override void OnImportStart()
        {
            _keys.AddRange(GetKeys(Path));
            _index = 0;
        }
        
        private List<string> GetKeys(string path)
        {
            return Directory.GetFiles(path, "*.PIC")
                .Select(System.IO.Path.GetFileNameWithoutExtension)
                .ToList();
        }

        private SimpleImageModel Parse(string path, string key)
        {
            var filePath = System.IO.Path.Combine(path, $"{key}.PIC");
            if (!File.Exists(filePath))
            {
                throw new Exception($"Missing PIC file: {key}");
            }

            var rawData = File.ReadAllBytes(filePath);
            using var memStream = new MemoryStream(rawData);
            using var reader = new BinaryReader(memStream);
            var image = _imageParser.Parse(key, reader);
            if (memStream.Position < rawData.Length)
            {
                _logger.LogWarning($"Loading image {key} data ended at offset {memStream.Position:X} but length was {rawData.Length:X}");
            }
            return image;
        }
    }
}