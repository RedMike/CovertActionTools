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
    /// Given a loaded model for Texts, returns multiple assets to save:
    ///   * TEXT.json file (modern + metadata)
    /// </summary>
    internal class TextExporter : BaseExporter<Dictionary<string, TextModel>>
    {
#if DEBUG
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions()
        {
            WriteIndented = true
        };
#else
        private static readonly JsonSerializerOptions JsonOptions = JsonSerializerOptions.Default;
#endif

        private readonly ILogger<TextExporter> _logger;
        
        private bool _done = false;

        public TextExporter(ILogger<TextExporter> logger)
        {
            _logger = logger;
        }

        protected override string Message => "Processing texts..";
        
        public override ExportStatus.ExportStage GetStage() => ExportStatus.ExportStage.ProcessingTexts;

        protected override Dictionary<string, TextModel> GetFromModel(PackageModel model)
        {
            return model.Texts;
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
            _logger.LogInformation($"Starting export of texts");
        }

        private IDictionary<string, byte[]> Export(Dictionary<string, TextModel> texts)
        {
            var dict = new Dictionary<string, byte[]>()
            {
                ["TEXT.json"] = GetModernTextData(texts),
            };

            return dict;
        }

        private byte[] GetModernTextData(Dictionary<string, TextModel> texts)
        {
            var json = JsonSerializer.Serialize(texts, JsonOptions);
            return Encoding.UTF8.GetBytes(json);
        }
    }
}