using System;
using System.Linq;
using CovertActionTools.Core.Models;

namespace CovertActionTools.Core.Models.Executables.Sections.Shared
{
    /// <summary>
    /// 64 clue-item name templates arranged as an 8x8 grid: 8 entries per
    /// <see cref="ClueType"/>, indexed 0..7. The TAC clue formatter (Code_1
    /// FUN_10e8_97c8) addresses an entry as
    /// <c>baseTable[clueType * 8 + index]</c>, looking it up via the pointer
    /// table that immediately follows in DS, then appends a numeric instance
    /// value (and, for MoneyHundreds / MoneyThousands, a "00" / "000" suffix
    /// from <see cref="Tac.ClueTargetStringsSection"/>).
    /// </summary>
    public class ClueItemNamesSection : ExactCountFixedSizeStringTableSection
    {
        public const int ItemsPerType = 8;
        public const int TotalItems = ClueTypeAbbreviationsSection.ClueTypeCount * ItemsPerType;

        protected override int[] StringSizes => new[]
        {
            // [Vehicle] (8)
            14, 13, 16, 10, 12, 14, 9, 11,
            // [Weapon] (8)
            14, 19, 15, 15, 18, 12, 10, 13,
            // [Address] (8) -- no '#' suffix, names already include trailing space
            13, 13, 10, 12, 14, 13, 14, 12,
            // [AirlineTicket] (8)
            8, 9, 9, 12, 8, 6, 11, 7,
            // [Telegram] (8)
            10, 6, 8, 8, 11, 10, 10, 10,
            // [MoneyHundreds] (8) -- all "$"
            2, 2, 2, 2, 2, 2, 2, 2,
            // [MoneyThousands] (8) -- all "$"
            2, 2, 2, 2, 2, 2, 2, 2,
            // [IdentityDocument] (8)
            17, 13, 20, 12, 19, 13, 13, 20,
        };

        public string GetString(ClueType type, int index)
        {
            ValidateIndex(type, index);
            return Strings[(int)type * ItemsPerType + index];
        }

        public void SetString(ClueType type, int index, string value)
        {
            ValidateIndex(type, index);
            Strings[(int)type * ItemsPerType + index] = value;
        }

        public ClueItemNamesSection Clone()
        {
            return new ClueItemNamesSection { Strings = Strings.ToList() };
        }

        private static void ValidateIndex(ClueType type, int index)
        {
            if ((int)type < 0 || (int)type >= ClueTypeAbbreviationsSection.ClueTypeCount)
            {
                throw new ArgumentOutOfRangeException(nameof(type),
                    $"ClueType {type} has no entry in this table.");
            }
            if (index < 0 || index >= ItemsPerType)
            {
                throw new ArgumentOutOfRangeException(nameof(index),
                    $"Index {index} is out of range (0..{ItemsPerType - 1}).");
            }
        }
    }
}
