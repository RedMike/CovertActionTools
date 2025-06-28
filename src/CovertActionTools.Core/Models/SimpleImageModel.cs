using System.Collections.Generic;
using System.Linq;

namespace CovertActionTools.Core.Models
{
    public class SimpleImageModel
    {
        public class Sprite
        {
            public int X { get; set; }
            public int Y { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }

            public Sprite Clone()
            {
                return new Sprite()
                {
                    X = X,
                    Y = Y,
                    Width = Width,
                    Height = Height
                };
            }
        }

        public class SpriteSheetData
        {
            public Dictionary<string, Sprite> Sprites { get; set; } = new();

            public SpriteSheetData Clone()
            {
                return new SpriteSheetData()
                {
                    Sprites = Sprites.ToDictionary(x => x.Key, x => x.Value.Clone())
                };
            }
        }
        
        /// <summary>
        /// ID that also determines the filename
        /// </summary>
        public string Key { get; set; } = string.Empty;

        public SharedMetadata Metadata { get; set; } = new();
        public SharedImageModel Image { get; set; } = new();
        /// <summary>
        /// Sprite sheet information for which sprites exist on the image
        /// For legacy images, this data will not cover ALL sprites on the sheet because
        /// a good deal of them are not used by the game.
        /// </summary>
        public SpriteSheetData? SpriteSheet { get; set; } = null;

        public SimpleImageModel Clone()
        {
            return new SimpleImageModel()
            {
                Key = Key,
                Metadata = Metadata.Clone(),
                Image = Image.Clone(),
                SpriteSheet = SpriteSheet?.Clone()
            };
        }
    }
}