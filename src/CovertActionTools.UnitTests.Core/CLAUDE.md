# Unit Tests

Unit tests mock dependencies to test classes in isolation. Use hand-crafted byte arrays for inputs.

## Folder Structure

Each domain area folder (e.g. `Compression/`, `Parsers/`) contains:
- Test files directly in the area folder
- `Data/` subfolder — test data generators (`*TestDataGenerator.cs`) and snapshot data (`*SnapshotData.cs`)
- `Stubs/` subfolder — stub/mock implementations (e.g. `StubLzwCompression.cs`)

## Snapshot Baselines

Snapshot data is stored as base64 constants in `*SnapshotData.cs` files in the `Data/` subfolder per domain area (e.g. `Compression/Data/LzwSnapshotData.cs`). These are captured from known-correct output and used for regression detection.

To regenerate LZW baselines: temporarily add a generator test that compresses test data and prints the base64, then copy the values into `LzwSnapshotData.cs`.
