# Unit Tests

Unit tests mock dependencies to test classes in isolation. Use hand-crafted byte arrays for inputs.

## Snapshot Baselines

Snapshot data is stored as base64 constants in `*SnapshotData.cs` files per domain area (e.g. `Compression/LzwSnapshotData.cs`). These are captured from known-correct output and used for regression detection.

To regenerate LZW baselines: temporarily add a generator test that compresses test data and prints the base64, then copy the values into `LzwSnapshotData.cs`.
