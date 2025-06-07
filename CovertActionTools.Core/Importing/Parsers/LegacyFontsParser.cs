using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CovertActionTools.Core.Conversion;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.Core.Importing.Parsers
{
    internal class LegacyFontsParser : BaseImporter<FontsModel>, ILegacyParser
    {
        private readonly ILogger<LegacyFontsParser> _logger;
        
        private FontsModel _result = new FontsModel();
        private bool _done = false;

        public LegacyFontsParser(ILogger<LegacyFontsParser> logger)
        {
            _logger = logger;
        }

        protected override string Message => "Processing fonts..";
        public override ImportStatus.ImportStage GetStage() => ImportStatus.ImportStage.ProcessingFonts;

        public override void SetResult(PackageModel model)
        {
            model.Fonts = GetResult();
        }

        protected override bool CheckIfValidForImportInternal(string path)
        {
            if (Directory.GetFiles(path, "FONTS.CV").Length == 0)
            {
                return false;
            }

            return true;
        }

        protected override int GetTotalItemCountInPath()
        {
            return Directory.GetFiles(Path, "FONTS.CV").Length;
        }

        protected override int RunImportStepInternal()
        {
            if (_done)
            {
                return 1;
            }

            _result = Parse(Path);
            _done = true;
            return 1;
        }

        protected override FontsModel GetResultInternal()
        {
            return _result;
        }

        protected override void OnImportStart()
        {
            _done = false;
        }

        private FontsModel Parse(string path)
        {
            var filePath = System.IO.Path.Combine(path, $"FONTS.CV");
            if (!File.Exists(filePath))
            {
                throw new Exception($"Missing FONTS file: FONTS.CV");
            }

            var rawData = File.ReadAllBytes(filePath);

            var fonts = new FontsModel();
            
            using var memStream = new MemoryStream(rawData);
            using var reader = new BinaryReader(memStream);

            var fontCount = reader.ReadUInt16();
            //the offsets point to the start of the face data, but there's config data before it
            var fontFaceOffsets = new List<ushort>();
            for (var i = 0; i < fontCount; i++)
            {
                fontFaceOffsets.Add(reader.ReadUInt16());
            }

            for (var f = 0; f < fontCount; f++)
            {
                var offset = fontFaceOffsets[f];
                
                //the 8 bytes before the font faces are config info
                memStream.Seek(offset - 8, SeekOrigin.Begin);
                var firstAsciiValue = reader.ReadByte();
                var lastAsciiValue = reader.ReadByte();
                var charCount = lastAsciiValue - firstAsciiValue + 1;
                var bytesPerRowPerChar = reader.ReadByte();
                var firstRow = reader.ReadByte();
                var lastRow = reader.ReadByte();
                var charHeight = lastRow - firstRow + 1;
                var horizontalPadding = reader.ReadByte();
                var verticalPadding = reader.ReadByte();
                
                var padding = reader.ReadByte();
                if (padding != 0x00)
                {
                    throw new Exception($"Expected padding to be 0x00 but was {padding:X2}");
                }
                
                //then the X bytes before that are the char widths
                var charWidths = new Dictionary<char, int>();
                memStream.Seek(offset - 8 - charCount, SeekOrigin.Begin);
                for (var i = 0; i < charCount; i++)
                {
                    var c = (char)(firstAsciiValue + i);
                    var width = reader.ReadByte();
                    charWidths[c] = width;
                }

                //the font faces are encoded as series of X bytes that are used as bitfields to encode 1-bit pixels
                //each character will have different widths, but the same height
                //the series of X bytes is the bytes for the first row of all chars, then the second row, etc
                var charFontData = new Dictionary<char, List<string>>();
                memStream.Seek(offset, SeekOrigin.Begin);
                for (var row = 0; row < charHeight; row++)
                {
                    for (var i = 0; i < charCount; i++)
                    {
                        var c = (char)(firstAsciiValue + i);
                        if (!charFontData.TryGetValue(c, out var charStrings))
                        {
                            charFontData[c] = new List<string>();
                            charStrings = charFontData[c];
                        }
                        var bytes = reader.ReadBytes(bytesPerRowPerChar);
                        var s = "";
                        for (var b = 0; b < bytes.Length; b++)
                        {
                            uint bitMask = 0x80; //unsigned to do logical shift
                            while (bitMask != 0)
                            {
                                var bit = (bytes[b] & bitMask) > 0;
                                s += bit ? "#" : " ";
                                bitMask = bitMask >> 1;
                            }
                        }
                        s = s.Substring(0, charWidths[c]);
                        charStrings.Add(s);
                    }
                }

                var charImageData = new Dictionary<char, byte[]>();
                foreach (var c in charFontData.Keys)
                {
                    var imageData = new byte[charWidths[c] * charHeight * 4];
                    var fontData = charFontData[c];
                    var q = 0;
                    
                    for (var i = 0; i < charHeight; i++)
                    {
                        var line = fontData[i];
                        for (var j = 0; j < line.Length; j++)
                        {
                            var transparent = line[j] == ' ';
                            imageData[q++] = (byte)(transparent ? 0 : 255);
                            imageData[q++] = (byte)(transparent ? 0 : 255);
                            imageData[q++] = (byte)(transparent ? 0 : 255);
                            imageData[q++] = (byte)(transparent ? 0 : 255);
                        }
                    }

                    var texture = ImageConversion.RgbaToTexture(charWidths[c], charHeight, imageData);
                    charImageData[c] = texture;
                }

                fonts.ExtraData.Fonts[f] = new FontsModel.FontMetadata()
                {
                    Comment = "Legacy import",
                    FirstAsciiValue = firstAsciiValue,
                    LastAsciiValue = lastAsciiValue,
                    HorizontalPadding = horizontalPadding,
                    VerticalPadding = verticalPadding,
                    CharacterWidths = charWidths,
                    CharHeight = (byte)charHeight
                };
                fonts.Fonts.Add(new FontsModel.Font()
                {
                    CharacterImages = charImageData
                });
            }

            return fonts;
        }
    }
}