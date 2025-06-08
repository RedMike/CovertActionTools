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
    /// Given a loaded model for Prose, returns multiple assets to save:
    ///   * PROSE.DTA file (legacy)
    /// </summary>
    internal class ProsePublisher : BaseExporter<Dictionary<string, ProseModel>>, ILegacyPublisher
    {
        private readonly ILogger<ProsePublisher> _logger;
        
        private bool _done = false;

        public ProsePublisher(ILogger<ProsePublisher> logger)
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
                ["PROSE.DTA"] = GetLegacyTextData(prose)
            };

            return dict;
        }

        private byte[] GetLegacyTextData(Dictionary<string, ProseModel> texts)
        {
            using var memStream = new MemoryStream();
            using var writer = new BinaryWriter(memStream);

            if (texts.Any(x => x.Value.Type == ProseModel.ProseType.Unknown))
            {
                throw new Exception("Attempting to write Unknown prose type");
            }

            //the texts are written in order of type, then crime ID, then ID
            //however duplicate texts get sorted and prefixed onto their last occurrence
            var duplicateTextIds = texts.Values
                .Where(x => texts.Count(t => 
                        t.Value.Message == x.Message && 
                        t.Value.Type == x.Type
                    ) > 1
                )
                .GroupBy(x => x.Message)
                .Select(x =>  x
                    .OrderBy(t => t.SecondaryId)
                    .ToList()
                )
                .ToDictionary(x => x.Last().GetMessagePrefix(), 
                    x => x.Take(x.Count - 1).Select(t => t.GetMessagePrefix()).ToList());
            
            var orderedTexts = texts
                .Values
                .OrderBy(x => x.Type)
                .ThenBy(x => x.SecondaryId);
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