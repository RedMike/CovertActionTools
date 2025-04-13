using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.Core.Importing.Parsers
{
    public class LegacyCatalogParser : BaseImporter<Dictionary<string, CatalogModel>>
    {
        private readonly ILogger<LegacyCatalogParser> _logger;
        private readonly SharedImageParser _imageParser;
        
        private readonly List<string> _keys = new();
        private readonly Dictionary<string, CatalogModel> _result = new Dictionary<string, CatalogModel>();
        
        private int _index = 0;

        public LegacyCatalogParser(ILogger<LegacyCatalogParser> logger, SharedImageParser imageParser)
        {
            _logger = logger;
            _imageParser = imageParser;
        }

        protected override string Message => "Processing catalogs..";
        protected override bool CheckIfValidForImportInternal(string path)
        {
            if (Directory.GetFiles(path, "*.CAT").Length == 0)
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

        protected override Dictionary<string, CatalogModel> GetResultInternal()
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
            return Directory.GetFiles(path, "*.CAT")
                .Select(System.IO.Path.GetFileNameWithoutExtension)
                .ToList();
        }

        private CatalogModel Parse(string path, string key)
        {
            var filePath = System.IO.Path.Combine(path, $"{key}.CAT");
            if (!File.Exists(filePath))
            {
                throw new Exception($"Missing CAT file: {key}");
            }

            var rawData = File.ReadAllBytes(filePath);
            
            using var memStream = new MemoryStream(rawData);
            using var reader = new BinaryReader(memStream);

            var entryCount = reader.ReadUInt16();
            var offsetsAndLengths = new Dictionary<string, (uint offset, uint length)>(); 
            for (var i = 0; i < entryCount; i++)
            {
                var entryName = "";
                for (var j = 0; j < 12; j++)
                {
                    var ch = (char)reader.ReadByte();
                    entryName += ch;
                }
                entryName = entryName.Trim().Trim('\0');

                reader.ReadUInt32(); //checksum, not used

                var length = reader.ReadUInt32();
                var offset = reader.ReadUInt32();
                offsetsAndLengths[entryName] = (offset, length);
            }
            
            var entries = new Dictionary<string, SimpleImageModel>();
            foreach (var pair in offsetsAndLengths)
            {
                memStream.Position = pair.Value.offset;
                var rawImageData = reader.ReadBytes((int)pair.Value.length);
                var imageModel = _imageParser.Parse(pair.Key, rawImageData);
                entries[pair.Key] = imageModel;
            }

            return new CatalogModel()
            {
                Entries = entries,
                ExtraData = new CatalogModel.Metadata()
                {
                    Name = key,
                    Comment = "Legacy import"
                }
            };
        }
    }
}