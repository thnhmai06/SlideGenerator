# Frontend Overview

[Tiếng Việt](../vi/overview.md)

## Purpose

- Desktop UI for creating and monitoring slide jobs.
- Backend is the source of truth; the UI connects via SignalR.
- Local-first: settings and UI state are stored on device.

## Architecture

- [src/app](../../src/app): app shell and providers.
- [src/features](../../src/features): feature screens (create-task, process, results, settings, about).
- [src/shared](../../src/shared): shared UI, contexts, services, utils, locales, styles.
- [electron](../../electron): main/preload processes and tray menu.

## Runtime flow

1. Electron main starts the renderer and optionally the backend.
2. Renderer connects to `/hubs/job`, `/hubs/sheet`, `/hubs/config`.
3. The UI updates from hub notifications and explicit queries.

## Storage keys

- `localStorage.slidegen.backend.url`: active backend base URL.
- `localStorage.slidegen.backend.url.pending`: pending URL (promoted once).
- `sessionStorage.slidegen.backend.url.pending.defer`: defers promotion for the session.
- `sessionStorage.slidegen.ui.inputsideBar.state`: Create Task draft.
- `sessionStorage.slidegen.group.meta`: cached group meta.
- `sessionStorage.slidegen.group.config`: cached group configs.

## Logs

`frontend/logs/<timestamp>/`:

- `process.log`: Electron main process.
- `renderer.log`: renderer (DevTools) logs.
- `backend.log`: backend output when launched by Electron.
