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
    ///   * TEXT.DTA file (legacy)
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
                var exportPath = Path;
                if (!string.IsNullOrEmpty(PublishPath) || !pair.Key.publish)
                {
                    var publishPath = PublishPath ?? exportPath;
                    File.WriteAllBytes(System.IO.Path.Combine(pair.Key.publish ? publishPath : exportPath, pair.Key.filename), pair.Value);
                }
            }

            _done = true;
            return 1;
        }

        protected override void OnExportStart()
        {
            _done = false;
            _logger.LogInformation($"Starting export of texts");
        }

        private IDictionary<(string filename, bool publish), byte[]> Export(Dictionary<string, TextModel> texts)
        {
            var dict = new Dictionary<(string filename, bool publish), byte[]>()
            {
                [("TEXT.DTA", true)] = GetLegacyTextData(texts),
                [("TEXT.json", false)] = GetModernTextData(texts),
            };

            return dict;
        }

        private byte[] GetLegacyTextData(Dictionary<string, TextModel> texts)
        {
            using var memStream = new MemoryStream();
            using var writer = new BinaryWriter(memStream);

            if (texts.Any(x => x.Value.Type == TextModel.StringType.Unknown))
            {
                throw new Exception("Attempting to write Unknown text type");
            }

            //the texts are written in order of type, then crime ID, then ID
            //however duplicate texts get sorted and prefixed onto their last occurrence
            var duplicateTextIds = texts.Values
                .Where(x => texts.Count(t => 
                        t.Value.Message == x.Message && 
                        t.Value.Type == x.Type &&
                        (t.Value.Type != TextModel.StringType.CrimeMessage || t.Value.Id == x.Id)
                    ) > 1
                )
                .GroupBy(x => x.Message)
                .Select(x =>  x
                    .OrderBy(t => t.CrimeId)
                    .ThenBy(t => t.Id)
                    .ToList()
                )
                .ToDictionary(x => x.Last().GetMessagePrefix(), 
                    x => x.Take(x.Count - 1).Select(t => t.GetMessagePrefix()).ToList());
            
            var orderedTexts = texts
                .Values
                .OrderBy(x => x.Type)
                .ThenBy(x => x.CrimeId)
                .ThenBy(x => x.Id);
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

            writer.Write('*');
            writer.Write(new [] {'E', 'N', 'D', '\r', '\n', (char)0x1A});

            return memStream.ToArray();
        }

        private byte[] GetModernTextData(Dictionary<string, TextModel> texts)
        {
            var json = JsonSerializer.Serialize(texts, JsonOptions);
            return Encoding.UTF8.GetBytes(json);
        }
    }
}