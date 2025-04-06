using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.Core.Importing.Parsers
{
    public class LegacyPlotParser: BaseImporter<Dictionary<string, PlotModel>>
    {
        private readonly ILogger<LegacyPlotParser> _logger;
        
        private Dictionary<string, PlotModel> _result = new Dictionary<string, PlotModel>();
        private bool _done = false;

        public LegacyPlotParser(ILogger<LegacyPlotParser> logger)
        {
            _logger = logger;
        }

        protected override string Message => "Processing plots..";
        protected override bool CheckIfValidForImportInternal(string path)
        {
            if (Directory.GetFiles(path, "PLOT.TXT").Length == 0)
            {
                return false;
            }

            return true;
        }

        protected override int GetTotalItemCountInPath()
        {
            return Directory.GetFiles(Path, "PLOT.TXT").Length;
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

        protected override Dictionary<string, PlotModel> GetResultInternal()
        {
            return _result;
        }

        protected override void OnImportStart()
        {
            _done = false;
        }

        private Dictionary<string, PlotModel> Parse(string path)
        {
            var filePath = System.IO.Path.Combine(path, $"PLOT.TXT");
            if (!File.Exists(filePath))
            {
                throw new Exception($"Missing file: PLOT.TXT");
            }

            var rawData = File.ReadAllBytes(filePath);

            var dict = new Dictionary<string, PlotModel>();

            using var memStream = new MemoryStream(rawData);
            using var reader = new BinaryReader(memStream);
            
            var c = reader.ReadByte();
            if (c != (byte)'*')
            {
                throw new Exception($"Invalid starting marker: {c}");
            }

            List<PlotModel> queuedModels = new();
            var wasLast = false;
            while (!wasLast)
            {
                var prefixBytes = reader.ReadChars(6);
                if (prefixBytes[0] != 'P' && prefixBytes[0] != 'p' && prefixBytes[1] != 'L' && prefixBytes[1] != 'l')
                {
                    throw new Exception($"Invalid prefix start: {string.Join(" ", prefixBytes)}");
                }

                var type = PlotModel.PlotStringType.Unknown;
                var missionSetId = int.Parse($"{prefixBytes[2]}{prefixBytes[3]}");
                int? crimeIndex = int.Parse($"{prefixBytes[4]}");
                var messageNumber = int.Parse($"{prefixBytes[5]}", NumberStyles.HexNumber);
                if (crimeIndex == 9)
                {
                    type = PlotModel.PlotStringType.Briefing;
                    crimeIndex = null;
                }
                else
                {
                    if (messageNumber < 5)
                    {
                        type = PlotModel.PlotStringType.Success;
                    } else if (messageNumber < 10)
                    {
                        type = PlotModel.PlotStringType.Failure;
                        messageNumber -= 5;
                    }
                    else
                    {
                        type = PlotModel.PlotStringType.BriefingPreviousFailure;
                        messageNumber -= 10;
                    }
                }
                
                var message = "";
                do
                {
                    message += reader.ReadChar();
                } while (message.Last() != (byte)'*' && message.Last() != (char)0x1A);

                if (message.Last() == (char)0x1A)
                {
                    wasLast = true;
                }
                
                //remove asterisk/0x1A
                message = message.Substring(0, message.Length - 1);
                //only trim one set of \r\n at the end/start
                var startsNewLine = message.StartsWith("\r\n");
                var endsNewLine = message.EndsWith("\r\n") && message.Length > 4;
                if (startsNewLine || endsNewLine)
                {
                    var length = message.Length - (startsNewLine ? 2 : 0) - (endsNewLine ? 2 : 0);
                    message = message.Substring(startsNewLine ? 2 : 0, length);
                }

                var model = new PlotModel()
                {
                    StringType = type,
                    MissionSetId = missionSetId,
                    CrimeIndex = crimeIndex,
                    MessageNumber = messageNumber,
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
                            _logger.LogDebug($"Queued text key {queuedModel.GetMessagePrefix()} as duplicate of {textKey}");
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