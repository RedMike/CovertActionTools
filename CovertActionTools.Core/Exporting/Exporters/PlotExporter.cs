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
    /// Given a loaded model for Plots, returns multiple assets to save:
    ///   * PLOT.TXT file (legacy)
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
            _logger.LogInformation($"Starting export of plots");
        }
        
        private IDictionary<string, byte[]> Export(Dictionary<string, PlotModel> plots)
        {
            var dict = new Dictionary<string, byte[]>()
            {
                [$"PLOT.TXT"] = GetLegacyPlotData(plots),
                [$"PLOT.json"] = GetModernPlotData(plots),
            };

            return dict;
        }
        
        private byte[] GetLegacyPlotData(Dictionary<string, PlotModel> plots)
        {
            using var memStream = new MemoryStream();
            using var writer = new BinaryWriter(memStream);

            if (plots.Any(x => x.Value.StringType == PlotModel.PlotStringType.Unknown))
            {
                throw new Exception("Attempting to write Unknown plot type");
            }

            //the plots are written in order of mission set, then type (first briefing), then crime index, then number
            //however duplicate texts get sorted and prefixed onto their last occurrence
            var duplicateTextIds = plots.Values
                .Where(x => plots.Count(t => 
                        t.Value.Message == x.Message
                    ) > 1
                )
                .GroupBy(x => x.Message)
                .Select(x =>  x
                    .OrderBy(t => t.MissionSetId)
                    .ThenBy(t => t.StringType == PlotModel.PlotStringType.Briefing ? int.MinValue : (int)t.StringType)
                    .ThenBy(t => t.CrimeIndex)
                    .ThenBy(t => t.MessageNumber)
                    .ToList()
                )
                .ToDictionary(x => x.Last().GetMessagePrefix(), 
                    x => x.Take(x.Count - 1).Select(t => t.GetMessagePrefix()).ToList());

            var orderedTexts = plots
                .Values
                .OrderBy(t => t.MissionSetId)
                .ThenBy(t => t.StringType == PlotModel.PlotStringType.Briefing ? int.MinValue : (int)t.StringType)
                .ThenBy(t => t.CrimeIndex)
                .ThenBy(t => t.MessageNumber);
            foreach (var text in orderedTexts)
            {
                var prefix = text.GetMessagePrefix();
                if (duplicateTextIds.Values.Any(x => x.Contains(prefix)))
                {
                    //it's a duplicate text so it'll be handled when we reach the key
                    continue;
                }
                writer.Write('*');
                writer.Write(prefix.ToArray());
                if (duplicateTextIds.TryGetValue(prefix, out var duplicates))
                {
                    writer.Write('\r');
                    writer.Write('\n');
                    //it's a text that has duplicates, so write those first
                    foreach (var duplicate in duplicates)
                    {
                        writer.Write('*');
                        writer.Write(duplicate.ToArray());
                        writer.Write('\r');
                        writer.Write('\n');
                    }
                }
                else
                {
                    writer.Write('\r');
                    writer.Write('\n');
                }
                
                //now we're writing the actual text
                var message = text.Message + "\r\n";
                writer.Write(message.ToArray());
            }

            //unlike the others, it has no end asterisk
            writer.Write(new [] {'\r', '\n', (char)0x1A});

            return memStream.ToArray();
        }

        private byte[] GetModernPlotData(Dictionary<string, PlotModel> plots)
        {
            var json = JsonSerializer.Serialize(plots, JsonOptions);
            return Encoding.UTF8.GetBytes(json);
        }
    }
}