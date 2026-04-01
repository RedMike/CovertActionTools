namespace CovertActionTools.IntegrationTests.Core.Parsers.Data;

/// <summary>
/// Base64-encoded TEXT.DTA data captured from the current (known-correct) test data generators.
/// Used by snapshot tests to detect regressions in the parsing logic.
///
/// To regenerate: run TextSnapshotGenerator and copy the output.
/// </summary>
public static class TextSnapshotData
{
    public const string MixedTexts = "Kk1TRzAyMDENClRoZSBjcmltZSBtZXNzYWdlIGJvZHkNCipTT1JHMDANClNlbmRlciBvcmcgbmFtZQ0KKkZMVUYwMw0KU29tZSBmbHVmZiB0ZXh0DQoqRU5EAA==";
}
