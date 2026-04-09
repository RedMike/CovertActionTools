using System.Collections.Generic;
using CovertActionTools.Core.Models.Executables.Records.Tac;

namespace CovertActionTools.Core.Models.Executables.Sections.Tac
{
    public class MovementSection : IExecutableSection
    {
        public FullDirectionRecord Movement { get; set; } = new();
        public FullDirectionRecord Jumping { get; set; } = new();
        public CardinalDirectionRecord TileAdjacency { get; set; } = new();
        
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
            offset += Movement.ReadBytes(fullPayload, offset);
            offset += Jumping.ReadBytes(fullPayload, offset);
            offset += TileAdjacency.ReadBytes(fullPayload, offset);
            return offset - startingOffset;
        }

        public byte[] WriteBytes()
        {
            var result = new List<byte>();
            result.AddRange(Movement.WriteBytes());
            result.AddRange(Jumping.WriteBytes());
            result.AddRange(TileAdjacency.WriteBytes());
            return result.ToArray();
        }
        
        public MovementSection Clone()
        {
            return new MovementSection
            {
                Movement = this.Movement.Clone(),
                Jumping = this.Jumping.Clone(),
                TileAdjacency = this.TileAdjacency.Clone()
            };
        }
    }
}