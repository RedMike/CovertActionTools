using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using CovertActionTools.Core.Importing;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.Core.Exporting.Exporters
{
    /// <summary>
    /// Given a loaded model for Clues, returns multiple assets to save:
    ///   * CLUES.json file (modern + metadata)
    /// </summary>
    internal class ClueExporter : BaseExporter<Dictionary<string, ClueModel>>
    {
        
#if DEBUG
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions()
        {
            WriteIndented = true
        };
#else
        private static readonly JsonSerializerOptions JsonOptions = JsonSerializerOptions.Default;
#endif

        private readonly ILogger<ClueExporter> _logger;
        
        private bool _done = false;

        public ClueExporter(ILogger<ClueExporter> logger)
        {
            _logger = logger;
        }

        protected override string Message => "Processing clues..";
        public override ExportStatus.ExportStage GetStage() => ExportStatus.ExportStage.ProcessingClues;

        protected override Dictionary<string, ClueModel> GetFromModel(PackageModel model)
        {
            return model.Clues;
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
            _logger.LogInformation($"Starting export of clues");
        }
        
        private IDictionary<string, byte[]> Export(Dictionary<string, ClueModel> clues)
        {
            var dict = new Dictionary<string, byte[]>()
            {
                ["CLUES.json"] = GetModernTextData(clues),
            };

            return dict;
        }

        private byte[] GetModernTextData(Dictionary<string, ClueModel> clues)
        {
            var json = JsonSerializer.Serialize(clues, JsonOptions);
            return Encoding.UTF8.GetBytes(json);
        }
    }
}