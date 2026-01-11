# Build và đóng gói

[English](../en/build-and-packaging.md)

## Build

Trong `frontend/`:

```bash
npm run build:backend
npm run build
```

Hoặc dùng script gộp:

```bash
npm run build:full
```

`build:backend` publish backend vào `frontend/backend`.

## Backend subprocess

Electron khởi chạy backend:

- Development: `dotnet run --project backend/src/SlideGenerator.Presentation/...`
- Production: `SlideGenerator.Presentation.exe` (hoặc `dotnet SlideGenerator.Presentation.dll`)

Biến môi trường:

- `SLIDEGEN_DISABLE_BACKEND=1` tắt auto-start.
- `SLIDEGEN_BACKEND_PATH` override đường dẫn exe/DLL.

## Ghi chú đóng gói

- `frontend/backend` được bundle vào app dưới `resources/backend`.
- Nếu chỉ ship DLL, máy đích cần .NET 10 runtime.
