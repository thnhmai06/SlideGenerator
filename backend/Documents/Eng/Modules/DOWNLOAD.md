# Module: Download Service

## The Hook (Q&A)

**Q: How do we handle unreliable internet connections?**  
The `DownloadService` implements **Retry Policies** and utilizes the `Network` gate from the Coordinator to prevent overwhelming the connection. It works hand-in-hand with the Cloud Resolvers to ensure the most reliable direct URL is used.

---

## 1. Key Features

- **Atomic Downloads**: Files are downloaded to a temporary location first to prevent partial/corrupted files from being used in the pipeline.
- **Stream-based**: Efficiently pipes data to disk to maintain a low memory footprint even for large image files.

---

## 2. Integration

Integrated directly into the `DownloadImage` step of the generation workflow. It relies on a shared `HttpClient` instance for optimal connection pooling.