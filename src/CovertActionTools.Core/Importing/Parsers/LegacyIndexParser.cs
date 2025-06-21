using System;
using System.IO;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.Core.Importing.Parsers
{
    internal class LegacyIndexParser : BaseImporter<PackageIndex>, ILegacyParser
    {
        private readonly ILogger<LegacyIndexParser> _logger;
        
        private PackageIndex _result = new PackageIndex();
        private bool _done = false;

        public LegacyIndexParser(ILogger<LegacyIndexParser> logger)
        {
            _logger = logger;
        }
        
        protected override string Message => "Processing index..";

        public override void SetResult(PackageModel model)
        {
            model.Index = _result;
        }

        protected override bool CheckIfValidForImportInternal(string path)
        {
            if (Directory.GetFiles(path, "COVERT.EXE").Length == 0)
            {
                return false;
            }

            return true;
        }

        protected override int GetTotalItemCountInPath()
        {
            return Directory.GetFiles(Path, "COVERT.EXE").Length;
        }

        protected override int RunImportStepInternal()
        {
            if (_done)
            {
                return 1;
            }

            _result = Parse(Path);
            _done = true;
            return 1;
        }

        protected override PackageIndex GetResultInternal()
        {
            return _result;
        }

        protected override void OnImportStart()
        {
            _done = false;
        }

        private PackageIndex Parse(string path)
        {
            var filePath = System.IO.Path.Combine(path, $"COVERT.EXE");
            if (!File.Exists(filePath))
            {
                throw new Exception($"Missing file: COVERT.EXE");
            }

            return new PackageIndex()
            {
                FormatVersion = Constants.CurrentFormatVersion,
                PackageVersion = new Version(1, 0, 0),
                Metadata = new SharedMetadata()
                {
                    Name = $"Import {DateTime.UtcNow:u}",
                    Comment = "Legacy import"
                }
            };
        }
    }
}