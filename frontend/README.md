# SlideGenerator Frontend

The modern desktop interface for SlideGenerator, built with **Tauri v2**, **React**, and **TypeScript**. It provides a user-friendly way to configure templates, manage datasets, and monitor generation progress in real-time.

## Tech Stack

- **Core:** [Tauri v2](https://tauri.app/) + [React 19](https://react.dev/)
- **Language:** TypeScript 5.0+
- **Build Tool:** Vite
- **Styling:** CSS Modules / Global SCSS
- **State Management:** React Context API
- **Testing:** Vitest + React Testing Library

## Quick Start

### Prerequisites
- Node.js (LTS)
- npm

### Installation

```bash
cd frontend
npm install
```

### Development

Start the app in desktop development mode (with Hot Module Replacement):

```bash
npm run dev
```

> **Note:** The Tauri migration is in progress. Runtime command wiring is being finalized for full desktop parity.

### Configure Backend Endpoint

You can override backend endpoints via Vite env variables:

```bash
VITE_BACKEND_URL=http://localhost:65500
VITE_SHEET_RPC_CHANNEL=sheets
VITE_JOB_RPC_CHANNEL=jobs
VITE_CONFIG_RPC_CHANNEL=config
```

Create `frontend/.env.local` for local development overrides.

## Project Structure

The codebase is organized by feature:

- **`src/app`**: Application shell, routing, and global providers.
- **`src/features`**: Self-contained feature modules.
    - `create-task`: Wizard for creating new generation jobs.
    - `process`: Real-time monitoring dashboard.
    - `results`: History and file management.
    - `settings`: Application configuration.
- **`src/shared`**: Reusable components, hooks, and services.
- **`src-tauri`**: Rust desktop host, plugins, and bundling config.

## Documentation

- **[Overview & Architecture](docs/en/overview.md)**: Deep dive into the frontend architecture.
- **[Development Guide](docs/en/development.md)**: Coding standards, testing, and adding features.
- **[Build & Packaging](docs/en/build-and-packaging.md)**: How to build and release the application.
- **[Usage Guide](docs/en/usage.md)**: User manual.

---

[🇻🇳 Vietnamese Documentation](docs/vi)
