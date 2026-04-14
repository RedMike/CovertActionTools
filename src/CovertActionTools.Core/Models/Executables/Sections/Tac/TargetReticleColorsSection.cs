using System;
using CovertActionTools.Core.Models.Executables.Sections;

namespace CovertActionTools.Core.Models.Executables.Sections.Tac
{
    /// <summary>
    /// 5-byte table at DS:0x1C8C..0x1C90 (TAC only). Each byte is a VGA palette index
    /// used as the fill colour for the 16x16 aiming reticle sprite, indexed by lock-on
    /// stage (0–4). Read by FUN_10e8_225d via <c>[BX + 0x1C8C]</c> where BX is
    /// clamped to 0–4, then passed to overlay stub 3 (VGA Set/Reset solid fill).
    /// </summary>
    public class TargetReticleColorsSection : IExecutableSection
    {
        public const int SectionSize = 5;

        public byte Stage0 { get; set; }
        public byte Stage1 { get; set; }
        public byte Stage2 { get; set; }
        public byte Stage3 { get; set; }
        public byte Stage4 { get; set; }

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
            Stage0 = fullPayload[startingOffset];
            Stage1 = fullPayload[startingOffset + 1];
            Stage2 = fullPayload[startingOffset + 2];
            Stage3 = fullPayload[startingOffset + 3];
            Stage4 = fullPayload[startingOffset + 4];
            return SectionSize;
        }

        public byte[] WriteBytes()
        {
            return new byte[] { Stage0, Stage1, Stage2, Stage3, Stage4 };
        }

        public TargetReticleColorsSection Clone()
        {
            return new TargetReticleColorsSection
            {
                Stage0 = Stage0,
                Stage1 = Stage1,
                Stage2 = Stage2,
                Stage3 = Stage3,
                Stage4 = Stage4
            };
        }
    }
}
