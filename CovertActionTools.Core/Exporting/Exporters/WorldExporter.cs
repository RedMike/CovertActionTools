using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using CovertActionTools.Core.Importing;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.Core.Exporting.Exporters
{
    /// <summary>
    /// Given a loaded model for a World, returns multiple assets to save:
    ///   * WORLDx.json file (modern + metadata)
    /// </summary>
    internal class WorldExporter : BaseExporter<Dictionary<int, WorldModel>>
    {
#if DEBUG
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions()
        {
            WriteIndented = true
        };
#else
        private static readonly JsonSerializerOptions JsonOptions = JsonSerializerOptions.Default;
#endif
        
        private readonly ILogger<WorldExporter> _logger;
        
        private readonly List<int> _keys = new();
        private int _index = 0;

        public WorldExporter(ILogger<WorldExporter> logger)
        {
            _logger = logger;
        }

        protected override string Message => "Processing worlds..";

        protected override Dictionary<int, WorldModel> GetFromModel(PackageModel model)
        {
            return model.Worlds;
        }

        protected override void Reset()
        {
            _keys.Clear();
            _index = 0;
        }
        
        protected override int GetTotalItemCountInPath()
        {
            return _keys.Count;
        }

        protected override int RunExportStepInternal()
        {
            var nextKey = _keys[_index];

            var files = Export(Data[nextKey]);
            foreach (var pair in files)
            {
                File.WriteAllBytes(System.IO.Path.Combine(Path, pair.Key), pair.Value);
            }

            return _index++;
        }

        protected override void OnExportStart()
        {
            _keys.AddRange(GetKeys());
            _index = 0;
        }
        
        private List<int> GetKeys()
        {
            return Data.Keys.ToList();
        }
        
        private IDictionary<string, byte[]> Export(WorldModel world)
        {
            var dict = new Dictionary<string, byte[]>()
            {
                [$"WORLD{world.Id}_world.json"] = GetModernWorldData(world),
            };

            return dict;
        }
        
        private byte[] GetModernWorldData(WorldModel world)
        {
            var json = JsonSerializer.Serialize(world, JsonOptions);
            return Encoding.UTF8.GetBytes(json);
        }
    }
}