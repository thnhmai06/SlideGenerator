# System Overview: Modular Monolith & IPC Sidecar

This document explains the high-level architecture of SlideGenerator, focusing on why certain design patterns were chosen and how they interact.

## The Modular Monolith

SlideGenerator is built as a **Modular Monolith**. While it is deployed as a single unit, the internal structure is strictly divided into independent modules.

### Why Modular Monolith?
- **Isolation**: Each module has its own responsibility (e.g., Image processing, Presentation editing).
- **Maintainability**: Clear boundaries prevent the "big ball of mud" where changes in one area unexpectedly break another.
- **Dependency Flow**: Dependencies are strictly hierarchical. Foundation modules (Settings, Resolver) have zero dependencies, while Orchestration modules (Generating) tie everything together.

### Module Categories
1. **Foundation**: Zero external dependencies. Core infrastructure.
2. **Core Services**: Depend on Foundation. Provide shared capabilities like Download and Cryptography.
3. **Feature Modules**: Domain-specific logic for MagickImage or Syncfusion abstractions.
4. **Orchestration**: The glue that coordinates workflows.

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

1. **Request**: The frontend sends a `workflow.start` request via IPC.
2. **Persistence**: The workflow state is persisted to a SQLite database. If the process crashes or is paused, it can resume from the last successful step.
3. **Observation**: A `WorkflowProgressObserver` listens to events and pushes notifications back to the frontend in real-time.
