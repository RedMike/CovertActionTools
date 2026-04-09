using System;
using System.Collections.Generic;

namespace CovertActionTools.Core.Models.Executables.Records.Tac
{
    public class MapObjectTypeRecord : IExecutableRecord
    {
        private const int RecordSize = 20; //fixed-size as a whole
        private const int NameSize = 12; //fixed-size
        
        /// <summary>
        /// Name, fixed 12 byte size with null right padding
        /// Shown in-game
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        public int SpriteSheetOffsetX { get; set; }
        public int SpriteSheetOffsetY { get; set; }
        
        public MapObjectBehavior Behavior { get; set; }
        public RoomSizeConstraint RoomSizeConstraint { get; set; }
        
        public int ReadBytes(byte[] fullPayload, int startingOffset)
        {
            var offset = startingOffset;
            Name = DataSegmentHelper.DecodeControlString(fullPayload, offset, NameSize);
            offset += NameSize;

            SpriteSheetOffsetX = BitConverter.ToUInt16(fullPayload, offset);
            offset += 2;

            SpriteSheetOffsetY = BitConverter.ToUInt16(fullPayload, offset);
            offset += 2;

            Behavior = (MapObjectBehavior)BitConverter.ToUInt16(fullPayload, offset);
            offset += 2;

            RoomSizeConstraint = (RoomSizeConstraint)BitConverter.ToUInt16(fullPayload, offset);
            offset += 2;
            
            return offset - startingOffset;
        }

        public byte[] WriteBytes()
        {
            var result = new List<byte>(RecordSize);
            var nameBytes = DataSegmentHelper.EncodeControlString(Name);
            result.AddRange(nameBytes);
            result.AddRange(new byte[NameSize - nameBytes.Length]); //padding

            result.AddRange(BitConverter.GetBytes((ushort)SpriteSheetOffsetX));
            result.AddRange(BitConverter.GetBytes((ushort)SpriteSheetOffsetY));
            result.AddRange(BitConverter.GetBytes((ushort)Behavior));
            result.AddRange(BitConverter.GetBytes((ushort)RoomSizeConstraint));

            return result.ToArray();
        }
        
        public MapObjectTypeRecord Clone()
        {
            return new MapObjectTypeRecord()
            {
                Name = Name,
                SpriteSheetOffsetX = SpriteSheetOffsetX,
                SpriteSheetOffsetY = SpriteSheetOffsetY,
                Behavior = Behavior,
                RoomSizeConstraint = RoomSizeConstraint
            };
        }
    }
}