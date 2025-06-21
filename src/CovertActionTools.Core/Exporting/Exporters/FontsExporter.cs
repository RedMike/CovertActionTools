using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using CovertActionTools.Core.Importing;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.Core.Exporting.Exporters
{
    /// <summary>
    /// Given a loaded model for Fonts, returns multiple assets to save:
    ///   * FONTS.json file (metadata)
    ///   * FONTS_X_Y.png PNG file where Y is ASCII code number
    /// </summary>
    internal class FontsExporter : BaseExporter<FontsModel>
    {
#if DEBUG
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions()
        {
            WriteIndented = true
        };
#else
        private static readonly JsonSerializerOptions JsonOptions = JsonSerializerOptions.Default;
#endif
        
        private readonly ILogger<FontsExporter> _logger;
        
        private bool _done = false;

        public FontsExporter(ILogger<FontsExporter> logger)
        {
            _logger = logger;
        }

        protected override string Message => "Processing fonts..";

        protected override FontsModel GetFromModel(PackageModel model)
        {
            return model.Fonts;
        }

        protected override void Reset()
        {
            _done = false;
        }

        protected override int GetTotalItemCountInPath()
        {
            return Data.Fonts.Count > 0 ? 1 : 0;
        }

        protected override int RunExportStepInternal()
        {
            if (Data.Fonts.Count == 0 || _done)
            {
                return 1;
            }
            
            var files = Export(Data);
            var path = GetPath(Path);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            foreach (var pair in files)
            {
                File.WriteAllBytes(System.IO.Path.Combine(path, pair.Key), pair.Value);
            }

            _done = true;
            return 1;
        }

        protected override void OnExportStart()
        {
            _done = false;
        }
        
        private IDictionary<string, byte[]> Export(FontsModel fonts)
        {
            var dict = new Dictionary<string, byte[]>()
            {
                ["FONTS.json"] = GetFontsMetadata(fonts),
            };

            for (var f = 0; f < fonts.Fonts.Count; f++)
            {
                var font = fonts.Fonts[f];
                foreach (var c in font.CharacterImages.Keys)
                {
                    var code = (byte)c;
                    dict[$"FONTS_{f}_{code}.png"] = font.CharacterImages[c];
                }
            }

            return dict;
        }
        
        private byte[] GetFontsMetadata(FontsModel fonts)
        {
            var json = JsonSerializer.Serialize(fonts.Data, JsonOptions);
            return Encoding.UTF8.GetBytes(json);
        }
        
        private string GetPath(string path)
        {
            return System.IO.Path.Combine(path, "font");
        }
    }
}