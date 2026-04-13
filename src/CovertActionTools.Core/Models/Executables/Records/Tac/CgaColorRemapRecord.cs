using System;
using CovertActionTools.Core.Models.Executables.Records;

namespace CovertActionTools.Core.Models.Executables.Records.Tac
{
    /// <summary>
    /// 16-byte CGA color remap record. Each byte maps one of the game's 16 VGA palette
    /// indices to CGA's 4-color palette (0=Black, 1=Cyan, 2=Magenta, 3=White). The packed
    /// byte format uses low nibble and high nibble as two separate 2-bit color values
    /// feeding CGA's interleaved bit planes. Consumed by overlay stub 27 (CGRAPHIC only;
    /// no-op on EGA/MCGA/Tandy) to build framebuffer pixel lookup tables. Different
    /// records are used for different UI contexts (gameplay, dialog boxes, entity cards,
    /// menus) to optimise readability within CGA's 4-color limitation.
    /// </summary>
    public class CgaColorRemapRecord : IExecutableRecord
    {
        public const int RecordSize = 16;

        public byte[] Data { get; set; } = new byte[RecordSize];

        public int ReadBytes(byte[] fullPayload, int startingOffset)
        {
            Array.Copy(fullPayload, startingOffset, Data, 0, RecordSize);
            return RecordSize;
        }

        public byte[] WriteBytes()
        {
            var result = new byte[RecordSize];
            Array.Copy(Data, result, RecordSize);
            return result;
        }

        public CgaColorRemapRecord Clone()
        {
            var result = new CgaColorRemapRecord();
            Array.Copy(Data, result.Data, RecordSize);
            return result;
        }
    }
}
