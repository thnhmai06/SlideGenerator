# Cloud Module

The **SlideGenerator.Cloud** module merges multi-cloud URI resolution with HTTP-based asset acquisition. It replaces the previous separate `Resolver` and `Download` modules.

## Responsibility
- Converting user-facing cloud share URLs into directly-downloadable URIs.
- Performing HEAD inspection (`ICloudClient.InspectAsync`) and file downloads (`ICloudClient.DownloadAsync`).
- Following redirects and applying provider-specific transformations.

## Key Abstractions
- **`ICloudResolver`**: Detects the cloud provider for a URI and routes it through the matching `CloudResolveModule`.
- **`ICloudClient`**: HTTP layer for inspect/download operations; transparently follows redirects.
- **`CloudResolveModule`**: Strategy base for per-provider logic (currently `GoogleDriveModule`).

## Supported Providers
- **Google Drive** (`CloudHost.GoogleDrive`): Resolves `/file/d/...` share URLs to direct download URIs.

OneDrive, SharePoint, and other providers from older drafts are **not yet implemented** in the current code — only Google Drive is wired.

## Domain Model
- **`CloudHost`** enum: Identifies the provider associated with a URI.
- **`ContentInfo`** record: Result of `InspectAsync` — resolved URI, content-type, content-length.

## Integration
The `CollectImage` step in the Generator uses `ICloudClient` to fetch each image into the workflow's temp folder, gated by `GateType.DownloadImage`.
