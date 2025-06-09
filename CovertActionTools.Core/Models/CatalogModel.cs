using System.Collections.Generic;
using System.Linq;

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

        public CatalogModel Clone()
        {
            return new CatalogModel()
            {
                Key = Key,
                ExtraData = new Metadata()
                {
                    Name = ExtraData.Name,
                    Comment = ExtraData.Comment,
                    Keys = ExtraData.Keys.ToList()
                },
                Entries = Entries.ToDictionary(x => x.Key,
                    x => x.Value.Clone())
            };
        }
    }
}