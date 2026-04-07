using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CovertActionTools.Core.Compression;
using CovertActionTools.Core.Models;
using CovertActionTools.Core.Models.Executables;
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
            var exeName = System.IO.Path.GetFileNameWithoutExtension(filePath).ToUpperInvariant();
            _logger.LogDebug("Parsing executable: {Name}", exeName);

            var model = ParseExecutable(filePath);
            _result[exeName] = model;

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

            // The decompressor's DeadZoneBoundary (output write position) may be inflated
            // by EXEPACK paragraph rounding. PackedDeadZoneSize (input read position) gives
            // the actual dead zone size. When they differ, shift the decompressed payload
            // left so code+data starts at the correct position relative to dsOffset.
            var deadZoneBoundary = decompResult.PackedDeadZoneSize;
            var shift = decompResult.DeadZoneBoundary - deadZoneBoundary;
            if (shift > 0)
            {
                Array.Copy(decompResult.Data, decompResult.DeadZoneBoundary,
                           decompResult.Data, deadZoneBoundary,
                           decompResult.Data.Length - decompResult.DeadZoneBoundary);
            }
            if (deadZoneBoundary > 0)
            {
                Array.Copy(exepackHeader.PackedData, 0, decompResult.Data, 0, deadZoneBoundary);
                _logger.LogDebug("Dead zone: {Size} bytes copied from packed data (shift={Shift})", deadZoneBoundary, shift);
            }

            // Split into dead zone and full payload (code + data)
            var deadZone = new byte[deadZoneBoundary];
            Array.Copy(decompResult.Data, 0, deadZone, 0, deadZoneBoundary);

            // The remaining bytes after the dead zone contain code segment + data segment
            var fullPayload = decompResult.Data;
            var name = System.IO.Path.GetFileNameWithoutExtension(filePath).ToUpperInvariant();

            // Determine DS boundary to split code and data segments
            var dsParagraph = GetDsParagraph(name);
            var dsOffset = dsParagraph * 16; // absolute offset in full payload

            var codeSegmentLength = dsOffset - deadZoneBoundary;
            var codeSegment = new byte[codeSegmentLength];
            Array.Copy(fullPayload, deadZoneBoundary, codeSegment, 0, codeSegmentLength);

            // Compute actual payload length (dead zone + raw decompressed bytes) to
            // exclude paragraph-rounding padding from the data segment.
            var rawPayloadLength = decompResult.Data.Length - decompResult.DeadZoneBoundary;
            var actualPayloadLength = deadZoneBoundary + rawPayloadLength;
            var dataSegmentLength = actualPayloadLength - dsOffset;
            var dataSegmentBytes = new byte[dataSegmentLength];
            Array.Copy(fullPayload, dsOffset, dataSegmentBytes, 0, dataSegmentLength);

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
                "Parsed {ExeName}: code={CodeSize}, data={DataSize}, deadZone={DeadZone}, relocations={RelocCount}",
                name, codeSegment.Length, dataSegmentBytes.Length, deadZoneBoundary,
                relocations.Length / 2);

            var model = new ExecutableModel
            {
                DeadZone = deadZone,
                CodeSegment = codeSegment,
                OriginalMzHeader = originalMzHeader,
                ExepackStub = stub,
                Relocations = relocations,
                EntryCS = exepackHeader.RealCS,
                EntryIP = exepackHeader.RealIP,
                StackSS = exepackHeader.RealSS,
                StackSP = exepackHeader.RealSP
            };

            // Parse data segment into per-EXE structured model
            switch (name)
            {
                case "TAC":
                    model.TacData = TacDataSegment.FromBytes(dataSegmentBytes);
                    break;
                case "FINAL":
                    model.FinalData = FinalDataSegment.FromBytes(dataSegmentBytes);
                    break;
                case "GAME":
                    model.GameData = GameDataSegment.FromBytes(dataSegmentBytes);
                    break;
                case "BUG":
                    model.BugData = BugDataSegment.FromBytes(dataSegmentBytes);
                    break;
                case "CHASE":
                    model.ChaseData = ChaseDataSegment.FromBytes(dataSegmentBytes);
                    break;
                case "CODE":
                    model.CodeData = CodeExeDataSegment.FromBytes(dataSegmentBytes);
                    break;
            }

            return model;
        }

        private static int GetDsParagraph(string exeName)
        {
            switch (exeName)
            {
                case "TAC": return TacDataSegment.DsParagraph;
                case "FINAL": return FinalDataSegment.DsParagraph;
                case "GAME": return GameDataSegment.DsParagraph;
                case "BUG": return BugDataSegment.DsParagraph;
                case "CHASE": return ChaseDataSegment.DsParagraph;
                case "CODE": return CodeExeDataSegment.DsParagraph;
                default: throw new InvalidOperationException($"Unknown executable: {exeName}");
            }
        }

        private static string[] GetMatchingFiles(string path)
        {
            return KnownExecutables
                .Select(n => System.IO.Path.Combine(path, $"{n}.EXE"))
                .Where(File.Exists)
                .ToArray();
        }
    }
}
