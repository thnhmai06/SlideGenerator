# Phát triển

[English](../en/development.md)

## Yêu cầu

- Node.js + npm
- .NET 10 SDK (nếu chạy backend khi dev)

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

- Electron có thể auto-start backend.
- Tắt auto-start bằng `SLIDEGEN_DISABLE_BACKEND=1`.
- Override đường dẫn backend bằng `SLIDEGEN_BACKEND_PATH`.

## Test

```bash
npm test
```

Bộ công cụ test:

- Vitest + Testing Library.
- MSW handlers nằm ở [test/mocks/handlers.ts](../../test/mocks/handlers.ts).
- Override handlers theo từng test với `server.use(...)`.

## Aliases

- `@/` ánh xạ tới `src/`.

## Logs

Xem `frontend/logs/<timestamp>/` cho process/renderer/backend logs.
