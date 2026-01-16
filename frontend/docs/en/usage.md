# Usage Guide

[Tiếng Việt](../vi/usage.md)

## Prerequisites

- Backend is running (Electron can start it automatically).
- Template: `.pptx` or `.potx`.
- Spreadsheet: `.xlsx` or `.xlsm`.

## Connect to backend

1. Open **Settings**.
2. Check host/port (defaults to local).
3. Save changes and restart backend if prompted.

## Create a task (group job)

1. Choose a PowerPoint template.
2. Choose a spreadsheet and wait for columns/sheets to load.
3. Add text and image replacements.
4. Optionally pick specific sheets to process.
5. Choose output folder.
6. Click **Create Task**.

Notes:

- A group job represents one template + one workbook + one output folder.
- A sheet job represents one sheet inside the group.
- Progress and counts are based on slide rows, not the number of jobs.

## Processing

Use **Processing** to:

- Pause/resume group or sheet jobs.
- Cancel jobs.
- View row-level logs grouped by row.

## Results

Use **Results** to:

- View completed/failed/cancelled groups.
- Open output folder or file.
- Remove a group or sheet (also clears backend state).

## Export/import configs

- **Create Task** supports JSON export/import for reuse.
- Each group has an export action for quick sharing.

## Check for updates

Use **About** to check for and install application updates:

1. Open the **About** tab.
2. Click **Check for updates** to check for new versions.
3. If an update is available, click **Download and install**.
4. After downloading, click **Install now** to restart and apply the update.
5. Alternatively, the update will be applied when you quit the application.
