using System.Collections.Generic;
using System.Linq;

namespace CovertActionTools.Core.Models
{
    public class CrimeModel
    {
        public enum EventType
        {
            Unknown = -1,
            Individual = 0,
            Message = 1,
            Package = 2,
            Meeting = 3
        }
        
        public class Participant
        {
            /// <summary>
            /// How common clues are/linking to actual suspect files
            /// Relative to other participants
            /// </summary>
            public int Exposure { get; set; }
            /// <summary>
            /// Printed role
            /// Legacy limited to 32 chars
            /// </summary>
            public string Role { get; set; } = string.Empty;
            
            /// <summary>
            /// Only one allowed per crime
            /// </summary>
            public bool IsMastermind { get; set; }
            /// <summary>
            /// Used in original game only for the 'black widow' characters
            /// </summary>
            public bool ForceFemale { get; set; }
            /// <summary>
            /// Used in original game only for the 'assassin' characters
            /// </summary>
            public bool CanComeOutOfHiding { get; set; }
            /// <summary>
            /// Spawns character only from allied organisations.
            /// Warning: can cause game start to never finish correctly if mis-used.
            /// </summary>
            public bool IsInsideContact { get; set; }
            
            /// <summary>
            /// Seems to be a way to set up which participants should match organisation/location.
            /// Lowest bit marks inside contacts (only allied orgs).
            /// Always 0 for Mastermind.
            /// Some participants' locations are overridden by other data (unclear, mission sets?)
            ///
            /// Logic is hard to follow:
            ///   * it seems that 1000 0000 and 0100 0000 values will put participants in the same organisation
            ///   * it seems that 0010 0000 and 0001 0000 values will put participants in the same location
            ///   * it seems that 0001 0000 values will _sometimes_ put participants in allied orgs
            ///   * it seems that 0000 0001 values will _always_ put participants in allied orgs
            ///   * if the left 4 bits are split into two groups, that allows participants to be grouped together in 2 ways
            ///   * but 0000 0100 has sometimes led to participants in allied orgs
            ///   * and 0000 0100, 0000 1000 values don't lead to the same anything
            ///   * whereas 0000 0010 values lead to the same location
            ///   * maybe a two tier system of preferred/fallback? still very hard to follow
            /// Values: 4C, 48, 08, 88, 20, A0, 02, 42, 21, 54, 0C, 86, 11, 50, 10
            /// </summary>
            public int Unknown2 { get; set; }

            /// <summary>
            /// The type of clue the suspect can receive (exclusively)
            /// </summary>
            public ClueType ClueType { get; set; } = ClueType.Unknown;
            
            /// <summary>
            /// Only used for scoring
            /// </summary>
            public int Rank { get; set; }
            
            #region Legacy Fields
            /// <summary>
            /// Likely legacy field because always 1 in crimes after 3rd.
            /// 
            /// Usually 1.
            /// 13 = Initiated Plan in 0
            /// 3 = Financed Operation in 0
            /// 9 = Tailed Victim in 0
            /// 65535 = Kidnapper in 0
            /// 4 = Everyone but Mastermind in 2 and 3
            /// Nothing but 1 in crimes after 3.
            /// Maybe a legacy field that didn't get used in the end?
            /// </summary>
            public int Unknown1 { get; set; }
            /// <summary>
            /// Always 0
            /// </summary>
            public int Unknown3 { get; set; }
            /// <summary>
            /// Likely legacy field because always 0600 in crimes after 3rd.
            /// 
            /// Values: 0, 0200, 0300, 0400, 0500, 0600, 0700
            /// Nothing but 0600 in crimes after 3.
            /// Maybe a legacy field that didn't get used in the end?
            /// </summary>
            public int Unknown4 { get; set; }
            /// <summary>
            /// Always 0
            /// </summary>
            public int Unknown5 { get; set; }
            #endregion
        }
        
        public class Event
        {
            /// <summary>
            /// Participant on which event is focused.
            /// For individual events, is the only participant and requirements/items apply to this one.
            /// For paired events, is the participant to whom items apply (received/destroyed).
            /// Event always happens at this participant's location (for airport surveillance/message traffic to/etc).
            /// </summary>
            public int MainParticipantId { get; set; }
            /// <summary>
            /// Secondary participant, not filled in for individual events.
            /// For paired events, is the participant to whom requirements apply (owns item).
            /// Note: requirements are skipped if the item does not exist yet, but only if it's a paired event!
            /// </summary>
            public int? SecondaryParticipantId { get; set; }
            
            /// <summary>
            /// Reference into Texts, uses prefix from crime ID
            /// </summary>
            public int MessageId { get; set; }

            /// <summary>
            /// Printed description
            /// Legacy limited to 32 chars
            /// </summary>
            public string ReceiveDescription { get; set; } = string.Empty;
            /// <summary>
            /// Printed description
            /// Legacy limited to 32 chars
            /// </summary>
            public string SendDescription { get; set; } = string.Empty;

            /// <summary>
            /// When true, can produce Message Traffic notifications/be caught by wire tap.
            /// TODO: figure out if pure messages can send items.
            /// </summary>
            public bool IsMessage { get; set; }
            /// <summary>
            /// When true, involves a package send including moving items.
            /// TODO: figure out if there are any special notifications/wire tap info
            /// </summary>
            public bool IsPackage { get; set; }
            /// <summary>
            /// When true, involves a physical meeting including moving items, can produce
            /// Airport Surveillance notifications/be caught by bugs/wire tap.
            /// </summary>
            public bool IsMeeting { get; set; }

            /// <summary>
            /// Simplified view of what the event was, as the game would describe it.
            /// </summary>
            public EventType Type => SecondaryParticipantId == null ? EventType.Individual : (
                    IsPackage ? EventType.Package : 
                        IsMessage ? EventType.Message :
                            IsMeeting ? EventType.Meeting : EventType.Unknown
                );

            /// <summary>
            /// Easy way to check if even is a communication rather than an action
            /// </summary>
            public bool IsPairedEvent => IsMessage || IsPackage || IsMeeting;
            
            
            /// <summary>
            /// When true, broadcasts message as a direct bulletin to the player.
            /// If Score is set, also counts as a "crime" and loses score.
            /// Any event type can be a bulletin, but usually it's an Individual event because
            /// otherwise the message text can be confusing.
            /// </summary>
            public bool IsBulletin { get; set; }
            
            /// <summary>
            /// When true,
            /// TODO: figure out what it actually does
            /// Some events have it set on a Received Message event (but not on the Send)
            /// </summary>
            public bool Unknown1 { get; set; }
            
            /// <summary>
            /// When true, item receiving/destroying is done against the secondary.
            /// </summary>
            public bool ItemsToSecondary { get; set; }
            
            /// <summary>
            /// Object IDs that are received by the Main Participant as a result of the event.
            /// If the event is paired and the object IDs already exist on a participant, are a requirement
            /// for Secondary Participant to own before event is valid.
            /// If the event is individual, not a requirement and will be moved from wherever they are to the Main
            /// Participant.
            /// If the event is paired but the object IDs don't already exist, not a requirement and will be created
            /// straight onto the Main Participant.
            /// </summary>
            public HashSet<int> ReceivedObjectIds { get; set; } = new();
            /// <summary>
            /// Object IDs that are destroyed by the Main Participant as a result of the event.
            /// Always a requirement for the Main Participant to own them before event is valid.
            /// TODO: can destroyed items be re-received after?
            /// </summary>
            public HashSet<int> DestroyedObjectIds { get; set; } = new();
            /// <summary>
            /// Only used for scoring
            /// If set and IsBulletin, shown on score screen as a crime
            /// </summary>
            public int Score { get; set; }
        }
        
        public class Object
        {
            /// <summary>
            /// Printed name
            /// Legacy limited to 16 chars
            /// </summary>
            public string Name { get; set; } = string.Empty;
            /// <summary>
            /// Index into ICONS image
            /// Legacy limited to 00-15
            /// </summary>
            public int PictureId { get; set; }
        }
        
        public int Id { get; set; }
        public List<Participant> Participants { get; set; } = new();
        public List<Event> Events { get; set; } = new();
        public List<Object> Objects { get; set; } = new();
        public SharedMetadata Metadata { get; set; } = new();

        public CrimeModel Clone()
        {
            return new CrimeModel()
            {
                Metadata = Metadata.Clone(),
                Id = Id,
                Events = Events
                    .Select(x => new Event()
                    {
                        MainParticipantId = x.MainParticipantId,
                        SecondaryParticipantId = x.SecondaryParticipantId,
                        MessageId = x.MessageId,
                        ReceiveDescription = x.ReceiveDescription,
                        SendDescription = x.SendDescription,
                        IsMessage = x.IsMessage,
                        IsPackage = x.IsPackage,
                        IsMeeting = x.IsMeeting,
                        IsBulletin = x.IsBulletin,
                        Unknown1 = x.Unknown1,
                        ItemsToSecondary = x.ItemsToSecondary,
                        ReceivedObjectIds = new HashSet<int>(x.ReceivedObjectIds),
                        DestroyedObjectIds = new HashSet<int>(x.DestroyedObjectIds),
                        Score = x.Score
                    })
                    .ToList(),
                Objects = Objects
                    .Select(x => new Object()
                    {
                        Name = x.Name,
                        PictureId = x.PictureId
                    }).ToList(),
                Participants = Participants
                    .Select(x => new Participant()
                    {
                        Exposure = x.Exposure,
                        Role = x.Role,
                        IsMastermind = x.IsMastermind,
                        ForceFemale = x.ForceFemale,
                        CanComeOutOfHiding = x.CanComeOutOfHiding,
                        IsInsideContact = x.IsInsideContact,
                        Unknown1 = x.Unknown1,
                        Unknown2 = x.Unknown2,
                        Unknown3 = x.Unknown3,
                        Unknown4 = x.Unknown4,
                        Unknown5 = x.Unknown5,
                        ClueType = x.ClueType,
                        Rank = x.Rank
                    })
                    .ToList()
            };
        }
    }
    
    /// <summary>
    /// Model that is closest to the layout of the file
    /// </summary>
    public class IntermediateCrimeModel
    {
        public enum EventType
        {
            Unknown = -1,
            ProcessItems = 0, //turns items into other items, not consistent
            SentMessage = 2, //must happen with 3
            ReceivedMessage = 3, //must happen with 2
            SentPackage = 4, //must happen with 5
            ReceivedPackage = 5, //must happen with 4
            MetWith = 8, //must happen with 9
            WasMetBy = 9, //must happen with 8
            OddReceivedMessage = 19, //must happen with 2? Used in some place instead of 3
            Bulletin = 32, //shown to player directly
            //TODO: is this meant to be split into two bitfields?
        }
        
        public enum ParticipantType //maybe a bitmask?
        {
            Unknown = -1,
            Normal = 0,
            Mastermind = 1, //only one per case
            Widow = 2, //special logic?
            Assassin = 64, //special logic?
        }

        public class Participant
        {
            /// <summary>
            /// How common clues are/linking to actual suspect files
            /// </summary>
            public int Exposure { get; set; }
            /// <summary>
            /// Printed role
            /// Legacy limited to 32 chars
            /// </summary>
            public string Role { get; set; } = string.Empty;
            /// <summary>
            /// Usually 1.
            /// 13 = Initiated Plan in 0
            /// 3 = Financed Operation in 0
            /// 9 = Tailed Victim in 0
            /// 65535 = Kidnapper in 0
            /// 4 = Everyone but Mastermind in 2 and 3
            /// Nothing but 1 in crimes after 3.
            /// Maybe a legacy field that didn't get used in the end?
            /// </summary>
            public int Unknown1 { get; set; }
            /// <summary>
            /// Affects some logic about clues? 
            /// </summary>
            public ParticipantType ParticipantType { get; set; } = ParticipantType.Unknown;
            
            /// <summary>
            /// Always 0 for Mastermind
            /// Values: 4C, 48, 08, 88, 20, A0, 02, 42, 21, 54, 0C, 86, 11, 50, 10
            /// Definitely a bitmap of some sort.
            /// Lowest bit marks inside contacts.
            /// Fourth bit marks "will interact with items/money"?
            /// Second highest bit marks "should messages be broadcast as bulletins"?
            /// </summary>
            public int Unknown2 { get; set; }

            public ClueType ClueType { get; set; } = ClueType.Unknown;
            /// <summary>
            /// Only used for scoring
            /// </summary>
            public int Rank { get; set; }
            /// <summary>
            /// Always 0
            /// </summary>
            public int Unknown3 { get; set; }
            /// <summary>
            /// Values: 0, 0200, 0300, 0400, 0500, 0600, 0700
            /// Nothing but 0600 in crimes after 3.
            /// Maybe a legacy field that didn't get used in the end?
            /// </summary>
            public int Unknown4 { get; set; }
            /// <summary>
            /// Always 0
            /// </summary>
            public int Unknown5 { get; set; }
        }

        public class Event
        {
            public int SourceParticipantId { get; set; }
            /// <summary>
            /// Reference into Texts, uses prefix from crime ID
            /// </summary>
            public int MessageId { get; set; }

            /// <summary>
            /// Printed description
            /// Legacy limited to 32 chars
            /// </summary>
            public string Description { get; set; } = string.Empty;
            /// <summary>
            /// Null for non-participant
            /// </summary>
            public int? TargetParticipantId { get; set; }

            public EventType EventType { get; set; } = EventType.Unknown;
            public HashSet<int> ReceivedObjectIds { get; set; } = new();
            public HashSet<int> DestroyedObjectIds { get; set; } = new();
            /// <summary>
            /// Only used for scoring
            /// </summary>
            public int Score { get; set; }

            public bool IsMessage() => ((int)EventType & 2) > 0;
            public bool IsPackage() => ((int)EventType & 4) > 0;
            public bool IsMeeting() => ((int)EventType & 8) > 0;
            public bool IsReceive() => ((int)EventType & 1) > 0;
            public bool IsBulletin() => ((int)EventType & 32) > 0;
            public bool IsUnknown1() => ((int)EventType & 16) > 0;
        }

        public int Id { get; set; }
        public List<Participant> Participants { get; set; } = new();
        public List<Event> Events { get; set; } = new();
        public List<CrimeModel.Object> Objects { get; set; } = new();
        public SharedMetadata Metadata { get; set; } = new();
    }
}