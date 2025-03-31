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
            SentMessage = 2, //must happen with 3
            ReceivedMessage = 3, //must happen with 2
            MetWith = 8, //must happen with 9
            WasMetBy = 9, //must happen with 8
            Crime = 20, //major crime
        }
        
        public class Participant
        {
            /// <summary>
            /// How common clues are/linking to actual suspect files
            /// </summary>
            public int Exposure { get; set; }
            /// <summary>
            /// Printed role
            /// Legacy limited to 16 chars
            /// </summary>
            public string Role { get; set; } = string.Empty;
            /// <summary>
            /// ?
            /// </summary>
            public int Unknown1 { get; set; }
            /// <summary>
            /// Only one mastermind per crime
            /// </summary>
            public bool Mastermind { get; set; }
            /// <summary>
            /// ?
            /// </summary>
            public byte Unknown2 { get; set; }

            public ClueType ClueType { get; set; } = ClueType.Unknown;
            /// <summary>
            /// Only used for scoring
            /// </summary>
            public int Rank { get; set; }
            /// <summary>
            /// ?
            /// </summary>
            public int Unknown3 { get; set; }
        }

        public class Event
        {
            public int SourceParticipantId { get; set; }
            public int MessageId { get; set; }

            /// <summary>
            /// Printed description
            /// Legacy limited to 16 chars
            /// </summary>
            public string Description { get; set; } = string.Empty;
            /// <summary>
            /// 0 for non-participant
            /// </summary>
            public int TargetParticipantId { get; set; }

            public EventType EventType { get; set; } = EventType.Unknown;
            public List<int> ReceivedObjectIds { get; set; } = new();
            public List<int> DestroyedObjectIds { get; set; } = new();
            /// <summary>
            /// Only used for scoring
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
    }
}