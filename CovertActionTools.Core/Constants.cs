using System.Collections.Generic;
using System.Linq;
using CovertActionTools.Core.Models;

namespace CovertActionTools.Core
{
    public static class Constants
    {
        public const int CurrentFormatVersion = 1;
        
        #region Colors
        /// <summary>
        /// Color that is explicitly transparent
        /// </summary>
        public static readonly (byte r, byte g, byte b, byte a) TransparentColor = (0, 0, 0, 0);

        /// <summary>
        /// Color that is used on particular sprites to replace enemy clothing colors (enemy allegiance)
        /// Not applied automatically to all sprites, but decided by engine at sprite draw time.
        /// </summary>
        public const byte EnemyClothingColorId = 14;
        /// <summary>
        /// Color that is used on particular sprites to replace enemy clothing colors (enemy allegiance)
        /// Not applied automatically to all sprites, but decided by engine at sprite draw time.
        /// Legacy applies it to character sprites only, but includes the player; on map sprites it stays as-is.
        /// </summary>
        public static readonly (byte r, byte g, byte b, byte a) EnemyClothingColor = (0xFF, 0xFF, 0x55, 255);
        
        /// <summary>
        /// Color that is used on particular sprites to replace player clothing colors (disguises)
        /// Not applied automatically to all sprites, but decided by engine at sprite draw time.
        /// Legacy applies it only to the big player sprite on the UI; on map sprites it becomes transparent!
        /// </summary>
        public const byte PlayerClothingColorId = 13;
        /// <summary>
        /// Color that is used on particular sprites to replace player clothing colors (disguises)
        /// Not applied automatically to all sprites, but decided by engine at sprite draw time.
        /// Legacy applies it only to the big player sprite on the UI; on map sprites it becomes transparent!
        /// </summary>
        public static readonly (byte r, byte g, byte b, byte a) PlayerClothingColor = (0xFF, 0x55, 0xFF, 255);
        
        
        //wire tap active lines take effect on wire tap line sprites _and_ others symbols (plain palette shift)
        //wire tap background colour does not matter
        //wire tap background grid colour does not matter
        //wire tap line colour on outside of chip area is used to actually follow the trace...but the actual wires
        //  that are active from the first chip don't match
        
        //VGA palette, with colour index 5 replaced with plain black at full alpha
        public static readonly Dictionary<byte, (byte r, byte g, byte b, byte a)> VgaColorMapping = new()
        {
            {0, TransparentColor},
            {1, (0, 0, 0xAA, 255)}, //char sprite backgrounds where unused?
            {2, (0, 0xAA, 0, 255)}, //wire tap background
            {3, (0, 0xAA, 0xAA, 255)}, //wire tap background grid
            {4, (0xAA, 0, 0, 255)}, //wire tap active line 1
            {5, (0, 0, 0, 255)}, //wire tap active line 3
            {6, (0xAA, 0x55, 0, 255)},
            {7, (0xAA, 0xAA, 0xAA, 255)}, //wire tap chip highlight
            {8, (0x55, 0x55, 0x55, 255)}, //wire tap chip/locked
            {9, (0x55, 0x55, 0xFF, 255)}, //char sprite separators(?), wire tap right connectors, wire tap symbols 2
            {10, (0x55, 0xFF, 0x55, 255)}, //wire tap lines
            {11, (0x55, 0xFF, 0xFF, 255)}, //wire tap symbols
            {12, (0xFF, 0x55, 0x55, 255)}, //wire tap active line 2
            {PlayerClothingColorId, PlayerClothingColor}, //wire tap active line 4
            {EnemyClothingColorId, EnemyClothingColor},
            {15, (0xFF, 0xFF, 0xFF, 255)}, //wire tap left connectors
        };
        
        //Reduced CGA palette, no 'transparent' colour because that comes from the VGA palette
        public static readonly Dictionary<byte, (byte r, byte g, byte b, byte a)> CgaColorMapping = new()
        {
            {0, (0, 0, 0, 255)},
            {1, (0, 0xFF, 0xFF, 255)},
            {2, (0xFF, 0, 0xFF, 255)},
            {3, (0xFF, 0xFF, 0xFF, 255)},
        };

        public static readonly Dictionary<(byte r, byte g, byte b, byte a), byte> ReverseVgaColorMapping =
            VgaColorMapping.ToDictionary(x => x.Value, x => x.Key);
        #endregion
        
        #region Filenames
        public static SimpleImageModel.ImageType GetLikelyImageType(string fileName)
        {
            if (fileName.StartsWith("AD"))
            {
                return SimpleImageModel.ImageType.QuitScreen;
            }

            if (fileName.StartsWith("EUROPE"))
            {
                return SimpleImageModel.ImageType.EuropeMap;
            }
            if (fileName.StartsWith("AFRICA"))
            {
                return SimpleImageModel.ImageType.AfricaMap;
            }
            if (fileName.StartsWith("CENTRAL"))
            {
                return SimpleImageModel.ImageType.AmericaMap;
            }

            if (fileName.StartsWith("BOARD"))
            {
                return SimpleImageModel.ImageType.WireTapBoard;
            }

            if (fileName.StartsWith("BUGS"))
            {
                return SimpleImageModel.ImageType.WireTapSprites;
            }

            if (fileName.StartsWith("CARS"))
            {
                return SimpleImageModel.ImageType.CarScreen;
            }
            if (fileName.StartsWith("CHASE"))
            {
                return SimpleImageModel.ImageType.CarSprites;
            }

            if (fileName.StartsWith("EQUIP1"))
            {
                return SimpleImageModel.ImageType.EquipScreenBase;
            }

            if (fileName.StartsWith("EQUIP2"))
            {
                return SimpleImageModel.ImageType.EquipScreenSprites;
            }

            if (fileName.StartsWith("FACES"))
            {
                return SimpleImageModel.ImageType.FaceSprites;
            }

            if (fileName.StartsWith("GENDER"))
            {
                return SimpleImageModel.ImageType.GenderSelect;
            }

            if (fileName.StartsWith("GUYS2"))
            {
                return SimpleImageModel.ImageType.OutdoorTerrain;
            }

            if (fileName.StartsWith("GUYS3"))
            {
                return SimpleImageModel.ImageType.IndoorTerrain;
            }

            if (fileName.StartsWith("HOTEL"))
            {
                return SimpleImageModel.ImageType.Hotel;
            }

            if (fileName.StartsWith("ICONS"))
            {
                return SimpleImageModel.ImageType.Icons;
            }

            if (fileName.StartsWith("LABS") || fileName.StartsWith("SNEAKIN") || fileName.StartsWith("WIRETAP"))
            {
                return SimpleImageModel.ImageType.TransitionScreen;
            }

            if (fileName.StartsWith("SPRITES"))
            {
                return SimpleImageModel.ImageType.CombatSprites;
            }

            if (fileName.StartsWith("STREET"))
            {
                return SimpleImageModel.ImageType.CarViewScreen;
            }

            if (fileName.StartsWith("TRAINING"))
            {
                return SimpleImageModel.ImageType.TrainingScreen;
            }

            return SimpleImageModel.ImageType.Unknown;
        }
        #endregion
    }
}