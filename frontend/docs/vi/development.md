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

## Code Style

- **TSDoc comments**: Tất cả functions, hooks, và components đều có TSDoc documentation.
- **TypeScript strict mode**: Bật để đảm bảo type safety.
- **ESLint + Prettier**: Auto-formatting khi save.

## Hướng dẫn tối ưu hiệu năng

- Sử dụng `React.memo` cho components nhận props ổn định.
- Wrap callbacks trong `useCallback` để tránh re-render không cần thiết.
- Sử dụng `useMemo` cho các tính toán tốn kém.
- Ưu tiên lazy loading cho feature modules.

## Aliases

- `@/` ánh xạ tới `src/`.

## Cấu trúc Project

```
src/
├── app/           # App shell, providers, routing
├── features/      # Feature modules
│   ├── create-task/  # Task creation workflow
│   ├── process/      # Job monitoring
│   ├── results/      # Completed jobs
│   ├── settings/     # App configuration
│   └── about/        # About screen
└── shared/        # Shared code
    ├── components/   # Reusable UI components
    ├── contexts/     # React contexts và hooks
    ├── services/     # Backend API và SignalR
    ├── utils/        # Utility functions
    ├── locales/      # i18n translations
    └── styles/       # Global CSS
```

## Logs

Xem `frontend/logs/<timestamp>/` cho process/renderer/backend logs.
