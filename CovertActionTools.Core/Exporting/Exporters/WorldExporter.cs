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
    ///   * WORLDx.DTA file (legacy)
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
                var exportPath = Path;
                if (!string.IsNullOrEmpty(PublishPath) || !pair.Key.publish)
                {
                    var publishPath = PublishPath ?? exportPath;
                    File.WriteAllBytes(System.IO.Path.Combine(pair.Key.publish ? publishPath : exportPath, pair.Key.filename), pair.Value);
                }
            }

            return _index++;
        }

        protected override void OnExportStart()
        {
            _keys.AddRange(GetKeys());
            _index = 0;
            _logger.LogInformation($"Starting export of worlds: {_keys.Count}");
        }
        
        private List<int> GetKeys()
        {
            return Data.Keys.ToList();
        }
        
        private IDictionary<(string filename, bool publish), byte[]> Export(WorldModel world)
        {
            var dict = new Dictionary<(string filename, bool publish), byte[]>()
            {
                [($"WORLD{world.Id}.DTA", true)] = GetLegacyWorldData(world),
                [($"WORLD{world.Id}_world.json", false)] = GetModernWorldData(world),
            };

            return dict;
        }
        
        private byte[] GetModernWorldData(WorldModel world)
        {
            var json = JsonSerializer.Serialize(world, JsonOptions);
            return Encoding.UTF8.GetBytes(json);
        }

        private byte[] GetLegacyWorldData(WorldModel world)
        {
            using var memStream = new MemoryStream();
            using var writer = new BinaryWriter(memStream);

            writer.Write((ushort)world.Cities.Count);
            writer.Write((ushort)world.Organisations.Count);

            foreach (var city in world.Cities)
            {
                foreach (var c in city.Name.Trim().Trim('\0').PadRight(12, (char)0))
                {
                    writer.Write(c);
                }
                
                foreach (var c in city.Country.Trim().Trim('\0').PadRight(12, (char)0))
                {
                    writer.Write(c);
                }

                writer.Write((ushort)city.Unknown1);
                writer.Write((ushort)city.Unknown2);
                writer.Write((ushort)0);
                writer.Write((ushort)0);
                writer.Write((byte)city.MapX);
                writer.Write((byte)city.MapY);
            }

            foreach (var org in world.Organisations)
            {
                foreach (var c in org.ShortName.Trim().Trim('\0').PadRight(6, (char)0))
                {
                    writer.Write(c);
                }
                
                foreach (var c in org.LongName.Trim().Trim('\0').PadRight(20, (char)0))
                {
                    writer.Write(c);
                }

                writer.Write((ushort)org.Unknown1);
                writer.Write((ushort)org.Unknown2);
                writer.Write((ushort)org.Unknown3);
                writer.Write((ushort)org.UniqueId);
                writer.Write((ushort)org.Unknown4);
            }

            return memStream.ToArray();
        }
    }
}