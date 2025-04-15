using System.Collections.Generic;

namespace CovertActionTools.Core.Models
{
    public class AnimationModel
    {
        public class Metadata
        {
            /// <summary>
            /// Actual name separate from key/filename, for development
            /// </summary>
            public string Name { get; set; } = string.Empty;
            /// <summary>
            /// Arbitrary comment, for development
            /// </summary>
            public string Comment { get; set; } = string.Empty;
            public int Format1 { get; set; }
            public int Format2 { get; set; }
        }
        
        /// <summary>
        /// ID that also determines the filename
        /// </summary>
        public string Key { get; set; } = string.Empty;

        public Dictionary<int, SimpleImageModel> Images { get; set; } = new();
        public Metadata ExtraData { get; set; } = new();
    }
}