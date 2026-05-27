# System Overview: Modular Monolith & IPC Sidecar

This document explains the high-level architecture of SlideGenerator, focusing on why certain design patterns were chosen and how they interact.

## The Modular Monolith

SlideGenerator is built as a **Modular Monolith**. While it is deployed as a single unit, the internal structure is strictly divided into independent modules.

### Why Modular Monolith?
- **Isolation**: Each module has its own responsibility (e.g., Image processing, Presentation editing).
- **Maintainability**: Clear boundaries prevent the "big ball of mud" where changes in one area unexpectedly break another.
- **Dependency Flow**: Dependencies are strictly hierarchical. Foundation modules (`Utilities`, `Settings`, `Cloud`) have zero or minimal dependencies, while Orchestration modules (`Generator`) tie everything together.

### Module Categories
1. **Foundation** (`Utilities`, `Settings`, `Cloud`): Zero external module dependencies. Core infrastructure and provider integrations.
2. **Core Services** (`Cryptography`, `Coordinator`, `Document`, `Logging`): Depend on Foundation. Provide shared capabilities such as concurrency throttling and Syncfusion abstractions.
3. **Feature Modules** (`Image`): Domain-specific logic for MagickImage processing and ROI/face detection.
4. **Orchestration** (`Summarization`, `Recipe`, `Generator`): The glue that scans inputs, persists user recipes, and runs the WorkflowCore pipeline.
5. **Entry Point** (`Ipc`): Executable that wires every module and exposes them through JSON-RPC 2.0.

---

## IPC Sidecar Pattern

The backend is designed to run as an **IPC Sidecar** for a frontend application (typically built with Tauri).

### Communication: JSON-RPC 2.0
- **Protocol**: Standardized JSON-RPC 2.0.
- **Transport**: `StreamJsonRpc` over `stdin` (incoming) and `stdout` (outgoing).
- **Framing**: New-line delimited JSON (NDJSON).
- **Logs**: All system logs are directed to `stderr` to avoid interfering with the RPC stream on `stdout`.

### Benefits
- **Language Agnostic**: The frontend can be written in any language (Rust/TypeScript) as long as it can speak JSON-RPC over stdio.
- **Performance**: Zero-latency local communication without the overhead of HTTP/TCP stacks.

---

## Execution Model

The system uses **WorkflowCore** to manage long-running, stateful processes.

1. **Request**: The frontend sends a `generator.active.start` request via IPC.
2. **Persistence**: The workflow state is persisted to a SQLite database. If the process crashes or is paused, it can resume from the last successful step.
3. **Observation**: A `WorkflowProgressObserver` listens to events and pushes notifications back to the frontend in real-time.
