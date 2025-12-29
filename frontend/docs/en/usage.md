# Usage Guide

Vietnamese version: [Vietnamese](../vi/usage.md)

## Table of contents

1. [Prerequisites](#prerequisites)
2. [Connect to backend](#connect-to-backend)
3. [Create a task](#create-a-task)
4. [Monitor processing](#monitor-processing)
5. [Results](#results)
6. [Export and import configs](#export-and-import-configs)

## Prerequisites

- The backend must be running (the Electron app can launch it as a subprocess).
- Template file: `.pptx` or `.potx`
- Spreadsheet file: `.xlsx` or `.xlsm`

## Connect to backend

1. Open **Settings**.
2. Update **Host** and **Port** if needed (default is local).
3. Save the configuration.
4. Restart the backend when prompted to apply changes.

## Create a task

1. In **Create Task**, select a template file and a spreadsheet file.
2. Wait for headers and placeholders to load.
3. Add text replacements and image replacements:
   - Placeholder must match a detected placeholder from the template.
   - Column must match a header from the spreadsheet.
4. Choose the output folder and click **Create Task**.

## Monitor processing

Open **Process** to:

- Pause/Resume group or sheet jobs.
- Stop a job (cancels and removes it on the backend, deleting output files).
- Review per-row logs.

## Results

Open **Result** to:

- See completed/failed/cancelled groups.
- Open the output folder or a single output file.
- Clear results from the app (removes them from the backend as well).

## Export and import configs

- **Create Task** lets you export/import a JSON config file.
- **Process/Result** provides an icon-only **Export configuration** button per group.
  It saves a JSON config compatible with Create Task import.
