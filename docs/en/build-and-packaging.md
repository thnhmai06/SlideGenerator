# Build and Packaging

Vietnamese version: [Vietnamese](../vi/build-and-packaging.md)

## Table of contents

1. [Build scripts](#build-scripts)
2. [Backend subprocess](#backend-subprocess)
3. [Packaging notes](#packaging-notes)

## Build scripts

From `frontend/`:

```
npm run build:backend
npm run build
```

Or use the combined script:

```
npm run build:full
```

`build:backend` publishes the backend into `frontend/backend`.

## Backend subprocess

The Electron main process launches the backend:

- Development: `dotnet run --project backend/src/SlideGenerator.Presentation/...`
- Production: runs `SlideGenerator.Presentation.exe` from `resources/backend`
  (or `dotnet SlideGenerator.Presentation.dll` if only the DLL is present).

Environment variables:

- `SLIDEGEN_DISABLE_BACKEND=1` disables auto-start.
- `SLIDEGEN_BACKEND_PATH` overrides the backend executable or DLL path.

## Packaging notes

- `frontend/backend` is copied into the packaged app as `resources/backend`.
- Ensure the target machine has the .NET 10 runtime if you ship the DLL.
