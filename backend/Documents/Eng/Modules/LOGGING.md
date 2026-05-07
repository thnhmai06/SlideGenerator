# Module: Logging

## The Hook (Q&A)

**Q: How do we track errors in a headless sidecar?**  
We use **Serilog** with multiple sinks. Logs are simultaneously written to local files for deep debugging and a **SQLite/Workflow Database** for structured tracking.

**Q: Can the frontend see the logs?**  
Errors and critical events are piped through the JSON-RPC `workflow/progress` notification and also captured in the database, which the frontend can query for historical audit trails.

---

## 1. Logging Strategy

- **File Sink**: Rolling log files stored in the local application data folder.
- **Workflow Sink**: A custom Serilog sink (`WorkflowDatabaseSink`) that records generation results and errors into a database for persistence across sessions.

---

## 2. Structured Data

Logs include context-rich data such as `WorkflowInstanceId`, `StepName`, and `Exception` details to make troubleshooting specific generation failures straightforward.