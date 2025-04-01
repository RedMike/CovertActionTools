using System;
using System.Collections.Generic;
using System.IO;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.Core.Importing.Parsers
{
    public interface ILegacyCrimeParser
    {
        CrimeModel Parse(string key, byte[] rawData);
    }
    
    internal class LegacyCrimeParser : ILegacyCrimeParser
    {
        private readonly ILogger<LegacyCrimeParser> _logger;

        public LegacyCrimeParser(ILogger<LegacyCrimeParser> logger)
        {
            _logger = logger;
        }

        public CrimeModel Parse(string key, byte[] rawData)
        {
            using var memStream = new MemoryStream(rawData);
            using var reader = new BinaryReader(memStream);

            var participantCount = reader.ReadUInt16();
            var eventCount = reader.ReadUInt16();

            var participants = ReadParticipants(key, reader, participantCount);
            var events = ReadEvents(key, reader, eventCount);
            var objects = ReadObjects(key, reader);

            var id = int.Parse(key.Replace("CRIME", ""));
            
            return new CrimeModel()
            {
                Id = id,
                Participants = participants,
                Events = events,
                Objects = objects,
                ExtraData = new CrimeModel.Metadata()
                {
                    Name = key,
                    Comment = "Legacy import"
                }
            };
        }

        private List<CrimeModel.Participant> ReadParticipants(string key, BinaryReader reader, int count)
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
                role = role.Trim();

                var unknown1 = reader.ReadUInt16();
                var unknown5 = reader.ReadUInt16();
                var unknown2 = reader.ReadByte();

                var clueType = (CrimeModel.ClueType)(int)reader.ReadUInt16();

                var rank = reader.ReadUInt16();

                var unknown3 = reader.ReadUInt16();
                var unknown4 = reader.ReadByte();
                
                participants.Add(new CrimeModel.Participant()
                {
                    Exposure = exposure,
                    Role = role,
                    Unknown1 = unknown1,
                    Unknown5 = unknown5,
                    Unknown2 = unknown2,
                    ClueType = clueType,
                    Rank = rank,
                    Unknown3 = unknown3,
                    Unknown4 = unknown4,
                });
            }

            return participants;
        }

        private List<CrimeModel.Event> ReadEvents(string key, BinaryReader reader, int count)
        {
            var events = new List<CrimeModel.Event>();
            
            for (var i = 0; i < count; i++)
            {
                var sourceParticipantId = reader.ReadUInt16();
                
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
                description = description.Trim();

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

        private List<CrimeModel.Object> ReadObjects(string key, BinaryReader reader)
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
                name = name.Trim();

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