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
    /// Given a loaded model for Clues, returns multiple assets to save:
    ///   * CLUES.TXT file (legacy)
    /// </summary>
    internal class CluePublisher : BaseExporter<Dictionary<string, ClueModel>>, ILegacyPublisher
    {
        private readonly ILogger<CluePublisher> _logger;
        
        private bool _done = false;

        public CluePublisher(ILogger<CluePublisher> logger)
        {
            _logger = logger;
        }

        protected override string Message => "Processing clues..";

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
        }
        
        private IDictionary<string, byte[]> Export(Dictionary<string, ClueModel> clues)
        {
            var dict = new Dictionary<string, byte[]>()
            {
                ["CLUES.TXT"] = GetLegacyTextData(clues),
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
    }
}