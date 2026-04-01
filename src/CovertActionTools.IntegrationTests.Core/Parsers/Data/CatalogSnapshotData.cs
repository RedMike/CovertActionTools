namespace CovertActionTools.IntegrationTests.Core.Parsers.Data;

/// <summary>
/// Base64-encoded CAT file data captured from the current (known-correct) implementation.
/// Used by snapshot tests to detect regressions in the Catalog parser pipeline.
///
/// To regenerate: run CatalogSnapshotGenerator and copy the output.
/// </summary>
public static class CatalogSnapshotData
{
    public const string CatFile_SingleEntry_Uniform =
        "AQBJTUcxLlBJQwAAAAAAAAAACwAAABoAAAAHAAQABAALVSAhAA==";
    public const string CatFile_TwoEntries_Mixed =
        "AgBBLlBJQwAAAAAAAAAAAAAACwAAADIAAABCLlBJQwAAAAAAAAAAAAAAEAAAAD0AAAAHAAQABAALVSAhAAcABAAEAAsqBAlZJGwIFiM=";
}
