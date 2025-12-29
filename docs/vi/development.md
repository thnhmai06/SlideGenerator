# Phát triển

## Mục lục

1. [Yêu cầu](#yêu-cầu)
2. [Cài đặt](#cài-đặt)
3. [Chạy dev](#chạy-dev)
4. [Tests](#tests)
5. [Tác vụ hữu ích](#tác-vụ-hữu-ích)

## Yêu cầu

- Node.js + npm
- .NET 10 SDK (để chạy backend trong môi trường dev)

## Cài đặt

Trong `frontend/`:

```
npm install
```

## Chạy dev

Trong `frontend/`:

```
npm run dev
```

Lưu ý:

- Electron main có thể tự khởi chạy backend.
- Để tắt auto-start, đặt `SLIDEGEN_DISABLE_BACKEND=1`.
- Bạn cũng có thể chạy backend thủ công trong `backend/` bằng `dotnet run`.
- Log được ghi trong `frontend/logs/<timestamp>/` (process, renderer, backend).

## Tests

Trong `frontend/`:

```
npm test
```

## Tác vụ hữu ích

Các VS Code task có trong `.vscode/tasks.json`:

- `test:frontend`
- `test:backend`
- `test:all`
