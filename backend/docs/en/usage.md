# Usage

Vietnamese version: [Vietnamese](../vi/usage.md)

## Run the backend

From `backend/`:

```
dotnet run --project src/SlideGenerator.Ipc
```

## Verify

- Health check: `GET /health`
- Hangfire dashboard: `/hangfire`

## Connect from the client

- Transport: stdio JSON-RPC 2.0
- Main methods: `jobs.create`, `jobs.get`, `jobs.list`, `jobs.pause`, `jobs.resume`, `jobs.cancel`
- Utility methods: `slides.scan`, `excel.scan`, `system.health`

## Quick examples

Create a group job (`jobs.create` params):

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

Pause a job (`jobs.pause` params):

```json
{ "jobId": "TASK_ID" }
```

Cancel a group (`jobs.cancel` params):

```json
{ "jobId": "TASK_ID" }
```

Get a specific job (`jobs.get` params):

```json
{ "jobId": "TASK_ID" }
```
