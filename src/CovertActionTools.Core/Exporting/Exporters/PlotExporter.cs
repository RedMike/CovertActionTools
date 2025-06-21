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
    /// Given a loaded model for Plots, returns multiple assets to save:
    ///   * PLOT.json file (modern + metadata)
    /// </summary>
    internal class PlotExporter : BaseExporter<Dictionary<string, PlotModel>>
    {
#if DEBUG
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions()
        {
            WriteIndented = true
        };
#else
        private static readonly JsonSerializerOptions JsonOptions = JsonSerializerOptions.Default;
#endif

        private readonly ILogger<PlotExporter> _logger;
        
        private bool _done = false;

        public PlotExporter(ILogger<PlotExporter> logger)
        {
            _logger = logger;
        }

        protected override string Message => "Processing plots..";

        protected override Dictionary<string, PlotModel> GetFromModel(PackageModel model)
        {
            return model.Plots;
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
        
        private IDictionary<string, byte[]> Export(Dictionary<string, PlotModel> plots)
        {
            var dict = new Dictionary<string, byte[]>()
            {
                ["PLOT.json"] = GetModernPlotData(plots),
            };

            return dict;
        }

        private byte[] GetModernPlotData(Dictionary<string, PlotModel> plots)
        {
            var json = JsonSerializer.Serialize(plots, JsonOptions);
            return Encoding.UTF8.GetBytes(json);
        }
    }
}