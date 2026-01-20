# Contributing to SlideGenerator

First off, thanks for taking the time to contribute! 🎉

The following is a set of guidelines for contributing to SlideGenerator. These are mostly guidelines, not rules. Use your best judgment, and feel free to propose changes to this document in a pull request.

## Table of Contents

- [Code of Conduct](#code-of-conduct)
- [How Can I Contribute?](#how-can-i-contribute)
  - [Reporting Bugs](#reporting-bugs)
  - [Suggesting Enhancements](#suggesting-enhancements)
  - [Pull Requests](#pull-requests)
- [Development Guide](#development-guide)
  - [Prerequisites](#prerequisites)
  - [Get Source Code](#get-source-code)
  - [Running](#running)
  - [Building](#building)
  - [Code Quality](#code-quality)
- [Documentation](#documentation)

## Code of Conduct

This project and everyone participating in it is governed by the [SlideGenerator Code of Conduct](CODE_OF_CONDUCT.md). By participating, you are expected to uphold this code. Please report unacceptable behavior to the project maintainers.

## How Can I Contribute?

### Reporting Bugs

This section guides you through submitting a bug report for SlideGenerator. Following these guidelines helps maintainers and the community understand your report, reproduce the behavior, and find related reports.

- **Use a clear and descriptive title** for the issue to identify the problem.
- **Describe the exact steps to reproduce the problem** in as much detail as possible.
- **Include screenshots or GIFs** which show you following the reproduction steps.
- **Explain which behavior you expected to see instead and why.**

### Suggesting Enhancements

This section guides you through submitting an enhancement suggestion for SlideGenerator, including completely new features and minor improvements to existing functionality.

- **Use a clear and descriptive title** for the issue to identify the suggestion.
- **Provide a step-by-step description of the suggested enhancement** in as much detail as possible.
- **Explain why this enhancement would be useful** to most SlideGenerator users.

### Pull Requests

The process is straightforward:

1.  **Fork** the repo on GitHub.
2.  **Clone** the project to your own machine.
3.  **Create a branch** for your feature or fix: `git checkout -b feature/amazing-feature`.
4.  **Commit** your changes to your own branch.
    *   Make sure to follow the [Code Quality](#code-quality) guidelines.
    *   Write clear, descriptive commit messages.
5.  **Push** your work back up to your fork.
6.  **Submit a Pull Request** so that we can review your changes.

## Development Guide

### Prerequisites

Ensure you have the following installed:

- **.NET 10.0 SDK** or later ([Download](https://dotnet.microsoft.com/download))
- **Node.js** (LTS version recommended) ([Download](https://nodejs.org/en/download))

We recommend using **Visual Studio** (for Backend) or **Visual Studio Code** (Full Stack) for the best development experience.

### Get Source Code

Clone the repository with submodules:

```bash
git clone https://github.com/thnhmai06/SlideGenerator --recurse-submodules
cd SlideGenerator
```

To update an existing clone:

```bash
git fetch
git pull
```

### Running

#### Backend

1. **Via Visual Studio:**
   - Open `SlideGenerator.sln`.
   - Set `SlideGenerator.Presentation` as the startup project.
   - Start Debugging (F5).

2. **Via VS Code:**
   - Open the "Run and Debug" tab.
   - Select `[Backend]` configuration and Start Debugging (F5).

3. **Via CLI:**
   ```bash
   cd backend
   dotnet run --project src/SlideGenerator.Presentation
   ```

#### Frontend

First, install dependencies:
```bash
cd frontend
npm install
```

1. **Via VS Code:**
   - Select `[Frontend]` configuration and Start Debugging (F5).

2. **Via CLI:**
   ```bash
   npm run dev
   ```

### Building

We use [Task](https://taskfile.dev/) (also known as `go-task`) as our build runner. It provides a consistent interface across platforms.

**Prerequisites:**
- Install Task: [Installation Guide](https://taskfile.dev/installation/)

**VS Code Integration:**
The project includes a `.vscode/tasks.json` that maps VS Code tasks to Taskfile commands. You can run them via the **Terminal -> Run Task** menu or by pressing `Ctrl+Shift+B` for a full build.

**Build Commands:**

Build everything (Backend + Frontend):
```bash
task build
```

Build components individually:
```bash
task build:backend
task build:frontend
```

Run tests:
```bash
task test
```

Specify target runtime (defaults to `win-x64`):
```bash
task build RUNTIME=linux-x64
```

**Supported Runtimes:** `win-x64`, `linux-x64`, `linux-arm`, `linux-arm64`.

### Code Quality

Before committing, please run the formatters using Task:

```bash
task format
```

This will run `dotnet format` for the backend and `npm run format` for the frontend.

## Documentation

**Backend:**
- [Overview](backend/README.md)
- [Architecture](backend/docs/en/architecture.md)
- [Job System](backend/docs/en/job-system.md)
- [SignalR API](backend/docs/en/signalr.md)
- [Configuration](backend/docs/en/configuration.md)
- [Usage](backend/docs/en/usage.md)

**Frontend:**
- [Overview](frontend/docs/en/overview.md)
- [Usage](frontend/docs/en/usage.md)
- [Development](frontend/docs/en/development.md)
- [Build & Packaging](frontend/docs/en/build-and-packaging.md)