# Development

Vietnamese version: [Vietnamese](../vi/development.md)

## Table of contents

1. [Prerequisites](#prerequisites)
2. [Install](#install)
3. [Run in development](#run-in-development)
4. [Tests](#tests)
5. [Useful tasks](#useful-tasks)

## Prerequisites

- Node.js + npm
- .NET 10 SDK (for running the backend in dev)

## Install

From `frontend/`:

```
npm install
```

## Run in development

From `frontend/`:

```
npm run dev
```

Notes:

- The Electron main process can spawn the backend automatically.
- To disable auto-start, set `SLIDEGEN_DISABLE_BACKEND=1`.
- You can also run the backend manually from `backend/` with `dotnet run`.

## Tests

From `frontend/`:

```
npm test
```

## Useful tasks

VS Code tasks are defined in `.vscode/tasks.json`:

- `test:frontend`
- `test:backend`
- `test:all`

