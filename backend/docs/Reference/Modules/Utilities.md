# Utilities Module

The **SlideGenerator.Utilities** module contains shared low-level helpers used across all other projects.

## Responsibility

- String / URI / path normalization.
- Cross-platform hard-link creation for zero-copy asset reuse.
- SQLite connection factory shared by Recipe and WorkflowCore persistence.

## Key Files

- **`Helper/Normalization.cs`**: Cleans up URIs and file paths.
- **`Helper/HardLink.cs`**: Cross-platform wrapper for creating file-system hard links. Used by the Generator's asset
  deduplication path.
- **`Database/SqliteConnectionFactory.cs`**: Centralized factory that returns Microsoft.Data.Sqlite connections for
  modules that need a local store (Recipe, WorkflowCore).

## Notes

This module replaces the former `SlideGenerator.Common` module. `NameAndPaths` (centralized app-directory resolution)
lives in `SlideGenerator.Settings/Domain/Rules/NameAndPaths.cs`, not here.
