namespace CovertActionTools.IntegrationTests.Core.Parsers.Data;

/// <summary>
/// Base64-encoded PROSE.DTA data captured from the current (known-correct) test data generators.
/// Used by snapshot tests to detect regressions in the parsing logic.
///
/// To regenerate: run ProseSnapshotGenerator and copy the output.
/// </summary>
public static class ProseSnapshotData
{
    public const string MixedProse = "KmFkdmljZTEKDQpCZSBjYXJlZnVsIG91dCB0aGVyZQ0KKmxvdW5nZQoNCllvdSBlbnRlciB0aGUgbG91bmdlDQoqc3VycHJpc2VMCg0KWW91IGxvc3QgdGhlIGFtYnVzaA0KKmVuZAo=";
}
