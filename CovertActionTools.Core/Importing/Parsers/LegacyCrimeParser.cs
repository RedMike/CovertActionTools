using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.Core.Importing.Parsers
{
    internal class LegacyCrimeParser : BaseImporter<Dictionary<int, CrimeModel>>, ILegacyParser
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
        public override ImportStatus.ImportStage GetStage() => ImportStatus.ImportStage.ProcessingCrimes;

        public override void SetResult(PackageModel model)
        {
            model.Crimes = GetResult();
        }

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
                throw new Exception($"Missing DTA file: {key}");
            }

            var rawData = File.ReadAllBytes(filePath);
            
            using var memStream = new MemoryStream(rawData);
            using var reader = new BinaryReader(memStream);

            var participantCount = reader.ReadUInt16();
            var eventCount = reader.ReadUInt16();

            var participants = ReadParticipants(reader, participantCount);
            var events = ReadEvents(reader, eventCount);
            var objects = ReadObjects(reader);
            
            var intermediateModel = new IntermediateCrimeModel()
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

            return Convert(intermediateModel);
        }

        private CrimeModel Convert(IntermediateCrimeModel intermediate)
        {
            var model = new CrimeModel()
            {
                Id = intermediate.Id,
                Objects = intermediate.Objects,
                ExtraData = intermediate.ExtraData
            };

            //participants are a simple 1-to-1
            foreach (var intermediateParticipant in intermediate.Participants)
            {
                var participant = new CrimeModel.Participant()
                {
                    Exposure = intermediateParticipant.Exposure,
                    Role = intermediateParticipant.Role,
                    IsMastermind = (intermediateParticipant.ParticipantType & IntermediateCrimeModel.ParticipantType.Mastermind) > 0,
                    ForceFemale = (intermediateParticipant.ParticipantType & IntermediateCrimeModel.ParticipantType.Widow) > 0,
                    CanComeOutOfHiding = (intermediateParticipant.ParticipantType & IntermediateCrimeModel.ParticipantType.Assassin) > 0,
                    IsInsideContact = (intermediateParticipant.Unknown2 & 0x01) > 0,
                    Unknown2 = intermediateParticipant.Unknown2,
                    ClueType = intermediateParticipant.ClueType,
                    Rank = intermediateParticipant.Rank,
                    Unknown1 = intermediateParticipant.Unknown1,
                    Unknown3 = intermediateParticipant.Unknown3,
                    Unknown4 = intermediateParticipant.Unknown4,
                    Unknown5 = intermediateParticipant.Unknown5,
                };
                model.Participants.Add(participant);
            }
            
            //events get their pairs merged together
            var processedEvents = new HashSet<int>();
            for (var i = 0; i < intermediate.Events.Count; i++)
            {
                if (!processedEvents.Add(i))
                {
                    continue;
                }

                var intermediateEvent = intermediate.Events[i];
                if (((int)intermediateEvent.EventType & 0x0F) == 0)
                {
                    //it's an individual event with no pair
                    if (intermediateEvent.TargetParticipantId != null)
                    {
                        _logger.LogError($"Found event {i} that is Individual but has target participant");
                    }
                    var individualEvent = new CrimeModel.Event()
                    {
                        MainParticipantId = intermediateEvent.SourceParticipantId,
                        SecondaryParticipantId = null,
                        ReceiveDescription = intermediateEvent.Description,
                        SendDescription = intermediateEvent.Description,
                        MessageId = intermediateEvent.MessageId,
                        IsMessage = false,
                        IsPackage = false,
                        IsMeeting = false,
                        IsBulletin = ((int)intermediateEvent.EventType & 0x20) > 0,
                        Unknown1 = ((int)intermediateEvent.EventType & 0x10) > 0,
                        ItemsToSecondary = false,
                        ReceivedObjectIds = intermediateEvent.ReceivedObjectIds,
                        DestroyedObjectIds = intermediateEvent.DestroyedObjectIds,
                        Score = intermediateEvent.Score
                    };
                    model.Events.Add(individualEvent);
                    continue;
                }
                
                //it's a paired event, both have to be parsed together to make sense of it
                var partnerEventId = intermediate.Events.FindIndex(x => 
                    x.MessageId == intermediateEvent.MessageId &&
                    x.IsReceive() != intermediateEvent.IsReceive() && 
                    (
                        (x.IsMessage() && intermediateEvent.IsMessage()) ||
                        (x.IsPackage() && intermediateEvent.IsPackage()) ||
                        (x.IsMeeting() && intermediateEvent.IsMeeting())
                    )
                );
                var forceItemsToSecondary = false;
                if (partnerEventId == -1)
                {
                    // for some reason, legacy data has events with duplicate 'receive' events that match each other
                    partnerEventId = intermediate.Events.FindIndex(x => 
                        x.MessageId == intermediateEvent.MessageId &&
                        //x.IsReceive() != intermediateEvent.IsReceive() && 
                        (
                            (x.IsMessage() && intermediateEvent.IsMessage()) ||
                            (x.IsPackage() && intermediateEvent.IsPackage()) ||
                            (x.IsMeeting() && intermediateEvent.IsMeeting())
                        ) &&
                        ((intermediateEvent.TargetParticipantId != null && x.SourceParticipantId == intermediateEvent.TargetParticipantId.Value) ||
                        (x.TargetParticipantId == intermediateEvent.SourceParticipantId))
                    );
                    if (intermediate.Events[partnerEventId].ReceivedObjectIds.Any() || intermediate.Events[partnerEventId].DestroyedObjectIds.Any())
                    {
                        forceItemsToSecondary = true;
                    }
                    if (partnerEventId == -1)
                    {
                        throw new Exception($"Unable to find partner event for: {i} {intermediateEvent.EventType} {intermediateEvent.MessageId} {intermediateEvent.Description}");
                    }
                }
                var partnerEvent = intermediate.Events[partnerEventId];
                processedEvents.Add(partnerEventId);
                
                var pairedEvent = new CrimeModel.Event()
                {
                    MainParticipantId = intermediateEvent.IsReceive() ? (partnerEvent.TargetParticipantId ?? intermediateEvent.SourceParticipantId) : (intermediateEvent.TargetParticipantId ?? partnerEvent.SourceParticipantId),
                    SecondaryParticipantId = intermediateEvent.IsReceive() ? partnerEvent.SourceParticipantId : intermediateEvent.SourceParticipantId,
                    MessageId = intermediateEvent.MessageId,
                    ReceiveDescription = intermediateEvent.IsReceive() ? intermediateEvent.Description : partnerEvent.Description,
                    SendDescription = intermediateEvent.IsReceive() ? partnerEvent.Description : intermediateEvent.Description,
                    IsMessage = intermediateEvent.IsMessage(),
                    IsPackage = intermediateEvent.IsPackage(),
                    IsMeeting = intermediateEvent.IsMeeting(),
                    IsBulletin = intermediateEvent.IsBulletin(),
                    Unknown1 = intermediateEvent.IsUnknown1(),
                    ItemsToSecondary = forceItemsToSecondary || 
                        (!intermediateEvent.IsReceive() && (intermediateEvent.ReceivedObjectIds.Any() || intermediateEvent.DestroyedObjectIds.Any())) ||
                        (!partnerEvent.IsReceive() && (partnerEvent.ReceivedObjectIds.Any() || partnerEvent.DestroyedObjectIds.Any())),
                    ReceivedObjectIds = intermediateEvent.ReceivedObjectIds.Any() ? intermediateEvent.ReceivedObjectIds : partnerEvent.ReceivedObjectIds,
                    DestroyedObjectIds = intermediateEvent.DestroyedObjectIds.Any() ? intermediateEvent.DestroyedObjectIds : partnerEvent.DestroyedObjectIds,
                    Score = intermediateEvent.Score != 0 ? intermediateEvent.Score : partnerEvent.Score
                };
                model.Events.Add(pairedEvent);
            }

            return model;
        }

        private List<IntermediateCrimeModel.Participant> ReadParticipants(BinaryReader reader, int count)
        {
            var participants = new List<IntermediateCrimeModel.Participant>();

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
                var type = (IntermediateCrimeModel.ParticipantType)(int)reader.ReadByte();
                var unknown3 = reader.ReadUInt16();

                var clueType = (ClueType)(int)reader.ReadByte();

                var rank = reader.ReadUInt16();

                var unknown4 = reader.ReadUInt16();
                var unknown5 = reader.ReadByte();
                
                participants.Add(new IntermediateCrimeModel.Participant()
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

        private List<IntermediateCrimeModel.Event> ReadEvents(BinaryReader reader, int count)
        {
            var events = new List<IntermediateCrimeModel.Event>();
            
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
                var type = (IntermediateCrimeModel.EventType)(int)reader.ReadByte();

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
                events.Add(new IntermediateCrimeModel.Event()
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