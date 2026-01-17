# Tổng quan Frontend

[English](../en/overview.md)

## Mục tiêu

- Ứng dụng desktop để tạo và theo dõi job tạo slide.
- Backend là nguồn dữ liệu chính; UI kết nối qua SignalR.
- Ưu tiên chạy local: settings và trạng thái UI được lưu trên máy.

## Kiến trúc

- [src/app](../../src/app): app shell và providers.
- [src/features](../../src/features): các màn hình theo feature (create-task, process, results, settings, about).
- [src/shared](../../src/shared): UI chung, contexts, services, utils, locales, styles.
- [electron](../../electron): main/preload, tray và [trình cập nhật](updater.md).

### Tầng Services

- `src/shared/services/signalr/`: SignalR client với auto-reconnect và request queuing.
- `src/shared/services/backend/`: Typed API functions cho jobs, sheets, config, health.
- Tất cả giao tiếp backend sử dụng typed request/response patterns.

### Quản lý State

- `AppContext`: State toàn cục (theme, settings, language).
- `JobContext`: Job groups, sheets, logs, real-time updates.
- Feature hooks: `useCreateTask`, `useProcess`, `useReplacements`, v.v.

## Luồng chạy

1. Electron main mở renderer và (tuỳ chọn) khởi chạy backend.
2. Renderer kết nối `/hubs/job`, `/hubs/sheet`, `/hubs/config`.
3. UI cập nhật từ notification và truy vấn trực tiếp.

## Tối ưu hiệu năng

- **React.memo**: Áp dụng cho các component chính (Sidebar, TitleBar, TagInput, ShapeSelector).
- **useMemo/useCallback**: Memoize các tính toán và callback tốn kém.
- **Lazy loading**: Feature menus được load theo yêu cầu với React.lazy.
- **Vite chunking**: Vendor libraries được tách thành chunks riêng (vendor-react, vendor-signalr).
- **Log trimming**: Logs tự động cắt tại 2500 entries để tránh memory bloat.

## Storage keys

- `localStorage.slidegen.backend.url`: base URL backend đang dùng.
- `localStorage.slidegen.backend.url.pending`: URL pending (được promote một lần).
- `sessionStorage.slidegen.backend.url.pending.defer`: trì hoãn promote trong session.
- `sessionStorage.slidegen.ui.inputsideBar.state`: bản nháp Create Task.
- `sessionStorage.slidegen.group.meta`: cache group meta.
- `sessionStorage.slidegen.group.config`: cache group config.

## Logs

`frontend/logs/<timestamp>/`:

- `process.log`: Electron main.
- `renderer.log`: renderer (DevTools).
- `backend.log`: backend khi chạy qua Electron.
