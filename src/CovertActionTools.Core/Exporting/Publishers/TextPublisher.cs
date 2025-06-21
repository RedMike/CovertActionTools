using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CovertActionTools.Core.Importing;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.Core.Exporting.Publishers
{
    /// <summary>
    /// Given a loaded model for Texts, returns multiple assets to save:
    ///   * TEXT.DTA file (legacy)
    /// </summary>
    internal class TextPublisher : BaseExporter<Dictionary<string, TextModel>>, ILegacyPublisher
    {
        private readonly ILogger<TextPublisher> _logger;
        
        private bool _done = false;

        public TextPublisher(ILogger<TextPublisher> logger)
        {
            _logger = logger;
        }

        protected override string Message => "Processing texts..";

        protected override Dictionary<string, TextModel> GetFromModel(PackageModel model)
        {
            return model.Index.TextIncluded ? model.Texts : new();
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

        private IDictionary<string, byte[]> Export(Dictionary<string, TextModel> texts)
        {
            var dict = new Dictionary<string, byte[]>()
            {
                ["TEXT.DTA"] = GetLegacyTextData(texts)
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
    }
}