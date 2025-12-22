# Tổng quan Frontend

## Mục lục

1. [Kiến trúc](#kiến-trúc)
2. [Thư mục chính](#thư-mục-chính)
3. [Mô hình runtime](#mô-hình-runtime)
4. [State và lưu trữ](#state-và-lưu-trữ)

## Kiến trúc

Frontend là ứng dụng Electron desktop, hiển thị UI React. Kết nối backend qua SignalR
và coi backend là nguồn dữ liệu chính cho trạng thái job.

## Thư mục chính

- `src/components`: giao diện (Tạo task, Xử lý, Kết quả, Cài đặt, Giới thiệu).
- `src/contexts`: state ứng dụng và job (SignalR subscriptions).
- `src/services`: wrapper gọi backend và SignalR client.
- `src/styles`: style global và theo component.
- `electron`: tiến trình main/preload của Electron.
- `assets`: ảnh, icon, animation.

## Mô hình runtime

- `electron/main.ts` tạo cửa sổ và có thể chạy backend như subprocess.
- UI dùng SignalR hubs (`/hubs/slide`, `/hubs/sheet`, `/hubs/config`).
- Backend URL lưu trong local storage (`slidegen.backend.url`).

## State và lưu trữ

- Trạng thái Create Task được cache trong session storage (`slidegen.ui.inputMenu.state`).
- Metadata và config của group lưu trong session storage:
  - `slidegen.group.meta`
  - `slidegen.group.config`
