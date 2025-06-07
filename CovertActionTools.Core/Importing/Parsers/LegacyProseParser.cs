using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.Core.Importing.Parsers
{
    internal class LegacyProseParser : BaseImporter<Dictionary<string, ProseModel>>, ILegacyParser
    {
        private readonly ILogger<LegacyProseParser> _logger;
        
        private Dictionary<string, ProseModel> _result = new Dictionary<string, ProseModel>();
        private bool _done = false;

        public LegacyProseParser(ILogger<LegacyProseParser> logger)
        {
            _logger = logger;
        }

        protected override string Message => "Processing prose..";
        public override ImportStatus.ImportStage GetStage() => ImportStatus.ImportStage.ProcessingProse;

        public override void SetResult(PackageModel model)
        {
            model.Prose = GetResult();
        }

        protected override bool CheckIfValidForImportInternal(string path)
        {
            if (Directory.GetFiles(path, "PROSE.DTA").Length == 0)
            {
                return false;
            }

            return true;
        }

        protected override int GetTotalItemCountInPath()
        {
            return Directory.GetFiles(Path, "PROSE.DTA").Length;
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

        protected override Dictionary<string, ProseModel> GetResultInternal()
        {
            return _result;
        }

        protected override void OnImportStart()
        {
            _done = false;
        }

        private Dictionary<string, ProseModel> Parse(string path)
        {
            var filePath = System.IO.Path.Combine(path, $"PROSE.DTA");
            if (!File.Exists(filePath))
            {
                throw new Exception($"Missing DTA file: PROSE.DTA");
            }

            var rawData = File.ReadAllBytes(filePath);
            
            var dict = new Dictionary<string, ProseModel>();
            
            using var memStream = new MemoryStream(rawData);
            using var reader = new BinaryReader(memStream);

            byte c = 0;
            do
            {
                c = reader.ReadByte();
            } while (c != (byte)'*');

            List<ProseModel> queuedModels = new();
            while (true)
            {
                var prefix = "";
                byte nextByte = 0;
                while (nextByte != (byte)'\n')
                {
                    if (nextByte != 0)
                    {
                        prefix += (char)nextByte;
                    }
                    nextByte = reader.ReadByte();
                }

                prefix = prefix.Trim('\r', '\n', '\t').Trim();
                if (prefix == "end")
                {
                    break;
                }
                var (type, secondaryId) = ProseModel.GetTypeForPrefix(prefix);
                if (type == ProseModel.ProseType.Unknown)
                {
                    throw new Exception($"Unknown prose type: {prefix}");
                }
                
                var message = "";
                do
                {
                    message += (char)reader.ReadByte();
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

                var model = new ProseModel()
                {
                    Type = type,
                    SecondaryId = secondaryId,
                    Message = message
                };

                if (string.IsNullOrEmpty(message))
                {
                    //it seems to be a way for the same message to be used for multiple entries
                    queuedModels.Add(model);
                }
                else
                {
                    var key = model.GetMessagePrefix();
                    if (dict.TryGetValue(key, out var existingValue))
                    {
                        _logger.LogError($"Duplicate text key, ignoring: {key}\n'{existingValue.Message}'\n'{message}'");
                        if (queuedModels.Any())
                        {
                            //it wasn't really a duplicate
                            foreach (var queuedModel in queuedModels)
                            {
                                queuedModel.Message = message;
                                dict[queuedModel.GetMessagePrefix()] = queuedModel;
                            }
                            queuedModels.Clear();
                        }
                        continue;
                    }

                    dict[key] = model;

                    if (queuedModels.Any())
                    {
                        foreach (var queuedModel in queuedModels)
                        {
                            _logger.LogDebug($"Queued text key {queuedModel.GetMessagePrefix()} as duplicate of {key}");
                            queuedModel.Message = message;
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