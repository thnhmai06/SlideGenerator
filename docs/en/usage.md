# Usage

Vietnamese version: [Vietnamese](../vi/usage.md)

## Run the backend

From `backend/`:

```
dotnet run --project src/SlideGenerator.Presentation
```

## Verify

- Health check: `GET /health`
- Hangfire dashboard: `/hangfire`

## Connect from the client

- Job hub: `/hubs/job` (alias: `/hubs/task`)
- Sheet hub: `/hubs/sheet`
- Config hub: `/hubs/config`

## Quick examples

Create a group job:

```json
{
  "type": "JobCreate",
  "taskType": "Group",
  "templatePath": "C:\\slides\\template.pptx",
  "spreadsheetPath": "C:\\data\\book.xlsx",
  "outputPath": "C:\\output",
  "sheetNames": ["Sheet1"]
}
```

Pause a job:

```json
{ "type": "JobControl", "taskId": "TASK_ID", "taskType": "Group", "action": "Pause" }
```

Remove a group (also deletes backend state):

```json
{ "type": "JobControl", "taskId": "TASK_ID", "taskType": "Group", "action": "Remove" }
```

Query active jobs:

```json
{ "type": "JobQuery", "scope": "Active" }
```
