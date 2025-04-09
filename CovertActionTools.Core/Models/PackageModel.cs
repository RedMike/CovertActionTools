using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CovertActionTools.Core.Models
{
    public class PackageModel
    {
        public Dictionary<string, SimpleImageModel> SimpleImages { get; set; } = new();
        public Dictionary<int, CrimeModel> Crimes { get; set; } = new();
        public Dictionary<string, TextModel> Texts { get; set; } = new();
        public Dictionary<string, ClueModel> Clues { get; set; } = new();
        public Dictionary<string, PlotModel> Plots { get; set; } = new();
        public Dictionary<int, WorldModel> Worlds { get; set; } = new();

        public bool IsModified(PackageModel other)
        {
            return GetHash() != other.GetHash();
        }
        
        private string GetHash()
        {
            //not very efficient but good enough
            var s = JsonSerializer.Serialize(this);
            using var sha1 = SHA1.Create();
            var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(s));
            return string.Join("", hash.Select(x => $"{x:X2}"));
        }

        public PackageModel Clone()
        {
            return JsonSerializer.Deserialize<PackageModel>(JsonSerializer.Serialize(this))!;
        }
    }
}