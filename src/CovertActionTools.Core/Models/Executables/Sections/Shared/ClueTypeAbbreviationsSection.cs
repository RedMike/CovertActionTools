using System;
using System.Linq;
using CovertActionTools.Core.Models;

namespace CovertActionTools.Core.Models.Executables.Sections.Shared
{
    /// <summary>
    /// Eight 3- or 4-byte clue-type abbreviations indexed 1:1 by <see cref="ClueType"/>:
    /// CAR, WPN, ADR, TKT, MSG, $, $, FCE. Used by the clue/intel formatter to render
    /// the type tag for each piece of evidence. The two "$" entries are intentional --
    /// MoneyHundreds and MoneyThousands share the same abbreviation but render with
    /// different numeric suffixes.
    /// </summary>
    public class ClueTypeAbbreviationsSection : ExactCountFixedSizeStringTableSection
    {
        public const int ClueTypeCount = 8;

        protected override int[] StringSizes => new[]
        {
            4, // [Vehicle]         "CAR"
            4, // [Weapon]          "WPN"
            4, // [Address]         "ADR"
            4, // [AirlineTicket]   "TKT"
            4, // [Telegram]        "MSG"
            2, // [MoneyHundreds]   "$"
            2, // [MoneyThousands]  "$"
            4, // [IdentityDocument]"FCE"
        };

        public string GetString(ClueType type)
        {
            ValidateType(type);
            return Strings[(int)type];
        }

        public void SetString(ClueType type, string value)
        {
            ValidateType(type);
            Strings[(int)type] = value;
        }

        public ClueTypeAbbreviationsSection Clone()
        {
            return new ClueTypeAbbreviationsSection { Strings = Strings.ToList() };
        }

        private static void ValidateType(ClueType type)
        {
            if ((int)type < 0 || (int)type >= ClueTypeCount)
            {
                throw new ArgumentOutOfRangeException(nameof(type),
                    $"ClueType {type} has no entry in this table.");
            }
        }
    }
}
