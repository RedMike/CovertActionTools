namespace CovertActionTools.IntegrationTests.Core.Parsers;

/// <summary>
/// Base64-encoded PIC file data captured from the current (known-correct) implementation.
/// Used by snapshot tests to detect regressions in the SimpleImage parser pipeline.
///
/// To regenerate: run SimpleImageParserSnapshotGenerator and copy the output.
/// </summary>
public static class SimpleImageParserSnapshotData
{
    public const string PicFile_4x4_Uniform = "BwAEAAQAC1UgIQA=";
    public const string PicFile_4x4_Varied = "BwAEAAQACyoECVkkbAgWIw==";
    public const string PicFile_16x8_Varied =
        "BwAQAAgACyoECVkkbAgWIwg6mYjVCJa4RQIoNUC2RgAwDcNICUgEZtWAIrHiADt0oAEEBe7OJChhDA25XOC+rIhTTMQkfKyW4FNQDgaBSQ==";
}
