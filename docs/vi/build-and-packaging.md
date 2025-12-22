# Build và đóng gói

## Mục lục

1. [Script build](#script-build)
2. [Backend subprocess](#backend-subprocess)
3. [Lưu ý đóng gói](#lưu-ý-đóng-gói)

## Script build

Trong `frontend/`:

```
npm run build:backend
npm run build
```

Hoặc dùng script gộp:

```
npm run build:full
```

`build:backend` publish backend vào `frontend/backend`.

## Backend subprocess

Electron main sẽ khởi chạy backend:

- Development: `dotnet run --project backend/src/SlideGenerator.Presentation/...`
- Production: chạy `SlideGenerator.Presentation.exe` trong `resources/backend`
  (hoặc `dotnet SlideGenerator.Presentation.dll` nếu chỉ có DLL).

Biến môi trường:

- `SLIDEGEN_DISABLE_BACKEND=1` để tắt auto-start.
- `SLIDEGEN_BACKEND_PATH` để override đường dẫn backend (exe hoặc DLL).

## Lưu ý đóng gói

- Thư mục `frontend/backend` được copy vào `resources/backend`.
- Nếu chỉ ship DLL, máy đích cần .NET 10 runtime.
