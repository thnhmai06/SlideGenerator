# Development

Vietnamese version: [Vietnamese](../vi/development.md)

## Prerequisites

- Node.js + npm
- .NET 10 SDK (for backend in dev)

## Install

```bash
cd frontend
npm install
```

## Run in development

```bash
npm run dev
```

Notes:

- Electron can auto-start the backend.
- Disable auto-start with `SLIDEGEN_DISABLE_BACKEND=1`.
- Override backend path with `SLIDEGEN_BACKEND_PATH`.

## Tests

```bash
npm test
```

## Logs

See `frontend/logs/<timestamp>/` for process/renderer/backend logs.
