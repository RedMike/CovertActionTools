namespace CovertActionTools.Core.Models
{
    public class ClueModel
    {
        public ClueType Type { get; set; }
        /// <summary>
        /// Participant ID if `CrimeId` is populated, numeric ID if not.
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// When not populated, seems to refer to any crime.
        /// </summary>
        public int? CrimeId { get; set; }
        
        public int Unknown1 { get; set; }
        
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