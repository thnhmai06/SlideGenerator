# SlideGenerator Frontend

Electron + React desktop client for SlideGenerator. It connects to the backend via SignalR and runs fully local.

## Quick start

```bash
cd frontend
npm install
npm run dev
```

## Structure

- [src/app](src/app): app shell, providers, and top-level layout.
- [src/features](src/features): feature screens (create-task, process, results, settings, about).
- [src/shared](src/shared): shared UI, contexts, services, utils, locales, and styles.
- [electron](electron): Electron main + preload.
- [assets](assets): fonts and images.
- [test](test): test setup and MSW handlers.

## Aliases

- `@/` maps to `src/`.

## Docs

- [Overview](docs/en/overview.md)
- [Usage](docs/en/usage.md)
- [Development](docs/en/development.md)
- [Build & packaging](docs/en/build-and-packaging.md)
- [Vietnamese](docs/vi/)
