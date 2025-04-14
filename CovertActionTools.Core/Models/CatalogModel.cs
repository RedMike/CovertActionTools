using System.Collections.Generic;

namespace CovertActionTools.Core.Models
{
    public class CatalogModel
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

            /// <summary>
            /// List of keys included in the catalog
            /// </summary>
            public List<string> Keys { get; set; } = new();
        }

        public string Key { get; set; } = string.Empty;
        /// <summary>
        /// Entries in file, legacy only has image entries.
        /// </summary>
        public Dictionary<string, SimpleImageModel> Entries { get; set; } = new();
        public Metadata ExtraData { get; set; } = new();
    }
}