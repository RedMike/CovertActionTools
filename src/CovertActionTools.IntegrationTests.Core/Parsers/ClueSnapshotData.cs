namespace CovertActionTools.IntegrationTests.Core.Parsers;

/// <summary>
/// Base64-encoded CLUES.TXT data captured from the current (known-correct) test data generators.
/// Used by snapshot tests to detect regressions in the parsing logic.
///
/// To regenerate: run ClueSnapshotGenerator and copy the output.
/// </summary>
public static class ClueSnapshotData
{
    public const string MixedClues = "KkMzMg0KMQ0KQWlybGluZSB0aWNrZXQgdG8gUGFyaXMNCipDMDEwNQ0KMjcNCklkZW50aXR5IGRvY3VtZW50IGZvdW5kDQoqDQoaAAA=";
}
