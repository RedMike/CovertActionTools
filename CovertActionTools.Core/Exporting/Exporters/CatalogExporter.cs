using System;
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
    /// Given a loaded model for a Catalog, returns multiple assets to save:
    ///   * CAT file (legacy catalog)
    ///   * JSON file _catalog (metadata)
    ///   * multiple PNG files _catalog
    /// </summary>
    internal class CatalogExporter : BaseExporter<Dictionary<string, CatalogModel>>
    {
#if DEBUG
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions()
        {
            WriteIndented = true
        };
#else
        private static readonly JsonSerializerOptions JsonOptions = JsonSerializerOptions.Default;
#endif
        
        private readonly ILogger<CatalogExporter> _logger;
        private readonly SharedImageExporter _imageExporter;
        
        private readonly List<string> _keys = new();
        private int _index = 0;

        public CatalogExporter(ILogger<CatalogExporter> logger, SharedImageExporter imageExporter)
        {
            _logger = logger;
            _imageExporter = imageExporter;
        }

        protected override string Message => "Processing catalogs..";
        
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
            _logger.LogInformation($"Starting export of catalogs: {_keys.Count}");
        }
        
        private List<string> GetKeys()
        {
            return Data.Keys.ToList();
        }
        
        private IDictionary<(string filename, bool publish), byte[]> Export(CatalogModel catalog)
        {
            var dict = new Dictionary<(string filename, bool publish), byte[]>
            {
                [($"{catalog.Key}.CAT", true)] = GetLegacyFileData(catalog),
                [($"{catalog.Key}_catalog.json", false)] = GetMetadata(catalog),
            };
            foreach (var entry in catalog.ExtraData.Keys)
            {
                var image = catalog.Entries[entry];
                dict.Add(($"{image.Key}_catalog_img.json", false), _imageExporter.GetMetadata(image));
                dict.Add(($"{image.Key}_modern.png", false), _imageExporter.GetModernImageData(image));
                dict.Add(($"{image.Key}_VGA.png", false), _imageExporter.GetVgaImageData(image));
            }
            return dict;
        }

        private byte[] GetLegacyFileData(CatalogModel catalog)
        {
            using var memStream = new MemoryStream();
            using var writer = new BinaryWriter(memStream);

            writer.Write((ushort)catalog.ExtraData.Keys.Count);
            var dataSegmentStart = 2 + //ushort
                                   (12 + 4 + 4 + 4) * catalog.ExtraData.Keys.Count + //list of entries
                                   2; //filler
            var currentPointerOffset = 2;
            var currentDataOffset = dataSegmentStart;
            foreach (var key in catalog.ExtraData.Keys)
            {
                var entry = catalog.Entries[key];
                var rawData = _imageExporter.GetLegacyFileData(entry);
                
                //first write the pointer info
                memStream.Seek(currentPointerOffset, SeekOrigin.Begin);
                var filename = entry.Key.Trim().Trim('\0');
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
        
        private byte[] GetMetadata(CatalogModel catalog)
        {
            var serialisedMetadata = JsonSerializer.Serialize(catalog.ExtraData, JsonOptions);
            var bytes = Encoding.UTF8.GetBytes(serialisedMetadata);
            return bytes;
        }
    }
}