using System;
using CovertActionTools.Core.Models.Executables.Sections;

namespace CovertActionTools.Core.Models.Executables.Sections.Tac
{
    /// <summary>
    /// 5-byte table at DS:0x1C8C..0x1C90 (TAC only — the byte sequence
    /// <c>00 08 03 07 0F</c> does not appear in any other game EXE). The current name
    /// "AlertLevelColors" is provisional and has <b>not been confirmed</b> against
    /// observed in-game UI.
    /// <para>
    /// <b>Mechanical facts (verified):</b>
    /// <list type="bullet">
    ///   <item>Sole read site in the entire TAC image is a single
    ///         <c>MOV AL, byte ptr [BX + 0x1c8c]</c> inside FUN_10e8_225d — confirmed
    ///         by brute-force scanning the full TAC image for the bytes <c>8c 1c</c>
    ///         as a ModRM displacement (one match).</item>
    ///   <item>Index register <c>BX</c> holds the return value of
    ///         <c>FUN_10e8_d21c(local_a, 0, 4)</c>. That function is exactly
    ///         <c>return min(max(a, b), c)</c> — i.e. <c>clamp(local_a, 0, 4)</c>.</item>
    ///   <item>The fetched byte is sign-extended to <c>int</c> via <c>CBW</c> and passed
    ///         as the 8th argument to a far call through the dispatch slot at
    ///         <c>CODE_0:0x01ad</c>, which is overlay <b>stub 3</b> of
    ///         <c>?GRAPHIC.EXE</c>.</item>
    ///   <item>Overlay stub 3 (<c>stub_3_0x0098</c> in EGRAPHIC.EXE) writes that
    ///         argument to the VGA Graphics Controller <b>Set/Reset</b> register
    ///         (port 0x3CE, index 0), enables Set/Reset on all four planes
    ///         (<c>0x3CE</c> index 1 = 0x0F), and runs a bit-mask-aware plane-write
    ///         loop over a rectangular region — mechanically, the argument is a 4-bit
    ///         VGA palette index used as a solid fill colour. All 5 table entries are
    ///         valid 16-colour indices (0, 3, 7, 8, 15).</item>
    ///   <item>The draw call's other arguments are: rect width = 0x10, height = 0x10,
    ///         rotate/function-select = 1, target buffer = <c>uRam000145e0</c>. Per
    ///         <c>scratch/exe-investigation/TAC.data-regions.md</c>, the latter holds
    ///         the DS-relative pointer to the front-buffer RastPort record (block 6 —
    ///         the only RastPort with Flag=1).</item>
    ///   <item>The draw is gated by <c>param_2 == 0</c> (the function's passive branch)
    ///         and <c>local_a != 0</c>, so entry 0 of the table is never fetched in
    ///         the normal path.</item>
    ///   <item>FUN_10e8_225d is called from FUN_10e8_1b32 (the per-frame entity
    ///         rendering loop) as <c>FUN_10e8_225d(0, 0, 0)</c> once per frame, and
    ///         as <c>FUN_10e8_225d(iRam00015144, 1, 0)</c> at the end of the frame
    ///         if the flag word at DS:0x1C8A (<c>iRam00012aba</c>) was raised during
    ///         the loop.</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Unverified:</b> what <c>local_a</c> semantically represents, what gameplay
    /// event the draw corresponds to, and what the rectangle looks like on screen.
    /// Prior notes labelling entity fields −0x3778 and −0x377A as "alert counter"
    /// and "pursuit counter" (see <c>TAC.decompiled.glossary.md</c>) are themselves
    /// tentative and have not been grounded in observed behaviour.
    /// </para>
    /// </summary>
    public class TacAlertLevelColorsSection : IExecutableSection
    {
        public const int EntryCount = 5;
        public const int SectionSize = EntryCount;

        public byte[] Values { get; set; } = new byte[EntryCount];

        public bool Viewable()
        {
            return true;
        }

        public bool Editable()
        {
            return true;
        }

        public int ReadBytes(byte[] fullPayload, int startingOffset)
        {
            Values = new byte[EntryCount];
            Array.Copy(fullPayload, startingOffset, Values, 0, EntryCount);
            return SectionSize;
        }

        public byte[] WriteBytes()
        {
            var result = new byte[SectionSize];
            Array.Copy(Values, result, EntryCount);
            return result;
        }

        public TacAlertLevelColorsSection Clone()
        {
            var result = new TacAlertLevelColorsSection
            {
                Values = new byte[EntryCount]
            };
            Array.Copy(Values, result.Values, EntryCount);
            return result;
        }
    }
}
