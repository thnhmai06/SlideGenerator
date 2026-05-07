# Architecture & System Design

The project utilizes a **Modular Monolith** architecture operating as an **IPC Sidecar**. Instead of over-engineering with complex repository layers or heavy REST APIs, the system communicates directly with a Frontend (e.g., Tauri or Electron) using standard I/O (`stdin/stdout`).

## 1. Design Philosophy (The Hook Q&A)

**Q: How do components interact from Request to Response?**  
A client writes a JSON-RPC payload to `stdin`. `SlideGenerator.Ipc` captures it, routes it to the specific handler (e.g., `WorkflowHandler.StartAsync`), and invokes `WorkflowCore`. The workflow performs the requested task in isolated steps, reporting progress via `WorkflowEventBus`, which pipes data back to `stdout` immediately.

**Q: Why avoid deep abstract layers?**  
Pragmatism. We don't need generic Repositories or CQRS for a sidecar that performs tightly defined file manipulations. Direct injection of scoped Services (like `ScanningService` or `ImageComposer`) reduces boilerplate, makes debugging trivial, and avoids the "spaghetti abstraction" trap.

---

## 2. System Interaction Flow (Data Flow)

The following diagram illustrates the exact practical data flow:

```mermaid
sequenceDiagram
    participant Frontend as Frontend (Tauri/Electron)
    participant IPC as Ipc (Program.cs / StreamJsonRpc)
    participant Handler as Handlers (Workflow/Scanning)
    participant Engine as WorkflowCore (Pipelines)
    participant Coordinator as Coordinator (GateLocker)
    
    Frontend->>IPC: Write JSON-RPC to stdin (e.g., workflow.start)
    IPC->>Handler: Route request & deserialize
    Handler->>Engine: Start WorkflowInstance (GeneratingTask)
    Handler-->>IPC: Return Workflow ID
    IPC-->>Frontend: Write response to stdout
    
    loop Workflow Steps
        Engine->>Coordinator: Request resource lock (Network/Excel)
        Coordinator-->>Engine: Lock Acquired
        Engine->>Engine: Execute Step (Download/Edit/Replace)
        Engine->>IPC: Publish Progress Event (EventBus)
        IPC-->>Frontend: Write Event to stdout
        Engine->>Coordinator: Release resource lock
    end
```

## 3. Module Hierarchy

The system is divided into isolated modules with a strict top-down dependency flow:

1. **Foundation Modules:** No external dependencies.
   - `SlideGenerator.Settings`: YAML-based configuration management.
   - `SlideGenerator.Cloud`: Multi-cloud direct link resolver.
2. **Core Services:** Depend on Settings; shared capabilities.
   - `SlideGenerator.Coordinator`: Concurrency throttling (`GateLocker`).
   - `SlideGenerator.Download`: HTTP downloading with throttling and progress tracking.
   - `SlideGenerator.Documents`: Excel/PowerPoint manipulation via Syncfusion.
3. **Feature Modules:** Specific domain operations.
   - `SlideGenerator.Images`: `MagickImage` processing, ROI & face detection (OpenCV YuNet).
   - `SlideGenerator.Logging`: System logging via Serilog (Async rolling file & DB).
4. **Orchestration & Entry Point:**
   - `SlideGenerator.Pipelines`: WorkflowCore orchestration for generation and scanning.
   - `SlideGenerator.Ipc`: Entry point handling JSON-RPC 2.0 dispatching.

## 4. Dependency Injection Strategy

Services are registered via local `Registration.cs` files in each module (e.g., `services.AddGeneratingServices()`). We strictly use `Transient` for stateless workers and `Singleton` for caches/locks (like `GateLocker`).

## 5. Error Handling & Stability

- Unhandled exceptions are globally caught in `AppDomain.CurrentDomain.UnhandledException` to log fatal errors before crashing.
- Workflow steps handle domain-specific errors (e.g., a broken image link skips the row rather than crashing the batch).
