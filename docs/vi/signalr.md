# SignalR API

English version: [English](../en/signalr.md)

## Endpoint

- `/hubs/job`: tao/dieu khien/truy van job + scan template.
- `/hubs/task`: legacy alias cua `/hubs/job`.
- `/hubs/sheet`: đọc workbook (header/row).
- `/hubs/config`: cấu hình backend.

## Mô hình request/response

- Client gửi JSON vào `ProcessRequest`.
- Bắt buộc có `type` (không phân biệt hoa thường).
- Phản hồi trả qua `ReceiveResponse`.
- Lỗi trả về `type = error` kèm message.

## Job hub messages

### Scan dữ liệu template

- `ScanShapes` / `ScanPlaceholders` / `ScanTemplate`
- Payload: `{ "filePath": "..." }`

### JobCreate

TaskCreate van duoc ho tro de tuong thich nguoc.

```json
{
  "type": "JobCreate",
  "taskType": "Group",
  "templatePath": "C:\\slides\\template.pptx",
  "spreadsheetPath": "C:\\data\\book.xlsx",
  "outputPath": "C:\\output",
  "sheetNames": ["Sheet1"],
  "textConfigs": [{ "pattern": "FullName", "columns": ["FullName"] }],
  "imageConfigs": [{ "shapeId": 4, "columns": ["Photo"], "roiType": "RuleOfThirds", "cropType": "Fit" }],
  "autoStart": true
}
```

Sheet job:

```json
{
  "type": "JobCreate",
  "taskType": "Sheet",
  "templatePath": "C:\\slides\\template.pptx",
  "spreadsheetPath": "C:\\data\\book.xlsx",
  "outputPath": "C:\\output\\Sheet1.pptx",
  "sheetName": "Sheet1"
}
```

### JobQuery

- `scope`: `Active`, `Completed`, hoặc `All`.
- `includePayload` trả về payload được dựng lại từ state.

```json
{ "type": "JobQuery", "taskId": "TASK_ID", "taskType": "Group", "includeSheets": true }
```

```json
{ "type": "JobQuery", "scope": "Active", "taskType": "Sheet" }
```

### JobControl

- `action`: `Pause`, `Resume`, `Cancel`, `Stop` (duoc hieu la Cancel), hoac `Remove` (xoa state backend).

```json
{ "type": "JobControl", "taskId": "TASK_ID", "taskType": "Group", "action": "Pause" }
```

## Subscriptions

- `SubscribeGroup(groupId)`
- `SubscribeSheet(sheetId)`

## Notifications

Notification được gửi qua `ReceiveNotification` cho client đã subscribe:

- Progress/status của group
- Progress/status/error của sheet
- Log sự kiện

