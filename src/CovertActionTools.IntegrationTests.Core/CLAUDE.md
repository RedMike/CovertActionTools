# Integration Tests

Integration tests use real dependencies (no mocks) to test the full pipeline.

## Folder Structure

Each domain area folder (e.g. `Parsers/`) contains:
- Test files directly in the area folder
- `Data/` subfolder — test data generators (`*TestDataGenerator.cs`), snapshot data (`*SnapshotData.cs`), and snapshot generators (`*SnapshotGenerator.cs`)
- `Stubs/` subfolder — stub implementations (e.g. `StubLzwCompression.cs`)

## Snapshot Baselines

Snapshot data is stored as base64 constants in `*SnapshotData.cs` files in the `Data/` subfolder per domain area. To regenerate baselines after an intentional format change:

1. Run the corresponding `*SnapshotGenerator` test (e.g. `dotnet test --filter GenerateSnapshots --logger "console;verbosity=detailed"`)
2. The generator outputs new values via `ITestOutputHelper` — check the test runner output
3. Copy the values into the matching `*SnapshotData.cs` file in the `Data/` subfolder

Keep snapshot images small (e.g. 4x4, 512x2) so the base64 constants stay manageable.
