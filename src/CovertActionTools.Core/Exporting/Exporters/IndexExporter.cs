using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using CovertActionTools.Core.Importing;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.Core.Exporting.Exporters
{
    internal class IndexExporter : BaseExporter<PackageIndex>
    {
#if DEBUG
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions()
        {
            WriteIndented = true
        };
#else
        private static readonly JsonSerializerOptions JsonOptions = JsonSerializerOptions.Default;
#endif
        
        private readonly ILogger<IndexExporter> _logger;
        
        private bool _done = false;

        public IndexExporter(ILogger<IndexExporter> logger)
        {
            _logger = logger;
        }

        protected override string Message => "Processing index..";
        protected override PackageIndex GetFromModel(PackageModel model)
        {
            return model.Index;
        }

        protected override void Reset()
        {
            _done = false;
        }

        protected override int GetTotalItemCountInPath()
        {
            return 1;
        }

        protected override int RunExportStepInternal()
        {
            if (_done)
            {
                return 1;
            }
            var files = Export(Data);
            foreach (var pair in files)
            {
                File.WriteAllBytes(System.IO.Path.Combine(Path, pair.Key), pair.Value);
            }

            _done = true;
            return 1;
        }

        protected override void OnExportStart()
        {
            _done = false;
        }
        
        private IDictionary<string, byte[]> Export(PackageIndex index)
        {
            var dict = new Dictionary<string, byte[]>()
            {
                ["index.json"] = GetIndexData(index),
            };

            return dict;
        }

        private byte[] GetIndexData(PackageIndex index)
        {
            var json = JsonSerializer.Serialize(index, JsonOptions);
            return Encoding.UTF8.GetBytes(json);
        }
    }
}