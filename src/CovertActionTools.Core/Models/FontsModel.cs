using System.Collections.Generic;
using System.Linq;

namespace CovertActionTools.Core.Models
{
    public class FontsModel
    {
        public class Font
        {
            public Dictionary<char, byte[]> CharacterImages { get; set; } = new();
        }

        public class FontMetadata
        {
            /// <summary>
            /// Arbitrary comment, for development
            /// </summary>
            public string Comment { get; set; } = string.Empty;
            public byte FirstAsciiValue { get; set; }
            public byte LastAsciiValue { get; set; }
            public byte HorizontalPadding { get; set; }
            public byte VerticalPadding { get; set; }
            public byte CharHeight { get; set; }
            public Dictionary<char, int> CharacterWidths { get; set; } = new();
        }
        
        public class FontData
        {
            public Dictionary<int, FontMetadata> Fonts { get; set; } = new();
        }
        
        public FontData Data { get; set; } = new();

        public List<Font> Fonts { get; set; } = new();

        public FontsModel Clone()
        {
            return new FontsModel()
            {
                Data = new FontData()
                {
                    Fonts = Data.Fonts.ToDictionary(x => x.Key,
                        x => new FontMetadata()
                        {
                            Comment = x.Value.Comment,
                            FirstAsciiValue = x.Value.FirstAsciiValue,
                            LastAsciiValue = x.Value.LastAsciiValue,
                            HorizontalPadding = x.Value.HorizontalPadding,
                            VerticalPadding = x.Value.VerticalPadding,
                            CharHeight = x.Value.CharHeight,
                            CharacterWidths = x.Value.CharacterWidths.ToDictionary(y => y.Key, y => y.Value)
                        })
                },
                Fonts = Fonts.Select(x => new Font()
                {
                    CharacterImages = x.CharacterImages.ToDictionary(x => x.Key, x => x.Value.ToArray())
                }).ToList()
            };
        }
    }
}