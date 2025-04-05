namespace CovertActionTools.Core.Models
{
    public class ClueModel
    {
        public enum ClueSource
        {
            Unknown = -1,
            ClandestinePhoto = 0, //not used in legacy game, but works
            WireTap = 1,
            CovertSurveillance = 2,
            FileRecordSearch = 3,
            LocalInformant = 4,
            InterpolDatabase = 5,
            LocalAuthorities = 6,
        }
        
        public ClueType Type { get; set; }
        /// <summary>
        /// Participant ID if `CrimeId` is populated, numeric ID if not.
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// When not populated, seems to refer to any crime.
        /// </summary>
        public int? CrimeId { get; set; }
        
        /// <summary>
        /// Influences only the display of the clue (image and "From:" line)
        /// </summary>
        public ClueSource Source { get; set; }
        
        public string Message { get; set; } = string.Empty;

        public string GetMessagePrefix()
        {
            if (CrimeId != null)
            {
                return $"C{CrimeId.Value:D2}{Id:D2}";
            }

            return $"C{Type:D}{Id:D1}";
        }
    }
}