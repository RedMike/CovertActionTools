using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.Core.Importing.Parsers
{
    public interface ILegacyTextParser
    {
        Dictionary<string, TextModel> Parse(string key, byte[] rawData);
    }

    public class LegacyTextParser : ILegacyTextParser
    {
        private readonly ILogger<LegacyTextParser> _logger;

        public LegacyTextParser(ILogger<LegacyTextParser> logger)
        {
            _logger = logger;
        }

        public Dictionary<string, TextModel> Parse(string key, byte[] rawData)
        {
            var dict = new Dictionary<string, TextModel>();
            
            using var memStream = new MemoryStream(rawData);
            using var reader = new BinaryReader(memStream);

            var c = reader.ReadByte();
            if (c != (byte)'*')
            {
                throw new Exception($"Invalid starting marker: {c}");
            }
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

                var prefix = "" + (char)prefixBytes[0] + (char)prefixBytes[1] + (char)prefixBytes[2];
                if (type != TextModel.StringType.CrimeMessage)
                {
                    prefix += (char)prefixBytes[3];
                }
                else
                {
                    prefix += $"{crimeId!.Value:##}";
                }

                var stringId = $"{prefix}{id:##}";
                dict[stringId] = new TextModel()
                {
                    Id = id,
                    Type = type,
                    Message = message,
                    CrimeId = crimeId,
                };
            }

            return dict;
        }
    }
}