using System;
using Xunit;
using Xunit.Abstractions;

namespace CovertActionTools.IntegrationTests.Core.Parsers;

/// <summary>
/// Generates base64-encoded CAT file data for snapshot tests.
/// Run this test to capture new baselines when the format changes intentionally.
/// </summary>
public class CatalogSnapshotGenerator
{
    private readonly ITestOutputHelper _output;

    public CatalogSnapshotGenerator(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void GenerateSnapshots()
    {
        var result1 = Convert.ToBase64String(
            CatalogIntegrationTestDataGenerator.BuildCatalogFile(new[]
            {
                new CatalogIntegrationTestDataGenerator.CatalogEntry
                {
                    Name = "IMG1",
                    Width = 4,
                    Height = 4,
                    Pixels = SharedImageTestDataGenerator.GenerateUniformPixels(4, 4)
                }
            }));

        var result2 = Convert.ToBase64String(
            CatalogIntegrationTestDataGenerator.BuildCatalogFile(new[]
            {
                new CatalogIntegrationTestDataGenerator.CatalogEntry
                {
                    Name = "A",
                    Width = 4,
                    Height = 4,
                    Pixels = SharedImageTestDataGenerator.GenerateUniformPixels(4, 4)
                },
                new CatalogIntegrationTestDataGenerator.CatalogEntry
                {
                    Name = "B",
                    Width = 4,
                    Height = 4,
                    Pixels = SharedImageTestDataGenerator.GenerateVariedPixels(4, 4)
                }
            }));

        _output.WriteLine($"CatFile_SingleEntry_Uniform = \"{result1}\";");
        _output.WriteLine($"CatFile_TwoEntries_Mixed = \"{result2}\";");
    }
}
