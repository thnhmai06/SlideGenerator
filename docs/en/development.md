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

## Code Style

- **TSDoc comments**: All exported functions, hooks, and components have TSDoc documentation.
- **TypeScript strict mode**: Enabled for type safety.
- **ESLint + Prettier**: Auto-formatting on save.

## Performance Guidelines

- Use `React.memo` for components that receive stable props.
- Wrap callbacks in `useCallback` to prevent unnecessary re-renders.
- Use `useMemo` for expensive computations.
- Prefer lazy loading for feature modules.

## Aliases

- `@/` maps to `src/`.

## Project Structure

```
src/
├── app/           # App shell, providers, routing
├── features/      # Feature modules
│   ├── create-task/  # Task creation workflow
│   ├── process/      # Job monitoring
│   ├── results/      # Completed jobs
│   ├── settings/     # App configuration
│   └── about/        # About screen
└── shared/        # Shared code
    ├── components/   # Reusable UI components
    ├── contexts/     # React contexts and hooks
    ├── services/     # Backend API and SignalR
    ├── utils/        # Utility functions
    ├── locales/      # i18n translations
    └── styles/       # Global CSS
```

## Logs

See `frontend/logs/<timestamp>/` for process/renderer/backend logs.
