# Module: IPC (JSON-RPC 2.0)

## The Hook (Q&A)

**Q: How does the sidecar communicate?**  
The application uses **JSON-RPC 2.0** over standard input (`stdin`) and standard output (`stdout`). This allows any frontend (Tauri, Electron, Python) to control the .NET backend as a decoupled process without the overhead of HTTP/TCP sockets.

**Q: How is progress reported back?**  
We use a "Push" model. The backend doesn't wait for the frontend to poll; instead, it sends unsolicited JSON-RPC notifications (`workflow/progress`) to `stdout` whenever a workflow step completes or changes status.

---

## 1. RPC Methods

Methods are organized by functional namespaces.

### Workflow (`workflow.*`)
- **`workflow.start`**: Starts a new generation task. Returns `instanceId`.
- **`workflow.cancel`**: Terminates a running task.
- **`workflow.pause` / `workflow.resume`**: Controls workflow execution state.

### Scanning (`scanning.*`)
- **`scanning.scanWorkbook`**: Reads an Excel file to list sheets and columns.
- **`scanning.scanPresentation`**: Reads a PowerPoint file to list slides and shapes.

### Settings (`settings.*`)
- **`settings.get`**: Retrieves current application configuration.
- **`settings.update`**: Persists new configuration to disk.
- **`settings.resetToDefaults`**: Reverts settings to factory state.

---

## 2. Communication Protocol

- **Encoding**: UTF-8.
- **Framing**: New-Line Delimited JSON (NDJSON). Each request/response must be a single line.
- **Serialization**: CamelCase property naming policy.

---

## 3. Sample Progress Notification

```json
{
  "jsonrpc": "2.0",
  "method": "workflow/progress",
  "params": {
    "workflowInstanceId": "...",
    "event": "StepCompleted",
    "stepName": "DownloadImage",
    "status": "Running",
    "timestamp": "2026-05-05T..."
  }
}
```