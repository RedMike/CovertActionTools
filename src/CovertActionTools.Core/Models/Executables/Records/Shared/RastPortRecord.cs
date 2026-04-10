using System;
using System.Collections.Generic;

namespace CovertActionTools.Core.Models.Executables.Records.Shared
{
    public class RastPortRecord : IExecutableRecord
    {
        public int Page { get; set; }
        public int OriginX { get; set; }
        public int OriginY { get; set; }
        public int WidthMinusOne { get; set; }
        public int HeightMinusOne { get; set; }
        /// <summary>
        /// TODO: figure out values
        /// </summary>
        public int Flag { get; set; }
        public int MaxColor { get; set; }
        public int BytesPerPixel { get; set; }
        
        public int ReadBytes(byte[] fullPayload, int startingOffset)
        {
            var offset = startingOffset;
            Page = BitConverter.ToUInt16(fullPayload, offset);
            offset += 2;
            OriginX = BitConverter.ToUInt16(fullPayload, offset);
            offset += 2;
            OriginY = BitConverter.ToUInt16(fullPayload, offset);
            offset += 2;
            WidthMinusOne = BitConverter.ToUInt16(fullPayload, offset);
            offset += 2;
            HeightMinusOne = BitConverter.ToUInt16(fullPayload, offset);
            offset += 2;
            Flag = BitConverter.ToUInt16(fullPayload, offset);
            offset += 2;
            MaxColor = BitConverter.ToUInt16(fullPayload, offset);
            offset += 2;
            BytesPerPixel = BitConverter.ToUInt16(fullPayload, offset);
            offset += 2;
            var tmp = BitConverter.ToUInt16(fullPayload, offset);
            if (tmp != 0)
            {
                throw new Exception($"Expected padding to be 0 but got real value: {tmp:X4} at offset {offset:X4}");
            }
            offset += 2;
            
            return offset - startingOffset;
        }

        public byte[] WriteBytes()
        {
            var result = new List<byte>();
            result.AddRange(BitConverter.GetBytes((ushort)Page));
            result.AddRange(BitConverter.GetBytes((ushort)OriginX));
            result.AddRange(BitConverter.GetBytes((ushort)OriginY));
            result.AddRange(BitConverter.GetBytes((ushort)WidthMinusOne));
            result.AddRange(BitConverter.GetBytes((ushort)HeightMinusOne));
            result.AddRange(BitConverter.GetBytes((ushort)Flag));
            result.AddRange(BitConverter.GetBytes((ushort)MaxColor));
            result.AddRange(BitConverter.GetBytes((ushort)BytesPerPixel));
            result.AddRange(BitConverter.GetBytes((ushort)0)); //padding
            return result.ToArray();
        }

        public RastPortRecord Clone()
        {
            return new RastPortRecord
            {
                Page = this.Page,
                OriginX = this.OriginX,
                OriginY = this.OriginY,
                WidthMinusOne = this.WidthMinusOne,
                HeightMinusOne = this.HeightMinusOne,
                Flag = this.Flag,
                MaxColor = this.MaxColor,
                BytesPerPixel = this.BytesPerPixel
            };
        }
    }
}