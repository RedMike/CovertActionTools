using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using CovertActionTools.Core.Importing;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.Core.Exporting.Exporters
{
    /// <summary>
    /// Given a loaded model for Prose, returns multiple assets to save:
    ///   * PROSE.DTA file (legacy)
    ///   * PROSE.json file (modern + metadata)
    /// </summary>
    internal class ProseExporter : BaseExporter<Dictionary<string, ProseModel>>
    {
#if DEBUG
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions()
        {
            WriteIndented = true
        };
#else
        private static readonly JsonSerializerOptions JsonOptions = JsonSerializerOptions.Default;
#endif

        private readonly ILogger<ProseExporter> _logger;
        
        private bool _done = false;

        public ProseExporter(ILogger<ProseExporter> logger)
        {
            _logger = logger;
        }

        protected override string Message => "Processing prose..";

        protected override Dictionary<string, ProseModel> GetFromModel(PackageModel model)
        {
            return model.Prose;
        }

        protected override void Reset()
        {
            _done = false;
        }

        protected override int GetTotalItemCountInPath()
        {
            return Data.Count > 0 ? 1 : 0;
        }

        protected override int RunExportStepInternal()
        {
            if (Data.Count == 0 || _done)
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
        
        private IDictionary<string, byte[]> Export(Dictionary<string, ProseModel> prose)
        {
            var dict = new Dictionary<string, byte[]>()
            {
                ["PROSE.json"] = GetModernProseData(prose),
            };

            return dict;
        }
        
        private byte[] GetModernProseData(Dictionary<string, ProseModel> texts)
        {
            var json = JsonSerializer.Serialize(texts, JsonOptions);
            return Encoding.UTF8.GetBytes(json);
        }
    }
}