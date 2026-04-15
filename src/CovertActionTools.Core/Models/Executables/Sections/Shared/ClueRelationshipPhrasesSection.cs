using System.Linq;

namespace CovertActionTools.Core.Models.Executables.Sections.Shared
{
    /// <summary>
    /// 40 clue relationship phrase slots used by the evidence connection system
    /// (e.g. " tied to ", " registered to ", " acquired by "). Each slot is a
    /// null-terminated string with a fixed binary size taken from the original
    /// layout; the same data appears in TAC, FINAL, GAME, and BUG EXEs.
    /// </summary>
    public class ClueRelationshipPhrasesSection : ExactCountFixedSizeStringTableSection
    {
        public const int PhraseCount = 40;

        protected override int[] StringSizes => new[]
        {
            10, // 0: " tied to "
            16, // 1: " registered to "
            14, // 2: " acquired by "
            15, // 3: " delivered to "
            16, // 4: " requested for "
            10, // 5: " tied to "
            16, // 6: " registered to "
            12, // 7: " traced to "
            16, // 8: " smuggled into "
            16, // 9: " requested for "
            10, // 10: " tied to "
            12, // 11: " rented by "
            16, // 12: " frequented by "
            8,  // 13: " is in "
            12, // 14: " linked to "
            15, // 15: " purchased by "
            15, // 16: " purchased by "
            12, // 17: " traced to "
            15, // 18: " purchased in "
            12, // 19: " linked to "
            14, // 20: " received by "
            16, // 21: " signed for by "
            12, // 22: " traced to "
            10, // 23: " sent to "
            12, // 24: " refers to "
            15, // 25: " withdrawn by "
            15, // 26: " withdrawn by "
            10, // 27: " paid by "
            17, // 28: " transaction in "
            16, // 29: " requested for "
            15, // 30: " withdrawn by "
            15, // 31: " withdrawn by "
            10, // 32: " paid by "
            17, // 33: " transaction in "
            16, // 34: " requested for "
            14, // 35: " assigned to "
            14, // 36: " assigned to "
            14, // 37: " assigned to "
            10, // 38: " used in "
            14, // 39: " assigned to "
        };

        public ClueRelationshipPhrasesSection Clone()
        {
            return new ClueRelationshipPhrasesSection { Strings = Strings.ToList() };
        }
    }
}
