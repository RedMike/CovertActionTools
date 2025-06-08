using System.Collections.Generic;
using System.IO;
using System.Linq;
using CovertActionTools.Core.Importing;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.Core.Exporting.Publishers
{
    /// <summary>
    /// Given a loaded model for a World, returns multiple assets to save:
    ///   * WORLDx.DTA file (legacy)
    /// </summary>
    internal class WorldPublisher  : BaseExporter<Dictionary<int, WorldModel>>, ILegacyPublisher
    {
        private readonly ILogger<WorldPublisher> _logger;
        
        private readonly List<int> _keys = new();
        private int _index = 0;

        public WorldPublisher(ILogger<WorldPublisher> logger)
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
                [$"WORLD{world.Id}.DTA"] = GetLegacyWorldData(world),
            };

            return dict;
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