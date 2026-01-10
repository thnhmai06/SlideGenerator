# Build and Packaging

Vietnamese version: [Vietnamese](../vi/build-and-packaging.md)

## Build

From `frontend/`:

```bash
npm run build:backend
npm run build
```

Or use the combined script:

```bash
npm run build:full
```

`build:backend` publishes the backend into `frontend/backend`.

## Backend subprocess

Electron launches the backend:

- Development: `dotnet run --project backend/src/SlideGenerator.Presentation/...`
- Production: `SlideGenerator.Presentation.exe` (or `dotnet SlideGenerator.Presentation.dll`)

Environment variables:

- `SLIDEGEN_DISABLE_BACKEND=1` disables auto-start.
- `SLIDEGEN_BACKEND_PATH` overrides the backend executable/DLL path.

## Packaging notes

- `frontend/backend` is bundled into the app as `resources/backend`.
- Ensure the target machine has .NET 10 runtime if you ship the DLL.
