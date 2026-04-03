using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CovertActionTools.Core.Compression;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.Core.Importing.Parsers
{
    internal class LegacyExecutableParser : BaseImporter<Dictionary<string, ExecutableModel>>, ILegacyParser
    {
        private static readonly string[] KnownExecutables = { "BUG", "CHASE", "CODE", "FINAL", "GAME", "TAC" };

        private readonly ILogger<LegacyExecutableParser> _logger;
        private readonly IExepackDecompression _decompression;

        private Dictionary<string, ExecutableModel> _result = new Dictionary<string, ExecutableModel>();
        private string[] _filesToProcess = Array.Empty<string>();
        private int _currentIndex = -1;

        public LegacyExecutableParser(ILogger<LegacyExecutableParser> logger, IExepackDecompression decompression)
        {
            _logger = logger;
            _decompression = decompression;
        }

        protected override string Message => "Processing executables..";

        public override void SetResult(PackageModel model)
        {
            model.Executables = GetResult();
        }

        protected override bool CheckIfValidForImportInternal(string path)
        {
            return GetMatchingFiles(path).Length > 0;
        }

        protected override int GetTotalItemCountInPath()
        {
            _filesToProcess = GetMatchingFiles(Path);
            return _filesToProcess.Length;
        }

        protected override int RunImportStepInternal()
        {
            _currentIndex++;
            if (_currentIndex >= _filesToProcess.Length)
            {
                return _filesToProcess.Length;
            }

            var filePath = _filesToProcess[_currentIndex];
            var name = System.IO.Path.GetFileNameWithoutExtension(filePath).ToUpperInvariant();
            _logger.LogDebug("Parsing executable: {Name}", name);

            var model = ParseExecutable(filePath);
            _result[name] = model;

            return _currentIndex;
        }

        protected override Dictionary<string, ExecutableModel> GetResultInternal()
        {
            return _result;
        }

        protected override void OnImportStart()
        {
            _result = new Dictionary<string, ExecutableModel>();
            _currentIndex = -1;
        }

        private ExecutableModel ParseExecutable(string filePath)
        {
            var fileData = File.ReadAllBytes(filePath);

            var mzHeader = ExepackUtilities.ParseMzHeader(fileData);
            if (mzHeader == null)
            {
                throw new InvalidOperationException(
                    $"Not a valid MZ executable: {System.IO.Path.GetFileName(filePath)}");
            }

            var exepackHeader = ExepackUtilities.DetectExepack(fileData, mzHeader);
            if (exepackHeader == null)
            {
                throw new InvalidOperationException(
                    $"Not an EXEPACK'd executable: {System.IO.Path.GetFileName(filePath)}");
            }

            // Decompress
            var decompResult = _decompression.Decompress(exepackHeader.PackedData, exepackHeader.DestLen);

            // Copy dead zone bytes from packed data into the decompressed output
            var deadZoneBoundary = decompResult.DeadZoneBoundary;
            if (deadZoneBoundary > 0)
            {
                Array.Copy(exepackHeader.PackedData, 0, decompResult.Data, 0, deadZoneBoundary);
                _logger.LogDebug("Dead zone: {Size} bytes copied from packed data", deadZoneBoundary);
            }

            // Split into dead zone and payload
            var deadZone = new byte[deadZoneBoundary];
            Array.Copy(decompResult.Data, 0, deadZone, 0, deadZoneBoundary);

            var payloadLength = decompResult.Data.Length - deadZoneBoundary;
            var rawPayloadData = new byte[payloadLength];
            Array.Copy(decompResult.Data, deadZoneBoundary, rawPayloadData, 0, payloadLength);

            // Extract relocations
            var relocations = ExepackUtilities.ExtractRelocations(exepackHeader.ExepackSegment);

            // Extract stub
            var stub = ExepackUtilities.ExtractStub(exepackHeader.ExepackSegment);
            if (stub == null)
            {
                throw new InvalidOperationException(
                    $"Could not extract EXEPACK stub from: {System.IO.Path.GetFileName(filePath)}");
            }

            // Preserve original MZ header
            var originalMzHeader = new byte[mzHeader.HeaderSize];
            Array.Copy(fileData, 0, originalMzHeader, 0, mzHeader.HeaderSize);

            _logger.LogDebug(
                "Parsed {Name}: payload={PayloadSize}, deadZone={DeadZone}, relocations={RelocCount}",
                System.IO.Path.GetFileName(filePath), rawPayloadData.Length, deadZoneBoundary,
                relocations.Length / 2);

            return new ExecutableModel
            {
                DeadZone = deadZone,
                RawPayloadData = rawPayloadData,
                OriginalMzHeader = originalMzHeader,
                ExepackStub = stub,
                Relocations = relocations,
                EntryCS = exepackHeader.RealCS,
                EntryIP = exepackHeader.RealIP,
                StackSS = exepackHeader.RealSS,
                StackSP = exepackHeader.RealSP
            };
        }

        private static string[] GetMatchingFiles(string path)
        {
            return KnownExecutables
                .Select(name => System.IO.Path.Combine(path, $"{name}.EXE"))
                .Where(File.Exists)
                .ToArray();
        }
    }
}
