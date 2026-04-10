using System;

namespace CovertActionTools.Core.Models.Executables.Records.Shared
{
    public class PointerRecord : IExecutableRecord
    {
        /// <summary>
        /// TODO: replace with fixups
        /// </summary>
        public int Pointer { get; set; }
        
        public int ReadBytes(byte[] fullPayload, int startingOffset)
        {
            var offset = startingOffset;
            Pointer = BitConverter.ToUInt16(fullPayload, offset);
            offset += 2;
            return offset - startingOffset;
        }

        public byte[] WriteBytes()
        {
            var result = new byte[2];
            Array.Copy(BitConverter.GetBytes((ushort)Pointer), result, 2);
            return result;
        }
        
        public PointerRecord Clone()
        {
            return new PointerRecord
            {
                Pointer = this.Pointer
            };
        }
    }
}