using System;

namespace CovertActionTools.Core.Models
{
    public class TextModel
    {
        public enum StringType
        {
            Unknown = -1,
            CrimeMessage = 0, //MSGXX##, XX is crime ID, has replacements $SNDORG, $SNDLOC, $VICTIM, $OBJECT, Dateline
            SenderOrganisation = 1, //SORG##, has replacement $SNDORG
            ReceiverOrganisation = 2, //RORG##, has replacement $RCVORG
            SenderLocation = 3, //SLOC##, has replacement $SNDLOC
            ReceiverLocation = 4, //RLOC##, has replacement $RCVLOC
            Fluff = 5, //FLUF##
            Alert = 6, //ALRT##
            AidingOrganisation = 7, //AIDD##, has replacement $HLPORG
        }
        
        public int Id { get; set; }
        public StringType Type { get; set; } = StringType.Unknown;
        public string Message { get; set; } = string.Empty;
        /// <summary>
        /// Only used for CrimeMessage types, identifies the crime the text will be used in
        /// </summary>
        public int? CrimeId { get; set; }
        
        /// <summary>
        /// Used to maintain ordering in the file
        /// TODO: find out if a reordered file still works correctly
        /// </summary>
        public int Order { get; set; }

        public static string GetTypePrefix(StringType type)
        {
            switch (type)
            {
                case StringType.CrimeMessage:
                    return "MSG";
                case StringType.SenderOrganisation:
                    return "SORG";
                case StringType.ReceiverOrganisation:
                    return "RORG";
                case StringType.SenderLocation:
                    return "SLOC";
                case StringType.ReceiverLocation:
                    return "RLOC";
                case StringType.Fluff:
                    return "FLUF";
                case StringType.Alert:
                    return "ALRT";
                case StringType.AidingOrganisation:
                    return "AIDD";
                default:
                    throw new Exception($"Unknown type: {type}");
            }
        }

        public string GetMessagePrefix()
        {
            switch (Type)
            {
                case StringType.CrimeMessage:
                    return $"{GetTypePrefix(Type)}{CrimeId:D2}{Id:D2}";
                case StringType.SenderOrganisation:
                    return $"{GetTypePrefix(Type)}{Id:D2}";
                case StringType.ReceiverOrganisation:
                    return $"{GetTypePrefix(Type)}{Id:D2}";
                case StringType.SenderLocation:
                    return $"{GetTypePrefix(Type)}{Id:D2}";
                case StringType.ReceiverLocation:
                    return $"{GetTypePrefix(Type)}{Id:D2}";
                case StringType.Fluff:
                    return $"{GetTypePrefix(Type)}{Id:D2}";
                case StringType.Alert:
                    return $"{GetTypePrefix(Type)}{Id:D2}";
                case StringType.AidingOrganisation:
                    return $"{GetTypePrefix(Type)}{Id:D2}";
                default:
                    throw new Exception($"Unknown type: {Type}");
            }
        }
    }
}