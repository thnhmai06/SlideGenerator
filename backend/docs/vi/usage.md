# Sử dụng

English version: [English](../en/usage.md)

## Chạy backend

Từ thư mục `backend/`:

```
dotnet run --project src/SlideGenerator.Presentation
```

## Kiểm tra

- Health check: `GET /health`
- Hangfire dashboard: `/hangfire`

## Kết nối từ client

- Job hub: `/hubs/job` (alias: `/hubs/task`)
- Sheet hub: `/hubs/sheet`
- Config hub: `/hubs/config`

## Ví dụ nhanh

Tạo group job:

```json
{
  "type": "JobCreate",
  "jobType": "Group",
  "templatePath": "C:\\slides\\template.pptx",
  "spreadsheetPath": "C:\\data\\book.xlsx",
  "outputPath": "C:\\output",
  "sheetNames": ["Sheet1"]
}
```

Tạm dừng job:

```json
{ "type": "JobControl", "jobId": "TASK_ID", "jobType": "Group", "action": "Pause" }
```

Xóa group (xóa cả backend state):

```json
{ "type": "JobControl", "jobId": "TASK_ID", "jobType": "Group", "action": "Remove" }
```

Query job đang chạy:

```json
{ "type": "JobQuery", "scope": "Active" }
```

