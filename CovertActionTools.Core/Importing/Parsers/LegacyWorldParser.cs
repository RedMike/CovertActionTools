using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.Core.Importing.Parsers
{
    internal class LegacyWorldParser : BaseImporter<Dictionary<int, WorldModel>>, ILegacyParser
    {
        private readonly ILogger<LegacyWorldParser> _logger;
        
        private readonly List<int> _keys = new();
        private readonly Dictionary<int, WorldModel> _result = new Dictionary<int, WorldModel>();
        
        private int _index = 0;

        public LegacyWorldParser(ILogger<LegacyWorldParser> logger)
        {
            _logger = logger;
        }

        protected override string Message => "Processing worlds..";

        public override void SetResult(PackageModel model)
        {
            model.Worlds = GetResult();
        }

        protected override bool CheckIfValidForImportInternal(string path)
        {
            if (Directory.GetFiles(path, "WORLD*.DTA").Length == 0)
            {
                return false;
            }

            return true;
        }

        protected override int GetTotalItemCountInPath()
        {
            return GetKeys(Path).Count;
        }

        protected override int RunImportStepInternal()
        {
            var nextKey = _keys[_index];

            _result[nextKey] = Parse(Path, nextKey);

            return _index++;
        }

        protected override Dictionary<int, WorldModel> GetResultInternal()
        {
            return _result;
        }

        protected override void OnImportStart()
        {
            _keys.AddRange(GetKeys(Path));
            _index = 0;
        }
        
        private List<int> GetKeys(string path)
        {
            return Directory.GetFiles(path, "WORLD*.DTA")
                .Select(System.IO.Path.GetFileNameWithoutExtension)
                .Select(x => int.TryParse(x.Replace("WORLD", ""), out var index) ? index : -1)
                .Where(x => x >= 0)
                .ToList();
        }

        private WorldModel Parse(string path, int key)
        {
            var filePath = System.IO.Path.Combine(path, $"WORLD{key}.DTA");
            if (!File.Exists(filePath))
            {
                throw new Exception($"Missing DTA file: {key}");
            }

            var rawData = File.ReadAllBytes(filePath);

            using var memStream = new MemoryStream(rawData);
            using var reader = new BinaryReader(memStream);

            var cityCount = reader.ReadUInt16();
            var orgCount = reader.ReadUInt16();
            var cities = new List<WorldModel.City>(cityCount);
            for (var i = 0; i < cityCount; i++)
            {
                var name = "";
                for (var j = 0; j < 12; j++)
                {
                    var ch = (char)reader.ReadByte();
                    name += ch;
                }
                name = name.Trim().Trim('\0');
                
                var country = "";
                for (var j = 0; j < 12; j++)
                {
                    var ch = (char)reader.ReadByte();
                    country += ch;
                }
                country = country.Trim().Trim('\0');

                var u1 = reader.ReadUInt16();
                var u2 = reader.ReadUInt16();

                var c = reader.ReadUInt16(); 
                if (c != 0)
                {
                    throw new Exception($"Unexpected data 1: {c:X4} {memStream.Position}");
                }
                c = reader.ReadUInt16(); 
                if (c != 0)
                {
                    throw new Exception($"Unexpected data 1: {c:X4} {memStream.Position}");
                }

                var x = reader.ReadByte();
                var y = reader.ReadByte();
                
                cities.Add(new WorldModel.City()
                {
                    Name = name,
                    Country = country,
                    Unknown1 = u1,
                    Unknown2 = u2,
                    MapX = x,
                    MapY = y
                });
            }

            var orgs = new List<WorldModel.Organisation>(orgCount);
            for (var i = 0; i < orgCount; i++)
            {
                var shortName = "";
                for (var j = 0; j < 6; j++)
                {
                    var ch = (char)reader.ReadByte();
                    shortName += ch;
                }
                shortName = shortName.Trim().Trim('\0');
                
                var longName = "";
                for (var j = 0; j < 20; j++)
                {
                    var ch = (char)reader.ReadByte();
                    longName += ch;
                }
                longName = longName.Trim().Trim('\0');

                var u1 = reader.ReadUInt16();
                var u2 = reader.ReadUInt16();
                var u3 = reader.ReadUInt16();

                var uniqueId = reader.ReadUInt16();
                var u4 = reader.ReadUInt16();
                
                orgs.Add(new WorldModel.Organisation()
                {
                    ShortName = shortName,
                    LongName = longName,
                    Unknown1 = u1,
                    Unknown2 = u2,
                    Unknown3 = u3,
                    UniqueId = uniqueId,
                    Unknown4 = u4
                });
            }

            return new WorldModel()
            {
                Id = key,
                Cities = cities,
                Organisations = orgs,
                Metadata = new SharedMetadata()
                {
                    Name = $"WORLD{key}",
                    Comment = "Legacy import"
                }
            };
        }
    }
}