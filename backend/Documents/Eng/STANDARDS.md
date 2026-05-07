# Tech Stack & Coding Standards

## Tech Stack

| Technology | Component | Rationale / Responsibility |
|---|---|---|
| **.NET 10** | Core SDK | Ensures maximum performance and modern language features (pinned via `global.json`). |
| **StreamJsonRpc** | IPC Layer | Provides blazingly fast, robust JSON-RPC 2.0 communication over `stdin/stdout`. |
| **WorkflowCore** | Orchestration | Resilient step-by-step execution, phase separation, and item-parallel operations. |
| **Syncfusion** | Documents (.xlsx, .pptx) | Fast, server-safe manipulation of Office files without needing MS Office installed. |
| **Magick.NET** | Image Processing | `MagickImage` is the primary type for fast cropping, resizing, and composition. |
| **OpenCVSharp** | Computer Vision | (YuNet Model) Face detection for intelligent ROI (Region of Interest) calculation. |
| **Serilog & EF Core**| Logging & DB | Comprehensive asynchronous logging to rolling files and entity framework databases. |

## Coding Standards

The project strictly adheres to specific engineering constraints to maintain a clean monolith:

- **Module Independence:** No circular dependencies. Modules must be registered entirely via `Registration.cs` within their own scope.
- **Data & Service Types:**
  - Use `record` or `sealed record` for Data Transfer Objects (DTOs) and Value Objects.
  - Use `sealed class` with constructor injection for Services.
- **Async Patterns:** Always utilize `ConfigureAwait(false)` in module logic and continuously propagate `CancellationToken`.
- **Image Handling:** `MagickImage` is strictly the primary type for manipulation.
- **Error Resilience:** Do not halt the batch process upon individual task failures. Errors are captured using `ConcurrentDictionary<string, Exception> Errors`.
- **XML Documentation:** All public APIs must be thoroughly documented using XML tags.
