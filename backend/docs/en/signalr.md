# SignalR API

Vietnamese version: [Vietnamese](../vi/signalr.md)

## Endpoints

- `/hubs/job`: job create/control/query + template scanning.
- `/hubs/task`: legacy alias for `/hubs/job`.
- `/hubs/sheet`: workbook inspection (headers/rows).
- `/hubs/config`: backend configuration.

## Request/response pattern

- Client sends a JSON object to `ProcessRequest`.
- `type` is required and case-insensitive.
- Responses are sent via `ReceiveResponse`.
- Errors are returned with type `error` and a message.

## Job hub messages

### Scan template data

- `ScanShapes` / `ScanPlaceholders` / `ScanTemplate`
- Payload: `{ "filePath": "..." }`

### JobCreate

Creates a group or sheet job. `TaskCreate` is accepted for backward compatibility.

```json
{
  "type": "JobCreate",
  "jobType": "Group",
  "templatePath": "C:\\slides\\template.pptx",
  "spreadsheetPath": "C:\\data\\book.xlsx",
  "outputPath": "C:\\output",
  "sheetNames": ["Sheet1"],
  "textConfigs": [{ "pattern": "FullName", "columns": ["FullName"] }],
  "imageConfigs": [{ "shapeId": 4, "columns": ["Photo"], "roiType": "RuleOfThirds", "cropType": "Fit" }],
  "autoStart": true
}
```

Sheet job example:

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

### JobQuery

- Query a single job by id, or list jobs by scope.
- `scope`: `Active`, `Completed`, or `All`.
- `includePayload` returns JSON reconstructed from persisted state (useful for export).

```json
{ "type": "JobQuery", "jobId": "TASK_ID", "jobType": "Group", "includeSheets": true }
```

```json
{ "type": "JobQuery", "scope": "Active", "jobType": "Sheet" }
```

### JobControl

- `action`: `Pause`, `Resume`, `Cancel`, `Stop` (treated as Cancel), or `Remove` (delete backend state).

```json
{ "type": "JobControl", "jobId": "TASK_ID", "jobType": "Group", "action": "Pause" }
```

## Subscriptions

- `SubscribeGroup(groupId)`
- `SubscribeSheet(sheetId)`

## Notifications

Notifications are delivered via `ReceiveNotification` to subscribed clients:

- Group progress/status
- Sheet progress/status/error
- Structured log events
