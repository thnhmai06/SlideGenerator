# Development Guide

[🇻🇳 Vietnamese Version](../vi/development.md)

## Environment Setup

### Prerequisites
- **Node.js** (LTS recommended)
- **npm** (comes with Node.js)
- **.NET 10 SDK** (Required if you intend to run/debug the backend locally)

### Installation
```bash
cd frontend
npm install
```

### Running in Development

This command starts the Vite dev server and the Tauri desktop host.

```bash
npm run dev
```

**Note:** The desktop runtime is being migrated to Tauri v2.
- **Disable Backend Spawn:** `SLIDEGEN_DISABLE_BACKEND=1` (Useful if you are running the backend in Visual Studio).
- **Custom Backend Path:** `SLIDEGEN_BACKEND_PATH=/path/to/executable`.

## Project Structure

We follow a **Feature-First** architecture.

```
src/
├── app/                  # App shell, Layouts, Providers
├── features/             # Feature modules
│   ├── create-task/      # Task creation wizard
│   ├── process/          # Job monitoring dashboard
│   ├── results/          # Completed jobs list
│   └── settings/         # App configuration
├── shared/               # Shared utilities
│   ├── components/       # Atomic UI components (Buttons, Inputs)
│   ├── contexts/         # React Contexts (JobContext, AppContext)
│   ├── hooks/            # Custom React Hooks
│   ├── services/         # API & RPC clients
│   └── styles/           # Global SCSS & Variables
└── assets/               # Static assets (Images, Fonts)
```

## Coding Standards

### TypeScript
- **Strict Mode:** Enabled. No `any` allowed unless absolutely necessary (and documented).
- **Interfaces:** Prefer `interface` over `type` for object definitions.
- **Naming:** PascalCase for components/interfaces, camelCase for functions/vars.

### React
- **Functional Components:** Use FCs with Hooks.
- **Props:** Always define a typed Props interface.
- **Performance:**
    - Use `React.memo` for list items or expensive components.
    - Use `useCallback` for event handlers passed to child components.

### Styling
- **CSS Modules:** Used for component-specific styles (`Component.module.scss`).
- **Global Styles:** Located in `src/shared/styles/`. Use CSS variables for theming.

## Testing

We use **Vitest** + **React Testing Library**.

### Running Tests
```bash
npm test
```

### Writing Tests
- **Unit Tests:** Focus on utility functions and hooks.
- **Component Tests:** Focus on user interactions and accessibility.
- **Mocking:** Use MSW (Mock Service Worker) for network requests. Handlers are in `test/mocks/handlers.ts`.

## Debugging

- **Renderer Process:** Use standard Chrome DevTools (Ctrl+Shift+I).
- **Desktop Host (Rust):** Debug via Rust tooling / VS Code Rust extension.
- **Backend:** Debug via Visual Studio or VS Code C# extension.

Next: [Build & Packaging](build-and-packaging.md)
