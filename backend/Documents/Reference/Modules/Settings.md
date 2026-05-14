# Settings Module

The **SlideGenerator.Settings** module handles application-wide configuration.

## Responsibility
- Persisting user preferences to YAML.
- Providing reactive updates to other modules via `ISettingProvider`.

## Persistence
- **Format**: YAML (via YamlDotNet).
- **Location**: `%LOCALAPPDATA%/SlideGenerator/settings.yaml`.
- **Reset**: Includes a default configuration provider to restore factory settings.

## Configuration Categories
- **Performance**: Thread counts, buffer sizes, and gate limits.
- **Cloud**: API keys and proxy settings (passwords are encrypted via the Cryptography module).
- **Logging**: Minimum log levels and retention policies.
- **App**: Visual preferences and default export paths.
