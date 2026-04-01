using System.Collections.Generic;
using CovertActionTools.Core.Models;

namespace CovertActionTools.Core.Importing.Parsers.SpriteSheets
{
    internal class LegacyEquip1SpriteSheetData : BaseLegacySpriteSheetData
    {
        public override string[] Keys => new[] { "EQUIP1", "EQUIP1M" };

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
                    { "gun", new SimpleImageModel.Sprite() { X = 172, Y = 46, Width = 59, Height = 23 } },
                    { "pistol", new SimpleImageModel.Sprite() { X = 176, Y = 48, Width = 28, Height = 14 } },
                    { "camera", new SimpleImageModel.Sprite() { X = 221, Y = 75, Width = 25, Height = 22 } },
                    { "camera_text", new SimpleImageModel.Sprite() { X = 223, Y = 89, Width = 15, Height = 9 } },
                    { "bugs", new SimpleImageModel.Sprite() { X = 291, Y = 4, Width = 16, Height = 33 } },
                    { "grenade_frag", new SimpleImageModel.Sprite() { X = 175, Y = 1, Width = 6, Height = 13 } },
                    { "grenade_gas", new SimpleImageModel.Sprite() { X = 175, Y = 16, Width = 6, Height = 13 } },
                    { "grenade_flash", new SimpleImageModel.Sprite() { X = 175, Y = 31, Width = 6, Height = 13 } },
                    { "gas_mask", new SimpleImageModel.Sprite() { X = 253, Y = 2, Width = 38, Height = 31 } },
                    { "motion", new SimpleImageModel.Sprite() { X = 268, Y = 1, Width = 10, Height = 18 } },
                    { "armor", new SimpleImageModel.Sprite() { X = 253, Y = 29, Width = 48, Height = 62 } },
                    { "safe", new SimpleImageModel.Sprite() { X = 264, Y = 69, Width = 54, Height = 27 } },
                    { "bullet", new SimpleImageModel.Sprite() { X = 180, Y = 72, Width = 5, Height = 7 } },
                    { "magazine", new SimpleImageModel.Sprite() { X = 180, Y = 82, Width = 11, Height = 15 } },
                    { "proceed", new SimpleImageModel.Sprite() { X = 69, Y = 183, Width = 89, Height = 15 } },
                    { "count", new SimpleImageModel.Sprite() { X = 2, Y = 190, Width = 61, Height = 9 } },
                    { "map_bg", new SimpleImageModel.Sprite() { X = 0, Y = 0, Width = 172, Height = 200 } },
                    { "map_main", new SimpleImageModel.Sprite() { X = 0, Y = 40, Width = 172, Height = 160 } },
                    { "map_mini", new SimpleImageModel.Sprite() { X = 181, Y = 100, Width = 120, Height = 97 } },
                    { "password_hint", new SimpleImageModel.Sprite() { X = 250, Y = 85, Width = 60, Height = 12 } },
                    { "password_prompt", new SimpleImageModel.Sprite() { X = 100, Y = 75, Width = 120, Height = 30 } },
                    { "info_search_prompt", new SimpleImageModel.Sprite() { X = 100, Y = 75, Width = 120, Height = 30 } },
                    { "clue_location_window", new SimpleImageModel.Sprite() { X = 50, Y = 45, Width = 146, Height = 101 } },
                    { "clue_location_map", new SimpleImageModel.Sprite() { X = 106, Y = 53, Width = 72, Height = 60 } },
                    { "clue_location_text", new SimpleImageModel.Sprite() { X = 68, Y = 113, Width = 95, Height = 40 } },
                    { "clue_person_frame", new SimpleImageModel.Sprite() { X = 114, Y = 67, Width = 54, Height = 56 } },
                    { "clue_person_border", new SimpleImageModel.Sprite() { X = 118, Y = 71, Width = 46, Height = 46 } },
                    { "frame", new SimpleImageModel.Sprite() { X = 0, Y = 0, Width = 25, Height = 40 } },
                    { "header", new SimpleImageModel.Sprite() { X = 26, Y = 0, Width = 143, Height = 20 } },
                    { "header_room", new SimpleImageModel.Sprite() { X = 30, Y = 2, Width = 70, Height = 10 } },
                    { "header_timer", new SimpleImageModel.Sprite() { X = 120, Y = 2, Width = 20, Height = 10 } },
                    { "header_status", new SimpleImageModel.Sprite() { X = 30, Y = 12, Width = 140, Height = 10 } },
                }
            };
        }
    }
}
