using System.Collections.Generic;
using CovertActionTools.Core.Models;

namespace CovertActionTools.Core.Importing.Parsers.SpriteSheets
{
    internal class LegacyFacesSpriteSheetData : BaseLegacySpriteSheetData
    {
        public override string[] Keys => new[] { "FACES", "FACESF" };

        public override SimpleImageModel.SpriteSheetData Create()
        {
            return new SimpleImageModel.SpriteSheetData()
            {
                Sprites = new Dictionary<string, SimpleImageModel.Sprite>()
                {
                    { "top_1", new SimpleImageModel.Sprite() { X = 1, Y = 1, Width = 30, Height = 33 } },
                    { "top_2", new SimpleImageModel.Sprite() { X = 33, Y = 1, Width = 30, Height = 33 } },
                    { "top_3", new SimpleImageModel.Sprite() { X = 65, Y = 1, Width = 30, Height = 33 } },
                    { "top_4", new SimpleImageModel.Sprite() { X = 97, Y = 1, Width = 30, Height = 33 } },
                    { "top_5", new SimpleImageModel.Sprite() { X = 129, Y = 1, Width = 30, Height = 33 } },
                    { "top_6", new SimpleImageModel.Sprite() { X = 161, Y = 1, Width = 30, Height = 33 } },
                    { "top_7", new SimpleImageModel.Sprite() { X = 193, Y = 1, Width = 30, Height = 33 } },
                    { "top_8", new SimpleImageModel.Sprite() { X = 225, Y = 1, Width = 30, Height = 33 } },
                    { "eyes_1", new SimpleImageModel.Sprite() { X = 1, Y = 36, Width = 30, Height = 33 } },
                    { "eyes_2", new SimpleImageModel.Sprite() { X = 33, Y = 36, Width = 30, Height = 33 } },
                    { "eyes_3", new SimpleImageModel.Sprite() { X = 65, Y = 36, Width = 30, Height = 33 } },
                    { "eyes_4", new SimpleImageModel.Sprite() { X = 97, Y = 36, Width = 30, Height = 33 } },
                    { "eyes_5", new SimpleImageModel.Sprite() { X = 129, Y = 36, Width = 30, Height = 33 } },
                    { "eyes_6", new SimpleImageModel.Sprite() { X = 161, Y = 36, Width = 30, Height = 33 } },
                    { "eyes_7", new SimpleImageModel.Sprite() { X = 193, Y = 36, Width = 30, Height = 33 } },
                    { "eyes_8", new SimpleImageModel.Sprite() { X = 225, Y = 36, Width = 30, Height = 33 } },
                    { "nose_1", new SimpleImageModel.Sprite() { X = 1, Y = 71, Width = 30, Height = 33 } },
                    { "nose_2", new SimpleImageModel.Sprite() { X = 33, Y = 71, Width = 30, Height = 33 } },
                    { "nose_3", new SimpleImageModel.Sprite() { X = 65, Y = 71, Width = 30, Height = 33 } },
                    { "nose_4", new SimpleImageModel.Sprite() { X = 97, Y = 71, Width = 30, Height = 33 } },
                    { "nose_5", new SimpleImageModel.Sprite() { X = 129, Y = 71, Width = 30, Height = 33 } },
                    { "nose_6", new SimpleImageModel.Sprite() { X = 161, Y = 71, Width = 30, Height = 33 } },
                    { "nose_7", new SimpleImageModel.Sprite() { X = 193, Y = 71, Width = 30, Height = 33 } },
                    { "nose_8", new SimpleImageModel.Sprite() { X = 225, Y = 71, Width = 30, Height = 33 } },
                    { "mouth_1", new SimpleImageModel.Sprite() { X = 1, Y = 106, Width = 30, Height = 33 } },
                    { "mouth_2", new SimpleImageModel.Sprite() { X = 33, Y = 106, Width = 30, Height = 33 } },
                    { "mouth_3", new SimpleImageModel.Sprite() { X = 65, Y = 106, Width = 30, Height = 33 } },
                    { "mouth_4", new SimpleImageModel.Sprite() { X = 97, Y = 106, Width = 30, Height = 33 } },
                    { "mouth_5", new SimpleImageModel.Sprite() { X = 129, Y = 106, Width = 30, Height = 33 } },
                    { "mouth_6", new SimpleImageModel.Sprite() { X = 161, Y = 106, Width = 30, Height = 33 } },
                    { "mouth_7", new SimpleImageModel.Sprite() { X = 193, Y = 106, Width = 30, Height = 33 } },
                    { "mouth_8", new SimpleImageModel.Sprite() { X = 225, Y = 106, Width = 30, Height = 33 } },
                    { "head_1", new SimpleImageModel.Sprite() { X = 1, Y = 141, Width = 30, Height = 33 } },
                    { "head_2", new SimpleImageModel.Sprite() { X = 33, Y = 141, Width = 30, Height = 33 } },
                    { "head_3", new SimpleImageModel.Sprite() { X = 65, Y = 141, Width = 30, Height = 33 } },
                    { "head_4", new SimpleImageModel.Sprite() { X = 97, Y = 141, Width = 30, Height = 33 } },
                    { "head_5", new SimpleImageModel.Sprite() { X = 129, Y = 141, Width = 30, Height = 33 } },
                    { "head_6", new SimpleImageModel.Sprite() { X = 161, Y = 141, Width = 30, Height = 33 } },
                    { "head_7", new SimpleImageModel.Sprite() { X = 193, Y = 141, Width = 30, Height = 33 } },
                    { "head_8", new SimpleImageModel.Sprite() { X = 225, Y = 141, Width = 30, Height = 33 } },
                    { "body_1", new SimpleImageModel.Sprite() { X = 257, Y = 0, Width = 44, Height = 19 } },
                    { "body_2", new SimpleImageModel.Sprite() { X = 257, Y = 20, Width = 44, Height = 19 } },
                    { "body_3", new SimpleImageModel.Sprite() { X = 257, Y = 40, Width = 44, Height = 19 } },
                    { "body_4", new SimpleImageModel.Sprite() { X = 257, Y = 60, Width = 44, Height = 19 } },
                    { "plane_n", new SimpleImageModel.Sprite() { X = 0, Y = 175, Width = 5, Height = 8 } },
                    { "plane_ne", new SimpleImageModel.Sprite() { X = 9, Y = 176, Width = 7, Height = 7 } },
                    { "plane_e", new SimpleImageModel.Sprite() { X = 18, Y = 178, Width = 8, Height = 5 } },
                    { "plane_se", new SimpleImageModel.Sprite() { X = 27, Y = 176, Width = 7, Height = 7 } },
                    { "plane_s", new SimpleImageModel.Sprite() { X = 36, Y = 175, Width = 5, Height = 8 } },
                    { "plane_sw", new SimpleImageModel.Sprite() { X = 45, Y = 176, Width = 7, Height = 7 } },
                    { "plane_w", new SimpleImageModel.Sprite() { X = 54, Y = 178, Width = 8, Height = 5 } },
                    { "plane_nw", new SimpleImageModel.Sprite() { X = 63, Y = 176, Width = 7, Height = 7 } },
                    { "plane_shadow_n", new SimpleImageModel.Sprite() { X = 0, Y = 184, Width = 5, Height = 8 } },
                    { "plane_shadow_ne", new SimpleImageModel.Sprite() { X = 9, Y = 185, Width = 7, Height = 7 } },
                    { "plane_shadow_e", new SimpleImageModel.Sprite() { X = 18, Y = 187, Width = 8, Height = 5 } },
                    { "plane_shadow_se", new SimpleImageModel.Sprite() { X = 27, Y = 185, Width = 7, Height = 7 } },
                    { "plane_shadow_s", new SimpleImageModel.Sprite() { X = 36, Y = 184, Width = 5, Height = 8 } },
                    { "plane_shadow_sw", new SimpleImageModel.Sprite() { X = 45, Y = 185, Width = 7, Height = 7 } },
                    { "plane_shadow_w", new SimpleImageModel.Sprite() { X = 54, Y = 187, Width = 8, Height = 5 } },
                    { "plane_shadow_nw", new SimpleImageModel.Sprite() { X = 63, Y = 185, Width = 7, Height = 7 } },
                    { "paperclip", new SimpleImageModel.Sprite() { X = 73, Y = 175, Width = 21, Height = 22 } },
                    { "corner_l", new SimpleImageModel.Sprite() { X = 307, Y = 0, Width = 10, Height = 8 } },
                    { "corner_r", new SimpleImageModel.Sprite() { X = 308, Y = 9, Width = 10, Height = 8 } },
                    { "trailer", new SimpleImageModel.Sprite() { X = 257, Y = 160, Width = 26, Height = 14 } },
                    { "clue_source_0", new SimpleImageModel.Sprite() { X = 97, Y = 175, Width = 30, Height = 24 } },
                    { "clue_source_1", new SimpleImageModel.Sprite() { X = 129, Y = 175, Width = 30, Height = 24 } },
                    { "clue_source_2", new SimpleImageModel.Sprite() { X = 161, Y = 175, Width = 30, Height = 24 } },
                    { "clue_source_3", new SimpleImageModel.Sprite() { X = 193, Y = 175, Width = 30, Height = 24 } },
                    { "clue_source_4", new SimpleImageModel.Sprite() { X = 225, Y = 175, Width = 30, Height = 24 } },
                    { "clue_source_5", new SimpleImageModel.Sprite() { X = 257, Y = 175, Width = 30, Height = 24 } },
                    { "clue_source_6", new SimpleImageModel.Sprite() { X = 289, Y = 175, Width = 30, Height = 24 } },
                    { "clue_source_7", new SimpleImageModel.Sprite() { X = 289, Y = 149, Width = 30, Height = 24 } },
                    { "clue_source_8", new SimpleImageModel.Sprite() { X = 289, Y = 123, Width = 30, Height = 24 } },
                }
            };
        }
    }
}
