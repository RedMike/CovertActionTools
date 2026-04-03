using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CovertActionTools.Core.Compression;
using CovertActionTools.Core.Importing;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.Core.Exporting.Publishers
{
    internal class ExecutablePublisher : BaseExporter<Dictionary<string, ExecutableModel>>, ILegacyPublisher
    {
        private readonly ILogger<ExecutablePublisher> _logger;
        private readonly IExepackCompression _compression;

        private List<KeyValuePair<string, ExecutableModel>> _itemList = new List<KeyValuePair<string, ExecutableModel>>();
        private int _currentIndex = -1;

        public ExecutablePublisher(ILogger<ExecutablePublisher> logger, IExepackCompression compression)
        {
            _logger = logger;
            _compression = compression;
        }

        protected override string Message => "Processing executables..";

        protected override Dictionary<string, ExecutableModel> GetFromModel(PackageModel model)
        {
            if (model.Index.ExecutableIncluded.Count == 0)
            {
                return new Dictionary<string, ExecutableModel>();
            }

            return model.Executables
                .Where(x => model.Index.ExecutableIncluded.Contains(x.Key))
                .ToDictionary(x => x.Key, x => x.Value);
        }

        protected override void Reset()
        {
            _itemList = new List<KeyValuePair<string, ExecutableModel>>();
            _currentIndex = -1;
        }

        protected override int GetTotalItemCountInPath()
        {
            _itemList = Data.ToList();
            return _itemList.Count;
        }

        protected override int RunExportStepInternal()
        {
            _currentIndex++;
            if (_currentIndex >= _itemList.Count)
            {
                return _itemList.Count;
            }

            var entry = _itemList[_currentIndex];
            var name = entry.Key;
            var model = entry.Value;

            _logger.LogDebug("Publishing executable: {Name}", name);

            var packedExe = BuildPackedExecutable(model);
            var outputPath = System.IO.Path.Combine(Path, $"{name}.EXE");
            File.WriteAllBytes(outputPath, packedExe);

            _logger.LogDebug("Written: {Path} ({Size} bytes)", outputPath, packedExe.Length);

            return _currentIndex;
        }

        protected override void OnExportStart()
        {
            _currentIndex = -1;
        }

        private byte[] BuildPackedExecutable(ExecutableModel model)
        {
            // Reconstruct the raw payload: code segment + data segment bytes
            var dataSegmentBytes = model.GetDataSegmentBytes();
            var rawPayload = new byte[model.CodeSegment.Length + dataSegmentBytes.Length];
            Array.Copy(model.CodeSegment, 0, rawPayload, 0, model.CodeSegment.Length);
            Array.Copy(dataSegmentBytes, 0, rawPayload, model.CodeSegment.Length, dataSegmentBytes.Length);

            // Compress the payload (dead zone is excluded, handled separately)
            var compressedPayload = _compression.Compress(rawPayload);

            // Full payload length = dead zone + code segment + data segment (for dest_len calculation)
            var fullPayloadLength = model.DeadZone.Length + rawPayload.Length;

            return ExepackUtilities.BuildPackedExe(
                model.DeadZone,
                compressedPayload,
                model.ExepackStub,
                model.OriginalMzHeader,
                model.Relocations,
                model.EntryCS,
                model.EntryIP,
                model.StackSS,
                model.StackSP,
                fullPayloadLength);
        }
    }
}
