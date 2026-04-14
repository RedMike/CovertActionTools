using System;
using CovertActionTools.Core.Models.Executables;
using CovertActionTools.Core.Models.Executables.Records.Shared;
using CovertActionTools.Core.Models.Executables.Sections;

namespace CovertActionTools.Core.Models.Executables.Sections.Tac
{
    /// <summary>
    /// Pause banner (8-byte fixed string) and quit confirmation dialog (41-byte menu
    /// string with 2 options) at DS:0x1C46..0x1C76. Word-alignment padding after the
    /// section is handled by <see cref="IPaddedToWord"/>.
    /// </summary>
    public class TacMenuStringsSection : IExecutableSection, IPaddedToWord
    {
        public const int PauseBannerSlotSize = 8;
        public const int QuitDialogSlotSize = 41;
        public const int SectionSize = PauseBannerSlotSize + QuitDialogSlotSize;

        public string PauseBanner { get; set; } = string.Empty;
        public MenuStringRecord QuitDialog { get; set; } = new MenuStringRecord(QuitDialogSlotSize, 2);

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
            var offset = startingOffset;
            var end = offset;
            while (end < offset + PauseBannerSlotSize && fullPayload[end] != 0) end++;
            PauseBanner = DataSegmentHelper.DecodeControlString(fullPayload, offset, end - offset);
            offset += PauseBannerSlotSize;
            offset += QuitDialog.ReadBytes(fullPayload, offset);
            return offset - startingOffset;
        }

        public byte[] WriteBytes()
        {
            var result = new byte[SectionSize];
            var encoded = DataSegmentHelper.EncodeControlString(PauseBanner);
            Array.Copy(encoded, 0, result, 0, Math.Min(encoded.Length, PauseBannerSlotSize - 1));
            var quitBytes = QuitDialog.WriteBytes();
            Array.Copy(quitBytes, 0, result, PauseBannerSlotSize, quitBytes.Length);
            return result;
        }

        public TacMenuStringsSection Clone()
        {
            return new TacMenuStringsSection
            {
                PauseBanner = PauseBanner,
                QuitDialog = QuitDialog.Clone()
            };
        }
    }
}
