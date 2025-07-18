﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using CovertActionTools.Core.Importing;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.Core.Exporting.Publishers
{
    /// <summary>
    /// Given a loaded model for a Crime, returns multiple assets to save:
    ///   * CRIMEx.DTA file (legacy)
    /// </summary>
    internal class CrimePublisher : BaseExporter<Dictionary<int, CrimeModel>>, ILegacyPublisher
    {
        private readonly ILogger<CrimePublisher> _logger;
        
        private readonly List<int> _keys = new();
        private int _index = 0;

        public CrimePublisher(ILogger<CrimePublisher> logger)
        {
            _logger = logger;
        }

        protected override string Message => "Processing crimes..";

        protected override Dictionary<int, CrimeModel> GetFromModel(PackageModel model)
        {
            return model.Crimes
                .Where(x => model.Index.CrimeIncluded.Contains(x.Key))
                .ToDictionary(x => x.Key, x => x.Value);
        }

        protected override void Reset()
        {
            _keys.Clear();
            _index = 0;
        }
        protected override int GetTotalItemCountInPath()
        {
            return _keys.Count;
        }

        protected override int RunExportStepInternal()
        {
            if (_index >= _keys.Count)
            {
                return _index;
            }
            var nextKey = _keys[_index];

            var files = Export(Data[nextKey]);
            foreach (var pair in files)
            {
                File.WriteAllBytes(System.IO.Path.Combine(Path, pair.Key), pair.Value);
            }

            return _index++;
        }

        protected override void OnExportStart()
        {
            _keys.AddRange(GetKeys());
            _index = 0;
        }
        
        private List<int> GetKeys()
        {
            return Data.Keys.ToList();
        }

        private IDictionary<string, byte[]> Export(CrimeModel crime)
        {
            var dict = new Dictionary<string, byte[]>()
            {
                [$"CRIME{crime.Id}.DTA"] = GetLegacyCrimeData(crime)
            };

            return dict;
        }

        private byte[] GetLegacyCrimeData(CrimeModel crime)
        {
            var intermediateCrime = Convert(crime);
            using var memStream = new MemoryStream();
            using var writer = new BinaryWriter(memStream);
            
            writer.Write((ushort)intermediateCrime.Participants.Count);
            writer.Write((ushort)(intermediateCrime.Events.Count + 1));
            
            foreach (var participant in intermediateCrime.Participants)
            {
                writer.Write((ushort)0xFFFF);
                writer.Write((ushort)participant.Exposure);
                foreach (var c in participant.Role.Trim().Trim('\0').PadRight(32, (char)0))
                {
                    writer.Write(c);
                }
            
                writer.Write((ushort)participant.Unknown1);
                writer.Write((byte)participant.Unknown2);
                writer.Write((byte)participant.ParticipantType);
                writer.Write((ushort)participant.Unknown3);
                writer.Write((byte)participant.ClueType);
                writer.Write((ushort)participant.Rank);
                writer.Write((ushort)participant.Unknown4);
                writer.Write((byte)participant.Unknown5);
            }
            
            foreach (var ev in intermediateCrime.Events)
            {
                writer.Write((ushort)ev.SourceParticipantId);
                writer.Write((ushort)0x0000);
                writer.Write((ushort)ev.MessageId);
                foreach (var c in ev.Description.Trim().Trim('\0').PadRight(32, (char)0))
                {
                    writer.Write(c);
                }
            
                writer.Write((byte)(ev.TargetParticipantId ?? 0));
                writer.Write((byte)ev.EventType);
                
                var receivedObjectBitmask = 0;
                for (var i = 0; i < 8; i++)
                {
                    if (ev.ReceivedObjectIds.Contains(i))
                    {
                        receivedObjectBitmask |= 1 << i;
                    }
                }
                writer.Write((byte)receivedObjectBitmask);
                
                var destroyedObjectBitmask = 0;
                for (var i = 0; i < 8; i++)
                {
                    if (ev.DestroyedObjectIds.Contains(i))
                    {
                        destroyedObjectBitmask |= 1 << i;
                    }
                }
                writer.Write((byte)destroyedObjectBitmask);
            
                writer.Write((ushort)ev.Score);
            }
            
            //marker
            writer.Write((byte)0xFF);
            for (var i = 0; i < 43; i++)
            {
                writer.Write((byte)0x00);
            }
            
            foreach (var obj in intermediateCrime.Objects)
            {
                foreach (var c in obj.Name.Trim().Trim('\0').PadRight(16, (char)0))
                {
                    writer.Write(c);
                }
            
                writer.Write((byte)obj.PictureId);
                writer.Write((byte)0xFF);
            }
            
            //always exactly 4 objects, but some are blank
            for (var j = 0; j < 4 - intermediateCrime.Objects.Count; j++)
            {
                //backfill a minimum number of items
                for (var i = 0; i < 16; i++)
                {
                    writer.Write((byte)0);
                }
            
                writer.Write((byte)0xFF); //object ID
                writer.Write((byte)0xFF); //marker
            }
            
            return memStream.ToArray();
        }

        private IntermediateCrimeModel Convert(CrimeModel crime)
        {
            var model = new IntermediateCrimeModel()
            {
                Id = crime.Id,
                Objects = crime.Objects,
                Metadata = crime.Metadata,
            };
            
            //participants are a simple 1-to-1
            foreach (var participant in crime.Participants)
            {
                model.Participants.Add(new IntermediateCrimeModel.Participant()
                {
                    Exposure = participant.Exposure,
                    Role = participant.Role,
                    ParticipantType = (
                        (participant.IsMastermind ? IntermediateCrimeModel.ParticipantType.Mastermind : 0) |
                        (participant.ForceFemale ? IntermediateCrimeModel.ParticipantType.Widow : 0) |
                        (participant.CanComeOutOfHiding ? IntermediateCrimeModel.ParticipantType.Assassin : 0)
                    ),
                    Unknown2 = participant.Unknown2 |
                        (participant.IsInsideContact ? 0x01 : 0),
                    ClueType = participant.ClueType,
                    Rank = participant.Rank,
                    Unknown1 = participant.Unknown1,
                    Unknown3 = participant.Unknown3,
                    Unknown4 = participant.Unknown4,
                    Unknown5 = participant.Unknown5
                });
            }
            
            foreach (var ev in crime.Events)
            {
                if (!ev.IsPairedEvent)
                {
                    //individual events are 1-to-1
                    model.Events.Add(new IntermediateCrimeModel.Event()
                    {
                        SourceParticipantId = ev.MainParticipantId,
                        TargetParticipantId = null,
                        Description = ev.ReceiveDescription,
                        MessageId = ev.MessageId,
                        EventType = (IntermediateCrimeModel.EventType)(
                            (ev.Unknown1 ? 0x10 : 0) |
                            (ev.IsBulletin ? 0x20 : 0)
                        ),
                        Score = ev.Score,
                        ReceivedObjectIds = ev.ReceivedObjectIds,
                        DestroyedObjectIds = ev.DestroyedObjectIds
                    });
                    continue;
                }
                
                //paired events get split into pairs
                //first receiving event
                model.Events.Add(new IntermediateCrimeModel.Event()
                {
                    SourceParticipantId = ev.MainParticipantId,
                    TargetParticipantId = null,
                    Description = ev.ReceiveDescription,
                    MessageId = ev.MessageId,
                    EventType = (IntermediateCrimeModel.EventType)(
                        0x01 |
                        (ev.Unknown1 ? 0x10 : 0) |
                        (ev.IsBulletin ? 0x20 : 0) |
                        (ev.IsMessage ? 0x02 : 0) |
                        (ev.IsPackage ? 0x04 : 0) |
                        (ev.IsMeeting ? 0x08 : 0)
                    ),
                    Score = ev.Score,
                    ReceivedObjectIds = ev.ItemsToSecondary ? new HashSet<int>() : ev.ReceivedObjectIds,
                    DestroyedObjectIds = ev.ItemsToSecondary ? new HashSet<int>() : ev.DestroyedObjectIds
                });
                //then the sending event
                model.Events.Add(new IntermediateCrimeModel.Event()
                {
                    SourceParticipantId = ev.SecondaryParticipantId ?? ev.MainParticipantId,
                    TargetParticipantId = ev.MainParticipantId,
                    Description = ev.SendDescription,
                    MessageId = ev.MessageId,
                    EventType = (IntermediateCrimeModel.EventType)(
                        (ev.Unknown1 ? 0x10 : 0) |
                        (ev.IsBulletin ? 0x20 : 0) |
                        (ev.IsMessage ? 0x02 : 0) |
                        (ev.IsPackage ? 0x04 : 0) |
                        (ev.IsMeeting ? 0x08 : 0)
                    ),
                    Score = ev.Score,
                    ReceivedObjectIds = ev.ItemsToSecondary ? ev.ReceivedObjectIds : new HashSet<int>(),
                    DestroyedObjectIds = ev.ItemsToSecondary ? ev.DestroyedObjectIds : new HashSet<int>()
                });
            }

            return model;
        }
    }
}