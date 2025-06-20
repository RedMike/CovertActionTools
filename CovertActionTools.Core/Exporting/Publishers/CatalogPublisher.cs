using System.Collections.Generic;
using System.IO;
using System.Linq;
using CovertActionTools.Core.Exporting.Shared;
using CovertActionTools.Core.Importing;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.Core.Exporting.Publishers
{
    /// <summary>
    /// Given a loaded model for a Catalog, returns multiple assets to save:
    ///   * CAT file (legacy catalog)
    /// </summary>
    internal class CatalogPublisher : BaseExporter<Dictionary<string, CatalogModel>>, ILegacyPublisher
    {
        private readonly ILogger<CatalogPublisher> _logger;
        private readonly SharedImageExporter _imageExporter;
        
        private readonly List<string> _keys = new();
        private int _index = 0;

        public CatalogPublisher(ILogger<CatalogPublisher> logger, SharedImageExporter imageExporter)
        {
            _logger = logger;
            _imageExporter = imageExporter;
        }

        protected override string Message => "Processing catalogs..";

        protected override Dictionary<string, CatalogModel> GetFromModel(PackageModel model)
        {
            return model.Catalogs
                .Where(x => model.Index.CatalogIncluded.Contains(x.Key))
                .ToDictionary(x => x.Key, x => x.Value);
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
            if (_index >= _keys.Count)
            {
                return _index;
            }
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
        
        private List<string> GetKeys()
        {
            return Data.Keys.ToList();
        }
        
        private IDictionary<string, byte[]> Export(CatalogModel catalog)
        {
            var dict = new Dictionary<string, byte[]>
            {
                [$"{catalog.Key}.CAT"] = GetLegacyFileData(catalog),
            };
            return dict;
        }

        private byte[] GetLegacyFileData(CatalogModel catalog)
        {
            using var memStream = new MemoryStream();
            using var writer = new BinaryWriter(memStream);

            writer.Write((ushort)catalog.Data.Keys.Count);
            var dataSegmentStart = 2 + //ushort
                                   (12 + 4 + 4 + 4) * catalog.Data.Keys.Count + //list of entries
                                   2; //filler
            var currentPointerOffset = 2;
            var currentDataOffset = dataSegmentStart;
            foreach (var key in catalog.Data.Keys)
            {
                var entry = catalog.Entries[key];
                var rawData = _imageExporter.GetLegacyFileData(entry);
                
                //first write the pointer info
                memStream.Seek(currentPointerOffset, SeekOrigin.Begin);
                var filename = key.Trim().Trim('\0');
                if (filename.Length > 8)
                {
                    _logger.LogWarning($"Filename larger than 12 chars, truncating: {filename}.PIC");
                    filename = filename.Substring(0, 8);
                }

                filename += ".PIC";
                foreach (var c in filename.PadRight(12, (char)0))
                {
                    writer.Write(c);
                }
                writer.Write((uint)0); //checksum, not used
                writer.Write((uint)rawData.Length);
                writer.Write((uint)currentDataOffset);
                
                //then write the data segment info
                memStream.SetLength(currentDataOffset + rawData.Length);
                memStream.Seek(currentDataOffset, SeekOrigin.Begin);
                writer.Write(rawData);
                
                //then increment correctly
                currentPointerOffset += 12 + 4 + 4 + 4;
                currentDataOffset += rawData.Length;
            }

            return memStream.ToArray();
        }
    }
}