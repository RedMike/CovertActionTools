namespace CovertActionTools.IntegrationTests.Core.Parsers;

/// <summary>
/// Base64-encoded crime file data captured from the current (known-correct) implementation.
/// Used by snapshot tests to detect regressions in the crime parsing format.
///
/// To regenerate: run CrimeSnapshotGenerator and copy the output.
/// </summary>
public static class CrimeSnapshotData
{
    public const string StandardCrimeFile =
        "AgADAP//MgBNYXN0ZXJtaW5kAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAEAAAEAAAIDAAAGAP//UABDb3Vy"
        + "aWVyAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAEASAAAAAUBAAAGAAAAAAABAEluaXRpYXRlZCB0aGUg"
        + "cGxhbgAAAAAAAAAAAAAAAAAAACAAAGQAAAAAAAUAU2VudCBvcmRlcnMAAAAAAAAAAAAAAAAAAAAAAAAA"
        + "AAABAgAAAAABAAAABQBSZWNlaXZlZCBvcmRlcnMAAAAAAAAAAAAAAAAAAAAAAAADAAAAAFNlY3JldCBQ"
        + "bGFucwAAAAAD/1JhbnNvbSBNb25leQAAAAAH/wAAAAAAAAAAAAAAAAAAAAD//wAAAAAAAAAAAAAAAAAAAA"
        + "D//w==";
}
