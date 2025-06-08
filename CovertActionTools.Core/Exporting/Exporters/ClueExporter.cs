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
    ///   * CLUES.TXT file (legacy)
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
            _logger.LogInformation($"Starting export of clues");
        }
        
        private IDictionary<(string filename, bool publish), byte[]> Export(Dictionary<string, ClueModel> clues)
        {
            var dict = new Dictionary<(string filename, bool publish), byte[]>()
            {
                [("CLUES.TXT", true)] = GetLegacyTextData(clues),
                [("CLUES.json", false)] = GetModernTextData(clues),
            };

            return dict;
        }

        private byte[] GetLegacyTextData(Dictionary<string, ClueModel> clues)
        {
            using var memStream = new MemoryStream();
            using var writer = new BinaryWriter(memStream);

            if (clues.Any(x => x.Value.Type == ClueType.Unknown))
            {
                throw new Exception("Attempting to write Unknown clue type");
            }

            //there are two orders:
            //  first, with crime ID null, ordered by clue type, then id
            //  second, with non-null crime ID, ordered by crime ID, then id
            //however duplicate texts get sorted and prefixed onto their last occurrence
            var duplicateTextIds = clues.Values
                .Where(x => clues.Count(t =>
                        t.Value.Message == x.Message && 
                        t.Value.Type == x.Type &&
                        ((t.Value.CrimeId != null && x.CrimeId != null) ||
                         (t.Value.CrimeId == null && x.CrimeId == null))
                    ) > 1
                )
                .GroupBy(x => x.Message)
                .Select(x => x
                            .OrderBy(t => t.CrimeId)
                            .ThenBy(t => t.Id)
                            .ToList())
                .ToDictionary(x => x.Last().GetMessagePrefix(), 
                    x => x.Take(x.Count - 1).Select(t => t.GetMessagePrefix()).ToList());
            
            var orderedClues = clues
                .Values
                .Where(x => x.CrimeId == null)
                .OrderBy(x => x.Type)
                .ThenBy(x => x.Id)
                .Concat(clues.Values
                    .Where(x => x.CrimeId != null)
                    .OrderBy(x => x.CrimeId)
                    .ThenBy(x => x.Id)
                );
            
            foreach (var clue in orderedClues)
            {
                var prefix = clue.GetMessagePrefix();
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
                
                //then we write the flags
                writer.Write($"{clue.Source:D}"[0]);
                if (clue.CrimeId != null)
                {
                    writer.Write($"{clue.Type:D}"[0]);
                }
                
                //now we're writing the actual text
                var message = clue.Message + "\r\n";
                writer.Write(message.ToArray());
            }
            
            writer.Write('*');
            writer.Write(new [] {'\r', '\n', (char)0x1A});

            return memStream.ToArray();
        }

        private byte[] GetModernTextData(Dictionary<string, ClueModel> clues)
        {
            var json = JsonSerializer.Serialize(clues, JsonOptions);
            return Encoding.UTF8.GetBytes(json);
        }
    }
}