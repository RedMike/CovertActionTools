using System.Collections.Generic;
using CovertActionTools.Core.Models;

namespace CovertActionTools.Core.Importing.Parsers.SpriteSheets
{
    internal class LegacyEquip2SpriteSheetData : BaseLegacySpriteSheetData
    {
        public override string[] Keys => new[] { "EQUIP2" };

        public override SimpleImageModel.SpriteSheetData Create()
        {
            return new SimpleImageModel.SpriteSheetData()
            {
                Sprites = new Dictionary<string, SimpleImageModel.Sprite>()
                {
                    { "gun_box", new SimpleImageModel.Sprite() { X = 2, Y = 15, Width = 61, Height = 25 } },
                    { "camera_box", new SimpleImageModel.Sprite() { X = 2, Y = 54, Width = 61, Height = 19 } },
                    { "bugs_box", new SimpleImageModel.Sprite() { X = 2, Y = 88, Width = 61, Height = 18 } },
                    { "grenades_frag_box", new SimpleImageModel.Sprite() { X = 2, Y = 122, Width = 61, Height = 17 } },
                    { "grenades_gas_box", new SimpleImageModel.Sprite() { X = 2, Y = 140, Width = 61, Height = 15 } },
                    { "grenades_flash_box", new SimpleImageModel.Sprite() { X = 2, Y = 156, Width = 61, Height = 15 } },
                    { "grenades_mix_box", new SimpleImageModel.Sprite() { X = 2, Y = 172, Width = 61, Height = 15 } },
                    { "gas_mask_box", new SimpleImageModel.Sprite() { X = 69, Y = 15, Width = 45, Height = 36 } },
                    { "motion_box", new SimpleImageModel.Sprite() { X = 116, Y = 15, Width = 42, Height = 36 } },
                    { "armor_box", new SimpleImageModel.Sprite() { X = 69, Y = 60, Width = 89, Height = 67 } },
                    { "safe_box", new SimpleImageModel.Sprite() { X = 69, Y = 136, Width = 89, Height = 45 } },
                    { "gun", new SimpleImageModel.Sprite() { X = 162, Y = 71, Width = 59, Height = 23 } },
                    { "pistol", new SimpleImageModel.Sprite() { X = 162, Y = 25, Width = 28, Height = 14 } },
                    { "camera", new SimpleImageModel.Sprite() { X = 272, Y = 101, Width = 25, Height = 22 } },
                    { "bugs", new SimpleImageModel.Sprite() { X = 303, Y = 41, Width = 16, Height = 33 } },
                    { "grenade_frag", new SimpleImageModel.Sprite() { X = 285, Y = 137, Width = 9, Height = 13 } },
                    { "grenade_gas", new SimpleImageModel.Sprite() { X = 285, Y = 152, Width = 9, Height = 13 } },
                    { "grenade_flash", new SimpleImageModel.Sprite() { X = 285, Y = 167, Width = 9, Height = 13 } },
                    { "gas_mask", new SimpleImageModel.Sprite() { X = 254, Y = 1, Width = 38, Height = 31 } },
                    { "motion", new SimpleImageModel.Sprite() { X = 246, Y = 41, Width = 10, Height = 18 } },
                    { "armor", new SimpleImageModel.Sprite() { X = 223, Y = 71, Width = 48, Height = 62 } },
                    { "safe", new SimpleImageModel.Sprite() { X = 220, Y = 172, Width = 54, Height = 27 } },
                    { "bullet", new SimpleImageModel.Sprite() { X = 201, Y = 41, Width = 3, Height = 7 } },
                    { "magazine", new SimpleImageModel.Sprite() { X = 188, Y = 41, Width = 11, Height = 15 } },
                    { "target", new SimpleImageModel.Sprite() { X = 298, Y = 101, Width = 15, Height = 13 } },
                    { "injury", new SimpleImageModel.Sprite() { X = 162, Y = 95, Width = 9, Height = 7 } },
                }
            };
        }
    }
}
