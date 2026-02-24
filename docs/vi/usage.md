# Sử dụng

English version: [English](../en/usage.md)

## Chạy backend

Từ thư mục `backend/`:

```
dotnet run --project src/SlideGenerator.Ipc
```

## Kiểm tra

- Health check: `GET /health`
- Hangfire dashboard: `/hangfire`

## Kết nối từ client

- Transport: stdio JSON-RPC 2.0
- Method chính: `jobs.create`, `jobs.get`, `jobs.list`, `jobs.pause`, `jobs.resume`, `jobs.cancel`
- Method tiện ích: `slides.scan`, `excel.scan`, `system.health`

## Ví dụ nhanh

Tạo group job (`jobs.create` params):

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

Tạm dừng job (`jobs.pause` params):

```json
{ "jobId": "TASK_ID" }
```

Hủy group (`jobs.cancel` params):

```json
{ "jobId": "TASK_ID" }
```

Lấy chi tiết job (`jobs.get` params):

```json
{ "jobId": "TASK_ID" }
```

