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
            //"CAMERA",
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
                                X = 175, Y = 0,
                                Width = 9, Height = 13
                            }
                        },
                        { "grenade_gas", new SimpleImageModel.Sprite()
                            {
                                X = 175, Y = 15,
                                Width = 9, Height = 13
                            }
                        },
                        { "grenade_flash", new SimpleImageModel.Sprite()
                            {
                                X = 175, Y = 30,
                                Width = 9, Height = 13
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
                                Width = 3, Height = 7
                            }
                        },
                        { "magazine", new SimpleImageModel.Sprite()
                            {
                                X = 180, Y = 82,
                                Width = 11, Height = 15
                            }
                        }
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
                                X = 175, Y = 0,
                                Width = 9, Height = 13
                            }
                        },
                        { "grenade_gas", new SimpleImageModel.Sprite()
                            {
                                X = 175, Y = 15,
                                Width = 9, Height = 13
                            }
                        },
                        { "grenade_flash", new SimpleImageModel.Sprite()
                            {
                                X = 175, Y = 30,
                                Width = 9, Height = 13
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
                                Width = 3, Height = 7
                            }
                        },
                        { "magazine", new SimpleImageModel.Sprite()
                            {
                                X = 180, Y = 82,
                                Width = 11, Height = 15
                            }
                        }
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
            //"FACES",
            //"FACESF",
            //"GUYS2",
            //"GUYS3",
            //"ICONS",
            //"SPRITES",
            //"SPRITESF",
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