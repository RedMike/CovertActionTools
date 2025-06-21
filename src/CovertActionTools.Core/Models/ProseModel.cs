using System;

namespace CovertActionTools.Core.Models
{
    public class ProseModel
    {
        public enum ProseType
        {
            Unknown = -1,
            Advice = 0, //advice a, 1, 2, 3, 4, 5 - $RPLC
            CharacterCapture = 1, //nice 0, 1 - $RPLC
            MastermindCapture = 2, //nice 2 - $RPLC
            Lounge = 3, //lounge
            CarFollowed = 4, //followed
            CarFollowedEnd = 5, //carcap
            Interrogated = 6, //grilled - $US, $NAME
            Escape = 7, //escape
            DoubleAgentAgree = 8, //doublea
            CharacterInterrogate = 9, //inter 1, 2
            Ambushed = 10, //surprise
            AmbushedLost = 11, //surpriseL - $RPLC
            AmbushedWon = 12, //surpriseW - $RPLC
        }
        
        /// <summary>
        /// Determines what variables get formatted as well as what the message is used for
        /// </summary>
        public ProseType Type { get; set; } = ProseType.Unknown;
        /// <summary>
        /// Only populated for some of the types
        /// </summary>
        public string SecondaryId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;

        public static (ProseType type, string secondaryId) GetTypeForPrefix(string prefix)
        {
            string secondaryId = string.Empty;
            if (prefix.StartsWith("advice"))
            {
                secondaryId = prefix.Substring("advice".Length);
                return (ProseType.Advice, secondaryId);
            }

            if (prefix.StartsWith("nice2"))
            {
                secondaryId = "2";
                return (ProseType.MastermindCapture, secondaryId);
            }

            if (prefix.StartsWith("nice"))
            {
                secondaryId = prefix.Substring("nice".Length);
                return (ProseType.CharacterCapture, secondaryId);
            }

            if (prefix.StartsWith("lounge"))
            {
                return (ProseType.Lounge, secondaryId);
            }

            if (prefix.StartsWith("followed"))
            {
                return (ProseType.CarFollowed, secondaryId);
            }

            if (prefix.StartsWith("carcap"))
            {
                return (ProseType.CarFollowedEnd, secondaryId);
            }

            if (prefix.StartsWith("grilled"))
            {
                return (ProseType.Interrogated, secondaryId);
            }

            if (prefix.StartsWith("escape"))
            {
                return (ProseType.Escape, secondaryId);
            }

            if (prefix.StartsWith("doublea"))
            {
                return (ProseType.DoubleAgentAgree, secondaryId);
            }

            if (prefix.StartsWith("inter"))
            {
                secondaryId = prefix.Substring("inter".Length);
                return (ProseType.CharacterInterrogate, secondaryId);
            }

            if (prefix.StartsWith("surpriseL"))
            {
                secondaryId = "L";
                return (ProseType.AmbushedLost, secondaryId);
            }

            if (prefix.StartsWith("surpriseW"))
            {
                secondaryId = "W";
                return (ProseType.AmbushedWon, secondaryId);
            }

            if (prefix.StartsWith("surprise"))
            {
                return (ProseType.Ambushed, secondaryId);
            }

            return (ProseType.Unknown, secondaryId);
        }

        public static string GetTypePrefix(ProseType type)
        {
            switch (type)
            {
                case ProseType.Advice:
                    return "advice";
                case ProseType.CharacterCapture:
                case ProseType.MastermindCapture:
                    return "nice";
                case ProseType.Lounge:
                    return "lounge";
                case ProseType.CarFollowed:
                    return "followed";
                case ProseType.CarFollowedEnd:
                    return "carcap";
                case ProseType.Interrogated:
                    return "grilled";
                case ProseType.Escape:
                    return "escape";
                case ProseType.DoubleAgentAgree:
                    return "doublea";
                case ProseType.CharacterInterrogate:
                    return "inter";
                case ProseType.Ambushed:
                case ProseType.AmbushedLost:
                case ProseType.AmbushedWon:
                    return "surprise";
                default:
                    throw new Exception($"Unknown type: {type}");
            }
        }

        public string GetMessagePrefix()
        {
            switch (Type)
            {
                case ProseType.Lounge:
                case ProseType.CarFollowed:
                case ProseType.CarFollowedEnd:
                case ProseType.Interrogated:
                case ProseType.Escape:
                case ProseType.DoubleAgentAgree:
                case ProseType.Ambushed:
                    return $"{GetTypePrefix(Type)}";
                case ProseType.Advice:
                case ProseType.CharacterCapture:
                case ProseType.MastermindCapture:
                case ProseType.CharacterInterrogate:
                case ProseType.AmbushedLost:
                case ProseType.AmbushedWon:
                    return $"{GetTypePrefix(Type)}{SecondaryId}";
                default:
                    throw new Exception($"Unknown type: {Type}");
            }
        }

        public ProseModel Clone()
        {
            return new ProseModel()
            {
                Type = Type,
                SecondaryId = SecondaryId,
                Message = Message
            };
        }
    }
}