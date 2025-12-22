# SignalR API

Vietnamese version: [Vietnamese](../vi/signalr.md)

## Table of contents

1. [Endpoints](#endpoints)
2. [Request/response pattern](#requestresponse-pattern)
3. [Slide hub messages](#slide-hub-messages)
4. [Subscriptions](#subscriptions)
5. [Notifications](#notifications)
6. [Examples](#examples)

## Endpoints

The backend exposes SignalR hubs:

- `/hubs/slide`
- `/hubs/sheet`
- `/hubs/config`

## Request/response pattern

Clients send a JSON message to `ProcessRequest`.

- The message includes a `type` field.
- The hub responds via `ReceiveResponse`.

## Slide hub messages

See code: `SlideGenerator.Presentation/Hubs/SlideHub.cs`.

Common message types:

- `ScanShapes`
- `GroupCreate`
- `GroupStatus`
- `GroupControl`
- `JobStatus`
- `JobControl`
- `GlobalControl`
- `GetAllGroups`

## Subscriptions

Clients subscribe for realtime updates:

- `SubscribeGroup(groupJobId)`
- `SubscribeSheet(sheetJobId)`

## Notifications

Notifications are scoped to subscribers and delivered via `ReceiveNotification`.

Core notifications:

- Job progress
- Job status
- Job error
- Group progress
- Group status

See also:

- [Job system](job-system.md)
- [Architecture](architecture.md)

## Examples

All requests are sent to `ProcessRequest` on the hub and receive a response from `ReceiveResponse`.

Scan a template (shapes + placeholders):

```json
{
  "type": "ScanTemplate",
  "filePath": "C:\\slides\\template.pptx"
}
```

Create a group job:

```json
{
  "type": "GroupCreate",
  "templatePath": "C:\\slides\\template.pptx",
  "spreadsheetPath": "C:\\data\\workbook.xlsx",
  "outputPath": "C:\\output",
  "textConfigs": [
    {
      "pattern": "FullName",
      "columns": ["FullName"]
    }
  ],
  "imageConfigs": [
    {
      "shapeId": 4,
      "columns": ["PhotoUrl"],
      "roiType": "Attention",
      "cropType": "Fit"
    }
  ],
  "sheetNames": ["Sheet1"]
}
```

Pause or resume a group:

```json
{
  "type": "GroupControl",
  "groupId": "GROUP_ID",
  "action": "Pause"
}
```

Get logs for a sheet job:

```json
{
  "type": "JobLogs",
  "jobId": "SHEET_JOB_ID"
}
```

Subscribe to realtime updates:

```
SubscribeGroup("GROUP_ID")
SubscribeSheet("SHEET_JOB_ID")
```
