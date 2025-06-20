using System.Collections.Generic;
using System.Linq;

namespace CovertActionTools.Core.Models
{
    public class CatalogModel
    {
        public class CatalogData
        {
            /// <summary>
            /// List of keys included in the catalog
            /// </summary>
            public List<string> Keys { get; set; } = new();

            public CatalogData Clone()
            {
                return new CatalogData()
                {
                    Keys = Keys.ToList()
                };
            }
        }

        public string Key { get; set; } = string.Empty;
        /// <summary>
        /// Entries in file, legacy only has image entries.
        /// </summary>
        public Dictionary<string, SimpleImageModel> Entries { get; set; } = new();
        public CatalogData Data { get; set; } = new();
        public SharedMetadata Metadata { get; set; } = new();

        public CatalogModel Clone()
        {
            return new CatalogModel()
            {
                Key = Key,
                Data = Data.Clone(),
                Metadata = Metadata.Clone(),
                Entries = Entries.ToDictionary(x => x.Key,
                    x => x.Value.Clone())
            };
        }
    }
}