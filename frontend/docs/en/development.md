# Development

[Tiếng Việt](../vi/development.md)

## Prerequisites

- Node.js + npm
- .NET 10 SDK (if you run the backend in dev)

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

Testing stack:

- Vitest + Testing Library.
- MSW handlers live in [test/mocks/handlers.ts](../../test/mocks/handlers.ts).
- Override handlers per-test with `server.use(...)`.

## Aliases

- `@/` maps to `src/`.

## Logs

See `frontend/logs/<timestamp>/` for process/renderer/backend logs.
