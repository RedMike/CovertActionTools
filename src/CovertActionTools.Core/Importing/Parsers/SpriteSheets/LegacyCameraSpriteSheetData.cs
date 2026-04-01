using System.Collections.Generic;
using CovertActionTools.Core.Models;

namespace CovertActionTools.Core.Importing.Parsers.SpriteSheets
{
    internal class LegacyCameraSpriteSheetData : BaseLegacySpriteSheetData
    {
        public override string[] Keys => new[] { "CAMERA" };

        public override SimpleImageModel.SpriteSheetData Create()
        {
            return new SimpleImageModel.SpriteSheetData()
            {
                Sprites = new Dictionary<string, SimpleImageModel.Sprite>()
                {
                    { "screen", new SimpleImageModel.Sprite()
                        {
                            X = 0, Y = 0,
                            Width = 146,
                            Height = 101
                        }
                    },
                    { "map", new SimpleImageModel.Sprite()
                        {
                            X = 200, Y = 1,
                            Width = 72, Height = 60
                        }
                    }
                }
            };
        }
    }
}
