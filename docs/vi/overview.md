# Tổng quan Frontend

## Mục lục

1. [Kiến trúc](#kiến-trúc)
2. [Thư mục chính](#thư-mục-chính)
3. [Mô hình runtime](#mô-hình-runtime)
4. [Logging](#logging)
5. [State và lưu trữ](#state-và-lưu-trữ)

## Kiến trúc

Frontend là ứng dụng Electron desktop, hiển thị UI React. Kết nối backend qua SignalR
và coi backend là nguồn dữ liệu chính cho trạng thái job.

## Thư mục chính

- `src/components`: giao diện (Tạo task, Xử lý, Kết quả, Cài đặt, Giới thiệu).
- `src/contexts`: state ứng dụng và job (SignalR subscriptions).
- `src/services`: wrapper gọi backend và SignalR client.
- `src/styles`: style global và theo component.
- `electron/main`: module main process (window, backend, logging, dialogs, settings).
- `electron/preload`: preload và API bridge sang renderer.
- `assets`: ảnh, icon, animation.

## Mô hình runtime

- `electron/main.ts` kết nối các module main process và tạo cửa sổ.
- UI dùng SignalR hubs (`/hubs/slide`, `/hubs/sheet`, `/hubs/config`).
- Backend URL lưu trong local storage (`slidegen.backend.url`).

## Logging

- Log nằm trong `frontend/logs/<timestamp>/`.
- `process.log`: log của Electron main process.
- `renderer.log`: log renderer (DevTools) từ UI.
- `backend.log`: log backend khi được Electron khởi động.

## State và lưu trữ

- Trạng thái Create Task được cache trong session storage (`slidegen.ui.inputMenu.state`).
- Metadata và config của group lưu trong session storage:
  - `slidegen.group.meta`
  - `slidegen.group.config`
