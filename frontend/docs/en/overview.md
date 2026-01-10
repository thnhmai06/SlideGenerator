# Frontend Overview

Vietnamese version: [Vietnamese](../vi/overview.md)

## Purpose

The frontend is a desktop Electron app that hosts a React UI. It connects to the backend via SignalR and treats the backend as the source of truth for job state.

## Architecture

- UI layer: React + TypeScript.
- Desktop shell: Electron (main + preload).
- Backend bridge: SignalR client wrappers in `src/services`.

## Runtime model

1. Electron main process starts the window and (optionally) the backend.
2. The UI connects to `/hubs/job` (alias: `/hubs/task`), `/hubs/sheet`, and `/hubs/config`.
3. Job data and notifications stream over SignalR.

## Storage

- Backend URL: `localStorage.slidegen.backend.url`.
- Create Task input cache: `sessionStorage.slidegen.ui.inputMenu.state`.
- Group metadata/config cache:
  - `sessionStorage.slidegen.group.meta`
  - `sessionStorage.slidegen.group.config`

## Logs

Logs are stored under `frontend/logs/<timestamp>/`:

- `process.log`: Electron main process.
- `renderer.log`: renderer (DevTools) logs.
- `backend.log`: backend output (when launched by Electron).
