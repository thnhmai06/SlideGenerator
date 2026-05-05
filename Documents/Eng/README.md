# SlideGenerator Backend

## The Hook (Q&A)

**Q: What problem does this project solve?**  
SlideGenerator automates the mass production of PowerPoint presentations by mapping Excel data directly into PowerPoint templates. It eliminates the manual, repetitive task of copying text and resizing images, turning hours of work into seconds.

**Q: Why is this lean architecture optimal for the project?**  
We use a JSON-RPC 2.0 .NET sidecar architecture that communicates over `stdin/stdout`. Instead of over-engineering with complex repository layers or heavy REST APIs, the system directly maps IPC requests to `WorkflowCore` pipelines. This pragmatism ensures high performance, minimal memory overhead, and easy integration with modern frontend frameworks (like Tauri or Electron).

**Q: How does the Data Flow actually work?**  
The frontend sends a JSON-RPC request via standard input. The IPC handler deserializes it and triggers a `WorkflowCore` pipeline. The pipeline systematically validates files, downloads/edits images, and assembles the slides. Progress is piped back via standard output continuously.

---

## 1. Project Overview

SlideGenerator is a highly optimized .NET console application operating as an IPC sidecar. It receives instructions to read `.xlsx` files, download assets, and generate `.pptx` files efficiently.

### Key Capabilities:
- **Direct IPC Communication**: Uses StreamJsonRpc for blazingly fast, reliable messaging.
- **Workflow Orchestration**: Powered by WorkflowCore for resilient step-by-step execution.
- **Smart Idempotency**: "Do it once, don't do it again." Reusing downloaded and cropped images efficiently.
- **Concurrency Control**: Uses a robust GateLocker to prevent OS file locks and CPU throttling.

---

## 2. Solution Structure

The solution is divided into tightly scoped, pragmatic modules to favor direct execution over deep abstraction.

- **`SlideGenerator.Ipc`**: The entry point. Handles `stdin/stdout` and JSON-RPC dispatching.
- **`SlideGenerator.Pipelines`**: The core engine. Defines the phases and steps using WorkflowCore.
- **`SlideGenerator.Coordinator`**: Manages concurrency and file locking (`GateLocker`).
- **`SlideGenerator.Documents`**: Wraps external libraries (like Syncfusion) for Excel and PowerPoint manipulation.
- **`SlideGenerator.Images`**: Handles image cropping, resizing, and smart ROI (Region of Interest) detection.
- **`SlideGenerator.Cloud`**: Resolves sharing links (Google Drive, OneDrive) into direct download links.

---

## 3. Getting Started

For local development, simply restore dependencies and build the solution using the standard .NET 8 CLI.

```bash
dotnet restore
dotnet build SlideGenerator.sln
```

See `CONTRIBUTING.md` for coding standards and `ARCHITECTURE.md` for system diagrams.