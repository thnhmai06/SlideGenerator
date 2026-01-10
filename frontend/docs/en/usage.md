# Usage Guide

Vietnamese version: [Vietnamese](../vi/usage.md)

## Prerequisites

- Backend is running (Electron can start it automatically).
- Template file: `.pptx` or `.potx`.
- Spreadsheet file: `.xlsx` or `.xlsm`.

## Connect to backend

1. Open **Settings**.
2. Confirm host/port (default is local).
3. Save changes and restart backend if prompted.

## Create a job

1. Choose a PowerPoint template and a spreadsheet.
2. Wait for shapes/placeholders and headers to load.
3. Add text and image replacements.
4. Choose output path.
5. Click **Create Task**.

Notes:

- Group job = one workbook + one template + output folder.
- Sheet job = one sheet → one output file.

## Monitor processing

Open **Process** to:

- Pause/Resume group or sheet jobs.
- Cancel jobs.
- Watch progress and row-level logs.
- Group progress shows slides completed/total (jobs completed/total), and % is based on slides.

## Results

Open **Result** to:

- View completed/failed/cancelled groups.
- Open output folder or file.
- Clear results from the UI (also removes backend state).
- Removing a group or sheet also deletes backend state.

## Export/import configs

- **Create Task** supports JSON export/import for reuse.
- Each group has an export action for quick sharing.
