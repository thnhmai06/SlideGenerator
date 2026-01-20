# SlideGenerator Frontend

The modern desktop interface for SlideGenerator, built with **Electron**, **React**, and **TypeScript**. It provides a user-friendly way to configure templates, manage datasets, and monitor generation progress in real-time.

## Tech Stack

- **Core:** [Electron](https://www.electronjs.org/) + [React 18](https://react.dev/)
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

Start the app in development mode (with Hot Module Replacement):

```bash
npm run dev
```

> **Note:** By default, Electron will attempt to launch the backend binary. To disable this behavior (e.g., when debugging the backend separately in Visual Studio), set the environment variable: `SLIDEGEN_DISABLE_BACKEND=1`.

## Project Structure

The codebase is organized by feature:

- **`src/app`**: Application shell, routing, and global providers.
- **`src/features`**: Self-contained feature modules.
    - `create-task`: Wizard for creating new generation jobs.
    - `process`: Real-time monitoring dashboard.
    - `results`: History and file management.
    - `settings`: Application configuration.
- **`src/shared`**: Reusable components, hooks, and services.
- **`electron`**: Main process code and preload scripts.

## Documentation

- **[Overview & Architecture](docs/en/overview.md)**: Deep dive into the frontend architecture.
- **[Development Guide](docs/en/development.md)**: Coding standards, testing, and adding features.
- **[Build & Packaging](docs/en/build-and-packaging.md)**: How to build and release the application.
- **[Usage Guide](docs/en/usage.md)**: User manual.

---

[🇻🇳 Vietnamese Documentation](docs/vi)
