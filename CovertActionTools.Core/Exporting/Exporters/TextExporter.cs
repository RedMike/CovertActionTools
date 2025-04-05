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
                [$"TEXT.DTA"] = GetLegacyTextData(texts),
                [$"TEXT.json"] = GetModernTextData(texts),
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

            //the texts are written in order of prefix and then ID
            //TODO: find out if the ordering matters
            // var textsByPrefix = texts
            //     .Values
            //     .GroupBy(t => t.Type)
            //     .OrderBy(x => x.Key);
            // foreach (var group in textsByPrefix)
            // {
            //     var type = group.Key;
            //     IEnumerable<TextModel> orderedTexts;
            //     if (type == TextModel.StringType.CrimeMessage)
            //     {
            //         //crime messages are in order of crime IDs first
            //         orderedTexts = group
            //             .OrderBy(x => x.CrimeId)
            //             .ThenBy(x => x.Id);
            //     }
            //     else
            //     {
            //         orderedTexts = group
            //             .OrderBy(x => x.Id);
            //     }
            //
            //     foreach (var text in orderedTexts)
            //     {
            //         var prefix = text.GetMessagePrefix();
            //         var message = "\r\n" + text.Message + "\r\n"; 
            //                             
            //         writer.Write('*');
            //         writer.Write(prefix.ToArray());
            //         writer.Write(message.ToArray());
            //     }
            // }
            var orderedTexts = texts.Values.OrderBy(x => x.Order).ToList();
            for (var i = 0; i < orderedTexts.Count; i++)
            {
                var text = orderedTexts[i];
                var prefix = text.GetMessagePrefix();
                writer.Write('*');
                writer.Write(prefix.ToArray());
                if (i < orderedTexts.Count - 1 && orderedTexts[i + 1].Message == text.Message)
                {
                    //we instead print just one \r\n
                    writer.Write('\r');
                    writer.Write('\n');
                    continue;
                }

                var message = "\r\n" + text.Message + "\r\n";
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