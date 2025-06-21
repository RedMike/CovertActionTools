namespace CovertActionTools.Core.Models
{
    public class SimpleImageModel
    {
        /// <summary>
        /// ID that also determines the filename
        /// </summary>
        public string Key { get; set; } = string.Empty;

        public SharedMetadata Metadata { get; set; } = new();
        public SharedImageModel Image { get; set; } = new();

        public SimpleImageModel Clone()
        {
            return new SimpleImageModel()
            {
                Key = Key,
                Metadata = Metadata.Clone(),
                Image = Image.Clone()
            };
        }
    }
}