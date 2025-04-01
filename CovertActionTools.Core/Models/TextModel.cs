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

    }
}