# Integration Tests

Integration tests use real dependencies (no mocks) to test the full pipeline.

## Snapshot Baselines

Snapshot data is stored as base64 constants in `*SnapshotData.cs` files per domain area. To regenerate baselines after an intentional format change:

1. Run the corresponding `*SnapshotGenerator` test (e.g. `dotnet test --filter GenerateSnapshots`)
2. The generator writes new values to `/tmp/<Area>Snapshots.txt`
3. Copy the values into the matching `*SnapshotData.cs` file

Keep snapshot images small (e.g. 4x4, 512x2) so the base64 constants stay manageable.
