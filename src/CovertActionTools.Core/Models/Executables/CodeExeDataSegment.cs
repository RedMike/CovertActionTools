using System;
using System.Linq;

namespace CovertActionTools.Core.Models.Executables
{
    /// <summary>
    /// Structured data segment for CODE.EXE.
    /// Field boundaries and interpretations are based on reverse engineering and may not
    /// be fully accurate. Unknown regions are preserved as raw byte arrays.
    /// </summary>
    public class CodeExeDataSegment
    {
        /// <summary>DS paragraph value for CODE.EXE.</summary>
        public const int DsParagraph = 0x036D;

        #region Layout Constants (DS-relative offsets)
        // DS*16 = 0x036D0
        private const int GraphicsDocsOffset = 0x006C;      // 0x00373C - 0x036D0
        private const int GraphicsDocsSize = 3716;           // 56 strings
        private const int NibbleSpriteOffset = 0x0EF0;       // 0x0045C0 - 0x036D0
        private const int NibbleSpriteSize = 226;
        private const int GraphicsDocPtrsOffset = 0x0FD2;    // 0x0046A2 - 0x036D0
        private const int GraphicsDocPtrCount = 56;
        private const int CryptoParamsOffset = 0x1042;       // 0x004712 - 0x036D0
        private const int CryptoParamsSize = 22;
        private const int CryptoAlphabetOffset = 0x1058;     // 0x004728 - 0x036D0
        private const int CryptoAlphabetSize = 91;
        private const int CryptoUiStringsOffset = 0x10B3;    // 0x004783 - 0x036D0
        private const int CryptoUiStringsSize = 181;
        #endregion

        #region Fields (in binary order)

        /// <summary>Data before graphics library docs: MSC runtime.</summary>
        public byte[] PreDocData { get; set; } = Array.Empty<byte>();

        /// <summary>56 embedded graphics library documentation strings.</summary>
        public string[] GraphicsLibraryDocs { get; set; } = Array.Empty<string>();

        /// <summary>Original byte size of the graphics docs region.</summary>
        public int GraphicsLibraryDocsByteSize { get; set; }

        /// <summary>Gap between docs and nibble sprite data.</summary>
        public byte[] Unknown1 { get; set; } = Array.Empty<byte>();

        /// <summary>226 bytes of 2bpp nibble sprite/pixel data (values 0x00-0x33).</summary>
        public byte[] NibbleSpriteData { get; set; } = Array.Empty<byte>();

        /// <summary>56 DS-relative pointers into the graphics documentation strings.</summary>
        public ushort[] GraphicsDocPointers { get; set; } = Array.Empty<ushort>();

        /// <summary>22-byte crypto screen parameter record (319x199, mode, plane count, code pointer).</summary>
        public byte[] CryptoScreenParams { get; set; } = Array.Empty<byte>();

        /// <summary>Crypto alphabet strings: two copies of A-Z plus [,\\ and a space buffer.</summary>
        public string[] CryptoAlphabetData { get; set; } = Array.Empty<string>();

        /// <summary>Original byte size of the crypto alphabet region.</summary>
        public int CryptoAlphabetByteSize { get; set; }

        /// <summary>Crypto UI strings: "CRYPTO WORKSTATION", "MESSAGE DECODED", timer display, etc.</summary>
        public string[] CryptoUiStrings { get; set; } = Array.Empty<string>();

        /// <summary>Original byte size of the crypto UI strings region.</summary>
        public int CryptoUiStringsByteSize { get; set; }

        /// <summary>Everything after: infrastructure, palette remap, file management, overlay, C runtime.</summary>
        public byte[] TrailingData { get; set; } = Array.Empty<byte>();

        #endregion

        public static CodeExeDataSegment FromBytes(byte[] dataSegment)
        {
            var segment = new CodeExeDataSegment();

            segment.PreDocData = DataSegmentHelper.Slice(dataSegment, 0, GraphicsDocsOffset);

            segment.GraphicsLibraryDocs = DataSegmentHelper.AllNullTerminatedStringsFromBytes(dataSegment, GraphicsDocsOffset, GraphicsDocsSize);
            segment.GraphicsLibraryDocsByteSize = GraphicsDocsSize;

            var docsEnd = GraphicsDocsOffset + GraphicsDocsSize;
            segment.Unknown1 = DataSegmentHelper.Slice(dataSegment, docsEnd, NibbleSpriteOffset - docsEnd);

            segment.NibbleSpriteData = DataSegmentHelper.Slice(dataSegment, NibbleSpriteOffset, NibbleSpriteSize);

            segment.GraphicsDocPointers = DataSegmentHelper.BytesToUInt16Array(dataSegment, GraphicsDocPtrsOffset, GraphicsDocPtrCount);

            segment.CryptoScreenParams = DataSegmentHelper.Slice(dataSegment, CryptoParamsOffset, CryptoParamsSize);

            segment.CryptoAlphabetData = DataSegmentHelper.AllNullTerminatedStringsFromBytes(dataSegment, CryptoAlphabetOffset, CryptoAlphabetSize);
            segment.CryptoAlphabetByteSize = CryptoAlphabetSize;

            segment.CryptoUiStrings = DataSegmentHelper.AllNullTerminatedStringsFromBytes(dataSegment, CryptoUiStringsOffset, CryptoUiStringsSize);
            segment.CryptoUiStringsByteSize = CryptoUiStringsSize;

            var uiStringsEnd = CryptoUiStringsOffset + CryptoUiStringsSize;
            segment.TrailingData = DataSegmentHelper.Slice(dataSegment, uiStringsEnd, dataSegment.Length - uiStringsEnd);

            return segment;
        }

        public byte[] ToBytes()
        {
            return DataSegmentHelper.Concatenate(
                PreDocData,
                DataSegmentHelper.NullTerminatedStringsToBytesFixedSize(GraphicsLibraryDocs, GraphicsLibraryDocsByteSize),
                Unknown1,
                NibbleSpriteData,
                DataSegmentHelper.UInt16ArrayToBytes(GraphicsDocPointers),
                CryptoScreenParams,
                DataSegmentHelper.NullTerminatedStringsToBytesFixedSize(CryptoAlphabetData, CryptoAlphabetByteSize),
                DataSegmentHelper.NullTerminatedStringsToBytesFixedSize(CryptoUiStrings, CryptoUiStringsByteSize),
                TrailingData
            );
        }

        public CodeExeDataSegment Clone()
        {
            return new CodeExeDataSegment
            {
                PreDocData = PreDocData.ToArray(),
                GraphicsLibraryDocs = GraphicsLibraryDocs.Select(s => s).ToArray(),
                GraphicsLibraryDocsByteSize = GraphicsLibraryDocsByteSize,
                Unknown1 = Unknown1.ToArray(),
                NibbleSpriteData = NibbleSpriteData.ToArray(),
                GraphicsDocPointers = GraphicsDocPointers.ToArray(),
                CryptoScreenParams = CryptoScreenParams.ToArray(),
                CryptoAlphabetData = CryptoAlphabetData.Select(s => s).ToArray(),
                CryptoAlphabetByteSize = CryptoAlphabetByteSize,
                CryptoUiStrings = CryptoUiStrings.Select(s => s).ToArray(),
                CryptoUiStringsByteSize = CryptoUiStringsByteSize,
                TrailingData = TrailingData.ToArray()
            };
        }
    }
}
