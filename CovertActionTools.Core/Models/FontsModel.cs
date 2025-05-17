using System.Collections.Generic;

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
        
        public class Metadata
        {
            public Dictionary<int, FontMetadata> Fonts { get; set; } = new();
        }
        
        public Metadata ExtraData { get; set; } = new();

        public List<Font> Fonts { get; set; } = new();
    }
}