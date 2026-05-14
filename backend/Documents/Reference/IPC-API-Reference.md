# IPC API Reference (JSON-RPC 2.0)

SlideGenerator communicates with the frontend sidecar via JSON-RPC 2.0 over standard I/O.

## Transport Configuration
- **Input (stdin)**: Incoming JSON-RPC requests.
- **Output (stdout)**: Outgoing responses and notifications (NDJSON).
- **Error (stderr)**: System logs (text/structured).

---

## Methods

### `workflow.start`
Starts a new slide generation process.
- **Params**: `GeneratingRequest` DTO.
- **Returns**: `string` (Workflow Instance ID).

### `workflow.cancel`
Terminates a running workflow.
- **Params**: `workflowInstanceId` (string).
- **Returns**: `bool` (Success).

### `scanning.scanWorkbook`
Extracts metadata from an Excel file.
- **Params**: `BookSummaryRequest` DTO.
- **Returns**: `WorkbookSummary`.

### `settings.get`
Retrieves current application settings.
- **Returns**: `SlideGeneratorSettings`.

---

## Notifications (Server → Client)

### `workflow/progress`
Pushed periodically during workflow execution.
- **Payload**:
  ```json
  {
    "workflowInstanceId": "...",
    "event": "StepCompleted",
    "phase": "PhaseB",
    "status": "Running",
    "timestamp": "..."
  }
  ```

## Serialization Rules
- **Naming**: `camelCase` for all properties.
- **Enums**: Serialized as **strings** (e.g., `"Center"`, `"RuleOfThirds"`).
- **Polymorphism**: Some DTOs (like `RoiOption`) use a `"type"` discriminator.
