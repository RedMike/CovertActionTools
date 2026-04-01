namespace CovertActionTools.IntegrationTests.Core.Parsers.Data;

/// <summary>
/// Base64-encoded world file data captured from the current (known-correct) implementation.
/// Used by snapshot tests to detect regressions in the world parsing format.
///
/// To regenerate: run WorldSnapshotGenerator and copy the output.
/// </summary>
public static class WorldSnapshotData
{
    public const string StandardWorldFile =
        "AwACAExvbmRvbgAAAAAAAEVuZ2xhbmQAAAAAAAABAAIAAAAAgC1QYXJpcwAAAAAAAABGcmFuY2UAAAAA"
        + "AAAAAAAAAAAAAIQyQmVybGluAAAAAAAAR2VybWFueQAAAAAAAAMABAAAAACMME1JNgAAAFNlY3JldCBJ"
        + "bnRlbCBTdmMAAAAAAAEAAAAACgAAAEtHQgAAAENvbW1pdHRlZSBTdGF0ZQAAAAAAAAAABQAAFAAAAA==";
}
