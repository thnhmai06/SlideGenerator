# Repository Context

This repository is a React + TypeScript frontend for Slide Generator.

## Runtime Targets

- `npm run dev` starts the Tauri desktop runtime.
- `npm run dev:web` starts the plain Vite web runtime.
- The frontend should be able to run in a normal browser without Tauri APIs.

## Current Architecture

- `src/app` contains the app shell and providers.
- `src/features` contains user-facing UI areas:
  - `create-task`
  - `process`
  - `results`
  - `settings`
  - `about`
- `src/shared` contains shared components, contexts, styles, utilities, platform adapters, and backend services.
- `src/shared/platform` is the boundary where desktop/Tauri capabilities should be isolated.
- `src/shared/services/backend` contains API clients for backend endpoints.
- `src-tauri` contains the desktop wrapper and should not own UI behavior.

## Refactor Goal

- UI must not depend directly on Tauri. Tauri should only provide the desktop host and window/platform APIs through a small adapter boundary.
- UI must not depend directly on a running backend. Backend services define how non-UI processing is executed, but the UI should still render in showcase mode when the backend is unavailable.
- Settings that represent backend-owned behavior may be displayed in the UI, but should be disabled when running without editable backend configuration.

## Acceptance Criteria

- Running `npm run dev:web` in a normal browser without Tauri APIs renders the full UI.
- The browser build should act as a frontend showcase, including representative states for create task, process, results, settings, and about screens.
- Desktop-specific APIs must be guarded behind platform adapters or capability checks.
- Backend-specific data should have browser-safe fallbacks or mock/showcase data where needed.
