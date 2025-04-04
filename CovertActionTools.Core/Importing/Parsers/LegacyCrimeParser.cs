using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.Core.Importing.Parsers
{
    public class LegacyCrimeParser : BaseImporter<Dictionary<int, CrimeModel>>
    {
        private readonly ILogger<LegacyCrimeParser> _logger;
        
        private readonly List<int> _keys = new();
        private readonly Dictionary<int, CrimeModel> _result = new Dictionary<int, CrimeModel>();
        
        private int _index = 0;

        public LegacyCrimeParser(ILogger<LegacyCrimeParser> logger)
        {
            _logger = logger;
        }

        protected override string Message => "Processing crimes..";
        protected override bool CheckIfValidForImportInternal(string path)
        {
            if (Directory.GetFiles(path, "CRIME*.DTA").Length == 0)
            {
                return false;
            }

            return true;
        }

        protected override int GetTotalItemCountInPath()
        {
            return GetKeys(Path).Count;
        }

        protected override int RunImportStepInternal()
        {
            var nextKey = _keys[_index];

            _result[nextKey] = Parse(Path, nextKey);

            return _index++;
        }

        protected override Dictionary<int, CrimeModel> GetResultInternal()
        {
            return _result;
        }

        protected override void OnImportStart()
        {
            _keys.AddRange(GetKeys(Path));
            _index = 0;
        }
        
        private List<int> GetKeys(string path)
        {
            return Directory.GetFiles(path, "CRIME*.DTA")
                .Select(System.IO.Path.GetFileNameWithoutExtension)
                .Select(x => int.TryParse(x.Replace("CRIME", ""), out var index) ? index : -1)
                .Where(x => x >= 0)
                .ToList();
        }

        private CrimeModel Parse(string path, int key)
        {
            var filePath = System.IO.Path.Combine(path, $"CRIME{key}.DTA");
            if (!File.Exists(filePath))
            {
                throw new Exception($"Missing JSON file: {key}");
            }

            var rawData = File.ReadAllBytes(filePath);
            
            using var memStream = new MemoryStream(rawData);
            using var reader = new BinaryReader(memStream);

            var participantCount = reader.ReadUInt16();
            var eventCount = reader.ReadUInt16();

            var participants = ReadParticipants(reader, participantCount);
            var events = ReadEvents(reader, eventCount);
            var objects = ReadObjects(reader);
            
            return new CrimeModel()
            {
                Id = key,
                Participants = participants,
                Events = events,
                Objects = objects,
                ExtraData = new CrimeModel.Metadata()
                {
                    Name = $"CRIME{key}",
                    Comment = "Legacy import"
                }
            };
        }

        private List<CrimeModel.Participant> ReadParticipants(BinaryReader reader, int count)
        {
            var participants = new List<CrimeModel.Participant>();

            for (var i = 0; i < count; i++)
            {
                //first ushort unknown
                var u = reader.ReadUInt16();
                if (u != 0xFFFF)
                {
                    throw new Exception($"Unknown data on participant ID {i}: {u:X4}");
                }

                var exposure = reader.ReadUInt16();
                var role = "";
                for (var j = 0; j < 32; j++)
                {
                    var ch = (char)reader.ReadByte();
                    role += ch;
                }
                role = role.Trim().Trim('\0');

                var unknown1 = reader.ReadUInt16();
                var unknown2 = reader.ReadByte();
                var type = (CrimeModel.ParticipantType)(int)reader.ReadByte();
                var unknown3 = reader.ReadUInt16();

                var clueType = (CrimeModel.ClueType)(int)reader.ReadByte();

                var rank = reader.ReadUInt16();

                var unknown4 = reader.ReadUInt16();
                var unknown5 = reader.ReadByte();
                
                participants.Add(new CrimeModel.Participant()
                {
                    Exposure = exposure,
                    Role = role,
                    Unknown1 = unknown1,
                    ParticipantType = type,
                    Unknown2 = unknown2,
                    ClueType = clueType,
                    Rank = rank,
                    Unknown3 = unknown3,
                    Unknown4 = unknown4,
                    Unknown5 = unknown5,
                });
            }

            return participants;
        }

        private List<CrimeModel.Event> ReadEvents(BinaryReader reader, int count)
        {
            var events = new List<CrimeModel.Event>();
            
            for (var i = 0; i < count; i++)
            {
                var sourceParticipantId = reader.ReadUInt16();

                bool ignore = sourceParticipantId == (byte)0xFF;

                var u = reader.ReadUInt16();
                if (u != 0)
                {
                    throw new Exception($"Unknown data on event {i}: {u:X4}");
                }

                var messageId = reader.ReadUInt16();
                
                var description = "";
                for (var j = 0; j < 32; j++)
                {
                    var ch = (char)reader.ReadByte();
                    description += ch;
                }
                description = description.Trim().Trim('\0');

                var targetParticipant = reader.ReadByte();
                var type = (CrimeModel.EventType)(int)reader.ReadByte();

                var receivedObjectBitmask = reader.ReadByte();
                HashSet<int> receivedObjects = new();
                for (var j = 0; j < 8; j++)
                {
                    if ((receivedObjectBitmask & 0x1) == 1)
                    {
                        receivedObjects.Add(j);
                    }

                    receivedObjectBitmask >>= 1;
                }
                
                var destroyedObjectBitmask = reader.ReadByte();
                HashSet<int> destroyedObjects = new();
                for (var j = 0; j < 8; j++)
                {
                    if ((destroyedObjectBitmask & 0x1) == 1)
                    {
                        destroyedObjects.Add(j);
                    }

                    destroyedObjectBitmask >>= 1;
                }

                var score = reader.ReadUInt16();

                if (ignore)
                {
                    continue;
                }
                events.Add(new CrimeModel.Event()
                {
                    SourceParticipantId = sourceParticipantId,
                    MessageId = messageId,
                    Description = description,
                    TargetParticipantId = targetParticipant == 0 ? null : targetParticipant,
                    EventType = type,
                    ReceivedObjectIds = receivedObjects,
                    DestroyedObjectIds = destroyedObjects,
                    Score = score
                });
            }

            return events;
        }

        private List<CrimeModel.Object> ReadObjects(BinaryReader reader)
        {
            var objects = new List<CrimeModel.Object>();

            //always exactly 4 objects, but some are blank
            for (var i = 0; i < 4; i++)
            {
                var name = "";
                for (var j = 0; j < 16; j++)
                {
                    var ch = (char)reader.ReadByte();
                    name += ch;
                }
                name = name.Trim().Trim('\0');

                var pictureId = reader.ReadByte();
                if (pictureId == 0xFF)
                {
                    //it's not a real item
                    reader.ReadByte();
                    continue;
                }

                var u = reader.ReadByte();
                if (u != 0xFF)
                {
                    throw new Exception($"Invalid data on object {i}: {u:X2}");
                }
                
                objects.Add(new CrimeModel.Object()
                {
                    Name = name,
                    PictureId = pictureId
                });
            }

            return objects;
        }
    }
}