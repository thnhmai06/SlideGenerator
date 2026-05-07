# Module: Cloud Resolvers

## The Hook (Q&A)

**Q: Why don't we just download directly from the link?**  
Most cloud sharing links (Google Drive, OneDrive) point to a "Preview" page, not the raw file. The `MultiCloudResolver` detects the provider and converts the user-facing link into a direct binary stream URL using specialized patterns and API tricks.

**Q: Which providers are supported?**  
Currently, the system supports **Google Drive**, **Google Photos**, **OneDrive**, and **SharePoint**. If a link is not recognized, it is treated as a standard direct URL.

---

## 1. Supported Resolvers

- **Google Drive**: Converts `/file/d/{id}/view` into direct download endpoints.
- **Google Photos**: Parses album or direct photo links into high-quality source URLs.
- **OneDrive/SharePoint**: Handles personal and business sharing links to extract the underlying content ID.

---

## 2. Integration

The `MultiCloudResolver` acts as a facade. It is called during the `DownloadImage` step of the pipeline.

```csharp
// Example usage
var directUri = await multiCloudResolver.ResolveUriAsync(userUri, httpClient);
```