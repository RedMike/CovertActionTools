namespace CovertActionTools.IntegrationTests.Core.Parsers.Data;

/// <summary>
/// Base64-encoded exported image data captured from the current (known-correct) implementation.
/// Used by snapshot tests to detect regressions in the export binary format.
///
/// To regenerate: run SharedImageSnapshotGenerator and copy the output.
/// </summary>
public static class SharedImageSnapshotData
{
    public const string Exported_4x4_Uniform_Format0x07 = "BwAEAAQAC1UgIQA=";
    public const string Exported_4x4_Varied_Format0x07 = "BwAEAAQACyoECVkkbAgWIw==";
    public const string Exported_512x2_Varied_Format0x07 =
        "BwAAAgIACyoECVkkbAgWIwg6mYjVCJa4RQIoNUC2RgAwDcNICUgEZtWAIrHiADt0oAEEBe7OJChh"
        + "DA25XOC+rIhTTMQkfKyW4FNQDgaBSRY+RCnWAJoPb4RUDREw7pk4IYg2pPFT79AzCSB+WbLgz0Wh"
        + "fL2oIBung0arMOz4/fEnaRSuYIV2IMvhAEYJIn7qsPrSRF4me3GcuHNACZwpffQYlAKFpUaqLBqk"
        + "0SuQRYSGFzpUdUljgxEqU7+GYNuS7B42MCbSwDtyy8Kwc4/mudKGJYIrT2KkBFAwrpslYYQAaECG"
        + "y5UXa9yeaWDXYh+3c0O6pHPQpU08cfgAaZGxQY6lX/QuISDn7suYbtCusSMnKQ8fJSUiHKqjQlm1"
        + "L0QMhXCz6Zy1OKN8QIsh94hCBxZKyAGBDLOUAwc0IKTSDSQAeBPBE0GEMIIqFuDiSR0IPJGBN8dM"
        + "kgIA42TAzjhj7CHCH2MUIMsCNezhABBw3POEJh2EYMYWzZjQQCOTbDLLC3XcogA+u0yQiD5YpKCOA"
        + "fmwgYEF4bDzjzZzWMCMK89AEwweYMQygikUwLLLI8u4cMU0EBhD4RNFNOJPBsDwsoMPjATRBShDqL"
        + "BKLrGU0Ycu04iSSDQiPHLKPTRQQoYZEiTQiBFdBDBFLNI8ssEcVIgiwDXGIOLAK6yokAY7kdhhjQY"
        + "t8FGLMD7k0QgLXSxgQTc5IKKAJXEoQwcF6EzBRCBMZBCKMvS4MIk4DVjhgBugINIHEPFsYcUZ7By"
        + "RgjtcIOOLMjbAEcI3nsDQzB34IIBJO5zs8YQZ9fxQACmu2CNKAA2MI8owFgA=";
    public const string Exported_4x4_Uniform_Format0x0F_Identity = "DwAEAAQAABEiMwARIjMAESIzABEiMwtVICEA";
    public const string Exported_4x4_Varied_Format0x0F_Offset3 = "DwAEAAQAMwARIjMAESIzABEiMwARIgsqBAlZJGwIFiM=";
}
