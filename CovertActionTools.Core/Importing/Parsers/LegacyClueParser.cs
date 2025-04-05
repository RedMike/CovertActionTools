using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.Core.Importing.Parsers
{
    public class LegacyClueParser : BaseImporter<Dictionary<string, ClueModel>>
    {
        private readonly ILogger<LegacyClueParser> _logger;
        
        private Dictionary<string, ClueModel> _result = new Dictionary<string, ClueModel>();
        private bool _done = false;

        public LegacyClueParser(ILogger<LegacyClueParser> logger)
        {
            _logger = logger;
        }

        protected override string Message => "Processing clues..";
        protected override bool CheckIfValidForImportInternal(string path)
        {
            if (Directory.GetFiles(path, "CLUES.TXT").Length == 0)
            {
                return false;
            }

            return true;
        }

        protected override int GetTotalItemCountInPath()
        {
            return Directory.GetFiles(Path, "CLUES.TXT").Length;
        }

        protected override int RunImportStepInternal()
        {
            if (_done)
            {
                return 1;
            }

            _result = Parse(Path);
            _done = true;
            return 1;
        }

        protected override Dictionary<string, ClueModel> GetResultInternal()
        {
            return _result;
        }

        protected override void OnImportStart()
        {
            _done = false;
        }
        
        private Dictionary<string, ClueModel> Parse(string path)
        {
            var filePath = System.IO.Path.Combine(path, $"CLUES.TXT");
            if (!File.Exists(filePath))
            {
                throw new Exception($"Missing JSON file: CLUES.TXT");
            }

            var rawData = File.ReadAllBytes(filePath);
            
            var dict = new Dictionary<string, ClueModel>();
            
            using var memStream = new MemoryStream(rawData);
            using var reader = new BinaryReader(memStream);

            var c = reader.ReadByte();
            if (c != (byte)'*')
            {
                throw new Exception($"Invalid starting marker: {c}");
            }

            List<ClueModel> queuedModels = new(); 
            while (true)
            {
                var prefixBytes = reader.ReadChars(5);
                if (prefixBytes[0] != 'C')
                {
                    if (prefixBytes[0] == '\r' && prefixBytes[1] == '\n' && prefixBytes[2] == (char)0x1A)
                    {
                        //it's the end marker
                        break;
                    }
                    throw new Exception($"Invalid prefix start: {string.Join(" ", prefixBytes)}");
                }
                ClueType type = ClueType.Unknown;
                int id = 0;
                int? crimeId = null;
                int u1 = 0;
                var duplicate = false;
                if (prefixBytes[3] == '\r' && prefixBytes[4] == '\n')
                {
                    //it's a non-crime specific clue
                    //C<clue type><numeric id>\r\n<u1><msg>
                    type = (ClueType)int.Parse($"{prefixBytes[1]}");
                    id = int.Parse($"{prefixBytes[2]}");
                    var next = reader.ReadChar();
                    while (next == '\r' || next == '\n' || next == ' ')
                    {
                        next = reader.ReadChar();
                    }
                    u1 = int.Parse($"{next}");
                }
                else
                {
                    //it's a crime-specific clue
                    //C<crime ID><participant ID>\r\n<u1><clue type><msg>
                    crimeId = int.Parse($"{prefixBytes[1]}{prefixBytes[2]}");
                    id = int.Parse($"{prefixBytes[3]}{prefixBytes[4]}");
                    var next = reader.ReadChar();
                    while (next == '\r' || next == '\n' || next == ' ')
                    {
                        next = reader.ReadChar();
                    }

                    if (next == '*')
                    {
                        duplicate = true;
                    }
                    else
                    {
                        u1 = int.Parse($"{next}");
                        type = (ClueType)int.Parse($"{reader.ReadChar()}");    
                    }
                }

                var message = "";
                if (duplicate)
                {
                    message = "*";
                }
                else
                {
                    do
                    {
                        message += reader.ReadChar();
                    } while (message.Last() != (byte)'*');
                }

                //remove asterisk
                message = message.Substring(0, message.Length - 1);
                //only trim one set of \r\n at the end/start
                var startsNewLine = message.StartsWith("\r\n");
                var endsNewLine = message.EndsWith("\r\n") && message.Length > 4;
                if (startsNewLine || endsNewLine)
                {
                    var length = message.Length - (startsNewLine ? 2 : 0) - (endsNewLine ? 2 : 0);
                    message = message.Substring(startsNewLine ? 2 : 0, length);
                }

                var model = new ClueModel()
                {
                    Type = type,
                    Id = id,
                    CrimeId = crimeId,
                    Unknown1 = u1,
                    Message = message
                };

                if (string.IsNullOrEmpty(message))
                {
                    //it seems to be a way for the same message to be used for multiple entries
                    queuedModels.Add(model);
                }
                else
                {
                    var textKey = model.GetMessagePrefix();
                    if (dict.TryGetValue(textKey, out var existingValue))
                    {
                        //TODO: figure out why the one key is triggering this and which message gets used
                        _logger.LogError($"Duplicate text key, ignoring: {textKey}\n'{existingValue.Message}'\n'{message}'");
                        if (queuedModels.Any())
                        {
                            //it wasn't really a duplicate
                            foreach (var queuedModel in queuedModels)
                            {
                                queuedModel.Message = message;
                                if (queuedModel.CrimeId != null)
                                {
                                    queuedModel.Unknown1 = model.Unknown1;
                                    queuedModel.Type = model.Type;
                                }
                                dict[queuedModel.GetMessagePrefix()] = queuedModel;
                            }
                            queuedModels.Clear();
                        }
                        continue;
                    }

                    dict[textKey] = model;

                    if (queuedModels.Any())
                    {
                        foreach (var queuedModel in queuedModels)
                        {
                            _logger.LogInformation($"Queued text key {queuedModel.GetMessagePrefix()} as duplicate of {textKey}");
                            queuedModel.Message = message;
                            if (queuedModel.CrimeId != null)
                            {
                                queuedModel.Unknown1 = model.Unknown1;
                                queuedModel.Type = model.Type;
                            }
                            dict[queuedModel.GetMessagePrefix()] = queuedModel;
                        }
                        queuedModels.Clear();
                    }
                }
            }

            return dict;
        }
    }
}