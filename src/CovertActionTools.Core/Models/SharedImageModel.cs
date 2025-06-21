using System;
using System.Collections.Generic;
using System.Linq;

namespace CovertActionTools.Core.Models
{
    public class SharedImageModel
    {
        public enum ImageType
        {
            Unknown = -1,
            QuitScreen = 0,
            EuropeMap = 1, //right side menu
            AfricaMap = 2, //right side menu
            AmericaMap = 3, //right side menu
            WireTapBoard = 4, 
            WireTapSprites = 5,
            CarScreen = 6, //selection colours
            CarSprites = 7, //sprite sheet
            EquipScreenBase = 8,
            EquipScreenSprites = 9, //sprite sheet
            FaceSprites = 10, //sprite sheeet
            GenderSelect = 11, //centre menu
            OutdoorTerrain = 12, //sprite sheet, no interactables
            IndoorTerrain = 13, //sprite sheet
            Hotel = 15, //menu top-left
            Icons = 16, //sprite sheet
            TransitionScreen = 17, 
            CombatSprites = 18, //sprite sheet
            CarViewScreen = 19, //sprite sheet
            TrainingScreen = 20, //menu top
        }
        
        public class ImageData
        {
            /// <summary>
            /// Type of image, mainly to correctly highlight areas on the editor
            /// </summary>
            public ImageType Type { get; set; } = ImageType.Unknown;
            /// <summary>
            /// Width/height of VGA images
            /// Usually 320
            /// </summary>
            public int Width { get; set; }
            /// <summary>
            /// Width/height of VGA images
            /// Usually 200
            /// </summary>
            public int Height { get; set; }
            
            /// <summary>
            /// Byte-byte mapping of VGA -> CGA colours
            /// If null, saved in specific format (0x7 instead of 0xF)
            /// </summary>
            public Dictionary<byte, byte>? LegacyColorMappings { get; set; }
            /// <summary>
            /// Legacy: only 11
            /// </summary>
            public byte CompressionDictionaryWidth { get; set; }

            public ImageData Clone()
            {
                return new ImageData()
                {
                    Type = Type,
                    Width = Width,
                    Height = Height,
                    LegacyColorMappings = LegacyColorMappings?.ToDictionary(x => x.Key, x => x.Value),
                    CompressionDictionaryWidth = CompressionDictionaryWidth
                };
            }
        }

        /// <summary>
        /// Stored as 1 byte index into VGA palette, left-to-right, top-to-bottom.
        /// </summary>
        public byte[] RawVgaImageData { get; set; } = Array.Empty<byte>();
        /// <summary>
        /// Stored as 4 bytes RGBA left-to-right, top-to-bottom.
        /// Is the converted version of `RawVgaImageData`. For display only, not saving.
        /// </summary>
        public byte[] VgaImageData { get; set; } = Array.Empty<byte>();
        /// <summary>
        /// Stored as 4 bytes RGBA left-to-right, top-to-bottom.
        /// Is the converted version of `RawCgaImageData`. For display only, not saving.
        /// </summary>
        public byte[] CgaImageData { get; set; } = Array.Empty<byte>();
        public ImageData Data { get; set; } = new();
        
        public SharedImageModel Clone()
        {
            return new SharedImageModel()
            {
                Data = Data.Clone(),
                CgaImageData = CgaImageData.ToArray(),
                RawVgaImageData = RawVgaImageData.ToArray(),
                VgaImageData = VgaImageData.ToArray(),
            };
        }
    }
}