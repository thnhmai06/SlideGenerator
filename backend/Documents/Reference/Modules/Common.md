# Common Module

The **SlideGenerator.Common** module contains shared utilities used across all other projects.

## Responsibility
- String normalization.
- Path manipulation.
- Shared constants and global enums.

## Key Utilities
- **`Normalization.cs`**: Cleans up URIs and file paths.
- **`NameAndPaths.cs`**: Centralized logic for resolving app directories (Logs, Temp, Workflows) across different OS platforms.
- **`HardLink.cs`**: Cross-platform wrapper for creating file-system hard links, enabling zero-copy asset reuse.
