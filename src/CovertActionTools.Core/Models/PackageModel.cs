using System.Collections.Generic;
using System.Linq;
using CovertActionTools.Core.Utilities;

namespace CovertActionTools.Core.Models
{
    public class PackageModel : ICloneable<PackageModel>
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
            return new PackageModel()
            {
                Index = Index.Clone(),
                SimpleImages = SimpleImages.ToDictionary(x => x.Key, x => x.Value.Clone()),
                Crimes = Crimes.ToDictionary(x => x.Key, x => x.Value.Clone()),
                Texts = Texts.ToDictionary(x => x.Key, x => x.Value.Clone()),
                Clues = Clues.ToDictionary(x => x.Key, x => x.Value.Clone()),
                Plots = Plots.ToDictionary(x => x.Key, x => x.Value.Clone()),
                Worlds = Worlds.ToDictionary(x => x.Key, x => x.Value.Clone()),
                Catalogs = Catalogs.ToDictionary(x => x.Key, x => x.Value.Clone()),
                Animations = Animations.ToDictionary(x => x.Key, x => x.Value.Clone()),
                Prose = Prose.ToDictionary(x => x.Key, x => x.Value.Clone()),
                Fonts = Fonts.Clone()
            };
        }
    }
}