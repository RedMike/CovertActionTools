using System.Collections.Generic;

namespace CovertActionTools.Core.Models
{
    public class PackageModel
    {
        public Dictionary<string, SimpleImageModel> SimpleImages { get; set; } = new();
    }
}