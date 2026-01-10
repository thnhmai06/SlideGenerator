# Phát triển

English version: [English](../en/development.md)

## Yêu cầu

- Node.js + npm
- .NET 10 SDK (để chạy backend khi dev)

## Cài đặt

```bash
cd frontend
npm install
```

## Chạy dev

```bash
npm run dev
```

Ghi chú:

- Electron có thể tự start backend.
- Tắt auto-start bằng `SLIDEGEN_DISABLE_BACKEND=1`.
- Override đường dẫn backend bằng `SLIDEGEN_BACKEND_PATH`.

## Test

```bash
npm test
```

## Logs

Xem `frontend/logs/<timestamp>/` cho process/renderer/backend logs.
