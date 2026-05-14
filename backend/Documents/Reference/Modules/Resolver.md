# Resolver Module

The **SlideGenerator.Resolver** module handles multi-cloud URI resolution.

## Responsibility
- Converting user-friendly cloud URLs into direct download streams.
- Handling authentication and redirects for major providers.

## Supported Providers
- **Google Drive**: Resolves `/file/d/...` links.
- **OneDrive / SharePoint**: Handles complex auth flows and direct download links.
- **Google Photos**: (In development) Support for library-based image fetching.

## Architecture
Uses the **Strategy Pattern**. Each provider implements a specific resolver, and the `CloudResolver` orchestrates them by matching the URI scheme or domain.
