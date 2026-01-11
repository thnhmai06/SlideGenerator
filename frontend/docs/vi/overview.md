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
- [electron](../../electron): main/preload và tray.

## Luồng chạy

1. Electron main mở renderer và (tuỳ chọn) khởi chạy backend.
2. Renderer kết nối `/hubs/job`, `/hubs/sheet`, `/hubs/config`.
3. UI cập nhật từ notification và truy vấn trực tiếp.

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
