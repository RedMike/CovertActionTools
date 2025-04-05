using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.Core.Importing.Parsers
{
    public class LegacyTextParser : BaseImporter<Dictionary<string, TextModel>>
    {
        private readonly ILogger<LegacyTextParser> _logger;
        
        private Dictionary<string, TextModel> _result = new Dictionary<string, TextModel>();
        private bool _done = false;

        public LegacyTextParser(ILogger<LegacyTextParser> logger)
        {
            _logger = logger;
        }

        protected override string Message => "Processing texts..";
        protected override bool CheckIfValidForImportInternal(string path)
        {
            if (Directory.GetFiles(path, "TEXT.DTA").Length == 0)
            {
                return false;
            }

            return true;
        }

        protected override int GetTotalItemCountInPath()
        {
            return Directory.GetFiles(Path, "TEXT.DTA").Length;
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

        protected override Dictionary<string, TextModel> GetResultInternal()
        {
            return _result;
        }

        protected override void OnImportStart()
        {
            _done = false;
        }

        private Dictionary<string, TextModel> Parse(string path)
        {
            var filePath = System.IO.Path.Combine(path, $"TEXT.DTA");
            if (!File.Exists(filePath))
            {
                throw new Exception($"Missing JSON file: TEXT.DTA");
            }

            var rawData = File.ReadAllBytes(filePath);
            
            var dict = new Dictionary<string, TextModel>();
            
            using var memStream = new MemoryStream(rawData);
            using var reader = new BinaryReader(memStream);

            var c = reader.ReadByte();
            if (c != (byte)'*')
            {
                throw new Exception($"Invalid starting marker: {c}");
            }

            TextModel? queuedModel = null; 
            while (true)
            {
                var type = TextModel.StringType.Unknown;
                int? crimeId = null;
                var prefixBytes = reader.ReadChars(4);
                if (prefixBytes[0] == 'M' && prefixBytes[1] == 'S' && prefixBytes[2] == 'G')
                {
                    type = TextModel.StringType.CrimeMessage;
                    var rawCrimeId = "" + (char)prefixBytes[3] + reader.ReadChar();
                    crimeId = int.Parse(rawCrimeId);
                    if (crimeId < 0 || crimeId > 12)
                    {
                        throw new Exception($"Invalid crime ID: {rawCrimeId}");
                    }
                } else if (prefixBytes[0] == 'S' && prefixBytes[1] == 'O' && prefixBytes[2] == 'R')
                {
                    type = TextModel.StringType.SenderOrganisation;
                } else if (prefixBytes[0] == 'R' && prefixBytes[1] == 'O' && prefixBytes[2] == 'R')
                {
                    type = TextModel.StringType.ReceiverOrganisation;
                } else if (prefixBytes[0] == 'S' && prefixBytes[1] == 'L' && prefixBytes[2] == 'O')
                {
                    type = TextModel.StringType.SenderLocation;
                } else if (prefixBytes[0] == 'R' && prefixBytes[1] == 'L' && prefixBytes[2] == 'O')
                {
                    type = TextModel.StringType.ReceiverLocation;
                } else if (prefixBytes[0] == 'F' && prefixBytes[1] == 'L' && prefixBytes[2] == 'U')
                {
                    type = TextModel.StringType.Fluff;
                } else if (prefixBytes[0] == 'A' && prefixBytes[1] == 'L' && prefixBytes[2] == 'R')
                {
                    type = TextModel.StringType.Alert;
                } else if (prefixBytes[0] == 'A' && prefixBytes[1] == 'I' && prefixBytes[2] == 'D')
                {
                    type = TextModel.StringType.AidingOrganisation;
                } else if (prefixBytes[0] == 'E' && prefixBytes[1] == 'N' && prefixBytes[2] == 'D')
                {
                    break;
                }
                else
                {
                    throw new Exception($"Invalid prefix: {"" + (char)(prefixBytes[0]) + (char)(prefixBytes[1]) + (char)(prefixBytes[2])}");
                }
                
                var rawId = "" + reader.ReadChar() + reader.ReadChar();
                var id = int.Parse(rawId);

                var message = "";
                do
                {
                    message += reader.ReadChar();
                } while (message.Last() != (byte)'*');

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

                var model = new TextModel()
                {
                    Id = id,
                    Type = type,
                    Message = message,
                    CrimeId = crimeId
                };

                if (string.IsNullOrEmpty(message))
                {
                    //it seems to be a way for the same message to be used for two entries
                    if (queuedModel != null)
                    {
                        throw new Exception($"Attempting to queue two models in a row: {queuedModel.GetMessagePrefix()} and {model.GetMessagePrefix()}");
                    }
                    queuedModel = model;
                }
                else
                {
                    var textKey = model.GetMessagePrefix();
                    if (dict.TryGetValue(textKey, out var existingValue))
                    {
                        //TODO: figure out why the one key is triggering this and which message gets used
                        _logger.LogError($"Duplicate text key {queuedModel?.GetMessagePrefix()}, ignoring: {textKey}\n'{existingValue.Message}'\n'{message}'");
                        if (queuedModel != null)
                        {
                            //it wasn't really a duplicate
                            queuedModel.Message = message;
                            dict[queuedModel.GetMessagePrefix()] = queuedModel;
                            queuedModel = null;
                        }
                        continue;
                    }

                    dict[textKey] = model;

                    if (queuedModel != null)
                    {
                        _logger.LogInformation($"Queued text key {queuedModel.GetMessagePrefix()} as duplicate of {textKey}");
                        queuedModel.Message = message;
                        dict[queuedModel.GetMessagePrefix()] = queuedModel;
                        queuedModel = null;
                    }
                }
            }

            return dict;
        }
    }
}