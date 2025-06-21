using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CovertActionTools.Core.Models
{
    public class PackageModel
    {
        public PackageIndex Index { get; set; } = new();
        public Dictionary<string, SimpleImageModel> SimpleImages { get; set; } = new();
        public Dictionary<int, CrimeModel> Crimes { get; set; } = new();
        public Dictionary<string, TextModel> Texts { get; set; } = new();
        public Dictionary<string, ClueModel> Clues { get; set; } = new();
        public Dictionary<string, PlotModel> Plots { get; set; } = new();
        public Dictionary<int, WorldModel> Worlds { get; set; } = new();
        public Dictionary<string, CatalogModel> Catalogs { get; set; } = new();
        public Dictionary<string, AnimationModel> Animations { get; set; } = new();
        public FontsModel Fonts { get; set; } = new();
        public Dictionary<string, ProseModel> Prose { get; set; } = new();
        
        public PackageModel Clone()
        {
            return JsonSerializer.Deserialize<PackageModel>(JsonSerializer.Serialize(this))!;
        }
    }
}