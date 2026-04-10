using System;
using System.Collections.Generic;

namespace CovertActionTools.Core.Models.Executables.Records.Tac
{
    public class RoomTypeRecord : IExecutableRecord
    {
        private const int RecordSize = 22; //fixed-size as a whole
        private const int NameSize = 16; //fixed-size
        
        /// <summary>
        /// Name, fixed 16 byte size with null right padding
        /// Shown in-game
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Surveillance quality, ushort with at most 100 value
        /// Increase granted when a bug is placed in this room type 
        /// </summary>
        public int SurveillanceQuality { get; set; }

        /// <summary>
        /// Size constraint, bitfield
        /// Separates room types by the area size they are allowed to cover
        /// Small is 40 or less, medium is up to 72, large is over 72
        /// </summary>
        public RoomSizeConstraint SizeConstraint { get; set; } = RoomSizeConstraint.Unknown;
        
        /// <summary>
        /// Enabled, bitfield
        /// Effectively a boolean, bit 0 is only one used
        /// When enabled, sets value 7 as per legacy game
        /// </summary>
        public bool Enabled { get; set; }
        
        public int ReadBytes(byte[] fullPayload, int startingOffset)
        {
            var offset = startingOffset;
            Name = DataSegmentHelper.DecodeControlString(fullPayload, offset, NameSize);
            offset += NameSize;

            SurveillanceQuality = BitConverter.ToUInt16(fullPayload, offset);
            offset += 2;

            SizeConstraint = (RoomSizeConstraint)BitConverter.ToUInt16(fullPayload, offset);
            offset += 2;

            var rawEnabled = BitConverter.ToUInt16(fullPayload, offset);
            Enabled = rawEnabled != 0;
            offset += 2;
            
            return offset - startingOffset;
        }

        public byte[] WriteBytes()
        {
            var result = new List<byte>(RecordSize);
            var nameBytes = DataSegmentHelper.EncodeControlString(Name);
            result.AddRange(nameBytes);
            result.AddRange(new byte[NameSize - nameBytes.Length]); //padding

            result.AddRange(BitConverter.GetBytes((ushort)SurveillanceQuality));
            result.AddRange(BitConverter.GetBytes((ushort)SizeConstraint));
            //for Enabled we use 07 because that's what the legacy game does
            result.AddRange(BitConverter.GetBytes((ushort)(Enabled ? (byte)0x07 : 0x00)));
            return result.ToArray();
        }
        
        public RoomTypeRecord Clone()
        {
            return new RoomTypeRecord()
            {
                Name = Name,
                SurveillanceQuality = SurveillanceQuality,
                SizeConstraint = SizeConstraint,
                Enabled = Enabled
            };
        }
    }
}