# SlideGenerator

## Overview

SlideGenerator is an offline desktop app that generates PowerPoint slides from spreadsheet data.
The repository is split into two main parts:

- `backend/` - ASP.NET Core + SignalR + Hangfire job system
- `frontend/` - Electron + React UI

Docs are split by module and language:

- Backend English: [backend/docs/en](backend/docs/en)
- Backend Vietnamese: [backend/docs/vi](backend/docs/vi)
- Frontend English: [frontend/docs/en](frontend/docs/en)
- Frontend Vietnamese: [frontend/docs/vi](frontend/docs/vi)

## Quick start

Backend:

```
cd backend
dotnet run --project src/SlideGenerator.Presentation
```

Frontend:

```
cd frontend
npm install
npm run dev
```

## Build scripts (root)

These scripts build the backend (with correct runtime identifier) and the frontend bundle.
They are OS-specific and live at the repository root.

Windows (PowerShell):

```
.\build.ps1 -Runtime win-x64
```

Linux (bash):

```
chmod +x ./build.sh
./build.sh linux-x64
```

Supported runtimes:

- `win-x64`
- `linux-x64`
- `linux-arm`
- `linux-arm64`

## Module docs

Backend:

- Overview: [backend/README.md](backend/README.md)
- Architecture: [backend/docs/en/architecture.md](backend/docs/en/architecture.md)
- Job system: [backend/docs/en/job-system.md](backend/docs/en/job-system.md)
- SignalR API: [backend/docs/en/signalr.md](backend/docs/en/signalr.md)
- Configuration: [backend/docs/en/configuration.md](backend/docs/en/configuration.md)
- Usage: [backend/docs/en/usage.md](backend/docs/en/usage.md)

Frontend:

- Overview: [frontend/docs/en/overview.md](frontend/docs/en/overview.md)
- Usage: [frontend/docs/en/usage.md](frontend/docs/en/usage.md)
- Development: [frontend/docs/en/development.md](frontend/docs/en/development.md)
- Build & packaging: [frontend/docs/en/build-and-packaging.md](frontend/docs/en/build-and-packaging.md)

## Contributors

- **PM, Main Developer: [@thnhmai06](https://github.com/thnhmai06)**
- UI/UX Idea: [@NAV-adsf23fd](https://github.com/NAV-adsf23fd)
- Framework: [SlideGenerator.Framework](https://github.com/thnhmai06/SlideGenerator.Framework)
