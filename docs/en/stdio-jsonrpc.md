# Stdio JSON-RPC Backend

This backend host runs as a line-delimited JSON-RPC 2.0 server over standard input/output.

- Requests: `stdin` (one JSON document per line)
- Responses + notifications: `stdout`
- Diagnostics/errors: `stderr`

## Methods

### `system.health`
Checks whether server loop is alive.

Request:
```json
{"jsonrpc":"2.0","id":1,"method":"system.health","params":{}}
```

### `slides.scan`
Scans a PPTX and returns per-slide image shape ids and mustache placeholders.

Params:
```json
{"filePath":"C:/path/to/template.pptx"}
```

### `excel.scan`
Scans an Excel workbook and returns sheet headers + data row counts.

Params:
```json
{"filePath":"C:/path/to/data.xlsx"}
```

### `jobs.create`
Creates a generation job, persists state in SQLite, and enqueues background processing.

Params shape:
- `templates`: `[{ templateKey, filePath, templateSlideIndex }]`
- `sheetPath`: workbook path
- `sheetTemplateMap`: object map `sheetName -> templateKey`
- `selectedSheets`: optional array; if null/empty, all mapped sheets are used
- `textConfig`: `[{ placeholder, columns[] }]`
- `imageConfig`: `[{ shapeId, columns[], roiMode }]`
- `outputFolder`: output directory

### `jobs.get`
Gets current snapshot for a job.

Params:
```json
{"jobId":"<guid>"}
```

### `jobs.list`
Lists all jobs.

### `jobs.pause` / `jobs.resume` / `jobs.cancel`
Controls a job lifecycle.

Params:
```json
{"jobId":"<guid>"}
```

## Notifications

### `jobs.updated`
Emitted whenever status/progress/checkpoint changes.

Payload is a full `JobSnapshot` object.

## Persistence

- SQLite file: `Jobs.db` (default path from `Config.DefaultDatabasePath`)
- Tables:
  - `jobs`
  - `job_sheets`
  - `job_rows`
- Resume behavior:
  - Pending/Running jobs are re-enqueued on startup.
  - Row-level checkpoints are used for best-effort exactly-once processing.

## Notes

- `sheetPath` is the canonical request field for Excel input in backend DTOs.
- `roiMode` accepts: `center`, `prominent`, `ruleofthirds`.
- Runtime logs diagnostics to `stderr`; JSON-RPC payloads are emitted only to `stdout`.
