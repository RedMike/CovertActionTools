namespace CovertActionTools.Core.Models.Executables.Sections.Tac
{
    /// <summary>
    /// 9-byte "HH:MM:SS\0" template whose digit slots are overwritten in place by
    /// the chronology formatter (TAC Code_0 FUN_10e8_b969) each time the clock is
    /// rendered. Only the two separator characters at positions 2 and 5 survive
    /// in the rendered output, so the section exposes only those two bytes for
    /// editing; the eight digit positions are always emitted as '0' and the
    /// trailing null is preserved.
    /// </summary>
    public class TimeTemplateStringSection : IExecutableSection
    {
        public const int SlotSize = 9;

        /// <summary>Separator between hours and minutes (byte 2 of the template).</summary>
        public char HoursMinutesSeparator { get; set; } = ':';

        /// <summary>Separator between minutes and seconds (byte 5 of the template).</summary>
        public char MinutesSecondsSeparator { get; set; } = ':';

        public bool Viewable() => true;
        public bool Editable() => true;

        public int ReadBytes(byte[] fullPayload, int startingOffset)
        {
            HoursMinutesSeparator = (char)fullPayload[startingOffset + 2];
            MinutesSecondsSeparator = (char)fullPayload[startingOffset + 5];
            return SlotSize;
        }

        public byte[] WriteBytes()
        {
            return new byte[]
            {
                (byte)'0', (byte)'0', (byte)HoursMinutesSeparator,
                (byte)'0', (byte)'0', (byte)MinutesSecondsSeparator,
                (byte)'0', (byte)'0', 0,
            };
        }

        public TimeTemplateStringSection Clone()
        {
            return new TimeTemplateStringSection
            {
                HoursMinutesSeparator = HoursMinutesSeparator,
                MinutesSecondsSeparator = MinutesSecondsSeparator,
            };
        }
    }
}
