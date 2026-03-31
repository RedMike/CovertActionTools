namespace CovertActionTools.IntegrationTests.Core.Parsers;

/// <summary>
/// Base64-encoded PLOT.TXT data captured from the current (known-correct) test data generators.
/// Used by snapshot tests to detect regressions in the parsing logic.
///
/// To regenerate: run PlotSnapshotGenerator and copy the output.
/// </summary>
public static class PlotSnapshotData
{
    public const string MixedPlots = "KlBMMDM5MA0KQnJpZWZpbmcgZm9yIG1pc3Npb24NCipQTDAzMDINCllvdSBzdWNjZWVkZWQNCipQTDAzMTcNCllvdSBmYWlsZWQNCipQTDAzMkENClByZXZpb3VzIGZhaWx1cmUNCho=";
}
