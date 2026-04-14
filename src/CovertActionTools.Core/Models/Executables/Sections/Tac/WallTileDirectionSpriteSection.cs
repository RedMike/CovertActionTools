using System;
using System.Collections.Generic;
using System.Linq;
using CovertActionTools.Core.Models.Executables.Records.Shared;

namespace CovertActionTools.Core.Models.Executables.Sections.Tac
{
    public class WallTileDirectionSpriteSection : IExecutableSection
    {
        private const int EntryCount = 16;

        public Dictionary<WallDirection, byte> Sprites { get; set; } = new();

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
            Sprites = new Dictionary<WallDirection, byte>();
            for (var i = 0; i < EntryCount; i++)
            {
                Sprites[(WallDirection)i] = fullPayload[startingOffset + i];
            }
            return EntryCount;
        }

        public byte[] WriteBytes()
        {
            var result = new byte[EntryCount];
            for (var i = 0; i < EntryCount; i++)
            {
                if (Sprites.TryGetValue((WallDirection)i, out var value))
                {
                    result[i] = value;
                }
            }
            return result;
        }

        public WallTileDirectionSpriteSection Clone()
        {
            return new WallTileDirectionSpriteSection
            {
                Sprites = Sprites.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            };
        }
    }
}
