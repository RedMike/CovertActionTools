using System;
using System.Linq;

namespace CovertActionTools.Core.Models.Executables.Sections.Shared
{
    /// <summary>
    /// Opaque blob containing:
    /// - MS Run-time Library header
    /// - Some opaque data (uninitialized variables/buffers)
    /// Always exactly 108 bytes in length.
    /// </summary>
    public class ExeFileHeaderSection : IExecutableSection
    {
        private const int BytesLength = 108;
        
        public byte[] OpaqueData { get; set; } = Array.Empty<byte>();

        public bool Viewable()
        {
            return false;
        }

        public bool Editable()
        {
            return false;
        }
        
        public ExeFileHeaderSection Clone()
        {
            return new ExeFileHeaderSection()
            {
                OpaqueData = OpaqueData.ToArray()
            };
        }

        public int ReadBytes(byte[] fullPayload, int startingOffset)
        {
            OpaqueData = DataSegmentHelper.Slice(fullPayload, startingOffset, BytesLength);
            return BytesLength;
        }

        public byte[] WriteBytes()
        {
            return OpaqueData.ToArray();
        }

    }
}