using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CovertActionTools.Core.Importing.Shared;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.Core.Importing.Parsers
{
    internal class LegacySimpleImageParser : BaseImporter<Dictionary<string, SimpleImageModel>>, ILegacyParser
    {
        //TODO: figure out values for all sprite sheets to encode them
        #region Legacy Data
        private static readonly Dictionary<string, SimpleImageModel.SpriteSheetData> LegacySpriteSheets = new()
        {
            //"BUGS",
            {"CAMERA", new SimpleImageModel.SpriteSheetData()
                {
                    Sprites = new()
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
                }
            },
            //"CHASE",
            {"EQUIP1", new SimpleImageModel.SpriteSheetData()
                {
                    //most sprites on the image are not used
                    //bullets/magazines are duplicated
                    //grenades are duplicated
                    //boxes get highlighted by replacing colours, not by placing the sprite in
                    Sprites = new Dictionary<string, SimpleImageModel.Sprite>()
                    {
                        { "gun_box", new SimpleImageModel.Sprite()
                            {
                                X = 2, Y = 15,
                                Width = 61, Height = 25
                            }
                        },
                        { "camera_box", new SimpleImageModel.Sprite()
                            {
                                X = 2, Y = 54,
                                Width = 61, Height = 19
                            }
                        },
                        { "bugs_box", new SimpleImageModel.Sprite()
                            {
                                X = 2, Y = 88,
                                Width = 61, Height = 18
                            }
                        },
                        { "grenades_frag_box", new SimpleImageModel.Sprite()
                            {
                                X = 2, Y = 122,
                                Width = 61, Height = 17
                            }
                        },
                        { "grenades_gas_box", new SimpleImageModel.Sprite()
                            {
                                X = 2, Y = 140,
                                Width = 61, Height = 15
                            }
                        },
                        { "grenades_flash_box", new SimpleImageModel.Sprite()
                            {
                                X = 2, Y = 156,
                                Width = 61, Height = 15
                            }
                        },
                        { "grenades_mix_box", new SimpleImageModel.Sprite()
                            {
                                X = 2, Y = 172,
                                Width = 61, Height = 15
                            }
                        },
                        { "gas_mask_box", new SimpleImageModel.Sprite()
                            {
                                X = 69, Y = 15,
                                Width = 45, Height = 36
                            }
                        },
                        { "motion_box", new SimpleImageModel.Sprite()
                            {
                                X = 116, Y = 15,
                                Width = 42, Height = 36
                            }
                        },
                        { "armor_box", new SimpleImageModel.Sprite()
                            {
                                X = 69, Y = 60,
                                Width = 89, Height = 67
                            }
                        },
                        { "safe_box", new SimpleImageModel.Sprite()
                            {
                                X = 69, Y = 136,
                                Width = 89, Height = 45
                            }
                        },
                        { "gun", new SimpleImageModel.Sprite()
                            {
                                X = 172, Y = 46,
                                Width = 59, Height = 23
                            }
                        },
                        { "pistol", new SimpleImageModel.Sprite()
                            {
                                X = 176, Y = 48,
                                Width = 28, Height = 14
                            }
                        },
                        { "camera", new SimpleImageModel.Sprite()
                            {
                                X = 221, Y = 75,
                                Width = 25, Height = 22
                            }
                        },
                        { "camera_text", new SimpleImageModel.Sprite()
                            {
                                X = 223, Y = 89,
                                Width = 15, Height = 9
                            }
                        },
                        { "bugs", new SimpleImageModel.Sprite()
                            {
                                X = 291, Y = 2,
                                Width = 16, Height = 33
                            }
                        },
                        { "grenade_frag", new SimpleImageModel.Sprite()
                            {
                                X = 175, Y = 1,
                                Width = 6, Height = 13
                            }
                        },
                        { "grenade_gas", new SimpleImageModel.Sprite()
                            {
                                X = 175, Y = 16,
                                Width = 6, Height = 13
                            }
                        },
                        { "grenade_flash", new SimpleImageModel.Sprite()
                            {
                                X = 175, Y = 31,
                                Width = 6, Height = 13
                            }
                        },
                        { "gas_mask", new SimpleImageModel.Sprite()
                            {
                                X = 253, Y = 2,
                                Width = 38, Height = 31
                            }
                        },
                        { "motion", new SimpleImageModel.Sprite()
                            {
                                X = 268, Y = 1,
                                Width = 10, Height = 18
                            }
                        },
                        { "armor", new SimpleImageModel.Sprite()
                            {
                                X = 253, Y = 29,
                                Width = 48, Height = 62
                            }
                        },
                        { "safe", new SimpleImageModel.Sprite()
                            {
                                X = 264, Y = 69,
                                Width = 54, Height = 27
                            }
                        },
                        { "bullet", new SimpleImageModel.Sprite()
                            {
                                X = 180, Y = 72,
                                Width = 5, Height = 7
                            }
                        },
                        { "magazine", new SimpleImageModel.Sprite()
                            {
                                X = 180, Y = 82,
                                Width = 11, Height = 15
                            }
                        },
                        { "proceed", new SimpleImageModel.Sprite()
                            {
                                X = 69, Y = 183,
                                Width = 89, Height = 15
                            }
                        },
                        { "count", new SimpleImageModel.Sprite()
                            {
                                X = 2, Y = 190,
                                Width = 61, Height = 9
                            }
                        },
                        { "map_bg", new SimpleImageModel.Sprite()
                            {
                                X = 0, Y = 0,
                                Width = 172, Height = 200
                            }
                        },
                        { "map_main", new SimpleImageModel.Sprite()
                            {
                                X = 0, Y = 40,
                                Width = 172, Height = 160
                            }
                        },
                        { "map_mini", new SimpleImageModel.Sprite()
                            {
                                X = 181, Y = 100,
                                Width = 120, Height = 97
                            }
                        },
                        { "frame", new SimpleImageModel.Sprite()
                            {
                                X = 0, Y = 0,
                                Width = 25, Height = 40
                            }
                        },
                        { "header", new SimpleImageModel.Sprite()
                            {
                                X = 26, Y = 0,
                                Width = 143, Height = 20
                            }
                        },
                        { "header_room", new SimpleImageModel.Sprite()
                            {
                                X = 30, Y = 2,
                                Width = 70, Height = 10
                            }
                        },
                        { "header_timer", new SimpleImageModel.Sprite()
                            {
                                X = 120, Y = 2,
                                Width = 20, Height = 10
                            }
                        },
                        { "header_status", new SimpleImageModel.Sprite()
                            {
                                X = 30, Y = 12,
                                Width = 140, Height = 10
                            }
                        },
                    }
                }
            },
            {"EQUIP1M", new SimpleImageModel.SpriteSheetData()
                    {
                        Sprites = new Dictionary<string, SimpleImageModel.Sprite>() {
                        { "gun_box", new SimpleImageModel.Sprite()
                            {
                                X = 2, Y = 15,
                                Width = 61, Height = 25
                            }
                        },
                        { "camera_box", new SimpleImageModel.Sprite()
                            {
                                X = 2, Y = 54,
                                Width = 61, Height = 19
                            }
                        },
                        { "bugs_box", new SimpleImageModel.Sprite()
                            {
                                X = 2, Y = 88,
                                Width = 61, Height = 18
                            }
                        },
                        { "grenades_frag_box", new SimpleImageModel.Sprite()
                            {
                                X = 2, Y = 122,
                                Width = 61, Height = 17
                            }
                        },
                        { "grenades_gas_box", new SimpleImageModel.Sprite()
                            {
                                X = 2, Y = 140,
                                Width = 61, Height = 15
                            }
                        },
                        { "grenades_flash_box", new SimpleImageModel.Sprite()
                            {
                                X = 2, Y = 156,
                                Width = 61, Height = 15
                            }
                        },
                        { "grenades_mix_box", new SimpleImageModel.Sprite()
                            {
                                X = 2, Y = 172,
                                Width = 61, Height = 15
                            }
                        },
                        { "gas_mask_box", new SimpleImageModel.Sprite()
                            {
                                X = 69, Y = 15,
                                Width = 45, Height = 36
                            }
                        },
                        { "motion_box", new SimpleImageModel.Sprite()
                            {
                                X = 116, Y = 15,
                                Width = 42, Height = 36
                            }
                        },
                        { "armor_box", new SimpleImageModel.Sprite()
                            {
                                X = 69, Y = 60,
                                Width = 89, Height = 67
                            }
                        },
                        { "safe_box", new SimpleImageModel.Sprite()
                            {
                                X = 69, Y = 136,
                                Width = 89, Height = 45
                            }
                        },
                        { "gun", new SimpleImageModel.Sprite()
                            {
                                X = 172, Y = 46,
                                Width = 59, Height = 23
                            }
                        },
                        { "pistol", new SimpleImageModel.Sprite()
                            {
                                X = 176, Y = 48,
                                Width = 28, Height = 14
                            }
                        },
                        { "camera", new SimpleImageModel.Sprite()
                            {
                                X = 221, Y = 75,
                                Width = 25, Height = 22
                            }
                        },
                        { "camera_text", new SimpleImageModel.Sprite()
                            {
                                X = 223, Y = 89,
                                Width = 15, Height = 9
                            }
                        },
                        { "bugs", new SimpleImageModel.Sprite()
                            {
                                X = 291, Y = 4,
                                Width = 16, Height = 33
                            }
                        },
                        { "grenade_frag", new SimpleImageModel.Sprite()
                            {
                                X = 175, Y = 1,
                                Width = 6, Height = 13
                            }
                        },
                        { "grenade_gas", new SimpleImageModel.Sprite()
                            {
                                X = 175, Y = 16,
                                Width = 6, Height = 13
                            }
                        },
                        { "grenade_flash", new SimpleImageModel.Sprite()
                            {
                                X = 175, Y = 31,
                                Width = 6, Height = 13
                            }
                        },
                        { "gas_mask", new SimpleImageModel.Sprite()
                            {
                                X = 253, Y = 2,
                                Width = 38, Height = 31
                            }
                        },
                        { "motion", new SimpleImageModel.Sprite()
                            {
                                X = 268, Y = 1,
                                Width = 10, Height = 18
                            }
                        },
                        { "armor", new SimpleImageModel.Sprite()
                            {
                                X = 253, Y = 29,
                                Width = 48, Height = 62
                            }
                        },
                        { "safe", new SimpleImageModel.Sprite()
                            {
                                X = 264, Y = 69,
                                Width = 54, Height = 27
                            }
                        },
                        { "bullet", new SimpleImageModel.Sprite()
                            {
                                X = 180, Y = 72,
                                Width = 5, Height = 7
                            }
                        },
                        { "magazine", new SimpleImageModel.Sprite()
                            {
                                X = 180, Y = 82,
                                Width = 11, Height = 15
                            }
                        },
                        { "proceed", new SimpleImageModel.Sprite()
                            {
                                X = 69, Y = 183,
                                Width = 89, Height = 15
                            }
                        },
                        { "count", new SimpleImageModel.Sprite()
                            {
                                X = 2, Y = 190,
                                Width = 61, Height = 9
                            }
                        },
                        { "map_bg", new SimpleImageModel.Sprite()
                            {
                                X = 0, Y = 0,
                                Width = 172, Height = 200
                            }
                        },
                        { "map_main", new SimpleImageModel.Sprite()
                            {
                                X = 0, Y = 40,
                                Width = 172, Height = 160
                            }
                        },
                        { "map_mini", new SimpleImageModel.Sprite()
                            {
                                X = 181, Y = 100,
                                Width = 120, Height = 97
                            }
                        },
                        { "frame", new SimpleImageModel.Sprite()
                            {
                                X = 0, Y = 0,
                                Width = 25, Height = 40
                            }
                        },
                        { "header", new SimpleImageModel.Sprite()
                            {
                                X = 26, Y = 0,
                                Width = 143, Height = 20
                            }
                        },
                        { "header_room", new SimpleImageModel.Sprite()
                            {
                                X = 30, Y = 2,
                                Width = 70, Height = 10
                            }
                        },
                        { "header_timer", new SimpleImageModel.Sprite()
                            {
                                X = 120, Y = 2,
                                Width = 20, Height = 10
                            }
                        },
                        { "header_status", new SimpleImageModel.Sprite()
                            {
                                X = 30, Y = 12,
                                Width = 140, Height = 10
                            }
                        },
                    }
                }
            },
            {"EQUIP2", new SimpleImageModel.SpriteSheetData()
                {
                    //most sprites on the image are not used
                    //bullets/magazines are duplicated
                    //grenades are duplicated
                    //boxes get highlighted by replacing colours, not by placing the sprite in
                    Sprites = new Dictionary<string, SimpleImageModel.Sprite>()
                    {
                        { "gun_box", new SimpleImageModel.Sprite()
                            {
                                X = 2, Y = 15,
                                Width = 61, Height = 25
                            }
                        },
                        { "camera_box", new SimpleImageModel.Sprite()
                            {
                                X = 2, Y = 54,
                                Width = 61, Height = 19
                            }
                        },
                        { "bugs_box", new SimpleImageModel.Sprite()
                            {
                                X = 2, Y = 88,
                                Width = 61, Height = 18
                            }
                        },
                        { "grenades_frag_box", new SimpleImageModel.Sprite()
                            {
                                X = 2, Y = 122,
                                Width = 61, Height = 17
                            }
                        },
                        { "grenades_gas_box", new SimpleImageModel.Sprite()
                            {
                                X = 2, Y = 140,
                                Width = 61, Height = 15
                            }
                        },
                        { "grenades_flash_box", new SimpleImageModel.Sprite()
                            {
                                X = 2, Y = 156,
                                Width = 61, Height = 15
                            }
                        },
                        { "grenades_mix_box", new SimpleImageModel.Sprite()
                            {
                                X = 2, Y = 172,
                                Width = 61, Height = 15
                            }
                        },
                        { "gas_mask_box", new SimpleImageModel.Sprite()
                            {
                                X = 69, Y = 15,
                                Width = 45, Height = 36
                            }
                        },
                        { "motion_box", new SimpleImageModel.Sprite()
                            {
                                X = 116, Y = 15,
                                Width = 42, Height = 36
                            }
                        },
                        { "armor_box", new SimpleImageModel.Sprite()
                            {
                                X = 69, Y = 60,
                                Width = 89, Height = 67
                            }
                        },
                        { "safe_box", new SimpleImageModel.Sprite()
                            {
                                X = 69, Y = 136,
                                Width = 89, Height = 45
                            }
                        },
                        { "gun", new SimpleImageModel.Sprite()
                            {
                                X = 162, Y = 71,
                                Width = 59, Height = 23
                            }
                        },
                        { "pistol", new SimpleImageModel.Sprite()
                            {
                                X = 162, Y = 25,
                                Width = 28, Height = 14
                            }
                        },
                        { "camera", new SimpleImageModel.Sprite()
                            {
                                X = 272, Y = 101,
                                Width = 25, Height = 22
                            }
                        },
                        { "bugs", new SimpleImageModel.Sprite()
                            {
                                X = 303, Y = 41,
                                Width = 16, Height = 33
                            }
                        },
                        { "grenade_frag", new SimpleImageModel.Sprite()
                            {
                                X = 285, Y = 137,
                                Width = 9, Height = 13
                            }
                        },
                        { "grenade_gas", new SimpleImageModel.Sprite()
                            {
                                X = 285, Y = 152,
                                Width = 9, Height = 13
                            }
                        },
                        { "grenade_flash", new SimpleImageModel.Sprite()
                            {
                                X = 285, Y = 167,
                                Width = 9, Height = 13
                            }
                        },
                        { "gas_mask", new SimpleImageModel.Sprite()
                            {
                                X = 254, Y = 1,
                                Width = 38, Height = 31
                            }
                        },
                        { "motion", new SimpleImageModel.Sprite()
                            {
                                X = 246, Y = 41,
                                Width = 10, Height = 18
                            }
                        },
                        { "armor", new SimpleImageModel.Sprite()
                            {
                                X = 223, Y = 71,
                                Width = 48, Height = 62
                            }
                        },
                        { "safe", new SimpleImageModel.Sprite()
                            {
                                X = 220, Y = 172,
                                Width = 54, Height = 27
                            }
                        },
                        { "bullet", new SimpleImageModel.Sprite()
                            {
                                X = 201, Y = 41,
                                Width = 3, Height = 7
                            }
                        },
                        { "magazine", new SimpleImageModel.Sprite()
                            {
                                X = 188, Y = 41,
                                Width = 11, Height = 15
                            }
                        },
                        { "target", new SimpleImageModel.Sprite()
                            {
                                X = 298, Y = 101,
                                Width = 15, Height = 13
                            }
                        },
                        { "injury", new SimpleImageModel.Sprite()
                            {
                                X = 162, Y = 95,
                                Width = 9, Height = 7
                            }
                        }
                    }
                }
            },
            {"FACES", new SimpleImageModel.SpriteSheetData()
                {
                    Sprites = new()
                    {
                        { "top_1", new SimpleImageModel.Sprite()
                            {
                                X = 1, Y = 1,
                                Width = 30,
                                Height = 33
                            }
                        },
                        { "top_2", new SimpleImageModel.Sprite()
                            {
                                X = 33, Y = 1,
                                Width = 30,
                                Height = 33
                            }
                        },
                        { "top_3", new SimpleImageModel.Sprite()
                            {
                                X = 65, Y = 1,
                                Width = 30,
                                Height = 33
                            }
                        },
                        { "top_4", new SimpleImageModel.Sprite()
                            {
                                X = 97, Y = 1,
                                Width = 30,
                                Height = 33
                            }
                        },
                        { "top_5", new SimpleImageModel.Sprite()
                            {
                                X = 129, Y = 1,
                                Width = 30,
                                Height = 33
                            }
                        },
                        { "top_6", new SimpleImageModel.Sprite()
                            {
                                X = 161, Y = 1,
                                Width = 30,
                                Height = 33
                            }
                        },
                        { "top_7", new SimpleImageModel.Sprite()
                            {
                                X = 193, Y = 1,
                                Width = 30,
                                Height = 33
                            }
                        },
                        { "top_8", new SimpleImageModel.Sprite()
                            {
                                X = 225, Y = 1,
                                Width = 30,
                                Height = 33
                            }
                        },
                        { "eyes_1", new SimpleImageModel.Sprite()
                            {
                                X = 1, Y = 36,
                                Width = 30,
                                Height = 33
                            }
                        },
                        { "eyes_2", new SimpleImageModel.Sprite()
                            {
                                X = 33, Y = 36,
                                Width = 30,
                                Height = 33
                            }
                        },
                        { "eyes_3", new SimpleImageModel.Sprite()
                            {
                                X = 65, Y = 36,
                                Width = 30,
                                Height = 33
                            }
                        },
                        { "eyes_4", new SimpleImageModel.Sprite()
                            {
                                X = 97, Y = 36,
                                Width = 30,
                                Height = 33
                            }
                        },
                        { "eyes_5", new SimpleImageModel.Sprite()
                            {
                                X = 129, Y = 36,
                                Width = 30,
                                Height = 33
                            }
                        },
                        { "eyes_6", new SimpleImageModel.Sprite()
                            {
                                X = 161, Y = 36,
                                Width = 30,
                                Height = 33
                            }
                        },
                        { "eyes_7", new SimpleImageModel.Sprite()
                            {
                                X = 193, Y = 36,
                                Width = 30,
                                Height = 33
                            }
                        },
                        { "eyes_8", new SimpleImageModel.Sprite()
                            {
                                X = 225, Y = 36,
                                Width = 30,
                                Height = 33
                            }
                        },
                        { "nose_1", new SimpleImageModel.Sprite()
                            {
                                X = 1, Y = 71,
                                Width = 30,
                                Height = 33
                            }
                        },
                        { "nose_2", new SimpleImageModel.Sprite()
                            {
                                X = 33, Y = 71,
                                Width = 30,
                                Height = 33
                            }
                        },
                        { "nose_3", new SimpleImageModel.Sprite()
                            {
                                X = 65, Y = 71,
                                Width = 30,
                                Height = 33
                            }
                        },
                        { "nose_4", new SimpleImageModel.Sprite()
                            {
                                X = 97, Y = 71,
                                Width = 30,
                                Height = 33
                            }
                        },
                        { "nose_5", new SimpleImageModel.Sprite()
                            {
                                X = 129, Y = 71,
                                Width = 30,
                                Height = 33
                            }
                        },
                        { "nose_6", new SimpleImageModel.Sprite()
                            {
                                X = 161, Y = 71,
                                Width = 30,
                                Height = 33
                            }
                        },
                        { "nose_7", new SimpleImageModel.Sprite()
                            {
                                X = 193, Y = 71,
                                Width = 30,
                                Height = 33
                            }
                        },
                        { "nose_8", new SimpleImageModel.Sprite()
                            {
                                X = 225, Y = 71,
                                Width = 30,
                                Height = 33
                            }
                        },
                        { "mouth_1", new SimpleImageModel.Sprite()
                            {
                                X = 1, Y = 106,
                                Width = 30,
                                Height = 33
                            }
                        },
                        { "mouth_2", new SimpleImageModel.Sprite()
                            {
                                X = 33, Y = 106,
                                Width = 30,
                                Height = 33
                            }
                        },
                        { "mouth_3", new SimpleImageModel.Sprite()
                            {
                                X = 65, Y = 106,
                                Width = 30,
                                Height = 33
                            }
                        },
                        { "mouth_4", new SimpleImageModel.Sprite()
                            {
                                X = 97, Y = 106,
                                Width = 30,
                                Height = 33
                            }
                        },
                        { "mouth_5", new SimpleImageModel.Sprite()
                            {
                                X = 129, Y = 106,
                                Width = 30,
                                Height = 33
                            }
                        },
                        { "mouth_6", new SimpleImageModel.Sprite()
                            {
                                X = 161, Y = 106,
                                Width = 30,
                                Height = 33
                            }
                        },
                        { "mouth_7", new SimpleImageModel.Sprite()
                            {
                                X = 193, Y = 106,
                                Width = 30,
                                Height = 33
                            }
                        },
                        { "mouth_8", new SimpleImageModel.Sprite()
                            {
                                X = 225, Y = 106,
                                Width = 30,
                                Height = 33
                            }
                        },
                        { "head_1", new SimpleImageModel.Sprite()
                            {
                                X = 1, Y = 141,
                                Width = 30,
                                Height = 33
                            }
                        },
                        { "head_2", new SimpleImageModel.Sprite()
                            {
                                X = 33, Y = 141,
                                Width = 30,
                                Height = 33
                            }
                        },
                        { "head_3", new SimpleImageModel.Sprite()
                            {
                                X = 65, Y = 141,
                                Width = 30,
                                Height = 33
                            }
                        },
                        { "head_4", new SimpleImageModel.Sprite()
                            {
                                X = 97, Y = 141,
                                Width = 30,
                                Height = 33
                            }
                        },
                        { "head_5", new SimpleImageModel.Sprite()
                            {
                                X = 129, Y = 141,
                                Width = 30,
                                Height = 33
                            }
                        },
                        { "head_6", new SimpleImageModel.Sprite()
                            {
                                X = 161, Y = 141,
                                Width = 30,
                                Height = 33
                            }
                        },
                        { "head_7", new SimpleImageModel.Sprite()
                            {
                                X = 193, Y = 141,
                                Width = 30,
                                Height = 33
                            }
                        },
                        { "head_8", new SimpleImageModel.Sprite()
                            {
                                X = 225, Y = 141,
                                Width = 30,
                                Height = 33
                            }
                        },
                        
                        { "body_1", new SimpleImageModel.Sprite()
                            {
                                X = 257, Y = 0,
                                Width = 44,
                                Height = 19
                            }
                        },
                        { "body_2", new SimpleImageModel.Sprite()
                            {
                                X = 257, Y = 20,
                                Width = 44,
                                Height = 19
                            }
                        },
                        { "body_3", new SimpleImageModel.Sprite()
                            {
                                X = 257, Y = 40,
                                Width = 44,
                                Height = 19
                            }
                        },
                        { "body_4", new SimpleImageModel.Sprite()
                            {
                                X = 257, Y = 60,
                                Width = 44,
                                Height = 19
                            }
                        },
                        { "plane_n", new SimpleImageModel.Sprite()
                            {
                                X = 0, Y = 175,
                                Width = 5, 
                                Height = 8
                            }
                        },
                        { "plane_ne", new SimpleImageModel.Sprite()
                            {
                                X = 9, Y = 176,
                                Width = 7, 
                                Height = 7
                            }
                        },
                        { "plane_e", new SimpleImageModel.Sprite()
                            {
                                X = 18, Y = 178,
                                Width = 8, 
                                Height = 5
                            }
                        },
                        { "plane_se", new SimpleImageModel.Sprite()
                            {
                                X = 27, Y = 176,
                                Width = 7, 
                                Height = 7
                            }
                        },
                        { "plane_s", new SimpleImageModel.Sprite()
                            {
                                X = 36, Y = 175,
                                Width = 5, 
                                Height = 8
                            }
                        },
                        { "plane_sw", new SimpleImageModel.Sprite()
                            {
                                X = 45, Y = 176,
                                Width = 7, 
                                Height = 7
                            }
                        },
                        { "plane_w", new SimpleImageModel.Sprite()
                            {
                                X = 54, Y = 178,
                                Width = 8, 
                                Height = 5
                            }
                        },
                        { "plane_nw", new SimpleImageModel.Sprite()
                            {
                                X = 63, Y = 176,
                                Width = 7, 
                                Height = 7
                            }
                        },
                        { "plane_shadow_n", new SimpleImageModel.Sprite()
                            {
                                X = 0, Y = 184,
                                Width = 5, 
                                Height = 8
                            }
                        },
                        { "plane_shadow_ne", new SimpleImageModel.Sprite()
                            {
                                X = 9, Y = 185,
                                Width = 7, 
                                Height = 7
                            }
                        },
                        { "plane_shadow_e", new SimpleImageModel.Sprite()
                            {
                                X = 18, Y = 187,
                                Width = 8, 
                                Height = 5
                            }
                        },
                        { "plane_shadow_se", new SimpleImageModel.Sprite()
                            {
                                X = 27, Y = 185,
                                Width = 7, 
                                Height = 7
                            }
                        },
                        { "plane_shadow_s", new SimpleImageModel.Sprite()
                            {
                                X = 36, Y = 184,
                                Width = 5, 
                                Height = 8
                            }
                        },
                        { "plane_shadow_sw", new SimpleImageModel.Sprite()
                            {
                                X = 45, Y = 185,
                                Width = 7, 
                                Height = 7
                            }
                        },
                        { "plane_shadow_w", new SimpleImageModel.Sprite()
                            {
                                X = 54, Y = 187,
                                Width = 8, 
                                Height = 5
                            }
                        },
                        { "plane_shadow_nw", new SimpleImageModel.Sprite()
                            {
                                X = 63, Y = 185,
                                Width = 7, 
                                Height = 7
                            }
                        },
                        { "paperclip", new SimpleImageModel.Sprite()
                            {
                                X = 73, Y = 175,
                                Width = 21,
                                Height = 22
                            }
                        },
                        { "corner_l", new SimpleImageModel.Sprite()
                            {
                                X = 307, Y = 0,
                                Width = 10,
                                Height = 8
                            }
                        },
                        { "corner_r", new SimpleImageModel.Sprite()
                            {
                                X = 308, Y = 9,
                                Width = 10,
                                Height = 8
                            }
                        },
                        { "trailer", new SimpleImageModel.Sprite()
                            {
                                X = 257, Y = 160,
                                Width = 26,
                                Height = 14
                            }
                        },
                        { "clue_source_0", new SimpleImageModel.Sprite()
                            {
                                X = 97, Y = 175,
                                Width = 30,
                                Height = 24
                            }
                        },
                        { "clue_source_1", new SimpleImageModel.Sprite()
                            {
                                X = 129, Y = 175,
                                Width = 30,
                                Height = 24
                            }
                        },
                        { "clue_source_2", new SimpleImageModel.Sprite()
                            {
                                X = 161, Y = 175,
                                Width = 30,
                                Height = 24
                            }
                        },
                        { "clue_source_3", new SimpleImageModel.Sprite()
                            {
                                X = 193, Y = 175,
                                Width = 30,
                                Height = 24
                            }
                        },
                        { "clue_source_4", new SimpleImageModel.Sprite()
                            {
                                X = 225, Y = 175,
                                Width = 30,
                                Height = 24
                            }
                        },
                        { "clue_source_5", new SimpleImageModel.Sprite()
                            {
                                X = 257, Y = 175,
                                Width = 30,
                                Height = 24
                            }
                        },
                        { "clue_source_6", new SimpleImageModel.Sprite()
                            {
                                X = 289, Y = 175,
                                Width = 30,
                                Height = 24
                            }
                        },
                        { "clue_source_7", new SimpleImageModel.Sprite()
                            {
                                X = 289, Y = 149,
                                Width = 30,
                                Height = 24
                            }
                        },
                        { "clue_source_8", new SimpleImageModel.Sprite()
                            {
                                X = 289, Y = 123,
                                Width = 30,
                                Height = 24
                            }
                        },
                    }
                }
            },
            {"FACESF", new SimpleImageModel.SpriteSheetData()
                {
                    Sprites = new()
                    {
                        { "top_1", new SimpleImageModel.Sprite()
                            {
                                X = 1, Y = 1,
                                Width = 30,
                                Height = 33
                            }
                        },
                        { "top_2", new SimpleImageModel.Sprite()
                            {
                                X = 33, Y = 1,
                                Width = 30,
                                Height = 33
                            }
                        },
                        { "top_3", new SimpleImageModel.Sprite()
                            {
                                X = 65, Y = 1,
                                Width = 30,
                                Height = 33
                            }
                        },
                        { "top_4", new SimpleImageModel.Sprite()
                            {
                                X = 97, Y = 1,
                                Width = 30,
                                Height = 33
                            }
                        },
                        { "top_5", new SimpleImageModel.Sprite()
                            {
                                X = 129, Y = 1,
                                Width = 30,
                                Height = 33
                            }
                        },
                        { "top_6", new SimpleImageModel.Sprite()
                            {
                                X = 161, Y = 1,
                                Width = 30,
                                Height = 33
                            }
                        },
                        { "top_7", new SimpleImageModel.Sprite()
                            {
                                X = 193, Y = 1,
                                Width = 30,
                                Height = 33
                            }
                        },
                        { "top_8", new SimpleImageModel.Sprite()
                            {
                                X = 225, Y = 1,
                                Width = 30,
                                Height = 33
                            }
                        },
                        { "eyes_1", new SimpleImageModel.Sprite()
                            {
                                X = 1, Y = 36,
                                Width = 30,
                                Height = 33
                            }
                        },
                        { "eyes_2", new SimpleImageModel.Sprite()
                            {
                                X = 33, Y = 36,
                                Width = 30,
                                Height = 33
                            }
                        },
                        { "eyes_3", new SimpleImageModel.Sprite()
                            {
                                X = 65, Y = 36,
                                Width = 30,
                                Height = 33
                            }
                        },
                        { "eyes_4", new SimpleImageModel.Sprite()
                            {
                                X = 97, Y = 36,
                                Width = 30,
                                Height = 33
                            }
                        },
                        { "eyes_5", new SimpleImageModel.Sprite()
                            {
                                X = 129, Y = 36,
                                Width = 30,
                                Height = 33
                            }
                        },
                        { "eyes_6", new SimpleImageModel.Sprite()
                            {
                                X = 161, Y = 36,
                                Width = 30,
                                Height = 33
                            }
                        },
                        { "eyes_7", new SimpleImageModel.Sprite()
                            {
                                X = 193, Y = 36,
                                Width = 30,
                                Height = 33
                            }
                        },
                        { "eyes_8", new SimpleImageModel.Sprite()
                            {
                                X = 225, Y = 36,
                                Width = 30,
                                Height = 33
                            }
                        },
                        { "nose_1", new SimpleImageModel.Sprite()
                            {
                                X = 1, Y = 71,
                                Width = 30,
                                Height = 33
                            }
                        },
                        { "nose_2", new SimpleImageModel.Sprite()
                            {
                                X = 33, Y = 71,
                                Width = 30,
                                Height = 33
                            }
                        },
                        { "nose_3", new SimpleImageModel.Sprite()
                            {
                                X = 65, Y = 71,
                                Width = 30,
                                Height = 33
                            }
                        },
                        { "nose_4", new SimpleImageModel.Sprite()
                            {
                                X = 97, Y = 71,
                                Width = 30,
                                Height = 33
                            }
                        },
                        { "nose_5", new SimpleImageModel.Sprite()
                            {
                                X = 129, Y = 71,
                                Width = 30,
                                Height = 33
                            }
                        },
                        { "nose_6", new SimpleImageModel.Sprite()
                            {
                                X = 161, Y = 71,
                                Width = 30,
                                Height = 33
                            }
                        },
                        { "nose_7", new SimpleImageModel.Sprite()
                            {
                                X = 193, Y = 71,
                                Width = 30,
                                Height = 33
                            }
                        },
                        { "nose_8", new SimpleImageModel.Sprite()
                            {
                                X = 225, Y = 71,
                                Width = 30,
                                Height = 33
                            }
                        },
                        { "mouth_1", new SimpleImageModel.Sprite()
                            {
                                X = 1, Y = 106,
                                Width = 30,
                                Height = 33
                            }
                        },
                        { "mouth_2", new SimpleImageModel.Sprite()
                            {
                                X = 33, Y = 106,
                                Width = 30,
                                Height = 33
                            }
                        },
                        { "mouth_3", new SimpleImageModel.Sprite()
                            {
                                X = 65, Y = 106,
                                Width = 30,
                                Height = 33
                            }
                        },
                        { "mouth_4", new SimpleImageModel.Sprite()
                            {
                                X = 97, Y = 106,
                                Width = 30,
                                Height = 33
                            }
                        },
                        { "mouth_5", new SimpleImageModel.Sprite()
                            {
                                X = 129, Y = 106,
                                Width = 30,
                                Height = 33
                            }
                        },
                        { "mouth_6", new SimpleImageModel.Sprite()
                            {
                                X = 161, Y = 106,
                                Width = 30,
                                Height = 33
                            }
                        },
                        { "mouth_7", new SimpleImageModel.Sprite()
                            {
                                X = 193, Y = 106,
                                Width = 30,
                                Height = 33
                            }
                        },
                        { "mouth_8", new SimpleImageModel.Sprite()
                            {
                                X = 225, Y = 106,
                                Width = 30,
                                Height = 33
                            }
                        },
                        { "head_1", new SimpleImageModel.Sprite()
                            {
                                X = 1, Y = 141,
                                Width = 30,
                                Height = 33
                            }
                        },
                        { "head_2", new SimpleImageModel.Sprite()
                            {
                                X = 33, Y = 141,
                                Width = 30,
                                Height = 33
                            }
                        },
                        { "head_3", new SimpleImageModel.Sprite()
                            {
                                X = 65, Y = 141,
                                Width = 30,
                                Height = 33
                            }
                        },
                        { "head_4", new SimpleImageModel.Sprite()
                            {
                                X = 97, Y = 141,
                                Width = 30,
                                Height = 33
                            }
                        },
                        { "head_5", new SimpleImageModel.Sprite()
                            {
                                X = 129, Y = 141,
                                Width = 30,
                                Height = 33
                            }
                        },
                        { "head_6", new SimpleImageModel.Sprite()
                            {
                                X = 161, Y = 141,
                                Width = 30,
                                Height = 33
                            }
                        },
                        { "head_7", new SimpleImageModel.Sprite()
                            {
                                X = 193, Y = 141,
                                Width = 30,
                                Height = 33
                            }
                        },
                        { "head_8", new SimpleImageModel.Sprite()
                            {
                                X = 225, Y = 141,
                                Width = 30,
                                Height = 33
                            }
                        },
                        
                        { "body_1", new SimpleImageModel.Sprite()
                            {
                                X = 257, Y = 0,
                                Width = 44,
                                Height = 19
                            }
                        },
                        { "body_2", new SimpleImageModel.Sprite()
                            {
                                X = 257, Y = 20,
                                Width = 44,
                                Height = 19
                            }
                        },
                        { "body_3", new SimpleImageModel.Sprite()
                            {
                                X = 257, Y = 40,
                                Width = 44,
                                Height = 19
                            }
                        },
                        { "body_4", new SimpleImageModel.Sprite()
                            {
                                X = 257, Y = 60,
                                Width = 44,
                                Height = 19
                            }
                        },
                        { "plane_n", new SimpleImageModel.Sprite()
                            {
                                X = 0, Y = 175,
                                Width = 5, 
                                Height = 8
                            }
                        },
                        { "plane_ne", new SimpleImageModel.Sprite()
                            {
                                X = 9, Y = 176,
                                Width = 7, 
                                Height = 7
                            }
                        },
                        { "plane_e", new SimpleImageModel.Sprite()
                            {
                                X = 18, Y = 178,
                                Width = 8, 
                                Height = 5
                            }
                        },
                        { "plane_se", new SimpleImageModel.Sprite()
                            {
                                X = 27, Y = 176,
                                Width = 7, 
                                Height = 7
                            }
                        },
                        { "plane_s", new SimpleImageModel.Sprite()
                            {
                                X = 36, Y = 175,
                                Width = 5, 
                                Height = 8
                            }
                        },
                        { "plane_sw", new SimpleImageModel.Sprite()
                            {
                                X = 45, Y = 176,
                                Width = 7, 
                                Height = 7
                            }
                        },
                        { "plane_w", new SimpleImageModel.Sprite()
                            {
                                X = 54, Y = 178,
                                Width = 8, 
                                Height = 5
                            }
                        },
                        { "plane_nw", new SimpleImageModel.Sprite()
                            {
                                X = 63, Y = 176,
                                Width = 7, 
                                Height = 7
                            }
                        },
                        { "plane_shadow_n", new SimpleImageModel.Sprite()
                            {
                                X = 0, Y = 184,
                                Width = 5, 
                                Height = 8
                            }
                        },
                        { "plane_shadow_ne", new SimpleImageModel.Sprite()
                            {
                                X = 9, Y = 185,
                                Width = 7, 
                                Height = 7
                            }
                        },
                        { "plane_shadow_e", new SimpleImageModel.Sprite()
                            {
                                X = 18, Y = 187,
                                Width = 8, 
                                Height = 5
                            }
                        },
                        { "plane_shadow_se", new SimpleImageModel.Sprite()
                            {
                                X = 27, Y = 185,
                                Width = 7, 
                                Height = 7
                            }
                        },
                        { "plane_shadow_s", new SimpleImageModel.Sprite()
                            {
                                X = 36, Y = 184,
                                Width = 5, 
                                Height = 8
                            }
                        },
                        { "plane_shadow_sw", new SimpleImageModel.Sprite()
                            {
                                X = 45, Y = 185,
                                Width = 7, 
                                Height = 7
                            }
                        },
                        { "plane_shadow_w", new SimpleImageModel.Sprite()
                            {
                                X = 54, Y = 187,
                                Width = 8, 
                                Height = 5
                            }
                        },
                        { "plane_shadow_nw", new SimpleImageModel.Sprite()
                            {
                                X = 63, Y = 185,
                                Width = 7, 
                                Height = 7
                            }
                        },
                        { "paperclip", new SimpleImageModel.Sprite()
                            {
                                X = 73, Y = 175,
                                Width = 21,
                                Height = 22
                            }
                        },
                        { "corner_l", new SimpleImageModel.Sprite()
                            {
                                X = 307, Y = 0,
                                Width = 10,
                                Height = 8
                            }
                        },
                        { "corner_r", new SimpleImageModel.Sprite()
                            {
                                X = 308, Y = 9,
                                Width = 10,
                                Height = 8
                            }
                        },
                        { "trailer", new SimpleImageModel.Sprite()
                            {
                                X = 257, Y = 160,
                                Width = 26,
                                Height = 14
                            }
                        },
                        { "clue_source_0", new SimpleImageModel.Sprite()
                            {
                                X = 97, Y = 175,
                                Width = 30,
                                Height = 24
                            }
                        },
                        { "clue_source_1", new SimpleImageModel.Sprite()
                            {
                                X = 129, Y = 175,
                                Width = 30,
                                Height = 24
                            }
                        },
                        { "clue_source_2", new SimpleImageModel.Sprite()
                            {
                                X = 161, Y = 175,
                                Width = 30,
                                Height = 24
                            }
                        },
                        { "clue_source_3", new SimpleImageModel.Sprite()
                            {
                                X = 193, Y = 175,
                                Width = 30,
                                Height = 24
                            }
                        },
                        { "clue_source_4", new SimpleImageModel.Sprite()
                            {
                                X = 225, Y = 175,
                                Width = 30,
                                Height = 24
                            }
                        },
                        { "clue_source_5", new SimpleImageModel.Sprite()
                            {
                                X = 257, Y = 175,
                                Width = 30,
                                Height = 24
                            }
                        },
                        { "clue_source_6", new SimpleImageModel.Sprite()
                            {
                                X = 289, Y = 175,
                                Width = 30,
                                Height = 24
                            }
                        },
                        { "clue_source_7", new SimpleImageModel.Sprite()
                            {
                                X = 289, Y = 149,
                                Width = 30,
                                Height = 24
                            }
                        },
                        { "clue_source_8", new SimpleImageModel.Sprite()
                            {
                                X = 289, Y = 123,
                                Width = 30,
                                Height = 24
                            }
                        },
                    }
                }
            },
            //"GUYS2",
            {
                "GUYS3", new SimpleImageModel.SpriteSheetData()
                {
                    Sprites = new()
                    {
                        { "wall_n", new SimpleImageModel.Sprite()
                            {
                                X = 0, Y = 0,
                                Width = 16, Height = 16
                            }
                        },
                        { "wall_e", new SimpleImageModel.Sprite()
                            {
                                X = 16, Y = 0,
                                Width = 16, Height = 16
                            }
                        },
                        { "wall_s", new SimpleImageModel.Sprite()
                            {
                                X = 32, Y = 0,
                                Width = 16, Height = 16
                            }
                        },
                        { "wall_w", new SimpleImageModel.Sprite()
                            {
                                X = 48, Y = 0,
                                Width = 16, Height = 16
                            }
                        },
                        { "wall_ne", new SimpleImageModel.Sprite()
                            {
                                X = 64, Y = 0,
                                Width = 16, Height = 16
                            }
                        },
                        { "wall_se", new SimpleImageModel.Sprite()
                            {
                                X = 80, Y = 0,
                                Width = 16, Height = 16
                            }
                        },
                        { "wall_sw", new SimpleImageModel.Sprite()
                            {
                                X = 96, Y = 0,
                                Width = 16, Height = 16
                            }
                        },
                        { "wall_nw", new SimpleImageModel.Sprite()
                            {
                                X = 112, Y = 0,
                                Width = 16, Height = 16
                            }
                        },
                        { "floor_1", new SimpleImageModel.Sprite()
                            {
                                X = 0, Y = 112,
                                Width = 16, Height = 16
                            }
                        },
                        { "floor_2", new SimpleImageModel.Sprite()
                            {
                                X = 16, Y = 112,
                                Width = 16, Height = 16
                            }
                        },
                        { "door_closed_n", new SimpleImageModel.Sprite()
                            {
                                X = 64, Y = 16,
                                Width = 16, Height = 16
                            }
                        },
                        { "door_closed_e", new SimpleImageModel.Sprite()
                            {
                                X = 80, Y = 16,
                                Width = 16, Height = 16
                            }
                        },
                        { "door_closed_s", new SimpleImageModel.Sprite()
                            {
                                X = 96, Y = 16,
                                Width = 16, Height = 16
                            }
                        },
                        { "door_closed_w", new SimpleImageModel.Sprite()
                            {
                                X = 112, Y = 16,
                                Width = 16, Height = 16
                            }
                        },
                        { "door_open_n", new SimpleImageModel.Sprite()
                            {
                                X = 64, Y = 32,
                                Width = 16, Height = 16
                            }
                        },
                        { "door_open_e", new SimpleImageModel.Sprite()
                            {
                                X = 80, Y = 32,
                                Width = 16, Height = 16
                            }
                        },
                        { "door_open_s", new SimpleImageModel.Sprite()
                            {
                                X = 96, Y = 32,
                                Width = 16, Height = 16
                            }
                        },
                        { "door_open_w", new SimpleImageModel.Sprite()
                            {
                                X = 112, Y = 32,
                                Width = 16, Height = 16
                            }
                        },
                        { "chair_s", new SimpleImageModel.Sprite()
                            {
                                X = 128, Y = 0,
                                Width = 16, Height = 16
                            }
                        },
                        { "chair_w", new SimpleImageModel.Sprite()
                            {
                                X = 144, Y = 0,
                                Width = 16, Height = 16
                            }
                        },
                        { "chair_n", new SimpleImageModel.Sprite()
                            {
                                X = 160, Y = 0,
                                Width = 16, Height = 16
                            }
                        },
                        { "chair_e", new SimpleImageModel.Sprite()
                            {
                                X = 176, Y = 0,
                                Width = 16, Height = 16
                            }
                        },
                        { "computer_n", new SimpleImageModel.Sprite()
                            {
                                X = 192, Y = 0,
                                Width = 16, Height = 16
                            }
                        },
                        { "computer_s", new SimpleImageModel.Sprite()
                            {
                                X = 224, Y = 0,
                                Width = 16, Height = 16
                            }
                        },
                        { "printer_w", new SimpleImageModel.Sprite()
                            {
                                X = 208, Y = 0,
                                Width = 16, Height = 16
                            }
                        },
                        { "printer_e", new SimpleImageModel.Sprite()
                            {
                                X = 242, Y = 0,
                                Width = 16, Height = 16
                            }
                        },
                        { "wall_cabinet_closed_s", new SimpleImageModel.Sprite()
                            {
                                X = 128, Y = 16,
                                Width = 16, Height = 16
                            }
                        },
                        { "wall_cabinet_closed_w", new SimpleImageModel.Sprite()
                            {
                                X = 144, Y = 16,
                                Width = 16, Height = 16
                            }
                        },
                        { "wall_cabinet_closed_n", new SimpleImageModel.Sprite()
                            {
                                X = 160, Y = 16,
                                Width = 16, Height = 16
                            }
                        },
                        { "wall_cabinet_closed_e", new SimpleImageModel.Sprite()
                            {
                                X = 176, Y = 16,
                                Width = 16, Height = 16
                            }
                        },
                        { "wall_cabinet_open_s", new SimpleImageModel.Sprite()
                            {
                                X = 128, Y = 32,
                                Width = 16, Height = 16
                            }
                        },
                        { "wall_cabinet_open_w", new SimpleImageModel.Sprite()
                            {
                                X = 144, Y = 32,
                                Width = 16, Height = 16
                            }
                        },
                        { "wall_cabinet_open_n", new SimpleImageModel.Sprite()
                            {
                                X = 160, Y = 32,
                                Width = 16, Height = 16
                            }
                        },
                        { "wall_cabinet_open_e", new SimpleImageModel.Sprite()
                            {
                                X = 176, Y = 32,
                                Width = 16, Height = 16
                            }
                        },
                        { "wall_floor_safe_closed_s", new SimpleImageModel.Sprite()
                            {
                                X = 192, Y = 16,
                                Width = 16, Height = 16
                            }
                        },
                        { "wall_floor_safe_open_s", new SimpleImageModel.Sprite()
                            {
                                X = 192, Y = 32,
                                Width = 16, Height = 16
                            }
                        },
                        { "wall_safe_closed_w", new SimpleImageModel.Sprite()
                            {
                                X = 108, Y = 16,
                                Width = 16, Height = 16
                            }
                        },
                        { "wall_safe_closed_e", new SimpleImageModel.Sprite()
                            {
                                X = 240, Y = 16,
                                Width = 16, Height = 16
                            }
                        },
                        { "wall_safe_open_w", new SimpleImageModel.Sprite()
                            {
                                X = 108, Y = 32,
                                Width = 16, Height = 16
                            }
                        },
                        { "wall_safe_open_e", new SimpleImageModel.Sprite()
                            {
                                X = 240, Y = 32,
                                Width = 16, Height = 16
                            }
                        },
                        { "wall_picture_closed_n", new SimpleImageModel.Sprite()
                            {
                                X = 224, Y = 16,
                                Width = 16, Height = 16
                            }
                        },
                        { "wall_picture_open_n", new SimpleImageModel.Sprite()
                            {
                                X = 224, Y = 32,
                                Width = 16, Height = 16
                            }
                        },
                        { "desk_closed_s_1", new SimpleImageModel.Sprite()
                            {
                                X = 0, Y = 16,
                                Width = 16, Height = 16
                            }
                        },
                        { "desk_closed_s_2", new SimpleImageModel.Sprite()
                            {
                                X = 16, Y = 16,
                                Width = 16, Height = 16
                            }
                        },
                        { "desk_open_s_1", new SimpleImageModel.Sprite()
                            {
                                X = 0, Y = 32,
                                Width = 16, Height = 16
                            }
                        },
                        { "desk_open_s_2", new SimpleImageModel.Sprite()
                            {
                                X = 16, Y = 32,
                                Width = 16, Height = 16
                            }
                        },
                        { "desk_closed_n_1", new SimpleImageModel.Sprite()
                            {
                                X = 32, Y = 16,
                                Width = 16, Height = 16
                            }
                        },
                        { "desk_closed_n_2", new SimpleImageModel.Sprite()
                            {
                                X = 48, Y = 16,
                                Width = 16, Height = 16
                            }
                        },
                        { "desk_open_n_1", new SimpleImageModel.Sprite()
                            {
                                X = 32, Y = 32,
                                Width = 16, Height = 16
                            }
                        },
                        { "desk_open_n_2", new SimpleImageModel.Sprite()
                            {
                                X = 48, Y = 32,
                                Width = 16, Height = 16
                            }
                        },
                        { "server_s", new SimpleImageModel.Sprite()
                            {
                                X = 0, Y = 48,
                                Width = 16, Height = 16
                            }
                        },
                        { "server_w", new SimpleImageModel.Sprite()
                            {
                                X = 16, Y = 48,
                                Width = 16, Height = 16
                            }
                        },
                        { "server_n", new SimpleImageModel.Sprite()
                            {
                                X = 32, Y = 48,
                                Width = 16, Height = 16
                            }
                        },
                        { "server_e", new SimpleImageModel.Sprite()
                            {
                                X = 48, Y = 48,
                                Width = 16, Height = 16
                            }
                        },
                        { "table_1", new SimpleImageModel.Sprite()
                            {
                                X = 80, Y = 48,
                                Width = 16, Height = 16
                            }
                        },
                        { "sofa_n_1", new SimpleImageModel.Sprite()
                            {
                                X = 96, Y = 48,
                                Width = 16, Height = 16
                            }
                        },
                        { "sofa_n_2", new SimpleImageModel.Sprite()
                            {
                                X = 112, Y = 48,
                                Width = 16, Height = 16
                            }
                        },
                        { "sofa_s_1", new SimpleImageModel.Sprite()
                            {
                                X = 128, Y = 48,
                                Width = 16, Height = 16
                            }
                        },
                        { "sofa_s_2", new SimpleImageModel.Sprite()
                            {
                                X = 144, Y = 48,
                                Width = 16, Height = 16
                            }
                        },
                        { "wall_record_s", new SimpleImageModel.Sprite()
                            {
                                X = 224, Y = 48,
                                Width = 16, Height = 16
                            }
                        },
                        { "wall_aquarium_n", new SimpleImageModel.Sprite()
                            {
                                X = 256, Y = 48,
                                Width = 16, Height = 16
                            }
                        },
                        { "plant_1", new SimpleImageModel.Sprite()
                            {
                                X = 272, Y = 0,
                                Width = 16, Height = 16
                            }
                        },
                        { "plant_2", new SimpleImageModel.Sprite()
                            {
                                X = 304, Y = 0,
                                Width = 16, Height = 16
                            }
                        },
                    }
                }
            },
            //"ICONS",
            {"SPRITES", new SimpleImageModel.SpriteSheetData()
            {
                Sprites = new()
                    {
                        {"frame_idle", new SimpleImageModel.Sprite()
                            {
                                X = 217, Y = 161,
                                Width = 23, Height = 38
                            }
                        },
                        {"frame_walk_1", new SimpleImageModel.Sprite()
                            {
                                X = 1, Y = 161,
                                Width = 23, Height = 38
                            }
                        },
                        {"frame_walk_2", new SimpleImageModel.Sprite()
                            {
                                X = 25, Y = 161,
                                Width = 23, Height = 38
                            }
                        },
                        {"frame_walk_3", new SimpleImageModel.Sprite()
                            {
                                X = 49, Y = 161,
                                Width = 23, Height = 38
                            }
                        },
                        {"frame_walk_4", new SimpleImageModel.Sprite()
                            {
                                X = 73, Y = 161,
                                Width = 23, Height = 38
                            }
                        },
                        {"frame_use", new SimpleImageModel.Sprite()
                            {
                                X = 145, Y = 161,
                                Width = 23, Height = 38
                            }
                        },
                        {"frame_crouch_1", new SimpleImageModel.Sprite()
                            {
                                X = 169, Y = 161,
                                Width = 23, Height = 38
                            }
                        },
                        {"frame_crouch_2", new SimpleImageModel.Sprite()
                            {
                                X = 193, Y = 161,
                                Width = 23, Height = 38
                            }
                        },
                        {"frame_fire_1", new SimpleImageModel.Sprite()
                            {
                                X = 97, Y = 161,
                                Width = 23, Height = 38
                            }
                        },
                        {"frame_fire_2", new SimpleImageModel.Sprite()
                            {
                                X = 121, Y = 161,
                                Width = 23, Height = 38
                            }
                        },
                        {"frame_grenade_1", new SimpleImageModel.Sprite()
                            {
                                X = 241, Y = 161,
                                Width = 23, Height = 38
                            }
                        },
                        {"frame_grenade_2", new SimpleImageModel.Sprite()
                            {
                                X = 265, Y = 161,
                                Width = 23, Height = 38
                            }
                        },
                        {"frame_grenade_3", new SimpleImageModel.Sprite()
                            {
                                X = 289, Y = 161,
                                Width = 23, Height = 38
                            }
                        },
                        {"walk_n_1", new SimpleImageModel.Sprite()
                            {
                                X = 1, Y = 1,
                                Width = 15, Height = 15
                            }
                        },
                        {"walk_n_2", new SimpleImageModel.Sprite()
                            {
                                X = 17, Y = 1,
                                Width = 15, Height = 15
                            }
                        },
                        {"walk_n_3", new SimpleImageModel.Sprite()
                            {
                                X = 33, Y = 1,
                                Width = 15, Height = 15
                            }
                        },
                        {"walk_n_4", new SimpleImageModel.Sprite()
                            {
                                X = 49, Y = 1,
                                Width = 15, Height = 15
                            }
                        },
                        {"shoot_n_1", new SimpleImageModel.Sprite()
                            {
                                X = 65, Y = 1,
                                Width = 15, Height = 15
                            }
                        },
                        {"shoot_n_2", new SimpleImageModel.Sprite()
                            {
                                X = 81, Y = 1,
                                Width = 15, Height = 15
                            }
                        },
                        {"use_n", new SimpleImageModel.Sprite()
                            {
                                X = 97, Y = 1,
                                Width = 15, Height = 15
                            }
                        },
                        {"crouch_n_1", new SimpleImageModel.Sprite()
                            {
                                X = 113, Y = 1,
                                Width = 15, Height = 15
                            }
                        },
                        {"crouch_n_2", new SimpleImageModel.Sprite()
                            {
                                X = 257, Y = 1,
                                Width = 15, Height = 15
                            }
                        },
                        {"grenade_n_1", new SimpleImageModel.Sprite()
                            {
                                X = 273, Y = 1,
                                Width = 15, Height = 15
                            }
                        },
                        {"grenade_n_2", new SimpleImageModel.Sprite()
                            {
                                X = 289, Y = 1,
                                Width = 15, Height = 15
                            }
                        },
                        {"grenade_n_3", new SimpleImageModel.Sprite()
                            {
                                X = 305, Y = 1,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_walk_n_1", new SimpleImageModel.Sprite()
                            {
                                X = 129, Y = 1,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_walk_n_2", new SimpleImageModel.Sprite()
                            {
                                X = 145, Y = 1,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_walk_n_3", new SimpleImageModel.Sprite()
                            {
                                X = 161, Y = 1,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_walk_n_4", new SimpleImageModel.Sprite()
                            {
                                X = 177, Y = 1,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_shoot_n_1", new SimpleImageModel.Sprite()
                            {
                                X = 193, Y = 1,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_shoot_n_2", new SimpleImageModel.Sprite()
                            {
                                X = 209, Y = 1,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_crouch_n", new SimpleImageModel.Sprite()
                            {
                                X = 241, Y = 1,
                                Width = 15, Height = 15
                            }
                        },
                        {"fire_n", new SimpleImageModel.Sprite()
                            {
                                X = 225, Y = 1,
                                Width = 15, Height = 15
                            }
                        },
                        {"walk_ne_1", new SimpleImageModel.Sprite()
                            {
                                X = 1, Y = 17,
                                Width = 15, Height = 15
                            }
                        },
                        {"walk_ne_2", new SimpleImageModel.Sprite()
                            {
                                X = 17, Y = 17,
                                Width = 15, Height = 15
                            }
                        },
                        {"walk_ne_3", new SimpleImageModel.Sprite()
                            {
                                X = 33, Y = 17,
                                Width = 15, Height = 15
                            }
                        },
                        {"walk_ne_4", new SimpleImageModel.Sprite()
                            {
                                X = 49, Y = 17,
                                Width = 15, Height = 15
                            }
                        },
                        {"shoot_ne_1", new SimpleImageModel.Sprite()
                            {
                                X = 65, Y = 17,
                                Width = 15, Height = 15
                            }
                        },
                        {"shoot_ne_2", new SimpleImageModel.Sprite()
                            {
                                X = 81, Y = 17,
                                Width = 15, Height = 15
                            }
                        },
                        {"use_ne", new SimpleImageModel.Sprite()
                            {
                                X = 97, Y = 17,
                                Width = 15, Height = 15
                            }
                        },
                        {"crouch_ne_1", new SimpleImageModel.Sprite()
                            {
                                X = 113, Y = 17,
                                Width = 15, Height = 15
                            }
                        },
                        {"crouch_ne_2", new SimpleImageModel.Sprite()
                            {
                                X = 257, Y = 17,
                                Width = 15, Height = 15
                            }
                        },
                        {"grenade_ne_1", new SimpleImageModel.Sprite()
                            {
                                X = 273, Y = 17,
                                Width = 15, Height = 15
                            }
                        },
                        {"grenade_ne_2", new SimpleImageModel.Sprite()
                            {
                                X = 289, Y = 17,
                                Width = 15, Height = 15
                            }
                        },
                        {"grenade_ne_3", new SimpleImageModel.Sprite()
                            {
                                X = 305, Y = 17,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_walk_ne_1", new SimpleImageModel.Sprite()
                            {
                                X = 129, Y = 17,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_walk_ne_2", new SimpleImageModel.Sprite()
                            {
                                X = 145, Y = 17,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_walk_ne_3", new SimpleImageModel.Sprite()
                            {
                                X = 161, Y = 17,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_walk_ne_4", new SimpleImageModel.Sprite()
                            {
                                X = 177, Y = 17,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_shoot_ne_1", new SimpleImageModel.Sprite()
                            {
                                X = 193, Y = 17,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_shoot_ne_2", new SimpleImageModel.Sprite()
                            {
                                X = 209, Y = 17,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_crouch_ne", new SimpleImageModel.Sprite()
                            {
                                X = 241, Y = 17,
                                Width = 15, Height = 15
                            }
                        },
                        {"fire_ne", new SimpleImageModel.Sprite()
                            {
                                X = 225, Y = 17,
                                Width = 15, Height = 15
                            }
                        },
                        {"walk_e_1", new SimpleImageModel.Sprite()
                            {
                                X = 1, Y = 33,
                                Width = 15, Height = 15
                            }
                        },
                        {"walk_e_2", new SimpleImageModel.Sprite()
                            {
                                X = 17, Y = 33,
                                Width = 15, Height = 15
                            }
                        },
                        {"walk_e_3", new SimpleImageModel.Sprite()
                            {
                                X = 33, Y = 33,
                                Width = 15, Height = 15
                            }
                        },
                        {"walk_e_4", new SimpleImageModel.Sprite()
                            {
                                X = 49, Y = 33,
                                Width = 15, Height = 15
                            }
                        },
                        {"shoot_e_1", new SimpleImageModel.Sprite()
                            {
                                X = 65, Y = 33,
                                Width = 15, Height = 15
                            }
                        },
                        {"shoot_e_2", new SimpleImageModel.Sprite()
                            {
                                X = 81, Y = 33,
                                Width = 15, Height = 15
                            }
                        },
                        {"use_e", new SimpleImageModel.Sprite()
                            {
                                X = 97, Y = 33,
                                Width = 15, Height = 15
                            }
                        },
                        {"crouch_e_1", new SimpleImageModel.Sprite()
                            {
                                X = 113, Y = 33,
                                Width = 15, Height = 15
                            }
                        },
                        {"crouch_e_2", new SimpleImageModel.Sprite()
                            {
                                X = 257, Y = 33,
                                Width = 15, Height = 15
                            }
                        },
                        {"grenade_e_1", new SimpleImageModel.Sprite()
                            {
                                X = 273, Y = 33,
                                Width = 15, Height = 15
                            }
                        },
                        {"grenade_e_2", new SimpleImageModel.Sprite()
                            {
                                X = 289, Y = 33,
                                Width = 15, Height = 15
                            }
                        },
                        {"grenade_e_3", new SimpleImageModel.Sprite()
                            {
                                X = 305, Y = 33,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_walk_e_1", new SimpleImageModel.Sprite()
                            {
                                X = 129, Y = 33,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_walk_e_2", new SimpleImageModel.Sprite()
                            {
                                X = 145, Y = 33,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_walk_e_3", new SimpleImageModel.Sprite()
                            {
                                X = 161, Y = 33,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_walk_e_4", new SimpleImageModel.Sprite()
                            {
                                X = 177, Y = 33,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_shoot_e_1", new SimpleImageModel.Sprite()
                            {
                                X = 193, Y = 33,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_shoot_e_2", new SimpleImageModel.Sprite()
                            {
                                X = 209, Y = 33,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_crouch_e", new SimpleImageModel.Sprite()
                            {
                                X = 241, Y = 33,
                                Width = 15, Height = 15
                            }
                        },
                        {"fire_e", new SimpleImageModel.Sprite()
                            {
                                X = 225, Y = 33,
                                Width = 15, Height = 15
                            }
                        },
                        {"walk_se_1", new SimpleImageModel.Sprite()
                            {
                                X = 1, Y = 49,
                                Width = 15, Height = 15
                            }
                        },
                        {"walk_se_2", new SimpleImageModel.Sprite()
                            {
                                X = 17, Y = 49,
                                Width = 15, Height = 15
                            }
                        },
                        {"walk_se_3", new SimpleImageModel.Sprite()
                            {
                                X = 33, Y = 49,
                                Width = 15, Height = 15
                            }
                        },
                        {"walk_se_4", new SimpleImageModel.Sprite()
                            {
                                X = 49, Y = 49,
                                Width = 15, Height = 15
                            }
                        },
                        {"shoot_se_1", new SimpleImageModel.Sprite()
                            {
                                X = 65, Y = 49,
                                Width = 15, Height = 15
                            }
                        },
                        {"shoot_se_2", new SimpleImageModel.Sprite()
                            {
                                X = 81, Y = 49,
                                Width = 15, Height = 15
                            }
                        },
                        {"use_se", new SimpleImageModel.Sprite()
                            {
                                X = 97, Y = 49,
                                Width = 15, Height = 15
                            }
                        },
                        {"crouch_se_1", new SimpleImageModel.Sprite()
                            {
                                X = 113, Y = 49,
                                Width = 15, Height = 15
                            }
                        },
                        {"crouch_se_2", new SimpleImageModel.Sprite()
                            {
                                X = 257, Y = 49,
                                Width = 15, Height = 15
                            }
                        },
                        {"grenade_se_1", new SimpleImageModel.Sprite()
                            {
                                X = 273, Y = 49,
                                Width = 15, Height = 15
                            }
                        },
                        {"grenade_se_2", new SimpleImageModel.Sprite()
                            {
                                X = 289, Y = 49,
                                Width = 15, Height = 15
                            }
                        },
                        {"grenade_se_3", new SimpleImageModel.Sprite()
                            {
                                X = 305, Y = 49,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_walk_se_1", new SimpleImageModel.Sprite()
                            {
                                X = 129, Y = 49,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_walk_se_2", new SimpleImageModel.Sprite()
                            {
                                X = 145, Y = 49,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_walk_se_3", new SimpleImageModel.Sprite()
                            {
                                X = 161, Y = 49,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_walk_se_4", new SimpleImageModel.Sprite()
                            {
                                X = 177, Y = 49,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_shoot_se_1", new SimpleImageModel.Sprite()
                            {
                                X = 193, Y = 49,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_shoot_se_2", new SimpleImageModel.Sprite()
                            {
                                X = 209, Y = 49,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_crouch_se", new SimpleImageModel.Sprite()
                            {
                                X = 241, Y = 49,
                                Width = 15, Height = 15
                            }
                        },
                        {"fire_se", new SimpleImageModel.Sprite()
                            {
                                X = 225, Y = 49,
                                Width = 15, Height = 15
                            }
                        },
                        {"walk_s_1", new SimpleImageModel.Sprite()
                            {
                                X = 1, Y = 65,
                                Width = 15, Height = 15
                            }
                        },
                        {"walk_s_2", new SimpleImageModel.Sprite()
                            {
                                X = 17, Y = 65,
                                Width = 15, Height = 15
                            }
                        },
                        {"walk_s_3", new SimpleImageModel.Sprite()
                            {
                                X = 33, Y = 65,
                                Width = 15, Height = 15
                            }
                        },
                        {"walk_s_4", new SimpleImageModel.Sprite()
                            {
                                X = 49, Y = 65,
                                Width = 15, Height = 15
                            }
                        },
                        {"shoot_s_1", new SimpleImageModel.Sprite()
                            {
                                X = 65, Y = 65,
                                Width = 15, Height = 15
                            }
                        },
                        {"shoot_s_2", new SimpleImageModel.Sprite()
                            {
                                X = 81, Y = 65,
                                Width = 15, Height = 15
                            }
                        },
                        {"use_s", new SimpleImageModel.Sprite()
                            {
                                X = 97, Y = 65,
                                Width = 15, Height = 15
                            }
                        },
                        {"crouch_s_1", new SimpleImageModel.Sprite()
                            {
                                X = 113, Y = 65,
                                Width = 15, Height = 15
                            }
                        },
                        {"crouch_s_2", new SimpleImageModel.Sprite()
                            {
                                X = 257, Y = 65,
                                Width = 15, Height = 15
                            }
                        },
                        {"grenade_s_1", new SimpleImageModel.Sprite()
                            {
                                X = 273, Y = 65,
                                Width = 15, Height = 15
                            }
                        },
                        {"grenade_s_2", new SimpleImageModel.Sprite()
                            {
                                X = 289, Y = 65,
                                Width = 15, Height = 15
                            }
                        },
                        {"grenade_s_3", new SimpleImageModel.Sprite()
                            {
                                X = 305, Y = 65,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_walk_s_1", new SimpleImageModel.Sprite()
                            {
                                X = 129, Y = 65,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_walk_s_2", new SimpleImageModel.Sprite()
                            {
                                X = 145, Y = 65,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_walk_s_3", new SimpleImageModel.Sprite()
                            {
                                X = 161, Y = 65,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_walk_s_4", new SimpleImageModel.Sprite()
                            {
                                X = 177, Y = 65,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_shoot_s_1", new SimpleImageModel.Sprite()
                            {
                                X = 193, Y = 65,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_shoot_s_2", new SimpleImageModel.Sprite()
                            {
                                X = 209, Y = 65,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_crouch_s", new SimpleImageModel.Sprite()
                            {
                                X = 241, Y = 65,
                                Width = 15, Height = 15
                            }
                        },
                        {"fire_s", new SimpleImageModel.Sprite()
                            {
                                X = 225, Y = 65,
                                Width = 15, Height = 15
                            }
                        },
                        {"walk_sw_1", new SimpleImageModel.Sprite()
                            {
                                X = 1, Y = 81,
                                Width = 15, Height = 15
                            }
                        },
                        {"walk_sw_2", new SimpleImageModel.Sprite()
                            {
                                X = 17, Y = 81,
                                Width = 15, Height = 15
                            }
                        },
                        {"walk_sw_3", new SimpleImageModel.Sprite()
                            {
                                X = 33, Y = 81,
                                Width = 15, Height = 15
                            }
                        },
                        {"walk_sw_4", new SimpleImageModel.Sprite()
                            {
                                X = 49, Y = 81,
                                Width = 15, Height = 15
                            }
                        },
                        {"shoot_sw_1", new SimpleImageModel.Sprite()
                            {
                                X = 65, Y = 81,
                                Width = 15, Height = 15
                            }
                        },
                        {"shoot_sw_2", new SimpleImageModel.Sprite()
                            {
                                X = 81, Y = 81,
                                Width = 15, Height = 15
                            }
                        },
                        {"use_sw", new SimpleImageModel.Sprite()
                            {
                                X = 97, Y = 81,
                                Width = 15, Height = 15
                            }
                        },
                        {"crouch_sw_1", new SimpleImageModel.Sprite()
                            {
                                X = 113, Y = 81,
                                Width = 15, Height = 15
                            }
                        },
                        {"crouch_sw_2", new SimpleImageModel.Sprite()
                            {
                                X = 257, Y = 81,
                                Width = 15, Height = 15
                            }
                        },
                        {"grenade_sw_1", new SimpleImageModel.Sprite()
                            {
                                X = 273, Y = 81,
                                Width = 15, Height = 15
                            }
                        },
                        {"grenade_sw_2", new SimpleImageModel.Sprite()
                            {
                                X = 289, Y = 81,
                                Width = 15, Height = 15
                            }
                        },
                        {"grenade_sw_3", new SimpleImageModel.Sprite()
                            {
                                X = 305, Y = 81,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_walk_sw_1", new SimpleImageModel.Sprite()
                            {
                                X = 129, Y = 81,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_walk_sw_2", new SimpleImageModel.Sprite()
                            {
                                X = 145, Y = 81,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_walk_sw_3", new SimpleImageModel.Sprite()
                            {
                                X = 161, Y = 81,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_walk_sw_4", new SimpleImageModel.Sprite()
                            {
                                X = 177, Y = 81,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_shoot_sw_1", new SimpleImageModel.Sprite()
                            {
                                X = 193, Y = 81,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_shoot_sw_2", new SimpleImageModel.Sprite()
                            {
                                X = 209, Y = 81,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_crouch_sw", new SimpleImageModel.Sprite()
                            {
                                X = 241, Y = 81,
                                Width = 15, Height = 15
                            }
                        },
                        {"fire_sw", new SimpleImageModel.Sprite()
                            {
                                X = 225, Y = 81,
                                Width = 15, Height = 15
                            }
                        },
                        {"walk_w_1", new SimpleImageModel.Sprite()
                            {
                                X = 1, Y = 97,
                                Width = 15, Height = 15
                            }
                        },
                        {"walk_w_2", new SimpleImageModel.Sprite()
                            {
                                X = 17, Y = 97,
                                Width = 15, Height = 15
                            }
                        },
                        {"walk_w_3", new SimpleImageModel.Sprite()
                            {
                                X = 33, Y = 97,
                                Width = 15, Height = 15
                            }
                        },
                        {"walk_w_4", new SimpleImageModel.Sprite()
                            {
                                X = 49, Y = 97,
                                Width = 15, Height = 15
                            }
                        },
                        {"shoot_w_1", new SimpleImageModel.Sprite()
                            {
                                X = 65, Y = 97,
                                Width = 15, Height = 15
                            }
                        },
                        {"shoot_w_2", new SimpleImageModel.Sprite()
                            {
                                X = 81, Y = 97,
                                Width = 15, Height = 15
                            }
                        },
                        {"use_w", new SimpleImageModel.Sprite()
                            {
                                X = 97, Y = 97,
                                Width = 15, Height = 15
                            }
                        },
                        {"crouch_w_1", new SimpleImageModel.Sprite()
                            {
                                X = 113, Y = 97,
                                Width = 15, Height = 15
                            }
                        },
                        {"crouch_w_2", new SimpleImageModel.Sprite()
                            {
                                X = 257, Y = 97,
                                Width = 15, Height = 15
                            }
                        },
                        {"grenade_w_1", new SimpleImageModel.Sprite()
                            {
                                X = 273, Y = 97,
                                Width = 15, Height = 15
                            }
                        },
                        {"grenade_w_2", new SimpleImageModel.Sprite()
                            {
                                X = 289, Y = 97,
                                Width = 15, Height = 15
                            }
                        },
                        {"grenade_w_3", new SimpleImageModel.Sprite()
                            {
                                X = 305, Y = 97,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_walk_w_1", new SimpleImageModel.Sprite()
                            {
                                X = 129, Y = 97,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_walk_w_2", new SimpleImageModel.Sprite()
                            {
                                X = 145, Y = 97,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_walk_w_3", new SimpleImageModel.Sprite()
                            {
                                X = 161, Y = 97,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_walk_w_4", new SimpleImageModel.Sprite()
                            {
                                X = 177, Y = 97,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_shoot_w_1", new SimpleImageModel.Sprite()
                            {
                                X = 193, Y = 97,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_shoot_w_2", new SimpleImageModel.Sprite()
                            {
                                X = 209, Y = 97,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_crouch_w", new SimpleImageModel.Sprite()
                            {
                                X = 241, Y = 97,
                                Width = 15, Height = 15
                            }
                        },
                        {"fire_w", new SimpleImageModel.Sprite()
                            {
                                X = 225, Y = 97,
                                Width = 15, Height = 15
                            }
                        },
                        {"walk_nw_1", new SimpleImageModel.Sprite()
                            {
                                X = 1, Y = 113,
                                Width = 15, Height = 15
                            }
                        },
                        {"walk_nw_2", new SimpleImageModel.Sprite()
                            {
                                X = 17, Y = 113,
                                Width = 15, Height = 15
                            }
                        },
                        {"walk_nw_3", new SimpleImageModel.Sprite()
                            {
                                X = 33, Y = 113,
                                Width = 15, Height = 15
                            }
                        },
                        {"walk_nw_4", new SimpleImageModel.Sprite()
                            {
                                X = 49, Y = 113,
                                Width = 15, Height = 15
                            }
                        },
                        {"shoot_nw_1", new SimpleImageModel.Sprite()
                            {
                                X = 65, Y = 113,
                                Width = 15, Height = 15
                            }
                        },
                        {"shoot_nw_2", new SimpleImageModel.Sprite()
                            {
                                X = 81, Y = 113,
                                Width = 15, Height = 15
                            }
                        },
                        {"use_nw", new SimpleImageModel.Sprite()
                            {
                                X = 97, Y = 113,
                                Width = 15, Height = 15
                            }
                        },
                        {"crouch_nw_1", new SimpleImageModel.Sprite()
                            {
                                X = 113, Y = 113,
                                Width = 15, Height = 15
                            }
                        },
                        {"crouch_nw_2", new SimpleImageModel.Sprite()
                            {
                                X = 257, Y = 113,
                                Width = 15, Height = 15
                            }
                        },
                        {"grenade_nw_1", new SimpleImageModel.Sprite()
                            {
                                X = 273, Y = 113,
                                Width = 15, Height = 15
                            }
                        },
                        {"grenade_nw_2", new SimpleImageModel.Sprite()
                            {
                                X = 289, Y = 113,
                                Width = 15, Height = 15
                            }
                        },
                        {"grenade_nw_3", new SimpleImageModel.Sprite()
                            {
                                X = 305, Y = 113,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_walk_nw_1", new SimpleImageModel.Sprite()
                            {
                                X = 129, Y = 113,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_walk_nw_2", new SimpleImageModel.Sprite()
                            {
                                X = 145, Y = 113,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_walk_nw_3", new SimpleImageModel.Sprite()
                            {
                                X = 161, Y = 113,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_walk_nw_4", new SimpleImageModel.Sprite()
                            {
                                X = 177, Y = 113,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_shoot_nw_1", new SimpleImageModel.Sprite()
                            {
                                X = 193, Y = 113,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_shoot_nw_2", new SimpleImageModel.Sprite()
                            {
                                X = 209, Y = 113,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_crouch_nw", new SimpleImageModel.Sprite()
                            {
                                X = 241, Y = 113,
                                Width = 15, Height = 15
                            }
                        },
                        {"fire_nw", new SimpleImageModel.Sprite()
                            {
                                X = 225, Y = 113,
                                Width = 15, Height = 15
                            }
                        },
                        {"circle_1", new SimpleImageModel.Sprite()
                            {
                                X = 3, Y = 130,
                                Width = 12, Height = 12
                            }
                        },
                        {"circle_2", new SimpleImageModel.Sprite()
                            {
                                X = 19, Y = 130,
                                Width = 12, Height = 12
                            }
                        },
                        {"circle_3", new SimpleImageModel.Sprite()
                            {
                                X = 35, Y = 130,
                                Width = 12, Height = 12
                            }
                        },
                        {"circle_4", new SimpleImageModel.Sprite()
                            {
                                X = 51, Y = 130,
                                Width = 12, Height = 12
                            }
                        },
                        {"enemy_down_1", new SimpleImageModel.Sprite()
                            {
                                X = 65, Y = 129,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_down_2", new SimpleImageModel.Sprite()
                            {
                                X = 81, Y = 129,
                                Width = 15, Height = 15
                            }
                        },
                        {"down_1", new SimpleImageModel.Sprite()
                            {
                                X = 97, Y = 129,
                                Width = 15, Height = 15
                            }
                        },
                        {"explosion_1", new SimpleImageModel.Sprite()
                            {
                                X = 1, Y = 145,
                                Width = 15, Height = 15
                            }
                        },
                        {"explosion_2", new SimpleImageModel.Sprite()
                            {
                                X = 17, Y = 145,
                                Width = 15, Height = 15
                            }
                        },
                        {"explosion_3", new SimpleImageModel.Sprite()
                            {
                                X = 33, Y = 145,
                                Width = 15, Height = 15
                            }
                        },
                        {"explosion_4", new SimpleImageModel.Sprite()
                            {
                                X = 49, Y = 145,
                                Width = 15, Height = 15
                            }
                        },
                        {"explosion_5", new SimpleImageModel.Sprite()
                            {
                                X = 65, Y = 145,
                                Width = 15, Height = 15
                            }
                        },
                        {"explosion_6", new SimpleImageModel.Sprite()
                            {
                                X = 81, Y = 145,
                                Width = 15, Height = 15
                            }
                        },
                        {"explosion_7", new SimpleImageModel.Sprite()
                            {
                                X = 97, Y = 145,
                                Width = 15, Height = 15
                            }
                        },
                        {"explosion_8", new SimpleImageModel.Sprite()
                            {
                                X = 113, Y = 145,
                                Width = 15, Height = 15
                            }
                        },
                    }
                }
            },
            
            {"SPRITESF", new SimpleImageModel.SpriteSheetData()
            {
                Sprites = new()
                    {
                        {"frame_idle", new SimpleImageModel.Sprite()
                            {
                                X = 217, Y = 161,
                                Width = 23, Height = 38
                            }
                        },
                        {"frame_walk_1", new SimpleImageModel.Sprite()
                            {
                                X = 1, Y = 161,
                                Width = 23, Height = 38
                            }
                        },
                        {"frame_walk_2", new SimpleImageModel.Sprite()
                            {
                                X = 25, Y = 161,
                                Width = 23, Height = 38
                            }
                        },
                        {"frame_walk_3", new SimpleImageModel.Sprite()
                            {
                                X = 49, Y = 161,
                                Width = 23, Height = 38
                            }
                        },
                        {"frame_walk_4", new SimpleImageModel.Sprite()
                            {
                                X = 73, Y = 161,
                                Width = 23, Height = 38
                            }
                        },
                        {"frame_use", new SimpleImageModel.Sprite()
                            {
                                X = 145, Y = 161,
                                Width = 23, Height = 38
                            }
                        },
                        {"frame_crouch_1", new SimpleImageModel.Sprite()
                            {
                                X = 169, Y = 161,
                                Width = 23, Height = 38
                            }
                        },
                        {"frame_crouch_2", new SimpleImageModel.Sprite()
                            {
                                X = 193, Y = 161,
                                Width = 23, Height = 38
                            }
                        },
                        {"frame_fire_1", new SimpleImageModel.Sprite()
                            {
                                X = 97, Y = 161,
                                Width = 23, Height = 38
                            }
                        },
                        {"frame_fire_2", new SimpleImageModel.Sprite()
                            {
                                X = 121, Y = 161,
                                Width = 23, Height = 38
                            }
                        },
                        {"frame_grenade_1", new SimpleImageModel.Sprite()
                            {
                                X = 241, Y = 161,
                                Width = 23, Height = 38
                            }
                        },
                        {"frame_grenade_2", new SimpleImageModel.Sprite()
                            {
                                X = 265, Y = 161,
                                Width = 23, Height = 38
                            }
                        },
                        {"frame_grenade_3", new SimpleImageModel.Sprite()
                            {
                                X = 289, Y = 161,
                                Width = 23, Height = 38
                            }
                        },
                        {"walk_n_1", new SimpleImageModel.Sprite()
                            {
                                X = 1, Y = 1,
                                Width = 15, Height = 15
                            }
                        },
                        {"walk_n_2", new SimpleImageModel.Sprite()
                            {
                                X = 17, Y = 1,
                                Width = 15, Height = 15
                            }
                        },
                        {"walk_n_3", new SimpleImageModel.Sprite()
                            {
                                X = 33, Y = 1,
                                Width = 15, Height = 15
                            }
                        },
                        {"walk_n_4", new SimpleImageModel.Sprite()
                            {
                                X = 49, Y = 1,
                                Width = 15, Height = 15
                            }
                        },
                        {"shoot_n_1", new SimpleImageModel.Sprite()
                            {
                                X = 65, Y = 1,
                                Width = 15, Height = 15
                            }
                        },
                        {"shoot_n_2", new SimpleImageModel.Sprite()
                            {
                                X = 81, Y = 1,
                                Width = 15, Height = 15
                            }
                        },
                        {"use_n", new SimpleImageModel.Sprite()
                            {
                                X = 97, Y = 1,
                                Width = 15, Height = 15
                            }
                        },
                        {"crouch_n_1", new SimpleImageModel.Sprite()
                            {
                                X = 113, Y = 1,
                                Width = 15, Height = 15
                            }
                        },
                        {"crouch_n_2", new SimpleImageModel.Sprite()
                            {
                                X = 257, Y = 1,
                                Width = 15, Height = 15
                            }
                        },
                        {"grenade_n_1", new SimpleImageModel.Sprite()
                            {
                                X = 273, Y = 1,
                                Width = 15, Height = 15
                            }
                        },
                        {"grenade_n_2", new SimpleImageModel.Sprite()
                            {
                                X = 289, Y = 1,
                                Width = 15, Height = 15
                            }
                        },
                        {"grenade_n_3", new SimpleImageModel.Sprite()
                            {
                                X = 305, Y = 1,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_walk_n_1", new SimpleImageModel.Sprite()
                            {
                                X = 129, Y = 1,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_walk_n_2", new SimpleImageModel.Sprite()
                            {
                                X = 145, Y = 1,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_walk_n_3", new SimpleImageModel.Sprite()
                            {
                                X = 161, Y = 1,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_walk_n_4", new SimpleImageModel.Sprite()
                            {
                                X = 177, Y = 1,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_shoot_n_1", new SimpleImageModel.Sprite()
                            {
                                X = 193, Y = 1,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_shoot_n_2", new SimpleImageModel.Sprite()
                            {
                                X = 209, Y = 1,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_crouch_n", new SimpleImageModel.Sprite()
                            {
                                X = 241, Y = 1,
                                Width = 15, Height = 15
                            }
                        },
                        {"fire_n", new SimpleImageModel.Sprite()
                            {
                                X = 225, Y = 1,
                                Width = 15, Height = 15
                            }
                        },
                        {"walk_ne_1", new SimpleImageModel.Sprite()
                            {
                                X = 1, Y = 17,
                                Width = 15, Height = 15
                            }
                        },
                        {"walk_ne_2", new SimpleImageModel.Sprite()
                            {
                                X = 17, Y = 17,
                                Width = 15, Height = 15
                            }
                        },
                        {"walk_ne_3", new SimpleImageModel.Sprite()
                            {
                                X = 33, Y = 17,
                                Width = 15, Height = 15
                            }
                        },
                        {"walk_ne_4", new SimpleImageModel.Sprite()
                            {
                                X = 49, Y = 17,
                                Width = 15, Height = 15
                            }
                        },
                        {"shoot_ne_1", new SimpleImageModel.Sprite()
                            {
                                X = 65, Y = 17,
                                Width = 15, Height = 15
                            }
                        },
                        {"shoot_ne_2", new SimpleImageModel.Sprite()
                            {
                                X = 81, Y = 17,
                                Width = 15, Height = 15
                            }
                        },
                        {"use_ne", new SimpleImageModel.Sprite()
                            {
                                X = 97, Y = 17,
                                Width = 15, Height = 15
                            }
                        },
                        {"crouch_ne_1", new SimpleImageModel.Sprite()
                            {
                                X = 113, Y = 17,
                                Width = 15, Height = 15
                            }
                        },
                        {"crouch_ne_2", new SimpleImageModel.Sprite()
                            {
                                X = 257, Y = 17,
                                Width = 15, Height = 15
                            }
                        },
                        {"grenade_ne_1", new SimpleImageModel.Sprite()
                            {
                                X = 273, Y = 17,
                                Width = 15, Height = 15
                            }
                        },
                        {"grenade_ne_2", new SimpleImageModel.Sprite()
                            {
                                X = 289, Y = 17,
                                Width = 15, Height = 15
                            }
                        },
                        {"grenade_ne_3", new SimpleImageModel.Sprite()
                            {
                                X = 305, Y = 17,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_walk_ne_1", new SimpleImageModel.Sprite()
                            {
                                X = 129, Y = 17,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_walk_ne_2", new SimpleImageModel.Sprite()
                            {
                                X = 145, Y = 17,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_walk_ne_3", new SimpleImageModel.Sprite()
                            {
                                X = 161, Y = 17,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_walk_ne_4", new SimpleImageModel.Sprite()
                            {
                                X = 177, Y = 17,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_shoot_ne_1", new SimpleImageModel.Sprite()
                            {
                                X = 193, Y = 17,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_shoot_ne_2", new SimpleImageModel.Sprite()
                            {
                                X = 209, Y = 17,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_crouch_ne", new SimpleImageModel.Sprite()
                            {
                                X = 241, Y = 17,
                                Width = 15, Height = 15
                            }
                        },
                        {"fire_ne", new SimpleImageModel.Sprite()
                            {
                                X = 225, Y = 17,
                                Width = 15, Height = 15
                            }
                        },
                        {"walk_e_1", new SimpleImageModel.Sprite()
                            {
                                X = 1, Y = 33,
                                Width = 15, Height = 15
                            }
                        },
                        {"walk_e_2", new SimpleImageModel.Sprite()
                            {
                                X = 17, Y = 33,
                                Width = 15, Height = 15
                            }
                        },
                        {"walk_e_3", new SimpleImageModel.Sprite()
                            {
                                X = 33, Y = 33,
                                Width = 15, Height = 15
                            }
                        },
                        {"walk_e_4", new SimpleImageModel.Sprite()
                            {
                                X = 49, Y = 33,
                                Width = 15, Height = 15
                            }
                        },
                        {"shoot_e_1", new SimpleImageModel.Sprite()
                            {
                                X = 65, Y = 33,
                                Width = 15, Height = 15
                            }
                        },
                        {"shoot_e_2", new SimpleImageModel.Sprite()
                            {
                                X = 81, Y = 33,
                                Width = 15, Height = 15
                            }
                        },
                        {"use_e", new SimpleImageModel.Sprite()
                            {
                                X = 97, Y = 33,
                                Width = 15, Height = 15
                            }
                        },
                        {"crouch_e_1", new SimpleImageModel.Sprite()
                            {
                                X = 113, Y = 33,
                                Width = 15, Height = 15
                            }
                        },
                        {"crouch_e_2", new SimpleImageModel.Sprite()
                            {
                                X = 257, Y = 33,
                                Width = 15, Height = 15
                            }
                        },
                        {"grenade_e_1", new SimpleImageModel.Sprite()
                            {
                                X = 273, Y = 33,
                                Width = 15, Height = 15
                            }
                        },
                        {"grenade_e_2", new SimpleImageModel.Sprite()
                            {
                                X = 289, Y = 33,
                                Width = 15, Height = 15
                            }
                        },
                        {"grenade_e_3", new SimpleImageModel.Sprite()
                            {
                                X = 305, Y = 33,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_walk_e_1", new SimpleImageModel.Sprite()
                            {
                                X = 129, Y = 33,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_walk_e_2", new SimpleImageModel.Sprite()
                            {
                                X = 145, Y = 33,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_walk_e_3", new SimpleImageModel.Sprite()
                            {
                                X = 161, Y = 33,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_walk_e_4", new SimpleImageModel.Sprite()
                            {
                                X = 177, Y = 33,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_shoot_e_1", new SimpleImageModel.Sprite()
                            {
                                X = 193, Y = 33,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_shoot_e_2", new SimpleImageModel.Sprite()
                            {
                                X = 209, Y = 33,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_crouch_e", new SimpleImageModel.Sprite()
                            {
                                X = 241, Y = 33,
                                Width = 15, Height = 15
                            }
                        },
                        {"fire_e", new SimpleImageModel.Sprite()
                            {
                                X = 225, Y = 33,
                                Width = 15, Height = 15
                            }
                        },
                        {"walk_se_1", new SimpleImageModel.Sprite()
                            {
                                X = 1, Y = 49,
                                Width = 15, Height = 15
                            }
                        },
                        {"walk_se_2", new SimpleImageModel.Sprite()
                            {
                                X = 17, Y = 49,
                                Width = 15, Height = 15
                            }
                        },
                        {"walk_se_3", new SimpleImageModel.Sprite()
                            {
                                X = 33, Y = 49,
                                Width = 15, Height = 15
                            }
                        },
                        {"walk_se_4", new SimpleImageModel.Sprite()
                            {
                                X = 49, Y = 49,
                                Width = 15, Height = 15
                            }
                        },
                        {"shoot_se_1", new SimpleImageModel.Sprite()
                            {
                                X = 65, Y = 49,
                                Width = 15, Height = 15
                            }
                        },
                        {"shoot_se_2", new SimpleImageModel.Sprite()
                            {
                                X = 81, Y = 49,
                                Width = 15, Height = 15
                            }
                        },
                        {"use_se", new SimpleImageModel.Sprite()
                            {
                                X = 97, Y = 49,
                                Width = 15, Height = 15
                            }
                        },
                        {"crouch_se_1", new SimpleImageModel.Sprite()
                            {
                                X = 113, Y = 49,
                                Width = 15, Height = 15
                            }
                        },
                        {"crouch_se_2", new SimpleImageModel.Sprite()
                            {
                                X = 257, Y = 49,
                                Width = 15, Height = 15
                            }
                        },
                        {"grenade_se_1", new SimpleImageModel.Sprite()
                            {
                                X = 273, Y = 49,
                                Width = 15, Height = 15
                            }
                        },
                        {"grenade_se_2", new SimpleImageModel.Sprite()
                            {
                                X = 289, Y = 49,
                                Width = 15, Height = 15
                            }
                        },
                        {"grenade_se_3", new SimpleImageModel.Sprite()
                            {
                                X = 305, Y = 49,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_walk_se_1", new SimpleImageModel.Sprite()
                            {
                                X = 129, Y = 49,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_walk_se_2", new SimpleImageModel.Sprite()
                            {
                                X = 145, Y = 49,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_walk_se_3", new SimpleImageModel.Sprite()
                            {
                                X = 161, Y = 49,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_walk_se_4", new SimpleImageModel.Sprite()
                            {
                                X = 177, Y = 49,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_shoot_se_1", new SimpleImageModel.Sprite()
                            {
                                X = 193, Y = 49,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_shoot_se_2", new SimpleImageModel.Sprite()
                            {
                                X = 209, Y = 49,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_crouch_se", new SimpleImageModel.Sprite()
                            {
                                X = 241, Y = 49,
                                Width = 15, Height = 15
                            }
                        },
                        {"fire_se", new SimpleImageModel.Sprite()
                            {
                                X = 225, Y = 49,
                                Width = 15, Height = 15
                            }
                        },
                        {"walk_s_1", new SimpleImageModel.Sprite()
                            {
                                X = 1, Y = 65,
                                Width = 15, Height = 15
                            }
                        },
                        {"walk_s_2", new SimpleImageModel.Sprite()
                            {
                                X = 17, Y = 65,
                                Width = 15, Height = 15
                            }
                        },
                        {"walk_s_3", new SimpleImageModel.Sprite()
                            {
                                X = 33, Y = 65,
                                Width = 15, Height = 15
                            }
                        },
                        {"walk_s_4", new SimpleImageModel.Sprite()
                            {
                                X = 49, Y = 65,
                                Width = 15, Height = 15
                            }
                        },
                        {"shoot_s_1", new SimpleImageModel.Sprite()
                            {
                                X = 65, Y = 65,
                                Width = 15, Height = 15
                            }
                        },
                        {"shoot_s_2", new SimpleImageModel.Sprite()
                            {
                                X = 81, Y = 65,
                                Width = 15, Height = 15
                            }
                        },
                        {"use_s", new SimpleImageModel.Sprite()
                            {
                                X = 97, Y = 65,
                                Width = 15, Height = 15
                            }
                        },
                        {"crouch_s_1", new SimpleImageModel.Sprite()
                            {
                                X = 113, Y = 65,
                                Width = 15, Height = 15
                            }
                        },
                        {"crouch_s_2", new SimpleImageModel.Sprite()
                            {
                                X = 257, Y = 65,
                                Width = 15, Height = 15
                            }
                        },
                        {"grenade_s_1", new SimpleImageModel.Sprite()
                            {
                                X = 273, Y = 65,
                                Width = 15, Height = 15
                            }
                        },
                        {"grenade_s_2", new SimpleImageModel.Sprite()
                            {
                                X = 289, Y = 65,
                                Width = 15, Height = 15
                            }
                        },
                        {"grenade_s_3", new SimpleImageModel.Sprite()
                            {
                                X = 305, Y = 65,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_walk_s_1", new SimpleImageModel.Sprite()
                            {
                                X = 129, Y = 65,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_walk_s_2", new SimpleImageModel.Sprite()
                            {
                                X = 145, Y = 65,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_walk_s_3", new SimpleImageModel.Sprite()
                            {
                                X = 161, Y = 65,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_walk_s_4", new SimpleImageModel.Sprite()
                            {
                                X = 177, Y = 65,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_shoot_s_1", new SimpleImageModel.Sprite()
                            {
                                X = 193, Y = 65,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_shoot_s_2", new SimpleImageModel.Sprite()
                            {
                                X = 209, Y = 65,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_crouch_s", new SimpleImageModel.Sprite()
                            {
                                X = 241, Y = 65,
                                Width = 15, Height = 15
                            }
                        },
                        {"fire_s", new SimpleImageModel.Sprite()
                            {
                                X = 225, Y = 65,
                                Width = 15, Height = 15
                            }
                        },
                        {"walk_sw_1", new SimpleImageModel.Sprite()
                            {
                                X = 1, Y = 81,
                                Width = 15, Height = 15
                            }
                        },
                        {"walk_sw_2", new SimpleImageModel.Sprite()
                            {
                                X = 17, Y = 81,
                                Width = 15, Height = 15
                            }
                        },
                        {"walk_sw_3", new SimpleImageModel.Sprite()
                            {
                                X = 33, Y = 81,
                                Width = 15, Height = 15
                            }
                        },
                        {"walk_sw_4", new SimpleImageModel.Sprite()
                            {
                                X = 49, Y = 81,
                                Width = 15, Height = 15
                            }
                        },
                        {"shoot_sw_1", new SimpleImageModel.Sprite()
                            {
                                X = 65, Y = 81,
                                Width = 15, Height = 15
                            }
                        },
                        {"shoot_sw_2", new SimpleImageModel.Sprite()
                            {
                                X = 81, Y = 81,
                                Width = 15, Height = 15
                            }
                        },
                        {"use_sw", new SimpleImageModel.Sprite()
                            {
                                X = 97, Y = 81,
                                Width = 15, Height = 15
                            }
                        },
                        {"crouch_sw_1", new SimpleImageModel.Sprite()
                            {
                                X = 113, Y = 81,
                                Width = 15, Height = 15
                            }
                        },
                        {"crouch_sw_2", new SimpleImageModel.Sprite()
                            {
                                X = 257, Y = 81,
                                Width = 15, Height = 15
                            }
                        },
                        {"grenade_sw_1", new SimpleImageModel.Sprite()
                            {
                                X = 273, Y = 81,
                                Width = 15, Height = 15
                            }
                        },
                        {"grenade_sw_2", new SimpleImageModel.Sprite()
                            {
                                X = 289, Y = 81,
                                Width = 15, Height = 15
                            }
                        },
                        {"grenade_sw_3", new SimpleImageModel.Sprite()
                            {
                                X = 305, Y = 81,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_walk_sw_1", new SimpleImageModel.Sprite()
                            {
                                X = 129, Y = 81,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_walk_sw_2", new SimpleImageModel.Sprite()
                            {
                                X = 145, Y = 81,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_walk_sw_3", new SimpleImageModel.Sprite()
                            {
                                X = 161, Y = 81,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_walk_sw_4", new SimpleImageModel.Sprite()
                            {
                                X = 177, Y = 81,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_shoot_sw_1", new SimpleImageModel.Sprite()
                            {
                                X = 193, Y = 81,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_shoot_sw_2", new SimpleImageModel.Sprite()
                            {
                                X = 209, Y = 81,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_crouch_sw", new SimpleImageModel.Sprite()
                            {
                                X = 241, Y = 81,
                                Width = 15, Height = 15
                            }
                        },
                        {"fire_sw", new SimpleImageModel.Sprite()
                            {
                                X = 225, Y = 81,
                                Width = 15, Height = 15
                            }
                        },
                        {"walk_w_1", new SimpleImageModel.Sprite()
                            {
                                X = 1, Y = 97,
                                Width = 15, Height = 15
                            }
                        },
                        {"walk_w_2", new SimpleImageModel.Sprite()
                            {
                                X = 17, Y = 97,
                                Width = 15, Height = 15
                            }
                        },
                        {"walk_w_3", new SimpleImageModel.Sprite()
                            {
                                X = 33, Y = 97,
                                Width = 15, Height = 15
                            }
                        },
                        {"walk_w_4", new SimpleImageModel.Sprite()
                            {
                                X = 49, Y = 97,
                                Width = 15, Height = 15
                            }
                        },
                        {"shoot_w_1", new SimpleImageModel.Sprite()
                            {
                                X = 65, Y = 97,
                                Width = 15, Height = 15
                            }
                        },
                        {"shoot_w_2", new SimpleImageModel.Sprite()
                            {
                                X = 81, Y = 97,
                                Width = 15, Height = 15
                            }
                        },
                        {"use_w", new SimpleImageModel.Sprite()
                            {
                                X = 97, Y = 97,
                                Width = 15, Height = 15
                            }
                        },
                        {"crouch_w_1", new SimpleImageModel.Sprite()
                            {
                                X = 113, Y = 97,
                                Width = 15, Height = 15
                            }
                        },
                        {"crouch_w_2", new SimpleImageModel.Sprite()
                            {
                                X = 257, Y = 97,
                                Width = 15, Height = 15
                            }
                        },
                        {"grenade_w_1", new SimpleImageModel.Sprite()
                            {
                                X = 273, Y = 97,
                                Width = 15, Height = 15
                            }
                        },
                        {"grenade_w_2", new SimpleImageModel.Sprite()
                            {
                                X = 289, Y = 97,
                                Width = 15, Height = 15
                            }
                        },
                        {"grenade_w_3", new SimpleImageModel.Sprite()
                            {
                                X = 305, Y = 97,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_walk_w_1", new SimpleImageModel.Sprite()
                            {
                                X = 129, Y = 97,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_walk_w_2", new SimpleImageModel.Sprite()
                            {
                                X = 145, Y = 97,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_walk_w_3", new SimpleImageModel.Sprite()
                            {
                                X = 161, Y = 97,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_walk_w_4", new SimpleImageModel.Sprite()
                            {
                                X = 177, Y = 97,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_shoot_w_1", new SimpleImageModel.Sprite()
                            {
                                X = 193, Y = 97,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_shoot_w_2", new SimpleImageModel.Sprite()
                            {
                                X = 209, Y = 97,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_crouch_w", new SimpleImageModel.Sprite()
                            {
                                X = 241, Y = 97,
                                Width = 15, Height = 15
                            }
                        },
                        {"fire_w", new SimpleImageModel.Sprite()
                            {
                                X = 225, Y = 97,
                                Width = 15, Height = 15
                            }
                        },
                        {"walk_nw_1", new SimpleImageModel.Sprite()
                            {
                                X = 1, Y = 113,
                                Width = 15, Height = 15
                            }
                        },
                        {"walk_nw_2", new SimpleImageModel.Sprite()
                            {
                                X = 17, Y = 113,
                                Width = 15, Height = 15
                            }
                        },
                        {"walk_nw_3", new SimpleImageModel.Sprite()
                            {
                                X = 33, Y = 113,
                                Width = 15, Height = 15
                            }
                        },
                        {"walk_nw_4", new SimpleImageModel.Sprite()
                            {
                                X = 49, Y = 113,
                                Width = 15, Height = 15
                            }
                        },
                        {"shoot_nw_1", new SimpleImageModel.Sprite()
                            {
                                X = 65, Y = 113,
                                Width = 15, Height = 15
                            }
                        },
                        {"shoot_nw_2", new SimpleImageModel.Sprite()
                            {
                                X = 81, Y = 113,
                                Width = 15, Height = 15
                            }
                        },
                        {"use_nw", new SimpleImageModel.Sprite()
                            {
                                X = 97, Y = 113,
                                Width = 15, Height = 15
                            }
                        },
                        {"crouch_nw_1", new SimpleImageModel.Sprite()
                            {
                                X = 113, Y = 113,
                                Width = 15, Height = 15
                            }
                        },
                        {"crouch_nw_2", new SimpleImageModel.Sprite()
                            {
                                X = 257, Y = 113,
                                Width = 15, Height = 15
                            }
                        },
                        {"grenade_nw_1", new SimpleImageModel.Sprite()
                            {
                                X = 273, Y = 113,
                                Width = 15, Height = 15
                            }
                        },
                        {"grenade_nw_2", new SimpleImageModel.Sprite()
                            {
                                X = 289, Y = 113,
                                Width = 15, Height = 15
                            }
                        },
                        {"grenade_nw_3", new SimpleImageModel.Sprite()
                            {
                                X = 305, Y = 113,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_walk_nw_1", new SimpleImageModel.Sprite()
                            {
                                X = 129, Y = 113,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_walk_nw_2", new SimpleImageModel.Sprite()
                            {
                                X = 145, Y = 113,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_walk_nw_3", new SimpleImageModel.Sprite()
                            {
                                X = 161, Y = 113,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_walk_nw_4", new SimpleImageModel.Sprite()
                            {
                                X = 177, Y = 113,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_shoot_nw_1", new SimpleImageModel.Sprite()
                            {
                                X = 193, Y = 113,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_shoot_nw_2", new SimpleImageModel.Sprite()
                            {
                                X = 209, Y = 113,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_crouch_nw", new SimpleImageModel.Sprite()
                            {
                                X = 241, Y = 113,
                                Width = 15, Height = 15
                            }
                        },
                        {"fire_nw", new SimpleImageModel.Sprite()
                            {
                                X = 225, Y = 113,
                                Width = 15, Height = 15
                            }
                        },
                        {"circle_1", new SimpleImageModel.Sprite()
                            {
                                X = 3, Y = 130,
                                Width = 12, Height = 12
                            }
                        },
                        {"circle_2", new SimpleImageModel.Sprite()
                            {
                                X = 19, Y = 130,
                                Width = 12, Height = 12
                            }
                        },
                        {"circle_3", new SimpleImageModel.Sprite()
                            {
                                X = 35, Y = 130,
                                Width = 12, Height = 12
                            }
                        },
                        {"circle_4", new SimpleImageModel.Sprite()
                            {
                                X = 51, Y = 130,
                                Width = 12, Height = 12
                            }
                        },
                        {"enemy_down_1", new SimpleImageModel.Sprite()
                            {
                                X = 65, Y = 129,
                                Width = 15, Height = 15
                            }
                        },
                        {"enemy_down_2", new SimpleImageModel.Sprite()
                            {
                                X = 81, Y = 129,
                                Width = 15, Height = 15
                            }
                        },
                        {"down_1", new SimpleImageModel.Sprite()
                            {
                                X = 97, Y = 129,
                                Width = 15, Height = 15
                            }
                        },
                        {"explosion_1", new SimpleImageModel.Sprite()
                            {
                                X = 1, Y = 145,
                                Width = 15, Height = 15
                            }
                        },
                        {"explosion_2", new SimpleImageModel.Sprite()
                            {
                                X = 17, Y = 145,
                                Width = 15, Height = 15
                            }
                        },
                        {"explosion_3", new SimpleImageModel.Sprite()
                            {
                                X = 33, Y = 145,
                                Width = 15, Height = 15
                            }
                        },
                        {"explosion_4", new SimpleImageModel.Sprite()
                            {
                                X = 49, Y = 145,
                                Width = 15, Height = 15
                            }
                        },
                        {"explosion_5", new SimpleImageModel.Sprite()
                            {
                                X = 65, Y = 145,
                                Width = 15, Height = 15
                            }
                        },
                        {"explosion_6", new SimpleImageModel.Sprite()
                            {
                                X = 81, Y = 145,
                                Width = 15, Height = 15
                            }
                        },
                        {"explosion_7", new SimpleImageModel.Sprite()
                            {
                                X = 97, Y = 145,
                                Width = 15, Height = 15
                            }
                        },
                        {"explosion_8", new SimpleImageModel.Sprite()
                            {
                                X = 113, Y = 145,
                                Width = 15, Height = 15
                            }
                        },
                    }
                }
            }
            //"STREET"
        };
        #endregion
        
        private readonly ILogger<LegacySimpleImageParser> _logger;
        private readonly SharedImageParser _imageParser;
        
        private readonly List<string> _keys = new();
        private readonly Dictionary<string, SimpleImageModel> _result = new Dictionary<string, SimpleImageModel>();
        
        private int _index = 0;

        public LegacySimpleImageParser(ILogger<LegacySimpleImageParser> logger, SharedImageParser imageParser)
        {
            _logger = logger;
            _imageParser = imageParser;
        }

        protected override string Message => "Processing simple images..";

        public override void SetResult(PackageModel model)
        {
            model.SimpleImages = GetResult();
        }

        protected override bool CheckIfValidForImportInternal(string path)
        {
            if (Directory.GetFiles(path, "*.PIC").Length == 0)
            {
                return false;
            }

            return true;
        }

        protected override int GetTotalItemCountInPath()
        {
            return _keys.Count;
        }

        protected override int RunImportStepInternal()
        {
            var nextKey = _keys[_index];

            _result[nextKey] = Parse(Path, nextKey);

            return _index++;
        }

        protected override Dictionary<string, SimpleImageModel> GetResultInternal()
        {
            return _result;
        }

        protected override void OnImportStart()
        {
            _keys.AddRange(GetKeys(Path));
            _index = 0;
        }
        
        private List<string> GetKeys(string path)
        {
            return Directory.GetFiles(path, "*.PIC")
                .Select(System.IO.Path.GetFileNameWithoutExtension)
                .ToList();
        }

        private SimpleImageModel Parse(string path, string key)
        {
            var filePath = System.IO.Path.Combine(path, $"{key}.PIC");
            if (!File.Exists(filePath))
            {
                throw new Exception($"Missing PIC file: {key}");
            }

            var rawData = File.ReadAllBytes(filePath);
            using var memStream = new MemoryStream(rawData);
            using var reader = new BinaryReader(memStream);
            var image = _imageParser.Parse(key, reader);
            if (memStream.Position < rawData.Length)
            {
                _logger.LogWarning($"Loading image {key} data ended at offset {memStream.Position:X} but length was {rawData.Length:X}");
            }

            SimpleImageModel.SpriteSheetData? spriteSheet = null;
            if (LegacySpriteSheets.TryGetValue(key, out var defaultSpriteSheet))
            {
                spriteSheet = defaultSpriteSheet.Clone();
            }
            return new SimpleImageModel()
            {
                Key = key,
                Image = image,
                SpriteSheet = spriteSheet,
                Metadata = new SharedMetadata()
                {
                    Name = key,
                    Comment = "Legacy importer"
                }
            };
        }
    }
}