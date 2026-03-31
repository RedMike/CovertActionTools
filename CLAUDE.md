# Core Rules
- NEVER commit OR push without explicit user permission
- Ask clarifying questions BEFORE making a plan
- ALWAYS make a plan before starting work, and wait for explicit user confirmation

# Branches
- `main` — latest released version
- `develop` — next version in progress; feature branches merge here

# Build
- Solution file: `src/CovertActionTools.sln`
- Build: `dotnet build` or `dotnet publish` from `src/`
- Core targets netstandard2.0 — do not use C# features unavailable in netstandard2.0 when editing Core
- App targets net8.0
- Tests: `dotnet test` from `src/` runs `CovertActionTools.UnitTests.Core` (xUnit, targets net8.0)

# Architecture
- Two projects: `CovertActionTools.Core` (library, all data/parsing logic, no GUI dependency) and `CovertActionTools.App` (ImGui + Veldrid desktop GUI, depends on Core)
- Data pipeline pattern: Legacy binary files → `LegacyParser` → `Importer` (JSON package) → `Exporter` (JSON) → `Publisher` (legacy binary). Each data type (Crime, Animation, Text, Image, etc.) has one of each
- All Core services are registered in `Core/ServiceCollectionExtension.cs`. All App ViewModels/Windows are auto-discovered in `App/Program.cs`. New importers/exporters/publishers must be added to `ServiceCollectionExtension`
- Every model implements `Clone()` — when adding new fields to a model, always add them to `Clone()` too

# Domain
- VGA 16-color palette is defined in `Core/Constants.cs`. Color indices 13 (player clothing) and 14 (enemy clothing) are special — replaced dynamically by the game engine at draw time
- Many models contain `Unknown*` fields (e.g. `Unknown1`, `Unknown2`). These are partially reverse-engineered from the binary format. Do not rename, remove, or repurpose them
