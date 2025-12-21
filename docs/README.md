# Frontend Overview

## Purpose

The frontend is an Electron + React client that talks to the backend via SignalR hubs.
It is optimized for local/offline usage and expects the backend to run on the same machine.

## Run

```bash
cd frontend
npm run dev
```

For VS Code, see `.vscode/launch.json` at the repo root.

## Backend integration

SignalR hubs:

- `/hubs/sheet`: workbook inspection and row/column access
- `/hubs/slide`: template scanning, job creation, job status/control, realtime updates
- `/hubs/config`: server configuration (server, download, job, image)

The default backend URL is `http://127.0.0.1:5000`.
The current value is stored in `localStorage.backendUrl`.

## Input workflow

1. Select a PowerPoint template file (`.pptx`, `.potx`).
2. Select a sheet file (`.xlsx`, `.xlsm`).
3. Configure replacements:
   - Text: placeholder -> column mappings.
   - Image: shape -> column mappings, plus ROI/Crop mode.
4. Choose an output folder.
5. Click "Create Task" to start the job.

The UI only accepts placeholders scanned from the template and column headers from the sheet.
Duplicate placeholders or shapes are blocked.

## Image replacement options

ROI types:

- `Attention`: focus on attention-based ROI.
- `Prominent`: focus on the most prominent subject.
- `Center`: use the center region.

Crop types:

- `Crop`: fill the shape by cropping.
- `Fit`: fit the image inside the shape.

## Settings

Settings are fetched from `/hubs/config` and include:

- Server: host, port, debug
- Download: limits and retry
- Job: max concurrent jobs
- Image: face/saliency padding and confidence

Edits are disabled while jobs are running or pending.

## Troubleshooting

- If SignalR negotiation fails, verify CORS and backend host/port.
- If config fails to load, check the backend log and ensure `/hubs/config` is reachable.
