# SignalR API

[🇻🇳 Vietnamese Version](../vi/signalr.md)

The backend exposes a real-time API via SignalR hubs. All communication follows a request/response pattern with asynchronous notifications.

## Hub Endpoints

| Endpoint | Description |
| :--- | :--- |
| `/hubs/job` | Main endpoint for creating, controlling, and querying jobs. |
| `/hubs/sheet` | Utilities for inspecting Excel workbooks (headers, rows). |
| `/hubs/config` | Read and write backend configuration. |

> **Note:** `/hubs/task` is a legacy alias for `/hubs/job`.

## Protocol

### Request Pattern
Clients send requests by invoking the `ProcessRequest` method on the Hub with a JSON payload.

- **Required Field:** `type` (case-insensitive string).
- **Response:** Sent back via the `ReceiveResponse` event.
- **Errors:** Returned as a message with type `error`.

## Job Hub Messages (`/hubs/job`)

### 1. Create Job (`JobCreate`)

Creates a new generation task.

**Group Job (Workbook + Template):**
```json
{
  "type": "JobCreate",
  "jobType": "Group",
  "templatePath": "C:\\slides\\template.pptx",
  "spreadsheetPath": "C:\\data\\book.xlsx",
  "outputPath": "C:\\output",
  "sheetNames": ["Sheet1", "Sheet2"],
  "textConfigs": [
    { "pattern": "{{Name}}", "columns": ["FullName"] }
  ],
  "imageConfigs": [
    {
      "shapeId": 4,
      "columns": ["Photo"],
      "roiType": "RuleOfThirds",
      "cropType": "Fit"
    }
  ],
  "autoStart": true
}
```

**Sheet Job (Single Sheet):**
```json
{
  "type": "JobCreate",
  "jobType": "Sheet",
  "templatePath": "C:\\slides\\template.pptx",
  "spreadsheetPath": "C:\\data\\book.xlsx",
  "outputPath": "C:\\output\\Sheet1.pptx",
  "sheetName": "Sheet1"
}
```

### 2. Control Job (`JobControl`)

Manage the state of running jobs.

- **Actions:** `Pause`, `Resume`, `Cancel`, `Stop` (same as Cancel), `Remove` (delete from history).

```json
{
  "type": "JobControl",
  "jobId": "GUID-ID-HERE",
  "jobType": "Group",
  "action": "Pause"
}
```

### 3. Query Job (`JobQuery`)

Retrieve job details.

- **Scope:** `Active`, `Completed`, `All`.
- **includePayload:** Returns the original JSON payload (reconstructed from DB).

```json
{
  "type": "JobQuery",
  "jobId": "GUID-ID-HERE",
  "jobType": "Group",
  "includeSheets": true
}
```

### 4. Scan Template
Helpers to inspect PPTX files.
- **Actions:** `ScanShapes`, `ScanPlaceholders`, `ScanTemplate`.

```json
{
  "type": "ScanShapes",
  "filePath": "C:\\slides\\template.pptx"
}
```

## Notifications

Clients must listen to `ReceiveNotification` to get real-time updates.

**Event Types:**
- `GroupProgress`: Overall progress of a group.
- `SheetProgress`: Progress of an individual sheet.
- `JobStatus`: State changes (e.g., Pending -> Processing).
- `LogEvent`: Structured log messages from the backend.

## Subscriptions

To receive detailed updates for specific jobs, clients must subscribe:

- `SubscribeGroup(groupId)`
- `SubscribeSheet(sheetId)`
