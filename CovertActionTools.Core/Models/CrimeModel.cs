using System.Collections.Generic;

namespace CovertActionTools.Core.Models
{
    public class CrimeModel
    {
        public enum ClueType
        {
            Unknown = -1,
            Vehicle = 0,
            Weapon = 1,
            Address = 2,
            AirlineTicket = 3,
            Telegram = 4,
            MoneyHundreds = 5,
            MoneyThousands = 6,
            IdentityDocument = 7,
        }

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
            Crime = 32, //major or minor crime
            
        }
        
        public enum ParticipantType //maybe a bitmask?
        {
            Unknown = -1,
            Normal = 0,
            Mastermind = 1, //only one per case
            Widow = 2, //special logic?
            Assassin = 64, //special logic?
        }

        public class Metadata
        {
            /// <summary>
            /// Actual name separate from key/filename, for development
            /// </summary>
            public string Name { get; set; } = string.Empty;
            
            /// <summary>
            /// Arbitrary comment, for development
            /// </summary>
            public string Comment { get; set; } = string.Empty;
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
            /// 
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
        }

        public class Object
        {
            /// <summary>
            /// Printed name
            /// Legacy limited to 32 chars
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
        public Metadata ExtraData { get; set; } = new();
    }
}