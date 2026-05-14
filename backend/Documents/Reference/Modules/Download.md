# Download Module

The **SlideGenerator.Download** module handles all outgoing HTTP requests for assets.

## Responsibility
- Downloading images and templates from the web.
- Handling retries and timeouts.
- Providing progress reporting for large files.

## Integration
The `DownloadService` is typically used within the `DownloadImage` workflow step, working in tandem with the `Coordinator` to respect global download limits.
