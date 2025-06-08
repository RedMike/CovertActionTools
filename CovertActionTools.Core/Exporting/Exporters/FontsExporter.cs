using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using CovertActionTools.Core.Importing;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace CovertActionTools.Core.Exporting.Exporters
{
    /// <summary>
    /// Given a loaded model for Fonts, returns multiple assets to save:
    ///   * FONTS.json file (metadata)
    ///   * FONTS_X_Y.png PNG file where Y is ASCII code number
    ///   * FONTS.CV file (legacy)
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
        
        public override ExportStatus.ExportStage GetStage() => ExportStatus.ExportStage.ProcessingFonts;

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
            foreach (var pair in files)
            {
                var exportPath = Path;
                if (!string.IsNullOrEmpty(PublishPath) || !pair.Key.publish)
                {
                    var publishPath = PublishPath ?? exportPath;
                    File.WriteAllBytes(System.IO.Path.Combine(pair.Key.publish ? publishPath : exportPath, pair.Key.filename), pair.Value);
                }
            }

            _done = true;
            return 1;
        }

        protected override void OnExportStart()
        {
            _done = false;
            _logger.LogInformation($"Starting export of fonts");
        }
        
        private IDictionary<(string filename, bool publish), byte[]> Export(FontsModel fonts)
        {
            var dict = new Dictionary<(string filename, bool publish), byte[]>()
            {
                [("FONTS.json", false)] = GetFontsMetadata(fonts),
                [("FONTS.CV", true)] = GetFontsLegacyFile(fonts)
            };

            for (var f = 0; f < fonts.Fonts.Count; f++)
            {
                var font = fonts.Fonts[f];
                foreach (var c in font.CharacterImages.Keys)
                {
                    var code = (byte)c;
                    dict[($"FONTS_{f}_{code}.png", false)] = font.CharacterImages[c];
                }
            }

            return dict;
        }

        private byte[] GetFontsLegacyFile(FontsModel fonts)
        {
            using var memStream = new MemoryStream();
            using var writer = new BinaryWriter(memStream);

            //first 2 bytes are the count
            writer.Write((ushort)fonts.Fonts.Count);
            //then there are 2 bytes per font which is the offset to the font face data, so we add these in last
            memStream.Seek(2 * fonts.Fonts.Count, SeekOrigin.Current);
            
            var fontFaceOffsets = new List<ushort>();
            //now for each font we compose the data
            for (var fontId = 0; fontId < fonts.Fonts.Count; fontId++)
            {
                var fontMetadata = fonts.ExtraData.Fonts[fontId];
                //first we convert the images back into useful data
                var fontStrings = new Dictionary<char, List<string>>();
                foreach (var c in fonts.Fonts[fontId].CharacterImages.Keys)
                {
                    var imageData = fonts.Fonts[fontId].CharacterImages[c];
                    var width = fontMetadata.CharacterWidths[c];
                    var height = fontMetadata.CharHeight;
                    using var skBitmap = SKBitmap.Decode(imageData, new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
                    var lines = new List<string>();
                    for (var i = 0; i < height; i++)
                    {
                        var s = "";
                        for (var j = 0; j < width; j++)
                        {
                            var col = skBitmap.GetPixel(j, i);
                            var transparent = !((col.Red > 0 || col.Green > 0 || col.Blue > 0) && col.Alpha > 0);
                            if (transparent)
                            {
                                s += " ";
                            }
                            else
                            {
                                s += "#";
                            }
                        }

                        lines.Add(s);
                    }

                    fontStrings[c] = lines;
                }
                //one byte per character of character widths
                for (var c = fontMetadata.FirstAsciiValue; c <= fontMetadata.LastAsciiValue; c++)
                {
                    writer.Write((byte)fontMetadata.CharacterWidths[(char)c]);
                }
                
                //then 8 bytes of config info
                writer.Write((byte)fontMetadata.FirstAsciiValue);
                writer.Write((byte)fontMetadata.LastAsciiValue);
                var maxCharWidth = fontMetadata.CharacterWidths.Values.Max();
                var bytesPerRowPerChar = (byte)Math.Ceiling(maxCharWidth / 8.0f);
                writer.Write((byte)bytesPerRowPerChar);
                writer.Write((byte)0); //char height starts at 0
                writer.Write((byte)(fontMetadata.CharHeight - 1));
                writer.Write((byte)fontMetadata.HorizontalPadding);
                writer.Write((byte)fontMetadata.VerticalPadding);
                writer.Write((byte)0); //padding
                
                //this is the start of the font face data, so the target of the pointer
                fontFaceOffsets.Add((ushort)memStream.Position);
                
                //font face is encoded as bitfields, first the top row of each char, then the next row of each char, etc
                for (var row = 0; row < fontMetadata.CharHeight; row++)
                {
                    for (var c = fontMetadata.FirstAsciiValue; c <= fontMetadata.LastAsciiValue; c++)
                    {
                        var lines = fontStrings[(char)c];
                        var line = lines[row].PadRight(maxCharWidth, ' ').Reverse();
                        byte currentByte = 0;
                        byte bitMask = 0x1;
                        var justReset = false;
                        foreach (var pixel in line)
                        {
                            byte bitToAdd = pixel == ' ' ? (byte)0x00 : (byte)0xFF;
                            currentByte |= (byte)(bitToAdd & bitMask);
                            if (bitMask == 0x80)
                            {
                                //when we've fully filled out a byte, reset to a blank byte on bit 0
                                bitMask = 0x1;
                                writer.Write((byte)currentByte);
                                currentByte = 0;
                                justReset = true;
                            }
                            else
                            {
                                bitMask = (byte)(bitMask << 1);
                                justReset = false;
                            }
                        }

                        if (!justReset)
                        {
                            //if we didn't fully fill out a byte yet, publish the one we have partially
                            writer.Write((byte)currentByte);
                        }
                    }
                }
            }
            
            //now we have all the offsets so we can go back and add the offsets
            memStream.Seek(2, SeekOrigin.Begin);
            foreach (var offset in fontFaceOffsets)
            {
                writer.Write((ushort)offset);
            }
            
            return memStream.ToArray();
        }
        
        private byte[] GetFontsMetadata(FontsModel fonts)
        {
            var json = JsonSerializer.Serialize(fonts.ExtraData, JsonOptions);
            return Encoding.UTF8.GetBytes(json);
        }
    }
}