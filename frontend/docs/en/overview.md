# Frontend Overview

Vietnamese version: [Vietnamese](../vi/overview.md)

## Table of contents

1. [Architecture](#architecture)
2. [Key folders](#key-folders)
3. [Runtime model](#runtime-model)
4. [Logging](#logging)
5. [State and storage](#state-and-storage)

## Architecture

The frontend is a desktop Electron app that hosts a React UI. It connects to the backend via
SignalR and treats the backend as the source of truth for job state.

## Key folders

- `src/components`: UI views (Create Task, Process, Result, Settings, About).
- `src/contexts`: app state and job state (SignalR subscriptions).
- `src/services`: backend API wrappers and SignalR client.
- `src/styles`: global and component styles.
- `electron/main`: Electron main process modules (window, backend, logging, dialogs, settings).
- `electron/preload`: Electron preload and API bridge to renderer.
- `assets`: images, icons, and animations.

## Runtime model

- `electron/main.ts` wires the main-process modules and creates the window.
- The UI uses SignalR hubs (`/hubs/slide`, `/hubs/sheet`, `/hubs/config`).
- The backend URL is stored in local storage (`slidegen.backend.url`).

## Logging

- Logs are stored under `frontend/logs/<timestamp>/`.
- `process.log` contains Electron main-process logs.
- `renderer.log` contains renderer (DevTools) logs forwarded from the UI.
- `backend.log` is written by the backend when launched by Electron.

## State and storage

- Input state for Create Task is cached in session storage (`slidegen.ui.inputMenu.state`).
- Group metadata and group configs are cached in session storage:
  - `slidegen.group.meta`
  - `slidegen.group.config`
