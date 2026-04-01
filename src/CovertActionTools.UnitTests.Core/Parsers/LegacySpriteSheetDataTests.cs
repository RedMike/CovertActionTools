using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CovertActionTools.Core.Importing.Parsers;
using CovertActionTools.Core.Importing.Shared;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

using CovertActionTools.UnitTests.Core.Parsers.Data;
using CovertActionTools.UnitTests.Core.Parsers.Stubs;

namespace CovertActionTools.UnitTests.Core.Parsers;

public class LegacySpriteSheetDataTests : IDisposable
{
    private readonly LegacySimpleImageParser _parser;
    private readonly StubLzwDecompression _stubDecompression;
    private readonly string _tempDir;

    public LegacySpriteSheetDataTests()
    {
        _stubDecompression = new StubLzwDecompression();
        var imageParser = new SharedImageParser(NullLogger<SharedImageParser>.Instance, _stubDecompression);
        _parser = new LegacySimpleImageParser(NullLogger<LegacySimpleImageParser>.Instance, imageParser);
        _tempDir = Path.Combine(Path.GetTempPath(), $"LegacySpriteSheetDataTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    #region Sprite sheet data per key

    public static IEnumerable<object[]> SpriteSheetKeys()
    {
        return LegacySpriteSheetData.ExpectedSpriteSheets.Keys.Select(k => new object[] { k });
    }

    [Theory]
    [MemberData(nameof(SpriteSheetKeys))]
    public void Parse_KnownKey_ReturnsSpriteSheetMatchingExpectedData(string key)
    {
        var expected = LegacySpriteSheetData.ExpectedSpriteSheets[key];
        var maxWidth = expected.Sprites.Values.Max(s => s.X + s.Width);
        var maxHeight = expected.Sprites.Values.Max(s => s.Y + s.Height);
        var width = (ushort)Math.Max(maxWidth, 4);
        var height = (ushort)Math.Max(maxHeight, 4);

        var pixels = SharedImageTestDataGenerator.GenerateUniformPixels(width, height);
        _stubDecompression.SetResult(pixels);
        var data = SimpleImageParserTestDataGenerator.BuildPicFile(width, height);
        SimpleImageParserTestDataGenerator.WritePicFile(_tempDir, key, data);

        var images = RunParser();

        Assert.True(images.ContainsKey(key), $"Parser did not produce key '{key}'");
        Assert.NotNull(images[key].SpriteSheet);
        var spriteSheet = images[key].SpriteSheet!;
        Assert.Equal(expected.Sprites.Count, spriteSheet.Sprites.Count);
        foreach (var (spriteName, expectedSprite) in expected.Sprites)
        {
            Assert.True(spriteSheet.Sprites.ContainsKey(spriteName),
                $"Missing sprite '{spriteName}' in {key}");
            var actual = spriteSheet.Sprites[spriteName];
            Assert.Equal(expectedSprite.X, actual.X);
            Assert.Equal(expectedSprite.Y, actual.Y);
            Assert.Equal(expectedSprite.Width, actual.Width);
            Assert.Equal(expectedSprite.Height, actual.Height);
        }
    }

    #endregion

    #region Helpers

    private Dictionary<string, SimpleImageModel> RunParser()
    {
        var model = new PackageModel();
        _parser.Start(_tempDir);
        while (!_parser.RunStep()) { }
        _parser.SetResult(model);
        return model.SimpleImages;
    }

    #endregion
}
